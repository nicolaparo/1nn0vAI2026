# Applicazioni allucinanti con GitHub Copilot SDK e C#

**Conferenza:** sabato 26 settembre 2026, 11:25–12:20 — Aula S1  
**Durata:** 55 minuti  
**Repository demo:** `nicolaparo/1nn0vAI2026`  
**Stack:** .NET 10, Blazor Web App Interactive Server, .NET Aspire, SQL Server AdventureWorks, GitHub Copilot SDK

> **Nota per il relatore**
> Questo documento è il contenuto delle slide, non uno script parola per parola. I blocchi di codice sono volutamente brevi: servono a mostrare le idee architetturali più importanti e a guidare la demo.

---

## Slide 1 — Titolo

# Applicazioni allucinanti con GitHub Copilot SDK e C#

Da assistente di coding a motore per workflow agentici controllati.

- Integrare un agente in una vera applicazione .NET
- Esporre strumenti C# al modello
- Dare contesto, memoria operativa e feedback all'utente
- Validare ogni modifica prima di renderla visibile

**Messaggio chiave:** un LLM non è l'applicazione. È un componente probabilistico dentro un sistema che deve restare deterministico, osservabile e governabile.

---

## Slide 2 — Obiettivi e agenda

### Alla fine della sessione sapremo:

1. Che cos'è il GitHub Copilot SDK e come si integra in C#.
2. Come creare sessioni, prompt e tool custom.
3. Come costruire un ciclo **edit → compile → fix → compile**.
4. Quando scegliere Copilot SDK e quando `Microsoft.Extensions.AI`.
5. Quali sono i vantaggi, i limiti e i rischi di sicurezza.

### Agenda — 55 minuti

| Minuti | Parte |
|---:|---|
| 0–5 | Il problema e l'architettura |
| 5–15 | SDK: client, sessione, prompt, tool |
| 15–30 | Demo del page updater Blazor |
| 30–38 | SDK vs `Microsoft.Extensions.AI` |
| 38–48 | Sicurezza, affidabilità e operatività |
| 48–55 | Pro/contro, checklist e Q&A |

---

## Slide 3 — Il problema: trasformare una richiesta in una modifica verificabile

Una richiesta utente come:

> “Aggiungi al dashboard un riepilogo del fatturato per cliente e mantieni il comportamento esistente.”

non è una semplice generazione di testo. Richiede:

- comprendere un file Razor e i contratti dei servizi;
- modificare il codice nel workspace corretto;
- rispettare routing, accessibilità e dipendenze;
- compilare il risultato;
- interpretare i diagnostici;
- correggere gli errori;
- presentare una modifica isolata e revisionabile.

### Il workflow adottato

```text
Richiesta utente
      │
      ▼
Copia privata della pagina
      │
      ▼
Sessione Copilot + strumenti limitati
      │
      ▼
Edit ──► Compile ──► Diagnostica ──► Fix
                              │
                              └────── loop
      │
      ▼
Validazione finale
      │
      ▼
Attivazione della pagina
```

---

## Slide 4 — La demo: AdventureWorks + Blazor + Aspire

Il repository contiene una piccola applicazione ERP/CRM:

- `AdventureWorks.AppHost`: orchestra l'app con .NET Aspire e SQL Server.
- `AdventureWorks.BlazorApp`: UI Blazor Interactive Server, servizi dati e integrazione Copilot.
- `AdventureWorks.ExternalPages`: pagine Razor compilabili in modo indipendente.
- `AdventureWorks.Tests` e `AdventureWorks.E2ETests`: test di compilazione, rendering e comportamento.

### Perché questo esempio è interessante

Il modello non risponde soltanto in chat: opera su una copia di una pagina Razor, usa tool applicativi e deve superare una verifica tecnica prima della pubblicazione.

```xml
<PackageReference Include="GitHub.Copilot.SDK" Version="1.0.11" />
```

