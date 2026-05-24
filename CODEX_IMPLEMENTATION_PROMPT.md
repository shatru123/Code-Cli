# Codex Prompt For Full Implementation

You are an autonomous senior AI systems engineer and principal .NET architect.

Your task is to transform this repository into a production-grade local AI coding assistant similar to Claude Code.

Repository:
https://github.com/shatru123/Code-Cli

Goals:
- Preserve ALL existing functionality
- Preserve terminal UX and ASCII banner
- Preserve existing commands
- Preserve backward compatibility
- Extend architecture without breaking current CLI

Core Requirements:

1. Cross Platform
- Windows
- macOS
- Linux
- Self-contained .NET 8 binaries
- Global CLI installation

2. No API Key Requirement
- Fully local execution
- Ollama support
- llama.cpp support
- GGUF model support
- Configurable provider system

3. Configurable AI Providers
Create:
- IModelProvider abstraction
- Ollama provider
- OpenAI-compatible provider
- llama.cpp provider
- Local provider registry
- Dynamic provider loading

4. Streaming Token Output
Implement true token streaming using:
- IAsyncEnumerable
- CancellationToken
- Console rendering pipeline

5. Repository Aware AI
Implement:
- repository scanner
- semantic file indexing
- dependency graph analysis
- Roslyn-based C# analysis
- symbol resolution
- code context builder

6. Autonomous Coding Agent
Add:
- planner
- executor
- validator
- self-healing retry loop
- compile verification
- code fix loop
- architecture reasoning

7. Bug Fixing Engine
Commands:
- code-cli fix
- code-cli diagnose
- code-cli optimize

Capabilities:
- compile error fixing
- stack trace analysis
- async deadlock detection
- memory leak analysis
- SOLID principle validation
- security review

8. Plugin Architecture
Implement:
- ITool interface
- terminal tool
- git tool
- search tool
- file edit tool
- diagnostics tool
- test runner tool

9. Model Configuration
Implement config file:
~/.code-cli/config.json

Example:
{
  "provider": "ollama",
  "model": "qwen2.5-coder:7b",
  "endpoint": "http://localhost:11434",
  "stream": true,
  "history_size": 20
}

10. macOS Support
Implement:
- osx-arm64 publish
- Apple Silicon support
- executable permissions
- shell installer
- Homebrew installation script

11. Linux Support
Implement:
- AppImage build
- deb/rpm scripts
- shell installer

12. Windows Support
Implement:
- self-contained exe
- winget manifest
- installer script

13. Interactive Terminal UX
Maintain existing ASCII branding.
Enhance terminal UX with:
- live streaming
- status indicators
- runtime detection
- model status
- spinner animations
- syntax highlighting
- command suggestions

14. Semantic Memory
Implement:
- embeddings
- vector memory
- repository memory cache
- context compression

15. Commands To Support
- code-cli chat
- code-cli ask
- code-cli write
- code-cli fix
- code-cli review
- code-cli explain
- code-cli optimize
- code-cli architecture
- code-cli diagnose
- code-cli models
- code-cli provider

16. Production Requirements
- SOLID principles
- async/await everywhere
- dependency injection
- structured logging
- retry policies
- cancellation tokens
- testability
- modular architecture

17. Testing
Add:
- unit tests
- integration tests
- provider tests
- streaming tests
- cross-platform validation

18. CI/CD
Add GitHub Actions:
- Windows build
- Linux build
- macOS build
- release artifacts
- automated testing

19. Performance
Optimize for:
- large repositories
- low memory usage
- streaming latency
- prompt caching
- parallel processing

20. Deliverables
Create:
- complete implementation
- working builds
- updated README
- architecture docs
- installation scripts
- migration docs
- release notes

Important:
- Do NOT remove existing functionality
- Do NOT remove terminal UI
- Do NOT remove ASCII banner
- Do NOT break current commands
- Ensure backward compatibility
- Keep code production-ready
- Follow clean architecture principles
