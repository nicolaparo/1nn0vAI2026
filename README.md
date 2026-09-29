# 1nn0vAI2026

Demo repository for the talk **"Applicazioni allucinanti con GitHub Copilot SDK e C#"** at **1nn0vAI 2026**
(slides content in [docs/presentation.md](docs/presentation.md), Italian).

🇬🇧 [English](#-english) · 🇮🇹 [Italiano](#-italiano)

---

# 🇬🇧 English

The repo contains an AdventureWorks ERP/CRM sample (a .NET 10 Blazor Web App orchestrated with .NET Aspire on
top of the AdventureWorks SQL Server database) and five demos of the **GitHub Copilot SDK** (`GitHub.Copilot.SDK`
1.0.11, model `gpt-5.6-luna`) in C#.

## ⚠️ Important: run from the terminal, not from Visual Studio

The demos **do not really work with Visual Studio** (F5 / the Aspire and console launch profiles are not set up
for them, and the interactive console apps do not behave well in its debugger window).
**They have no problem starting from the terminal**: every demo has a `run-demo-*.ps1` script at the repo root.
Open PowerShell in the repo root and run the script, e.g. `./run-demo-4.ps1`.
Visual Studio Code with the terminal works fine too.

## Common requirements (all demos)

- Windows with PowerShell (the scripts are `.ps1`)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A **GitHub account with an active GitHub Copilot subscription**, signed in on the machine
  (Copilot CLI installed and authenticated), with access to the model `gpt-5.6-luna`
- Internet access (Copilot calls the GitHub service)

## Demos at a glance

| # | Script | Project | Showcases | Extra requirements |
| --- | --- | --- | --- | --- |
| 1 | `run-demo-1.ps1` | `AdventureWorks.AppHost` | An agent that rewrites a live app page, with in-app compilation and tests | **Docker Desktop installed and running** |
| 2 | `run-demo-2.ps1` | `CopilotSDKDemo.PermissionDemo` | Intercepting a built-in tool from C# | none |
| 3 | `run-demo-3-a.ps1` / `run-demo-3-b.ps1` | `CopilotSDKDemo.SandboxDemo` | The sandbox as a second gate, on and off | none (uses temp files) |
| 4 | `run-demo-4.ps1` | `CopilotSDKDemo.InstructionsDemo` | The folder's `copilot-instructions.md` is honored | none |
| 5 | `run-demo-5-a.ps1` / `run-demo-5-b.ps1` | `CopilotSDKDemo.CustomPromptDemo` | A prompt defined in the app, alone and combined with the folder's | none |

Demos 2 to 5 are console apps: they only need the common requirements above, **not Docker**.

## Demo 1 - AdventureWorks: Copilot edits a page of the running app

`./run-demo-1.ps1`

**Requirements:** the common ones, plus
- **Docker Desktop installed *and running*** on the local machine (Aspire starts SQL Server as a container through the
  Docker API). Wait for *Engine running* and check with `docker info`. Without it you get an error such as
  `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine`.
- Internet access on the first run: the `chriseaton/adventureworks` image is pulled and the database restored inside
  the container, which takes a few minutes. The container is persistent, so later runs are fast.

The script starts the Aspire AppHost, which runs the SQL Server container and the Blazor app (see the Aspire
dashboard for the endpoints). The user types a change request for the page they are looking at (dashboard,
customers, customer detail, orders). `PageUpdateService` then:

1. Copies the page into a private folder under `AdventureWorks.UserPages/` and starts a Copilot session with that
   folder as `WorkingDirectory`.
2. Gives the agent two custom tools: `report_progress` (live status shown in the UI) and `compile_page`.
3. `compile_page` compiles the Razor page in-process with Roslyn. If it fails, the diagnostics go back to the agent,
   which fixes the page and calls the tool again.
4. When the compilation succeeds, the tests run automatically (`PageTestRunner`: `dotnet test AdventureWorks.Tests`,
   filtered to `CandidatePage_*`, bUnit with fake data services). A failure goes back to the agent, so the loop is
   edit, compile, test, until green.
5. After the session the app validates again. Only if that passes is the page activated and shown to the user.

Notes: the first test run does a full build into `AdventureWorks.UserPages/.test-artifacts/` (git-ignored) and takes
longer. The repo also has integration tests (`AdventureWorks.Tests/Integration`, Testcontainers) and Playwright
E2E tests (`scripts/run-e2e-tests.ps1`); both **also need Docker Desktop running**.

## Demo 2 - PermissionDemo: intercept a default tool

`./run-demo-2.ps1`. **Requirements:** the common ones (no Docker).

Put breakpoints on the three `>>> BREAKPOINT` lines in `Program.cs`, in firing order:

1. `OnPreToolUse` hook: fires before *any* tool, can allow, deny or rewrite args.
2. `OnPermissionRequest`: fires for permission-gated operations (shell, write, url...). Shell is rejected here.
3. `ViewFileHandler`: a custom tool named `view` with `OverridesBuiltInTool = true` replaces the built-in tool.
   The handler does nothing and returns a canned string.

## Demo 3 - SandboxDemo: confine what an approved command can do

- `./run-demo-3-a.ps1`: sandbox ON, the attacks are blocked.
- `./run-demo-3-b.ps1`: sandbox OFF (`--no-sandbox`), to show the contrast.

**Requirements:** the common ones (no Docker). Everything happens in `%TEMP%\CopilotSDKDemo.SandboxDemo`; the demo
only touches a fake secrets file it creates itself. The Copilot sandbox and the `GitHub.Copilot.Rpc` types are
experimental (`GHCP001`) and may change or behave differently depending on the OS/CLI version.

The permission handler approves everything on purpose. The sandbox, set through
`session.Rpc.Options.UpdateAsync(sandboxConfig: ...)`, is the second gate: read/write only in the workspace, a denied
`protected` folder, no outbound network. The demo ends by checking the protected file is intact.

## Demo 4 - InstructionsDemo: the folder's copilot-instructions.md is honored

`./run-demo-4.ps1`. **Requirements:** the common ones (no Docker).

The session's `WorkingDirectory` is `Workspace/`. The code contains no persona: the pirate replies come from
`Workspace/.github/copilot-instructions.md`, loaded by the runtime from the working directory
(`SkipCustomInstructions = false`). Edit that file and re-run to change the behavior.

## Demo 5 - CustomPromptDemo: a prompt defined in the app

- `./run-demo-5-a.ps1`: cat prompt only.
- `./run-demo-5-b.ps1`: also load the folder's pirate instructions (`--with-folder`).

**Requirements:** the common ones (no Docker).

Same `Workspace/` folder, but the persona is a `SystemMessage` (`SystemMessageMode.Append`) set in `Program.cs`.
With `SkipCustomInstructions = true` the reply is purely feline; with `--with-folder` cat and pirate get mixed.

## Projects

| Project | Description |
| --- | --- |
| `AdventureWorks.AppHost` | Aspire orchestration: SQL Server from `chriseaton/adventureworks:latest`, plus the Blazor app. |
| `AdventureWorks.ServiceDefaults` | Shared telemetry, health checks, resilience, service discovery. |
| `AdventureWorks.BlazorApp` | Blazor Web App (Interactive Server), EF Core model, data services, Copilot page-update service. |
| `AdventureWorks.Abstractions` / `SharedComponents` / `ExternalPages` | Contracts, shared Razor components, and the pages the agent can rewrite. |
| `AdventureWorks.Tests` / `E2ETests` | bUnit, integration (Testcontainers) and Playwright tests. |
| `CopilotSDKDemo.*` | Console demos 2 to 5. |
| `Workspace/` | Sample folder used by demos 4 and 5. |

---

# 🇮🇹 Italiano

Il repository contiene un esempio ERP/CRM AdventureWorks (una Blazor Web App .NET 10 orchestrata con .NET Aspire
sul database SQL Server AdventureWorks) e cinque demo del **GitHub Copilot SDK** (`GitHub.Copilot.SDK` 1.0.11,
modello `gpt-5.6-luna`) in C#.

## ⚠️ Importante: avviare dal terminale, non da Visual Studio

Le demo **non funzionano davvero con Visual Studio** (F5 e i profili di avvio Aspire/console non sono pensati per
queste demo, e le app console interattive si comportano male nella finestra del debugger).
**Non ci sono invece problemi ad avviarle dal terminale**: ogni demo ha uno script `run-demo-*.ps1` nella root.
Apri PowerShell nella root del repo ed esegui lo script, ad es. `./run-demo-4.ps1`.
Anche Visual Studio Code con il terminale integrato funziona bene.

## Requisiti comuni (tutte le demo)

- Windows con PowerShell (gli script sono `.ps1`)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Un **account GitHub con una sottoscrizione GitHub Copilot attiva**, autenticato sulla macchina
  (Copilot CLI installata e con login effettuato), con accesso al modello `gpt-5.6-luna`
- Connessione a Internet (Copilot usa il servizio GitHub)

## Le demo in sintesi

| # | Script | Progetto | Cosa mostra | Requisiti aggiuntivi |
| --- | --- | --- | --- | --- |
| 1 | `run-demo-1.ps1` | `AdventureWorks.AppHost` | Un agente che riscrive una pagina di un'app in esecuzione, con compilazione e test in-app | **Docker Desktop installato e in esecuzione** |
| 2 | `run-demo-2.ps1` | `CopilotSDKDemo.PermissionDemo` | Intercettare da C# un tool predefinito | nessuno |
| 3 | `run-demo-3-a.ps1` / `run-demo-3-b.ps1` | `CopilotSDKDemo.SandboxDemo` | La sandbox come secondo cancello, attiva e disattiva | nessuno (usa file temporanei) |
| 4 | `run-demo-4.ps1` | `CopilotSDKDemo.InstructionsDemo` | Il `copilot-instructions.md` della cartella viene rispettato | nessuno |
| 5 | `run-demo-5-a.ps1` / `run-demo-5-b.ps1` | `CopilotSDKDemo.CustomPromptDemo` | Un prompt definito nell'app, da solo e combinato con quello della cartella | nessuno |

Le demo da 2 a 5 sono app console: servono solo i requisiti comuni qui sopra, **non Docker**.

## Demo 1 - AdventureWorks: Copilot modifica una pagina dell'app in esecuzione

`./run-demo-1.ps1`

**Requisiti:** quelli comuni, più
- **Docker Desktop installato *e in esecuzione*** sulla macchina locale (Aspire avvia SQL Server come container
  tramite le API Docker). Attendi *Engine running* e verifica con `docker info`. Senza Docker compare un errore tipo
  `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine`.
- Connessione a Internet al primo avvio: viene scaricata l'immagine `chriseaton/adventureworks` e il database viene
  ripristinato nel container, operazione che richiede qualche minuto. Il container è persistente, quindi gli avvii
  successivi sono rapidi.

Lo script avvia l'AppHost Aspire, che esegue il container SQL Server e la Blazor app (gli endpoint sono nella
dashboard Aspire). L'utente scrive una richiesta di modifica per la pagina che sta guardando (dashboard, clienti,
dettaglio cliente, ordini). `PageUpdateService` quindi:

1. Copia la pagina in una cartella privata sotto `AdventureWorks.UserPages/` e avvia una sessione Copilot con quella
   cartella come `WorkingDirectory`.
2. Dà all'agente due tool personalizzati: `report_progress` (stato in tempo reale mostrato nella UI) e `compile_page`.
3. `compile_page` compila la pagina Razor in-process con Roslyn. Se fallisce, la diagnostica torna all'agente, che
   corregge la pagina e richiama il tool.
4. Quando la compilazione riesce, i test partono automaticamente (`PageTestRunner`: `dotnet test AdventureWorks.Tests`
   filtrato su `CandidatePage_*`, bUnit con servizi dati finti). Un fallimento torna all'agente, quindi il ciclo è
   modifica, compilazione, test, fino al verde.
5. Al termine della sessione l'app valida di nuovo. Solo se la validazione passa, la pagina viene attivata e mostrata
   all'utente.

Note: la prima esecuzione dei test fa una build completa in `AdventureWorks.UserPages/.test-artifacts/` (ignorata da
git) e richiede più tempo. Il repo contiene anche test di integrazione (`AdventureWorks.Tests/Integration`,
Testcontainers) e test E2E Playwright (`scripts/run-e2e-tests.ps1`): **entrambi richiedono Docker Desktop in
esecuzione**.