> Il numero di versione è quello presente nel repository della demo: verificare sempre la versione disponibile e compatibile prima di un uso in produzione.

---

## Slide 5 — Il modello mentale del Copilot SDK

Il Copilot SDK fornisce controllo programmatico sul runtime di Copilot:

- `CopilotClient`: avvia e gestisce il runtime.
- `CopilotSession`: rappresenta una conversazione con contesto e stato.
- `SessionConfig`: modello, directory di lavoro, tool, permessi, hook.
- `CopilotTool`: funzioni applicative esposte all'agente.
- eventi e hook: osservabilità, auditing e policy.

### Non è soltanto un client HTTP verso un modello

Il valore aggiunto è il **harness agentico**: sessioni operative, tool, permessi, ciclo di esecuzione e integrazione con il contesto di lavoro.

```csharp
await copilotClient.StartAsync(cancellationToken);

await using var session = await copilotClient.CreateSessionAsync(
    new SessionConfig
    {
        Model = "gpt-5.6-luna",
        WorkingDirectory = workspacePath,
        Tools = tools,
        OnPermissionRequest = permissionHandler,
    },
    cancellationToken);
```

---

## Slide 6 — Una sessione con contesto operativo

Il contesto non è soltanto il prompt: è anche il luogo in cui l'agente può operare e gli strumenti che può usare.

```csharp
await using var session = await copilotClient.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-5.6-luna",
    WorkingDirectory = Path.GetDirectoryName(candidatePath),
    OnPermissionRequest = PermissionHandler.ApproveAll, // SOLO demo
    Tools =
    [
        CopilotTool.DefineTool(
            ReportCopilotProgressAsync,
            factoryOptions: new AIFunctionFactoryOptions
            {
                Name = "report_progress",
                Description = "Send a brief progress update to the user after each meaningful operation.",
            }),
        CopilotTool.DefineTool(
            CompilePageAsync,
            factoryOptions: new AIFunctionFactoryOptions
            {
                Name = "compile_page",
                Description = "Compile the current Razor page and return actionable diagnostics.",
            }),
    ],
}, cancellationToken);
```

### Osservazioni

- Il tool ha un nome e una descrizione: sono parte del contratto con il modello.
- Il tool deve avere input/output piccoli, chiari e verificabili.
- `WorkingDirectory` deve puntare a una sandbox, non alla root indiscriminata dell'host.
- Il permesso deve essere deciso in base alla richiesta concreta, non automaticamente.

---

## Slide 7 — Prompt: policy e obiettivo insieme

Un buon prompt per un agente applicativo definisce:

1. **Obiettivo:** cosa deve cambiare.
2. **Perimetro:** dove può operare.
3. **Vincoli:** cosa non deve rompere.
4. **Strumenti:** quando usarli.
5. **Criterio di completamento:** come dimostrare che ha finito.

```csharp
var prompt = $"""
    You are editing the Blazor page in {Path.GetFileName(candidatePath)}.
    Implement this user request: <UserRequest>{request}</UserRequest>

    Work only in the current workspace and edit the target Razor page.
    Preserve the existing page's default functionality, service contracts,
    routing assumptions, and accessibility. Do not edit files outside the
    workspace. Review your changes when finished.

    You have a report_progress tool. Call it with a short, user-friendly
    update before and after each meaningful operation so the user can see
    what is happening.

    You have a compile_page tool. Invoke it after every edit. If compilation
    fails, use the diagnostics to fix the page and invoke compile_page again.
    Continue this edit/compile loop until compilation succeeds.
    """;
""";
```

### Principio

Il prompt non sostituisce i controlli di sicurezza. È una guida comportamentale; la policy deve essere applicata dal codice.

---

## Slide 8 — Tool custom: il modello propone, l'app decide

Il tool di compilazione incapsula una capacità concreta dell'applicazione:

```csharp
async Task<string> CompilePageAsync()
{
    await ReportProgressAsync("Copilot is compiling the current page version.");

    try
    {
        await compiler.CompileAsync(pageName, candidatePath, cancellationToken);
        await ReportProgressAsync("Copilot compilation succeeded.");
        return "Compilation succeeded. The page is valid and ready to be reviewed.";
    }
    catch (InvalidOperationException exception)
    {
        await ReportProgressAsync(
            "Copilot found compilation errors and is revising the page.");

        return $"Compilation failed. Fix these errors and invoke compile_page again:" +
               Environment.NewLine + exception.Message;
    }
}
```

### Perché è importante

- il modello non deve inventare se il codice compila;
- il compilatore è una fonte di verità più forte del testo generato;
- i diagnostici diventano feedback strutturato nel loop agentico;
- il tool è anche un punto naturale per logging, timeout e autorizzazione.

---

## Slide 9 — Il loop agentico: edit → compile → fix

```csharp
await ReportProgressAsync("Copilot is planning the requested page changes.");

await session.SendAndWaitAsync(
    new MessageOptions { Prompt = prompt },
    TimeSpan.FromMinutes(5),
    cancellationToken);

await ReportProgressAsync("Validating the updated page.");
await compiler.CompileAsync(pageName, candidatePath, cancellationToken);

await ReportProgressAsync("Loading the validated page.");
await workspace.ActivatePageAsync(pageName, candidatePath);
```

### Il punto sottile

Il codice applicativo mantiene l'ultima parola:

- la sessione può fallire o andare in timeout;
- una compilazione finale indipendente verifica il risultato;
- soltanto dopo la validazione la pagina viene attivata;
- l'utente riceve progressi, ma non deve fidarsi ciecamente del modello.

### Regola pratica

> Ogni azione dell'agente che modifica stato deve avere una verifica deterministica successiva.

---

## Slide 10 — Separare il workspace di lavoro dalla pagina attiva

Prima di chiamare Copilot:

```csharp
await ReportProgressAsync("Preparing a private copy of this page.");

var sourcePath = compiler.GetPagePath(pageName);
var candidatePath = workspace.PreparePage(pageName, sourcePath);
```

Dopo la compilazione:

```csharp
await compiler.CompileAsync(pageName, candidatePath, cancellationToken);
await workspace.ActivatePageAsync(pageName, candidatePath);
```

### Vantaggi

- rollback più semplice;
- nessuna modifica diretta al file sorgente durante la generazione;
- possibilità di confrontare candidate e versione attiva;
- riduzione del blast radius di un prompt malevolo o di un errore del modello;
- punto chiaro per approvazione umana, test e audit.

### Attenzione

Una copia privata non è automaticamente una sandbox di sicurezza: se il processo conserva gli stessi privilegi, il rischio può restare invariato.

---

## Slide 11 — Feedback all'utente: progressi senza esporre il ragionamento interno

```csharp
async Task<string> ReportCopilotProgressAsync(string update)
{
    await ReportProgressAsync(update);
    return "Progress update sent to the user.";
}
```

Il tool comunica **stato operativo**, non chain-of-thought:

- “Sto preparando la copia”
- “Sto compilando la pagina”
- “Ho trovato errori di compilazione”
- “La modifica è stata validata”

### Buona UX agentica

- aggiornamenti brevi e comprensibili;
- stato esplicito: in esecuzione, riuscito, fallito, annullato;
- timeout e cancellazione visibili;
- diagnostica tecnica nei log, messaggio sicuro e utile nell'interfaccia;
- correlazione tramite `sessionId` nei log applicativi.

---

## Slide 12 — Dove entra `Microsoft.Extensions.AI`

`Microsoft.Extensions.AI` definisce astrazioni .NET comuni, tra cui:

- `IChatClient` per richieste chat complete o streaming;
- `IEmbeddingGenerator<TInput,TEmbedding>` per embedding;
- tool/function calling e utility per comporre pipeline;
- logging, telemetria, caching e middleware tramite pattern familiari a .NET;
- integrazione naturale con dependency injection e testing.

