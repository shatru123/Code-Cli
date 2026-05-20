# Code-Cli

Local AI coding assistant built with .NET 8 + Ollama.

## Features

- 100% local and offline
- No API key required
- Interactive AI chat
- Production-ready code generation
- Bug fixing and code reviews
- Repository intelligence
- Automatic project scanning
- Docker + local Ollama runtime support
- Streaming responses
- Terminal-first workflow

---

## New: Repository Intelligence

Code-Cli now includes the foundation for repository-aware AI workflows.

### New Command

```bash
code-cli explain-project
```

This command automatically:

- detects `.sln` files
- detects `.csproj` files
- scans repository structure
- counts C# source files
- analyzes project layout

No manual project explanation required.

---

## Example

```bash
code-cli explain-project
```

Example output:

```text
PROJECT ANALYSIS

Root Path      : /Users/shatrughna/Projects/Code-Cli
Solutions      : 1
Projects       : 1
C# Files       : 42

Detected Projects:
  • Code-Cli.csproj
```

---

## Current Commands

| Command | Description |
|---|---|
| `chat` | Interactive coding assistant |
| `ask` | Ask coding questions |
| `write` | Generate production-ready code |
| `fix` | Detect and fix bugs |
| `review` | Review code quality |
| `explain` | Explain source code |
| `explain-project` | Analyze current repository |
| `models` | List installed models |
| `config` | Show configuration |

---

## Recommended Models

### Best Balance

```bash
ollama pull qwen2.5-coder:7b
```

### More Powerful

```bash
ollama pull qwen2.5-coder:14b
```

### High-End Setup

```bash
ollama pull qwen2.5-coder:32b
```

---

## Vision

Code-Cli is evolving from:

```text
AI chatbot
```

into:

```text
Autonomous AI Software Engineer
```

Planned upgrades:

- Roslyn semantic analysis
- Architecture understanding
- Semantic code search
- Autonomous bug fixing
- Multi-file reasoning
- Build validation
- Test execution
- Intelligent file patching
- AI agent workflows

---

## Tech Stack

- .NET 8
- Ollama
- Qwen Coder
- C# 12
- Docker
- Local LLM inference

---

## Run Locally

```bash
git clone https://github.com/shatru123/Code-Cli.git
cd Code-Cli

ollama serve
ollama pull qwen2.5-coder:7b

# Start chat
dotnet run --project Code-Cli.csproj -- chat

# Analyze repository
dotnet run --project Code-Cli.csproj -- explain-project
```

---

## License

MIT
