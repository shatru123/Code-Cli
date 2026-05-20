<div align="center">

```
  ██████╗ ██████╗ ██████╗ ███████╗███╗   ███╗ █████╗ ████████╗███████╗
 ██╔════╝██╔═══██╗██╔══██╗██╔════╝████╗ ████║██╔══██╗╚══██╔══╝██╔════╝
 ██║     ██║   ██║██║  ██║█████╗  ██╔████╔██║███████║   ██║   █████╗
 ██║     ██║   ██║██║  ██║██╔══╝  ██║╚██╔╝██║██╔══██║   ██║   ██╔══╝
 ╚██████╗╚██████╔╝██████╔╝███████╗██║ ╚═╝ ██║██║  ██║   ██║   ███████╗
  ╚═════╝ ╚═════╝ ╚═════╝ ╚══════╝╚═╝     ╚═╝╚═╝  ╚═╝   ╚═╝   ╚══════╝
```

**Local AI Coding Assistant for your terminal**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![Ollama](https://img.shields.io/badge/Powered%20by-Ollama-black?style=flat-square)](https://ollama.ai)
[![No API Key](https://img.shields.io/badge/API%20Key-Not%20Required-green?style=flat-square)]()
[![100% Offline](https://img.shields.io/badge/Works-100%25%20Offline-blue?style=flat-square)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](LICENSE)

*Write. Fix. Review. Explain. — All from your terminal. No subscription. No internet. Forever free.*

</div>

---

## What is Code-Cli?

Code-Cli is a **command-line AI coding assistant** built with **.NET 8** that runs entirely on your own machine. It requires **no API key**, no internet connection after setup, and no subscription fee — ever.

It uses **Ollama** to run open-source AI models locally, giving you a private, fast coding assistant that lives in your terminal.

```
code-cli chat
code-cli ask "How do I implement the repository pattern in C#?"
code-cli fix MyService.cs --error "NullReferenceException at line 42"
code-cli review Controllers/AuthController.cs
code-cli write "REST API with JWT auth in ASP.NET Core 8"
code-cli explain Program.cs
```

---

## Features

| Command | Description |
|---|---|
| `chat` | Interactive coding session with conversation history |
| `ask` | Quick one-off coding questions |
| `write` | Generate production-ready code from a plain description |
| `fix` | Detect and fix all bugs in a source file |
| `review` | Full production-readiness audit — security, performance, SOLID |
| `explain` | Detailed walkthrough of any code file |
| `models` | List all locally installed AI models |
| `config` | View or edit configuration |

---

## Prerequisites

Before using Code-Cli you need two things installed on your machine:

### 1. .NET 8 SDK *(only needed to build from source)*

Download and install from:
👉 https://dotnet.microsoft.com/download/dotnet/8.0

Verify it works:
```bash
dotnet --version
# Should print 8.x.x
```

### 2. Ollama Runtime *(required to run — provides the AI brain)*

Code-Cli now supports two Ollama runtime modes:

- **Local Ollama**: install Ollama directly on your machine
- **Docker Ollama**: let Code-Cli start and use an Ollama Docker container

#### Local Ollama

Download and install from:
👉 https://ollama.ai

- **Windows**: Download the `.exe` installer, run it, click Next → Next → Finish
- **macOS**: Download the `.zip`, drag to Applications
- **Linux**: Run the install script shown on the site

Verify Ollama is installed:
```bash
ollama --version
```

#### Docker Ollama

Install Docker Desktop or Docker Engine, then verify Docker is available:

```bash
docker --version
docker info
```

---

## Installation

### Option A — Download pre-built exe *(easiest)*

1. Go to [Releases](https://github.com/shatru123/Code-Cli/releases)
2. Download `code-cli.exe` (Windows) or `code-cli` (Linux/macOS)
3. Move it to a folder in your PATH (e.g. `C:\Tools\`)
4. Open a new terminal — `code-cli` is ready

### Option B — Build from source

**Windows:**
```batch
git clone https://github.com/shatru123/Code-Cli.git
cd Code-Cli
build.bat       # builds publish\code-cli.exe
install.bat     # copies to %USERPROFILE%\.code-cli\bin and adds to PATH
```

**Linux / macOS:**
```bash
git clone https://github.com/shatru123/Code-Cli.git
cd Code-Cli
chmod +x build.sh
./build.sh
sudo cp ./publish/code-cli /usr/local/bin/code-cli
```

Open a **new terminal** after install.

---

## First-Time Setup (Pull an AI Model)

### Local Ollama setup

After installing Ollama locally, pull a code-focused AI model. This is a one-time download:

```bash
# Best overall — recommended (requires ~4 GB disk + 8 GB RAM)
ollama pull qwen2.5-coder:7b

# Lighter option (requires ~800 MB disk + 4 GB RAM)
ollama pull qwen2.5-coder:1.5b

# Alternative — also very good
ollama pull deepseek-coder:6.7b
```

Then start the Ollama server:
```bash
ollama serve
```

> **Note:** On Windows and macOS, Ollama starts automatically after install and runs in the background. You only need `ollama serve` on Linux.

### Docker Ollama setup

If you prefer Docker, switch Code-Cli to Docker mode:

```bash
code-cli chat --runtime docker
```

On first use, Code-Cli will create and start an Ollama container automatically using:

```bash
docker run -d --name code-cli-ollama -p 11434:11434 -v code-cli-ollama:/root/.ollama ollama/ollama:latest
```

Then pull a model inside the container:

```bash
docker exec -it code-cli-ollama ollama pull qwen2.5-coder:7b
```

If you want a faster first run on lower-spec machines, use:

```bash
docker exec -it code-cli-ollama ollama pull qwen2.5-coder:1.5b
```

---

## macOS Quick Start

### Run with local Ollama

```bash
git clone https://github.com/shatru123/Code-Cli.git
cd Code-Cli
dotnet run --project Code-Cli.csproj -- chat --runtime local
```

### Run with Docker Ollama

```bash
git clone https://github.com/shatru123/Code-Cli.git
cd Code-Cli
dotnet run --project Code-Cli.csproj -- chat --runtime docker
```

### Run after Docker is configured as default runtime

```bash
cd Code-Cli
dotnet run --project Code-Cli.csproj -- chat
```

---

## Usage Guide

### Interactive Chat

Start a persistent conversation with full context memory:

```bash
code-cli chat
```

Inside the chat session you can type naturally, or use inline commands:

| Inline command | What it does |
|---|---|
| `/fix MyFile.cs` | Fix bugs in a file |
| `/review MyFile.cs` | Full code review |
| `/explain MyFile.cs` | Explain a file |
| `/model qwen2.5-coder:14b` | Switch model mid-session |
| `clear` | Clear chat history |
| `exit` | Quit |

**Example session:**
```
You     ▶ Write a generic repository in C# with EF Core
Code-Cli ▶ ...generates full implementation...

You     ▶ /fix OrderService.cs
Code-Cli ▶ ...finds and fixes all bugs...

You     ▶ How do I add caching to this?
Code-Cli ▶ ...continues with context...
```

---

### Ask a Question

```bash
code-cli ask "What is the difference between IEnumerable and IQueryable?"
code-cli ask How do I implement retry logic with Polly in .NET 8
code-cli ask "Explain CQRS pattern with a C# example"
```

---

### Generate Production-Ready Code

```bash
code-cli write "JWT authentication middleware in ASP.NET Core 8"
code-cli write "Generic repository pattern with EF Core and Unit of Work"
code-cli write "Thread-safe LRU cache in C#"
code-cli write "Retry decorator with exponential backoff"

# Save output directly to a file
code-cli write "Redis cache helper with sliding expiration" --output RedisHelper.cs
```

---

### Fix Bugs in a File

```bash
# Auto-detect all bugs
code-cli fix Program.cs

# With error context (more targeted fix)
code-cli fix OrderService.cs --error "System.NullReferenceException at line 42"
code-cli fix PaymentController.cs --error "Object reference not set to an instance"

# Save the fixed version
code-cli fix MyService.cs --output MyService.fixed.cs
```

---

### Code Review

Performs a full production-readiness audit covering security, performance, SOLID principles, error handling, and test coverage:

```bash
code-cli review Services/UserService.cs
code-cli review Controllers/AuthController.cs

# Save report as markdown
code-cli review PaymentService.cs --output review-report.md
```

---

### Explain Code

```bash
code-cli explain Program.cs
code-cli explain Algorithms/QuickSort.cs

# Save explanation to file
code-cli explain ComplexService.cs --output explanation.md
```

---

### List Installed Models

```bash
code-cli models
dotnet run --project Code-Cli.csproj -- models
```

---

## All Options & Flags

| Flag | Default | Description |
|---|---|---|
| `--model <name>` | `qwen2.5-coder:7b` | Use a specific AI model |
| `--host <url>` | `http://localhost:11434` | Ollama server URL |
| `--runtime <type>` | `local` | Choose `local` or `docker` Ollama runtime |
| `--output <file>` | *(print to console)* | Save response to a file |
| `--error <message>` | *(none)* | Error context for `fix` command |
| `--no-stream` | *(streaming on)* | Wait for full response before printing |
| `--verbose` | *(off)* | Show connection info |

---

## Configuration

Config is stored at `~/.code-cli/config.json` and is created automatically on first run.

```json
{
  "model": "qwen2.5-coder:7b",
  "runtime": "local",
  "host": "http://localhost:11434",
  "docker_image": "ollama/ollama:latest",
  "docker_container_name": "code-cli-ollama",
  "docker_volume": "code-cli-ollama",
  "docker_auto_start": true,
  "stream": true,
  "history_size": 10,
  "preferred_language": "auto"
}
```

View current config:
```bash
code-cli config
dotnet run --project Code-Cli.csproj -- config
```

---

## Recommended Models

| Model | Disk Size | Min RAM | Best For |
|---|---|---|---|
| `qwen2.5-coder:7b` | ~4 GB | 8 GB | Best balance ✅ Recommended |
| `qwen2.5-coder:14b` | ~8 GB | 16 GB | Highest quality |
| `qwen2.5-coder:1.5b` | ~1 GB | 4 GB | Low-spec machines, fastest |
| `deepseek-coder:6.7b` | ~3.8 GB | 8 GB | Excellent code quality |
| `codellama:7b` | ~3.8 GB | 8 GB | Good general-purpose |

Switch model anytime:
```bash
code-cli chat --model qwen2.5-coder:14b
code-cli ask "..." --model deepseek-coder:6.7b
```

---

## Architecture

```
code-cli.exe
│
├── CLI Router (Program.cs)
│       └── Parses commands, flags, and file paths
│
├── CodeAssistantService
│       └── Selects the right expert system prompt per command
│           ├── CodeWriter   — production-ready code generation
│           ├── BugFixer     — structured bug analysis + fix
│           ├── CodeReviewer — SOLID/security/performance audit
│           ├── Explainer    — step-by-step code walkthrough
│           └── ChatAssistant — conversational with history
│
└── OllamaService
        └── HTTP POST → http://localhost:11434/api/generate
                    Streaming NDJSON response (token by token)
                    ↓
                Local LLM (qwen2.5-coder, deepseek-coder, etc.)
                Runs 100% on your GPU / CPU

└── OllamaRuntimeManager
        └── Chooses local or docker runtime
            Can auto-start `ollama/ollama` in Docker mode
```

---

## Troubleshooting

**"Cannot connect to Ollama"**
```bash
ollama serve    # Start Ollama manually (Linux)
# On Windows/Mac: check system tray — Ollama should be running
```

**"Cannot connect in Docker mode"**
```bash
docker info
docker ps -a | grep code-cli-ollama
docker start code-cli-ollama
```

**"No .NET SDKs were found"**
```bash
dotnet --list-sdks
```

If `dotnet` shows only the runtime and no SDKs, install the .NET 8 SDK and make sure your shell resolves the SDK copy first.

On macOS with a user-local install, add this to `~/.zshrc` or `~/.zprofile`:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

Then open a new terminal or run:

```bash
source ~/.zshrc
```

**Slow responses**
```bash
# Use a smaller model
ollama pull qwen2.5-coder:1.5b
code-cli chat --model qwen2.5-coder:1.5b
```

**Model not found**
```bash
ollama list                        # See what's installed
ollama pull qwen2.5-coder:7b       # Pull the default model
```

**Model not found in Docker mode**
```bash
docker exec -it code-cli-ollama ollama list
docker exec -it code-cli-ollama ollama pull qwen2.5-coder:7b
```

**Out of memory crash**
```bash
# Use the 1.5b model — works on 4 GB RAM
ollama pull qwen2.5-coder:1.5b
```

**Windows: colors not showing correctly**
- Run in Windows Terminal (not the old CMD) for full color support
- Download from Microsoft Store: **Windows Terminal**

---

## Tech Stack

- **Runtime:** .NET 8 (C# 12)
- **AI Engine:** Ollama (local or Docker runtime)
- **Default Model:** Qwen 2.5 Coder 7B
- **Transport:** HTTP streaming (NDJSON) via `HttpClient`
- **Output:** Single self-contained `.exe` (no install required)
- **Config:** JSON at `~/.code-cli/config.json`
- **Dependencies:** Zero NuGet packages — pure .NET BCL only

---

## License

MIT — Free to use, modify, and distribute for any purpose.

---

<div align="center">
Built with ❤️ by Shatrughna using .NET 8 and Ollama
</div>