Esempio concettuale di una chiamata chat:

```csharp
public sealed class SummaryService(IChatClient chatClient)
{
    public async Task<string> SummarizeAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var response = await chatClient.GetResponseAsync(
        [
            new ChatMessage(ChatRole.System,
                "Summarize accurately. Do not invent facts."),
            new ChatMessage(ChatRole.User, text),
        ],
        cancellationToken: cancellationToken);

        return response.Text ?? string.Empty;
    }
}
```

> Il codice è indicativo: le firme e le opzioni dipendono dalla versione dei pacchetti e dal provider scelto.

---

## Slide 13 — Copilot SDK vs `Microsoft.Extensions.AI`

| Dimensione | GitHub Copilot SDK | `Microsoft.Extensions.AI` |
|---|---|---|
| Focus | Agenti e workflow operativi | Astrazioni AI per applicazioni .NET |
| Livello | Harness agentico: sessione, tool, permessi, contesto operativo | Contratti e pipeline: `IChatClient`, embedding, middleware |
| Strumenti | Tool applicativi e capacità del runtime Copilot | Function/tool calling e composizione lato applicazione |
| Modello | Pensato per un'esperienza Copilot e le sue capacità | Provider-agnostic rispetto al servizio sottostante |
| Stato | Conversazione/sessione operativa e ciclo di lavoro | Stato gestito dall'applicazione e dalla cronologia messaggi |
| Controlli | Permission handler, hook, strumenti disponibili, workspace | Policy, DI, middleware, rate limit e validazione costruiti dall'app |
| Caso ideale | Agente che legge/modifica/compila e usa servizi esterni | Chat, estrazione, classificazione, RAG, embedding e componenti riusabili |
| Portabilità | Più accoppiato all'ecosistema Copilot | Maggiore portabilità tra provider e implementazioni |

### Sintesi

- **Copilot SDK:** “dammi un agente operativo pronto da governare”.
- **`Microsoft.Extensions.AI`:** “dammi un contratto .NET pulito per integrare AI nel mio sistema”.

Sono strumenti complementari, non necessariamente alternativi.

---

## Slide 14 — Quando scegliere uno o l'altro

### Scegli Copilot SDK quando:

- il caso d'uso è realmente agentico e multi-step;
- il modello deve usare file, shell, repository o tool applicativi;
- servono sessioni con contesto operativo;
- vuoi sfruttare permission handler, hook, skill o MCP del runtime Copilot;
- il workflow ha un criterio di completamento e verifiche intermedie;
- l'esperienza Copilot è parte centrale del prodotto.

### Scegli `Microsoft.Extensions.AI` quando:

- devi aggiungere una singola capacità AI a un'app esistente;
- vuoi cambiare provider o modello con il minimo accoppiamento;
- stai costruendo chat, summarization, classificazione, RAG o embedding;
- vuoi incapsulare l'AI dietro interfacce testabili via DI;
- la tua applicazione controlla già il loop, i tool e lo stato;
- vuoi comporre middleware di telemetria, caching e policy .NET.

### Usa entrambi quando:

- Copilot SDK orchestra un workflow agentico;
- `Microsoft.Extensions.AI` alimenta servizi specializzati, RAG o classificatori;
- vuoi mantenere una separazione tra “motore operativo” e “capabilities AI”.

---

## Slide 15 — Vantaggi dell'approccio adottato

### 1. Automazione con feedback

L'agente non si ferma alla prima risposta: modifica, compila e corregge.

### 2. Guardrail tecnici

La compilazione Razor è un controllo deterministico e ripetibile.

### 3. Blast radius ridotto

La modifica avviene su una copia privata e viene attivata solo dopo la verifica.

### 4. Esperienza utente trasparente

Gli eventi di progresso rendono visibile il lavoro senza mostrare dettagli inutili.

### 5. Tool piccoli e composabili

`report_progress` e `compile_page` hanno responsabilità precise.

### 6. Punto di estensione naturale

