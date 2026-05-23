<div align="center">

```
  ██████╗ ██████╗ ██████╗ ███████╗███╗   ███╗ █████╗ ████████╗███████╗
 ██╔════╝██╔═══██╗██╔══██╗██╔════╝████╗ ████║██╔══██╗╚══██╔══╝██╔════╝
 ██║     ██║   ██║██║  ██║█████╗  ██╔████╔██║███████║   ██║   █████╗
 ██║     ██║   ██║██║  ██║██╔══╝  ██║╚██╔╝██║██╔══██║   ██║   ██╔══╝
 ╚██████╗╚██████╔╝██████╔╝███████╗██║ ╚═╝ ██║██║  ██║   ██║   ███████╗
  ╚═════╝ ╚═════╝ ╚═════╝ ╚══════╝╚═╝     ╚═╝╚═╝  ╚═╝   ╚═╝   ╚══════╝
```

**AI Coding Assistant for your terminal**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![Claude](https://img.shields.io/badge/Claude-claude--sonnet--4--5-D97AFF?style=flat-square)](https://anthropic.com)
[![Ollama](https://img.shields.io/badge/Ollama-qwen2.5--coder-black?style=flat-square)](https://ollama.ai)
[![No API Key](https://img.shields.io/badge/Ollama-No%20Key%20Required-green?style=flat-square)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](LICENSE)
[![Version](https://img.shields.io/badge/version-2.0.0-blue?style=flat-square)]()

*Write. Fix. Review. Refactor. Test. Analyse. — All from your terminal.*

**[📄 Sample Output](docs/sample-output.html)**

</div>

---

## What is Code-Cli?

Code-Cli v2 is a **command-line AI coding assistant** built with **.NET 8 / C# 12** that supports two providers:

- **[Claude](https://anthropic.com)** — Anthropic's API, set your key once with `config --set-key`
- **[Ollama](https://ollama.ai)** — 100% local and offline, no API key required, Docker supported

Switch providers at any time with a single flag. Zero NuGet dependencies — pure .NET BCL only.

```
code-cli chat
code-cli ask "How do I implement the repository pattern in C#?"
code-cli write "JWT authentication middleware in ASP.NET Core 8"
code-cli fix MyService.cs --error "NullReferenceException at line 42"
code-cli review Controllers/AuthController.cs
code-cli refactor OrderService.cs --goal "extract CQRS handlers"
code-cli test Services/PaymentService.cs --framework xunit
code-cli analyse . --focus Controllers --output report.md
```

---

## What's New in v2

| Feature | Details |
|---|---|
| **Claude provider** | Full SSE streaming via Anthropic API · `claude-sonnet-4-5` default |
| **`refactor` command** | Goal-driven refactoring plan + complete refactored file |
| **`test` command** | Full unit test suite with AAA pattern, Theory/InlineData |
| **`analyse` command** | Single file or whole project — 120k char context, file tree |
| **`config --set-key`** | One-time key setup, auto-switches provider to Claude |
| **`config --set-provider`** | Switch between Claude / Ollama / openai-compatible / llama.cpp |
| **`ProjectContextBuilder`** | Smart repo scanner: skips bin/obj/node_modules, focus pattern |
| **Provider abstraction** | `IModelProvider` — add any backend in one class |

---

## Commands

| Command | Arguments | Description |
|---|---|---|
| `chat` | | Interactive session with `/fix` `/review` `/refactor` `/test` `/analyse` |
| `ask` | `<question>` | One-off coding question |
| `write` | `<description>` | Generate production-ready code |
| `fix` | `<file> [--error <msg>]` | Detect and fix all bugs with severity ratings |
| `review` | `<file>` | 7-axis production-readiness audit with score |
| `explain` | `<file>` | Purpose · flow · concepts · data path · examples |
| `refactor` | `<file> [--goal <goal>]` | Goal-driven refactor plan + complete output |
| `test` | `<file> [--framework <fw>]` | Full test class — happy path + edge + null cases |
| `analyse` | `[path] [--focus <pat>]` | File or whole project, saves to `--output` |
| `diagnose` | `[path]` | Risks, bugs, and production issues |
| `optimize` | `[path]` | Performance and maintainability suggestions |
| `architecture` | | Explain repository architecture and extension points |
| `models` | | List available models for the active provider |
| `provider` | | Show active provider, endpoint, model |
| `config` | | View / update configuration |

---

## Prerequisites

### Option A — Claude (recommended)

No local install needed beyond the .NET SDK.

1. Get an API key from [console.anthropic.com](https://console.anthropic.com)
2. Run: `code-cli config --set-key sk-ant-...`
3. Done — `code-cli chat` works immediately

### Option B — Ollama (100% offline)

**Local:**
```bash
# Install from https://ollama.ai, then:
ollama pull qwen2.5-coder:7b
ollama serve          # Linux only — auto-starts on Windows/macOS
```

**Docker:**
```bash
docker run -d --name code-cli-ollama \
  -p 11434:11434 \
  -v code-cli-ollama:/root/.ollama \
  ollama/ollama:latest

docker exec -it code-cli-ollama ollama pull qwen2.5-coder:7b
```

---

## Installation

### Option A — Build from source

**Windows:**
```batch
git clone https://github.com/shatru123/Code-Cli.git
cd Code-Cli
build.bat       # → publish\code-cli.exe
install.bat     # → adds to PATH
```

**Linux / macOS:**
```bash
git clone https://github.com/shatru123/Code-Cli.git
cd Code-Cli
chmod +x build.sh && ./build.sh
sudo cp ./publish/code-cli /usr/local/bin/code-cli
```

### Option B — Run directly (no install)
```bash
git clone https://github.com/shatru123/Code-Cli.git
cd Code-Cli
dotnet run --project Code-Cli.csproj -- chat
```

---

## Quick Start

### Claude
```bash
code-cli config --set-key sk-ant-...
code-cli chat
```

### Ollama
```bash
ollama pull qwen2.5-coder:7b
code-cli chat --provider ollama
```

### Switch providers any time
```bash
code-cli config --set-provider claude
code-cli config --set-provider ollama
# or per-command:
code-cli ask "Explain CQRS" --provider claude
```

---

## Usage Examples

### Generate code
```bash
code-cli write "Generic repository with EF Core and Unit of Work"
code-cli write "Rate limiting middleware for ASP.NET Core 8" --output RateLimiter.cs
```

### Fix bugs
```bash
code-cli fix OrderService.cs
code-cli fix PaymentController.cs --error "Object reference not set at line 42"
```

### Review & Explain
```bash
code-cli review Services/UserService.cs --output review-report.md
code-cli explain Program.cs
```

### Refactor
```bash
code-cli refactor OrderService.cs --goal "extract CQRS command/query handlers"
code-cli refactor Controllers/AuthController.cs --goal "apply guard clauses and reduce nesting"
```

### Generate Tests
```bash
code-cli test Services/PaymentService.cs --framework xunit
code-cli test Repositories/UserRepository.cs --framework pytest
```

### Analyse a project
```bash
# Single file
code-cli analyse Services/OrderService.cs

# Whole project — builds file tree + all source files
code-cli analyse . --focus Controllers --output report.md
```

### Chat session
```bash
code-cli chat
# Inside chat:
# /fix MyService.cs
# /refactor OrderService.cs --goal "extract interfaces"
# /test UserService.cs
# /analyse src/Services
# /model claude-opus-4-5
```

---

## All Flags

| Flag | Default | Description |
|---|---|---|
| `--provider <p>` | `ollama` | `claude` · `ollama` · `openai-compatible` · `llama.cpp` |
| `--model <name>` | provider default | Override the AI model |
| `--host <url>` | `http://localhost:11434` | Ollama server URL |
| `--runtime <r>` | `local` | `local` or `docker` |
| `--output <file>` | print to console | Save response to file |
| `--error <msg>` | none | Error context for `fix` |
| `--goal <g>` | default goal | Refactoring goal for `refactor` |
| `--framework <fw>` | auto-detect | Test framework for `test` |
| `--focus <pattern>` | none | File pattern priority for `analyse` |
| `--no-stream` | streaming on | Wait for full response |
| `--verbose` | off | Show connection details |

---

## Configuration

Stored at `~/.code-cli/config.json`, created automatically on first run.

```json
{
  "provider": "ollama",
  "model": "qwen2.5-coder:7b",
  "anthropic_api_key": "",
  "anthropic_model": "claude-sonnet-4-5",
  "endpoint": "http://localhost:11434",
  "runtime": "local",
  "stream": true,
  "history_size": 10,
  "max_context_files": 8,
  "docker_image": "ollama/ollama:latest",
  "docker_container_name": "code-cli-ollama",
  "docker_auto_start": true
}
```

**Config shortcuts:**
```bash
code-cli config --set-key sk-ant-...        # Save Claude key + switch to Claude
code-cli config --set-provider ollama       # Switch back to Ollama
code-cli config                             # Print full config (key masked)
```

---

## Recommended Models

### Claude (via API)
| Model | Speed | Quality | Use for |
|---|---|---|---|
| `claude-sonnet-4-5` | Fast | ⭐⭐⭐⭐⭐ | Default — best balance ✅ |
| `claude-opus-4-5` | Slower | ⭐⭐⭐⭐⭐ | Most complex tasks |
| `claude-haiku-4-5-20251001` | Fastest | ⭐⭐⭐⭐ | Quick questions |

### Ollama (local)
| Model | Disk | RAM | Use for |
|---|---|---|---|
| `qwen2.5-coder:7b` | ~4 GB | 8 GB | Best local balance ✅ |
| `qwen2.5-coder:14b` | ~8 GB | 16 GB | Highest local quality |
| `qwen2.5-coder:1.5b` | ~1 GB | 4 GB | Low-spec machines |
| `deepseek-coder:6.7b` | ~3.8 GB | 8 GB | Excellent alternative |

---

## Architecture

```
code-cli.exe
│
├── Program.cs — CLI router
│   └── flag parsing · provider/runtime selection · config mutations
│
├── Commands (AllCommands.cs)
│   ├── AskCommand       WriteCommand      FixCommand
│   ├── ReviewCommand    ExplainCommand    RefactorCommand
│   ├── TestCommand      AnalyseCommand    DiagnoseCommand
│   ├── OptimizeCommand  ArchitectureCommand
│   └── ChatCommand      (REPL with /slash commands)
│
├── CodeAssistantService
│   └── Selects expert system prompt per command
│       Delegates streaming to IModelProvider
│
├── IModelProvider  ◄─ provider abstraction
│   ├── ClaudeModelProvider      (Anthropic SSE API)
│   ├── OllamaModelProvider      (NDJSON streaming)
│   ├── OpenAiCompatibleProvider (OpenAI-format REST)
│   └── LlamaCppModelProvider    (llama.cpp server)
│
├── ModelProviderRegistry
│   └── Creates the correct provider from config.Provider
│
├── ProjectContextBuilder
│   └── File tree + source files (120k char budget)
│       Skips bin/obj/node_modules/.git
│       --focus pattern prioritises matching files
│
└── ConfigManager
    └── ~/.code-cli/config.json
        SetApiKey() · SetProvider()
        ANTHROPIC_API_KEY env var auto-detection
```

---

## Troubleshooting

**Cannot connect to Claude**
```bash
code-cli config --set-key sk-ant-...
# or via env:
export ANTHROPIC_API_KEY=sk-ant-...
code-cli chat
```

**Cannot connect to Ollama**
```bash
ollama serve          # Linux — start manually
# Windows/macOS: check system tray
```

**Docker mode not starting**
```bash
docker info           # verify daemon is running
docker start code-cli-ollama
```

**Slow responses (Ollama)**
```bash
ollama pull qwen2.5-coder:1.5b
code-cli chat --model qwen2.5-coder:1.5b
```

**No .NET SDK found**
```bash
dotnet --list-sdks   # should show 8.x.x
# Install from https://dotnet.microsoft.com/download/dotnet/8.0
```

---

## Tech Stack

- **Runtime:** .NET 8 / C# 12
- **AI providers:** Anthropic Claude (SSE), Ollama (NDJSON), OpenAI-compatible, llama.cpp
- **Transport:** `HttpClient` streaming — SSE for Claude, NDJSON for Ollama
- **Output:** Single self-contained `.exe` (no install required after build)
- **Config:** JSON at `~/.code-cli/config.json`
- **Dependencies:** Zero NuGet packages — pure .NET BCL only

---

## Vision

Code-Cli is evolving from a smart CLI assistant into an **autonomous AI software engineer**:

- ✅ Multi-provider AI (Claude + Ollama)
- ✅ Repository context builder (file tree + source scanning)
- ✅ Refactor, test generation, project analysis
- 🔜 Roslyn semantic analysis — understand symbols, not just text
- 🔜 Multi-file reasoning — changes that span the whole codebase
- 🔜 Build validation — compile after each AI suggestion
- 🔜 Test execution — run tests and feed results back to AI
- 🔜 Autonomous bug-fix loop — fix → build → test → repeat

---

## License

MIT — Free to use, modify, and distribute for any purpose.

---

<div align="center">
Built with ❤️ by Shatrughna · .NET 8 · Claude + Ollama
</div>