## Demo 2 - PermissionDemo: intercettare un tool predefinito

`./run-demo-2.ps1`. **Requisiti:** quelli comuni (niente Docker).

Metti i breakpoint sulle tre righe `>>> BREAKPOINT` di `Program.cs`, nell'ordine in cui scattano:

1. Hook `OnPreToolUse`: scatta prima di *qualsiasi* tool, può consentire, negare o riscrivere gli argomenti.
2. `OnPermissionRequest`: scatta per le operazioni soggette a permesso (shell, write, url...). Qui la shell viene
   rifiutata.
3. `ViewFileHandler`: un tool custom chiamato `view` con `OverridesBuiltInTool = true` sostituisce quello integrato.
   L'handler non fa nulla e restituisce una stringa fissa.

## Demo 3 - SandboxDemo: confinare ciò che un comando approvato può fare

- `./run-demo-3-a.ps1`: sandbox attiva, gli attacchi vengono bloccati.
- `./run-demo-3-b.ps1`: sandbox disattivata (`--no-sandbox`), per mostrare il contrasto.

**Requisiti:** quelli comuni (niente Docker). Tutto avviene in `%TEMP%\CopilotSDKDemo.SandboxDemo`; la demo tocca solo
un finto file di segreti creato da lei stessa. La sandbox di Copilot e i tipi `GitHub.Copilot.Rpc` sono sperimentali
(`GHCP001`) e possono cambiare o comportarsi diversamente a seconda di sistema operativo/versione della CLI.