Si possono aggiungere test, linting, preview, approvazione umana e audit senza cambiare il concetto base.

---

## Slide 16 — Contro e limiti

### Complessità operativa

Avviare e gestire un runtime agentico è più complesso di una semplice chiamata a `IChatClient`.

### Costi e latenza

Più iterazioni, tool call e diagnostica significano più token, tempo e richieste.

### Non-determinismo

Lo stesso prompt può produrre modifiche diverse. Servono test ed evaluation, non solo una demo riuscita.

### Debug più difficile

Un errore può stare nel prompt, nel modello, nel tool, nei permessi, nel compilatore o nello stato della sessione.

### Dipendenza dal runtime

L'uso di feature specifiche del Copilot SDK aumenta il valore ottenuto, ma anche l'accoppiamento alla piattaforma.

### Falsa sensazione di autonomia

Un agente che compila non è necessariamente corretto, sicuro, accessibile o conforme alle regole di business.

### Limite della demo

Compilare una pagina non dimostra che il comportamento sia corretto: servono test funzionali, security test e revisione umana per i casi ad alto impatto.

---

## Slide 17 — Sicurezza: il rischio principale è l'autorità dell'agente

Un agente può combinare:

- input non affidabile;
- contesto recuperato da file, web o database;
- capacità di scrivere file o eseguire comandi;
- credenziali del processo;
- tool che chiamano sistemi esterni.

Questa combinazione crea un rischio molto maggiore di una semplice risposta testuale.

### Principali minacce

- **Prompt injection:** testo nei dati o nei file che tenta di cambiare le istruzioni.
- **Tool hijacking:** argomenti manipolati per indurre un tool a eseguire un'azione diversa.
- **Eccesso di privilegi:** accesso a file, shell, rete o database non necessario.
- **Data exfiltration:** segreti o dati personali inviati al modello o a un servizio esterno.
- **Confused deputy:** l'agente usa i privilegi dell'app per conto di un utente non autorizzato.
- **Supply-chain/MCP risk:** tool o server esterni non verificati.
- **Output injection:** testo generato inserito in HTML, SQL, shell, log o comandi senza encoding/parametrizzazione.
- **Denial of service:** loop infiniti, prompt enormi, richieste costose o tool ripetuti.

---

## Slide 18 — Il problema più evidente nel codice della demo

Nel repository troviamo:

```csharp
OnPermissionRequest = PermissionHandler.ApproveAll,
```

### Perché è comodo

- la demo non si interrompe con richieste di autorizzazione;
- il flusso agentico è facile da mostrare sul palco.

### Perché è pericoloso

Approva ogni richiesta di tool, incluse operazioni che possono leggere/scrivere file o eseguire comandi. In produzione è una scelta insicura salvo un ambiente realmente isolato e usa-e-getta.

### Policy più prudente

```csharp
OnPermissionRequest = async (request, invocation) =>
{
    return request switch
    {
        PermissionRequestShell shell =>
            PermissionDecision.Reject(
                $"Shell non consentita: {shell.FullCommandText}"),

        PermissionRequestFileWrite write when
            !IsInsideWorkspace(write.Path, candidateDirectory) =>
            PermissionDecision.Reject(
                "Scrittura fuori dal workspace non consentita."),

        _ => PermissionDecision.ApproveOnce(),
    };
};
```

> I nomi esatti dei tipi e delle proprietà possono variare con la versione dell'SDK: verificare sempre l'API installata. Il principio resta: **deny by default, approvazione minima, una volta sola**.

---

## Slide 19 — Difese concrete da implementare

### Least privilege

- account di processo dedicato;
- workspace temporaneo con permessi minimi;
- niente credenziali cloud inutili nell'ambiente dell'agente;
- accesso di rete e filesystem limitato;
- tool disponibili esplicitamente elencati.

### Validazione dei tool

