using System.Diagnostics;
using System.Net.Http.Json;
using CodeCli.Models;

namespace CodeCli.Services;

public sealed class OllamaRuntimeManager(AppConfig config)
{
    private readonly AppConfig _config = config;

    public bool UsesDocker => _config.Runtime.Equals("docker", StringComparison.OrdinalIgnoreCase);

    public async Task<RuntimePreparationResult> PrepareAsync(CancellationToken ct = default)
    {
        if (!UsesDocker)
            return RuntimePreparationResult.Ok("Using local Ollama runtime.");

        if (!_config.DockerAutoStart)
            return RuntimePreparationResult.Ok("Docker runtime selected. Auto-start is disabled.");

        var dockerCheck = await RunDockerAsync(ct, "--version");
        if (!dockerCheck.Success)
        {
            return RuntimePreparationResult.Failure(
                "Docker CLI was not found. Install Docker Desktop or Docker Engine, or switch runtime back to local.");
        }

        var daemonCheck = await RunDockerAsync(ct, "info");
        if (!daemonCheck.Success)
        {
            return RuntimePreparationResult.Failure(
                "Docker is installed but the daemon is not running. Start Docker and try again.");
        }

        var exists = await RunDockerAsync(ct, "container", "inspect", _config.DockerContainerName);
        if (!exists.Success)
        {
            var createResult = await RunDockerAsync(
                ct,
                "run", "-d",
                "--name", _config.DockerContainerName,
                "-p", $"{ResolveHostPort(_config.Endpoint)}:11434",
                "-v", $"{_config.DockerVolume}:/root/.ollama",
                _config.DockerImage);

            if (!createResult.Success)
                return RuntimePreparationResult.Failure($"Unable to create the Ollama Docker container. {createResult.Error}");

            return await WaitForOllamaAsync($"Started Docker container '{_config.DockerContainerName}'.", ct);
        }

        var running = await RunDockerAsync(
            ct,
            "inspect", "-f", "{{.State.Running}}", _config.DockerContainerName);

        if (!running.Success)
            return RuntimePreparationResult.Failure($"Unable to inspect the Ollama Docker container. {running.Error}");

        if (!running.Output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            var startResult = await RunDockerAsync(ct, "start", _config.DockerContainerName);
            if (!startResult.Success)
                return RuntimePreparationResult.Failure($"Unable to start the Ollama Docker container. {startResult.Error}");

            return await WaitForOllamaAsync($"Started Docker container '{_config.DockerContainerName}'.", ct);
        }

        return await WaitForOllamaAsync($"Docker container '{_config.DockerContainerName}' is already running.", ct);
    }

    public IEnumerable<string> GetStartupHelp()
    {
        if (!UsesDocker)
        {
            return
            [
                "1. Install Ollama from https://ollama.ai",
                "2. Run: ollama serve",
                "3. Pull a model: ollama pull qwen2.5-coder:7b"
            ];
        }

        return
        [
            "1. Install Docker Desktop or Docker Engine",
            $"2. Start the container: docker run -d --name {_config.DockerContainerName} -p {ResolveHostPort(_config.Endpoint)}:11434 -v {_config.DockerVolume}:/root/.ollama {_config.DockerImage}",
            $"3. Pull a model inside the container: docker exec -it {_config.DockerContainerName} ollama pull qwen2.5-coder:7b"
        ];
    }

    private static int ResolveHostPort(string host)
    {
        if (Uri.TryCreate(host, UriKind.Absolute, out var uri) && uri.Port > 0)
            return uri.Port;

        return 11434;
    }

    private async Task<RuntimePreparationResult> WaitForOllamaAsync(string readyMessage, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            try
            {
                var response = await http.GetFromJsonAsync<OllamaModelsResponse>($"{_config.Endpoint.TrimEnd('/')}/api/tags", ct);
                if (response is not null)
                    return RuntimePreparationResult.Ok(readyMessage);
            }
            catch
            {
            }

            await Task.Delay(1000, ct);
        }

        return RuntimePreparationResult.Failure(
            $"The Docker container started, but Ollama did not become ready at {_config.Endpoint} within 30 seconds.");
    }

    private static async Task<CommandResult> RunDockerAsync(CancellationToken ct, params string[] args)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
                startInfo.ArgumentList.Add(arg);

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            return new CommandResult(process.ExitCode == 0, stdout.Trim(), stderr.Trim());
        }
        catch (Exception ex)
        {
            return new CommandResult(false, string.Empty, ex.Message);
        }
    }
}

public sealed record RuntimePreparationResult(bool Success, string Message)
{
    public static RuntimePreparationResult Failure(string message) => new(false, message);
    public static RuntimePreparationResult Ok(string message) => new(true, message);
}

internal sealed record CommandResult(bool Success, string Output, string Error);
