# Code-Cli

Local AI coding assistant built with .NET 8 and a configurable local provider system.

## Features

- 100% local and offline
- No API key required
- Interactive AI chat
- Production-ready code generation
- Bug fixing and code reviews
- Repository intelligence
- Automatic project scanning
- Docker + local Ollama runtime support
- Provider abstraction for `ollama`, `openai-compatible`, and `llama.cpp`
- Repository architecture, diagnose, and optimize commands
- Streaming responses
- Terminal-first workflow

---

## New: Repository Intelligence + Provider System

Code-Cli now includes the foundation for repository-aware AI workflows and configurable model providers.

### New Commands

```bash
code-cli explain-project
code-cli architecture
code-cli diagnose
code-cli optimize
code-cli provider
```

These commands can now:

- detects `.sln` files
- detects `.csproj` files
- scans repository structure
- counts C# source files
- analyzes project layout
- route requests through a provider registry
- support Ollama, OpenAI-compatible servers, and llama.cpp endpoints

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
| `diagnose` | Diagnose a file or repository |
| `optimize` | Optimize a file or repository |
| `architecture` | Explain current repository architecture |
| `provider` | Show active provider and endpoint |
| `models` | List installed models from the active provider |
| `config` | Show configuration |

---

## Provider Configuration

Example `~/.code-cli/config.json`:

```json
{
  "provider": "ollama",
  "model": "qwen2.5-coder:7b",
  "endpoint": "http://localhost:11434",
  "stream": true,
  "history_size": 20,
  "runtime": "local"
}
```

Supported providers:

- `ollama`
- `openai-compatible`
- `llama.cpp`

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

### Local Ollama

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

### Docker Ollama

```bash
git clone https://github.com/shatru123/Code-Cli.git
cd Code-Cli

docker run -d --name code-cli-ollama -p 11434:11434 -v code-cli-ollama:/root/.ollama ollama/ollama:latest
docker exec -it code-cli-ollama ollama pull qwen2.5-coder:1.5b

dotnet run --project Code-Cli.csproj -- chat --runtime docker
```

### OpenAI-Compatible or llama.cpp endpoint

```bash
dotnet run --project Code-Cli.csproj -- provider --provider openai-compatible --endpoint http://localhost:8080/v1
dotnet run --project Code-Cli.csproj -- ask "Explain this repository" --provider llama.cpp --endpoint http://localhost:8080/v1
```

---

## License

MIT