- schema tipizzato e validazione server-side;
- allowlist di path, estensioni, comandi e endpoint;
- limiti di dimensione, tempo e numero di invocazioni;
- rifiuto di path traversal e symlink non attesi;
- query parametrizzate, mai SQL costruito dal testo del modello.

### Controllo del ciclo di vita

- timeout e cancellation token;
- massimo numero di iterazioni;
- idempotenza dove possibile;
- rollback o versionamento della candidate;
- approvazione umana prima di azioni irreversibili.

### Dati e privacy

- classificare i dati prima di inserirli nel contesto;
- redigere segreti e dati personali nei log;
- non inserire token, connection string o prompt riservati;
- definire retention e confini del provider;
- verificare policy aziendali e requisiti normativi.

---

## Slide 20 — Hook, audit e osservabilità

Gli hook sono il posto giusto per applicare policy trasversali senza duplicarle nei singoli tool.

```csharp
var hooks = new SessionHooks
{
    OnPreToolUse = async (input, invocation) =>
    {
        logger.LogInformation(
            "Copilot session {SessionId}: tool {ToolName}",
            invocation.SessionId,
            input.ToolName);

        if (!IsAllowed(input.ToolName, input.ToolArgs))
        {
            return new PreToolUseHookOutput
            {
                PermissionDecision = "deny",
                PermissionDecisionReason = "Tool call non autorizzata."
            };
        }

        return new PreToolUseHookOutput
        {
            PermissionDecision = "allow"
        };
    }
};
```

### Metriche utili

- tempo totale e tempo per tool;
- numero di iterazioni edit/compile;
- percentuale di compilazioni riuscite al primo tentativo;
- rifiuti di permission e motivazioni;
- token/costo per workflow;
- errori per modello e versione del prompt;
- modifiche attivate e rollback.

**Audit utile = chi, quale sessione, quale tool, con quali argomenti, quale decisione, quale risultato.**

---

## Slide 21 — Testing ed evaluation

Non basta verificare che “la demo funziona”. Testare almeno:

### Test del tool

- input validi e invalidi;
- path fuori dal workspace;
- timeout e cancellazione;
- diagnostici troppo grandi o contenenti dati sensibili.

### Test dell'agente

- richieste ambigue;
- prompt injection dentro file Razor o dati DB;
- richieste di modificare file non consentiti;
- loop di compilazione;
- modello che ignora o usa male un tool.

### Test del prodotto

- test di rendering e accessibilità;
- regressione di routing e autorizzazione;
- snapshot/diff della candidate;
- approvazione human-in-the-loop;
- evaluation su un dataset di richieste rappresentative.

### Criterio di successo

```text
La modifica compila
+ i test passano
+ il diff è nel perimetro
+ nessun controllo di sicurezza è stato violato
+ un umano può revisionare il risultato
```

---

## Slide 22 — Miglioramenti possibili alla demo

### Prima della produzione

1. Sostituire `ApproveAll` con policy per tipo di richiesta.
2. Eseguire l'agente in un worker/container isolato.
3. Aggiungere un diff strutturato e approvazione esplicita.
4. Limitare il numero di cicli e il budget.
5. Aggiungere hook di audit e redazione dei dati.
6. Eseguire test e lint dopo la compilazione.
7. Separare identità dell'utente, identità dell'agente e identità dei tool.
8. Definire rollback e scadenza delle candidate.

### Evoluzione architetturale

```text
Blazor UI
   │
   ▼
Application service
   │  autorizzazione, rate limit, audit
   ▼
Agent worker isolato
   │
   ├── Copilot SDK session
   ├── sandbox workspace
   ├── compile/test tools
   └── policy hooks
```

---

## Slide 23 — Decision tree

