using System.Diagnostics;
using System.Net.Http.Json;
using CodeCli.Models;
using CodeCli.UI;

namespace CodeCli.Services;

/// <summary>
/// Manages the Ollama runtime lifecycle for both local and Docker modes.
///
/// Full zero-friction flow when runtime=docker:
///   1. Verify Docker CLI + daemon are available
///   2. Create the container if it doesn't exist (docker run)
///   3. Start the container if it's stopped (docker start)
///   4. Wait for the Ollama HTTP API to become ready
///   5. Check whether the configured model is already pulled
///   6. If not → stream docker exec ollama pull with live progress
///
/// The user never needs to manually run docker exec or ollama pull.
/// </summary>
public sealed class OllamaRuntimeManager(AppConfig config)
{
    private readonly AppConfig _config = config;

    public bool UsesDocker =>
        _config.Runtime.Equals("docker", StringComparison.OrdinalIgnoreCase);

    // ── Main entry point ──────────────────────────────────────────────────────

    public async Task<RuntimePreparationResult> PrepareAsync(CancellationToken ct = default)
    {
        if (!UsesDocker)
            return RuntimePreparationResult.Ok("Using local Ollama runtime.");

        if (!_config.DockerAutoStart)
            return RuntimePreparationResult.Ok("Docker auto-start is disabled.");

        // 1. Docker CLI present?
        var dockerCheck = await RunDockerAsync(ct, "--version");
        if (!dockerCheck.Success)
            return RuntimePreparationResult.Failure(
                "Docker CLI not found. Install Docker Desktop or Docker Engine.");

        // 2. Docker daemon running?
        var daemonCheck = await RunDockerAsync(ct, "info");
        if (!daemonCheck.Success)
            return RuntimePreparationResult.Failure(
                "Docker daemon is not running. Start Docker Desktop and try again.");

        // 3. Create container if it doesn't exist
        var exists = await RunDockerAsync(ct, "container", "inspect", _config.DockerContainerName);
        if (!exists.Success)
        {
            ConsoleUI.Info($"Creating Ollama container '{_config.DockerContainerName}'…");

            // docker run will pull the image automatically if not present locally
            var createResult = await RunDockerAsync(
                ct,
                "run", "-d",
                "--name",   _config.DockerContainerName,
                "-p",       $"{ResolveHostPort(_config.Endpoint)}:11434",
                "-v",       $"{_config.DockerVolume}:/root/.ollama",
                "--restart", "unless-stopped",
                _config.DockerImage);

            if (!createResult.Success)
                return RuntimePreparationResult.Failure(
                    $"Could not create Ollama container: {createResult.Error}");

            ConsoleUI.Success($"Container created from image '{_config.DockerImage}'.");
        }
        else
        {
            // 4. Start container if stopped
            var running = await RunDockerAsync(
                ct, "inspect", "-f", "{{.State.Running}}", _config.DockerContainerName);

            if (!running.Output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleUI.Info($"Starting container '{_config.DockerContainerName}'…");
                var startResult = await RunDockerAsync(ct, "start", _config.DockerContainerName);
                if (!startResult.Success)
                    return RuntimePreparationResult.Failure(
                        $"Could not start container: {startResult.Error}");
            }
        }

        // 5. Wait for Ollama API to be ready (up to 60s — image may need to initialise)
        ConsoleUI.Info("Waiting for Ollama to be ready…");
        var ready = await WaitForOllamaAsync(ct, timeoutSeconds: 60);
        if (!ready)
            return RuntimePreparationResult.Failure(
                $"Ollama did not become ready at {_config.Endpoint} within 60 seconds.");

        ConsoleUI.Success("Ollama is ready.");

        // 6. Ensure the configured model is pulled inside the container
        var modelReady = await EnsureModelPulledAsync(_config.Model, ct);
        if (!modelReady)
            return RuntimePreparationResult.Failure(
                $"Failed to pull model '{_config.Model}' inside the container.");

        return RuntimePreparationResult.Ok(
            $"Container '{_config.DockerContainerName}' running. Model '{_config.Model}' ready.");
    }

