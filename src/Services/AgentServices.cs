using System.Text;

namespace CodeCli.Services;

public sealed class AutonomousCodingAgent(CodeAssistantService assistant, CodeContextBuilder contextBuilder)
{
    private readonly CodeAssistantService _assistant = assistant;
    private readonly CodeContextBuilder _contextBuilder = contextBuilder;

    public IAsyncEnumerable<string> DiagnoseRepositoryAsync(string rootPath, int maxFiles, CancellationToken ct = default)
    {
        var context = _contextBuilder.BuildRepositoryContext(rootPath, maxFiles);
        var prompt = $"""
            Diagnose the repository using the context below.
            Focus on build risks, architecture issues, probable bugs, missing validation, and maintainability concerns.

            {context}
            """;

        return _assistant.AskWithPromptAsync(Prompts.RepositoryDiagnostician, prompt, ct);
    }

    public IAsyncEnumerable<string> OptimizeRepositoryAsync(string rootPath, int maxFiles, CancellationToken ct = default)
    {
        var context = _contextBuilder.BuildRepositoryContext(rootPath, maxFiles);
        var prompt = $"""
            Analyze this repository and propose concrete optimizations.
            Cover performance, memory, architecture simplification, testability, and CLI UX.

            {context}
            """;

        return _assistant.AskWithPromptAsync(Prompts.Optimizer, prompt, ct);
    }

    public IAsyncEnumerable<string> ExplainArchitectureAsync(string rootPath, int maxFiles, CancellationToken ct = default)
    {
        var context = _contextBuilder.BuildRepositoryContext(rootPath, maxFiles);
        var prompt = $"""
            Explain the architecture of this repository.
            Include major modules, runtime flow, extension points, risks, and recommended next refactors.

            {context}
            """;

        return _assistant.AskWithPromptAsync(Prompts.ArchitectureAdvisor, prompt, ct);
    }

    public async Task<AgentExecutionResult> CreateExecutionPlanAsync(string objective, string rootPath, int maxFiles, CancellationToken ct = default)
    {
        var context = _contextBuilder.BuildRepositoryContext(rootPath, maxFiles);
        var plan = await _assistant.CompleteWithPromptAsync(
            Prompts.Planner,
            $"""
            Create a step-by-step execution plan for this goal:
            {objective}

            Repository context:
            {context}
            """,
            ct);

        return new AgentExecutionResult(objective, plan);
    }
}

public sealed record AgentExecutionResult(string Objective, string Plan);