```text
Devo solo ottenere o trasformare testo?
 ├─ Sì → Microsoft.Extensions.AI / IChatClient
 └─ No

Devo usare embedding, RAG o cambiare provider facilmente?
 ├─ Sì → Microsoft.Extensions.AI + componenti dedicati
 └─ No

Devo perseguire un obiettivo in più passi con tool e stato operativo?
 ├─ No → Microsoft.Extensions.AI può essere sufficiente
 └─ Sì

L'agente deve lavorare su workspace, repository o strumenti Copilot?
 ├─ Sì → GitHub Copilot SDK
 └─ No → valutare Microsoft.Extensions.AI o un framework agentico

Serve il meglio dei due mondi?
 └─ Copilot SDK per l'orchestrazione + MEAI per capability AI riusabili
```

---

## Slide 24 — Checklist per gli sviluppatori

### Prima del primo prompt

- [ ] Qual è l'azione minima necessaria?
- [ ] Quali dati possono entrare nel contesto?
- [ ] Quali tool sono davvero necessari?
- [ ] Qual è il perimetro di filesystem/rete/database?
- [ ] Cosa succede se il modello sbaglia?

### Prima di dare un permesso

- [ ] L'operazione è read-only o muta stato?
- [ ] Il path/endpoint è in allowlist?
- [ ] L'argomento è validato dal server?
- [ ] Serve approvazione umana?
- [ ] È reversibile e auditabile?

### Prima del rilascio

- [ ] Test di prompt injection e tool abuse
- [ ] Rate limit, timeout e cancellation
- [ ] Logging senza segreti
- [ ] Rollback e gestione delle versioni
- [ ] Evaluation e monitoraggio dei costi
- [ ] Revisione delle dipendenze e dei permessi

---

## Slide 25 — Takeaway finali

1. **Un agente utile è un workflow verificabile, non un prompt più lungo.**
2. **I tool sono API di sicurezza:** piccoli, tipizzati, limitati e auditabili.
3. **La compilazione è un guardrail, non una prova di correttezza completa.**
4. **Copilot SDK è forte nell'orchestrazione agentica operativa.**
5. **`Microsoft.Extensions.AI` è forte nelle astrazioni .NET, nella portabilità e nelle pipeline.**
6. **`ApproveAll` va bene per una demo isolata, non come policy di produzione.**
7. **Il modello propone; l'applicazione autorizza, verifica e decide.**

> Il futuro non è “lasciare fare tutto all'AI”. È progettare sistemi in cui l'AI può fare cose utili senza poter fare qualunque cosa.

---

## Slide 26 — Q&A / Demo finale

### Domande per il pubblico

- Dove mettereste il confine tra agente e semplice chiamata AI?
- Quale tool non dareste mai a un agente in produzione?
- Preferireste una preview con approvazione o un'automazione end-to-end?
- Quale parte del workflow vorreste rendere agentica nella vostra applicazione?

### Demo finale suggerita

1. Aprire il dashboard AdventureWorks.
2. Inserire una richiesta di modifica della pagina.
3. Mostrare gli aggiornamenti di progresso.
4. Mostrare il tool `compile_page` e un eventuale fix.
5. Mostrare la pagina validata.
6. Mostrare il diff e ribadire il confine tra demo e produzione.

---

# Note tecniche e fonti

- Codice della demo: `AdventureWorks.BlazorApp/Services/PageUpdateService.cs`.
- Integrazione del pacchetto: `AdventureWorks.BlazorApp/AdventureWorks.BlazorApp.csproj`.
- Architettura applicativa: `README.md`.
- Documentazione ufficiale GitHub Copilot SDK: <https://github.com/github/copilot-sdk/tree/main/docs>.
- Documentazione .NET `Microsoft.Extensions.AI`: <https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai>.
- Ecosistema .NET AI e guida alla scelta degli strumenti: <https://learn.microsoft.com/dotnet/ai/dotnet-ai-ecosystem>.
- Interfaccia `IChatClient` e considerazioni sui rischi: <https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.ichatclient>.

## Nota sulle versioni

SDK, pacchetti NuGet, nomi dei modelli e API possono cambiare. Prima della conferenza e prima di un deployment verificare la documentazione e le versioni effettivamente usate dal repository e dall'ambiente di esecuzione.