    // ── Model pull ────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks if <paramref name="model"/> is already pulled inside the container.
    /// If not, streams <c>docker exec … ollama pull</c> with live progress output.
    /// Returns true when the model is ready to use.
    /// </summary>
    private async Task<bool> EnsureModelPulledAsync(string model, CancellationToken ct)
    {
        // Ask Ollama which models are already present
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var resp = await http.GetFromJsonAsync<OllamaModelsResponse>(
                $"{_config.Endpoint.TrimEnd('/')}/api/tags", ct);

            if (resp?.Models.Any(m =>
                    m.Name.Equals(model, StringComparison.OrdinalIgnoreCase) ||
                    m.Name.StartsWith(model.Split(':')[0], StringComparison.OrdinalIgnoreCase)) == true)
            {
                ConsoleUI.Success($"Model '{model}' already available.");
                return true;
            }
        }
        catch
        {
            // Ollama just became ready — proceed to pull anyway
        }

        // Stream the pull with live progress
        ConsoleUI.Info($"Pulling model '{model}' inside container (this may take a few minutes)…");
        Console.WriteLine();

        return await StreamDockerExecAsync(
            ct,
            "exec", _config.DockerContainerName, "ollama", "pull", model);
    }

    // ── Startup help ──────────────────────────────────────────────────────────

    public IEnumerable<string> GetStartupHelp()
    {
        if (!UsesDocker)
        {
            return
            [
                "1. Install Ollama from https://ollama.ai",
                "2. Run: ollama serve",
                $"3. Pull the model: ollama pull {_config.Model}"
            ];
        }

        return
        [
            "1. Install Docker Desktop or Docker Engine",
            "2. Start Docker Desktop",
            $"3. Re-run code-cli — it will create the container and pull '{_config.Model}' automatically"
        ];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int ResolveHostPort(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.Port > 0)
            return uri.Port;
        return 11434;
    }

    private async Task<bool> WaitForOllamaAsync(CancellationToken ct, int timeoutSeconds = 60)
    {
        using var http     = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        var       deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            try
            {
                var resp = await http.GetFromJsonAsync<OllamaModelsResponse>(
                    $"{_config.Endpoint.TrimEnd('/')}/api/tags", ct);
                if (resp is not null) return true;
            }
            catch { }

            await Task.Delay(1_000, ct);
        }

        return false;
    }

    /// <summary>
    /// Runs a docker command and returns captured stdout/stderr + exit code.
    /// Used for commands where we want the full output (inspect, start, run, etc.).
    /// </summary>
    private static async Task<CommandResult> RunDockerAsync(
        CancellationToken ct, params string[] args)
    {
        try
        {
            var si = new ProcessStartInfo
            {
                FileName               = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            foreach (var a in args) si.ArgumentList.Add(a);

            using var proc = new Process { StartInfo = si };
            proc.Start();

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            return new CommandResult(
                proc.ExitCode == 0,
                (await stdoutTask).Trim(),
                (await stderrTask).Trim());
        }
        catch (Exception ex)
        {
            return new CommandResult(false, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// Runs a docker command and streams stdout line-by-line to the console in real time.
    /// Used for <c>ollama pull</c> so the user sees live download progress.
    /// Returns true if the process exits with code 0.
    /// </summary>
    private static async Task<bool> StreamDockerExecAsync(
        CancellationToken ct, params string[] args)
    {
        try
        {
            var si = new ProcessStartInfo
            {
                FileName               = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            foreach (var a in args) si.ArgumentList.Add(a);

            using var proc = new Process { StartInfo = si };

            // Write stdout live — each line is a progress update from ollama pull
            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                // Overwrite the current line for progress lines (pulling, verifying…)
                if (e.Data.TrimStart().StartsWith("pulling") ||
                    e.Data.TrimStart().StartsWith("verifying") ||
                    e.Data.TrimStart().StartsWith("writing"))
                {
                    Console.Write($"\r  \x1b[96m{e.Data.Trim(),-70}\x1b[0m");
                }
                else if (e.Data.TrimStart().StartsWith("success") ||
                         e.Data.Contains("success"))
                {
                    Console.WriteLine();
                    ConsoleUI.Success(e.Data.Trim());
                }
                else if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Console.WriteLine($"  {e.Data}");
                }
            };

            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    ConsoleUI.Warning(e.Data.Trim());
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await proc.WaitForExitAsync(ct);
            Console.WriteLine();

            return proc.ExitCode == 0;
        }
        catch (Exception ex)
        {
            ConsoleUI.Error($"docker exec failed: {ex.Message}");
            return false;
        }
    }
}

public sealed record RuntimePreparationResult(bool Success, string Message)
{
    public static RuntimePreparationResult Failure(string msg) => new(false, msg);
    public static RuntimePreparationResult Ok(string msg)      => new(true,  msg);
}

internal sealed record CommandResult(bool Success, string Output, string Error);
