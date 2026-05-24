# Local AI Agent Architecture

## Goals

- Cross-platform CLI support
- Windows/macOS/Linux compatibility
- No API key requirement
- Configurable local AI providers
- Production-ready code generation
- Repository-aware debugging and analysis

## Supported Providers

- Ollama
- llama.cpp
- OpenAI-compatible local endpoints
- DeepSeek Coder
- Qwen Coder
- Phi

## Provider Abstraction

```csharp
public interface IModelProvider
{
    Task<string> GenerateAsync(string prompt);
    IAsyncEnumerable<string> StreamAsync(string prompt);
}
```

## Recommended Runtime

Default runtime should be Ollama.

```bash
ollama run qwen2.5-coder:7b
```

## Cross Platform Publishing

```bash
dotnet publish -c Release -r win-x64 --self-contained true

dotnet publish -c Release -r osx-x64 --self-contained true

dotnet publish -c Release -r linux-x64 --self-contained true
```

## Planned Commands

```bash
code-cli review
code-cli diagnose
code-cli architecture
code-cli optimize
code-cli fix
```

## Future Enhancements

- Multi-agent orchestration
- Semantic code search
- Roslyn-powered diagnostics
- Plugin system
- VS Code extension
- Autonomous refactoring
