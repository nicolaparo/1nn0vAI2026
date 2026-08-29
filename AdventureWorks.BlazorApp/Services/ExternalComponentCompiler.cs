using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Text;
using AdventureWorks.SharedComponents;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;

namespace AdventureWorks.BlazorApp.Services;

public sealed class ExternalComponentCompiler(IWebHostEnvironment environment)
{
    private const string ExternalPagesDirectory = "AdventureWorks.ExternalPages";
    private readonly ConcurrentDictionary<string, CachedPage> cache = new(StringComparer.OrdinalIgnoreCase);

    public Task<Type> CompileAsync(
        string pageName,
        string? filePath = null,
        CancellationToken cancellationToken = default)
    {
        filePath ??= GetPagePath(pageName);
        var lastWriteTime = File.GetLastWriteTimeUtc(filePath);

        if (cache.TryGetValue(filePath, out var cachedPage) &&
            cachedPage.LastWriteTimeUtc == lastWriteTime)
        {
            return cachedPage.Compilation;
        }

        var compilation = CompilePageAsync(filePath, lastWriteTime, cancellationToken);
        cache[filePath] = new CachedPage(lastWriteTime, compilation);
        return compilation;
    }

    private Task<Type> CompilePageAsync(
        string filePath,
        DateTime lastWriteTimeUtc,
        CancellationToken cancellationToken)
    {
        var externalPagesDirectory = Path.GetDirectoryName(filePath)!;
        var metadataReferences = GetMetadataReferences().ToArray();
        var projectEngine = RazorProjectEngine.Create(
            RazorConfiguration.Default,
            RazorProjectFileSystem.Create(externalPagesDirectory),
            builder =>
            {
                CompilerFeatures.Register(builder);
                if (builder.Features.OfType<DefaultMetadataReferenceFeature>().SingleOrDefault() is { } referenceFeature)
                {
                    referenceFeature.References = metadataReferences;
                }

                builder.SetRootNamespace("AdventureWorks.BlazorApp");
            });

        var projectItem = projectEngine.FileSystem.GetItem($"/{Path.GetFileName(filePath)}");
        var tagHelperCompilation = CSharpCompilation.Create(
            "AdventureWorks.ExternalComponentTagHelpers",
            references: metadataReferences);
        var discoveryService = projectEngine.Engine.Features
            .Single(feature => feature.GetType().Name == "TagHelperDiscoveryService");
        var tagHelpers = (TagHelperCollection)discoveryService
            .GetType()
            .GetMethod("GetTagHelpers", [typeof(Compilation), typeof(CancellationToken)])!
            .Invoke(discoveryService, [tagHelperCompilation, cancellationToken])!;
        var imports = (ImmutableArray<RazorSourceDocument>)typeof(RazorProjectEngine)
            .GetMethod("GetImportSources", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(projectEngine, [projectItem])!;
        var codeDocument = projectEngine.Process(
            RazorSourceDocument.ReadFrom(projectItem),
            RazorFileKind.Component,
            imports,
            tagHelpers,
            cancellationToken);

        var razorDocument = codeDocument.GetType()
            .GetMethod("GetCSharpDocument", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(codeDocument, null)!;
        var diagnostics = (IReadOnlyList<RazorDiagnostic>)razorDocument
            .GetType()
            .GetProperty("Diagnostics")!
            .GetValue(razorDocument)!;
        if (diagnostics.Count > 0)
        {
            throw CreateCompilationException(filePath, diagnostics.Select(diagnostic => diagnostic.GetMessage()));
        }

        var generatedCode = razorDocument
            .GetType()
            .GetProperty("Text")!
            .GetValue(razorDocument)!
            .ToString()!;
        var syntaxTree = CSharpSyntaxTree.ParseText(generatedCode, path: filePath, encoding: Encoding.UTF8);
        var assemblyName = $"AdventureWorks.ExternalComponents.{Path.GetFileNameWithoutExtension(filePath)}.{lastWriteTimeUtc.Ticks}";
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            metadataReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var assemblyStream = new MemoryStream();
        using var symbolsStream = new MemoryStream();
        var emitResult = compilation.Emit(assemblyStream, symbolsStream, cancellationToken: cancellationToken);
        if (!emitResult.Success)
        {
            throw CreateCompilationException(
                filePath,
                emitResult.Diagnostics
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(diagnostic => diagnostic.ToString()));
        }

        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream, symbolsStream);
        var componentType = assembly.GetTypes().SingleOrDefault(type =>
            typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(type));

        return Task.FromResult(componentType
            ?? throw new InvalidOperationException($"External page '{filePath}' does not define a Blazor component."));
    }

    public string GetPagePath(string pageName)
    {
        if (!pageName.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
            pageName.Contains(Path.DirectorySeparatorChar) ||
            pageName.Contains(Path.AltDirectorySeparatorChar) ||
            pageName.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Page names must be a file name ending in .razor.", nameof(pageName));
        }

        var contentRootPath = Path.Combine(environment.ContentRootPath, ExternalPagesDirectory, pageName);
        var sourceRootPath = Path.Combine(
            Directory.GetParent(environment.ContentRootPath)?.FullName ?? environment.ContentRootPath,
            ExternalPagesDirectory,
            pageName);
        var filePath = File.Exists(contentRootPath) ? contentRootPath : sourceRootPath;
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"External page '{pageName}' was not found.", contentRootPath);
        }

        return filePath;
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!string.IsNullOrEmpty(assembly.Location))
            {
                paths.Add(assembly.Location);
            }
        }

        paths.Add(typeof(KpiCard).Assembly.Location);
        paths.Add(typeof(AdventureWorks.Abstractions.CustomerProfile).Assembly.Location);
        paths.Add(typeof(QuickGrid<>).Assembly.Location);

        foreach (var path in Directory.GetFiles(
                     Path.GetDirectoryName(typeof(object).Assembly.Location)!,
                     "*.dll"))
        {
            paths.Add(path);
        }

        return paths.SelectMany(path =>
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var peReader = new PEReader(stream);
                if (!peReader.HasMetadata)
                {
                    return Array.Empty<MetadataReference>();
                }

                return [MetadataReference.CreateFromFile(path)];
            }
            catch (BadImageFormatException)
            {
                return Array.Empty<MetadataReference>();
            }
        });
    }

    private static InvalidOperationException CreateCompilationException(
        string filePath,
        IEnumerable<string> diagnostics) =>
        new($"Unable to compile external page '{filePath}':{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics)}");

    private sealed record CachedPage(DateTime LastWriteTimeUtc, Task<Type> Compilation);
}