L'handler dei permessi approva tutto di proposito. La sandbox, configurata con
`session.Rpc.Options.UpdateAsync(sandboxConfig: ...)`, è il secondo cancello: lettura/scrittura solo nel workspace,
cartella `protected` negata, nessuna rete in uscita. La demo termina verificando che il file protetto sia intatto.

## Demo 4 - InstructionsDemo: il copilot-instructions.md della cartella viene rispettato

`./run-demo-4.ps1`. **Requisiti:** quelli comuni (niente Docker).

La `WorkingDirectory` della sessione è `Workspace/`. Il codice non contiene nessun personaggio: le risposte da pirata
arrivano da `Workspace/.github/copilot-instructions.md`, caricato dal runtime dalla cartella di lavoro
(`SkipCustomInstructions = false`). Modifica quel file e rilancia per cambiare il comportamento.

## Demo 5 - CustomPromptDemo: un prompt definito nell'app

- `./run-demo-5-a.ps1`: solo il prompt del gatto.
- `./run-demo-5-b.ps1`: carica anche le istruzioni da pirata della cartella (`--with-folder`).

**Requisiti:** quelli comuni (niente Docker).

Stessa cartella `Workspace/`, ma il personaggio è un `SystemMessage` (`SystemMessageMode.Append`) impostato in
`Program.cs`. Con `SkipCustomInstructions = true` la risposta è puramente felina; con `--with-folder` gatto e pirata
si mescolano.

## Progetti

| Progetto | Descrizione |
| --- | --- |
| `AdventureWorks.AppHost` | Orchestrazione Aspire: SQL Server da `chriseaton/adventureworks:latest`, più la Blazor app. |
| `AdventureWorks.ServiceDefaults` | Telemetria, health check, resilienza e service discovery condivisi. |
| `AdventureWorks.BlazorApp` | Blazor Web App (Interactive Server), modello EF Core, servizi dati, servizio di aggiornamento pagine con Copilot. |
| `AdventureWorks.Abstractions` / `SharedComponents` / `ExternalPages` | Contratti, componenti Razor condivisi e pagine che l'agente può riscrivere. |
| `AdventureWorks.Tests` / `E2ETests` | Test bUnit, di integrazione (Testcontainers) e Playwright. |
| `CopilotSDKDemo.*` | Demo console da 2 a 5. |
| `Workspace/` | Cartella di esempio usata dalle demo 4 e 5. |
