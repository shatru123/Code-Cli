using System.Text;

namespace CodeCli.UI;

public static class ConsoleUI
{
    // ── ANSI Color codes ──────────────────────────────────────────────────────

    private const string Reset   = "\x1b[0m";
    private const string Bold    = "\x1b[1m";
    private const string Dim     = "\x1b[2m";

    private const string Cyan    = "\x1b[96m";
    private const string Green   = "\x1b[92m";
    private const string Yellow  = "\x1b[93m";
    private const string Red     = "\x1b[91m";
    private const string Blue    = "\x1b[94m";
    private const string Magenta = "\x1b[95m";
    private const string White   = "\x1b[97m";
    private const string Gray    = "\x1b[90m";

    static ConsoleUI()
    {
        // Enable ANSI on Windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var handle = GetStdHandle(-11);
                GetConsoleMode(handle, out uint mode);
                SetConsoleMode(handle, mode | 0x0004);
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch { /* fallback gracefully */ }
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int nStdHandle);
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(nint handle, out uint mode);
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(nint handle, uint mode);

    // ── Banner ────────────────────────────────────────────────────────────────

    public static void PrintBanner()
    {
        Console.WriteLine();
        Console.WriteLine($"{Bold}{Cyan}  ██████╗ ██████╗ ██████╗ ███████╗███╗   ███╗ █████╗ ████████╗███████╗{Reset}");
        Console.WriteLine($"{Bold}{Cyan} ██╔════╝██╔═══██╗██╔══██╗██╔════╝████╗ ████║██╔══██╗╚══██╔══╝██╔════╝{Reset}");
        Console.WriteLine($"{Bold}{Cyan} ██║     ██║   ██║██║  ██║█████╗  ██╔████╔██║███████║   ██║   █████╗  {Reset}");
        Console.WriteLine($"{Bold}{Cyan} ██║     ██║   ██║██║  ██║██╔══╝  ██║╚██╔╝██║██╔══██║   ██║   ██╔══╝  {Reset}");
        Console.WriteLine($"{Bold}{Cyan} ╚██████╗╚██████╔╝██████╔╝███████╗██║ ╚═╝ ██║██║  ██║   ██║   ███████╗{Reset}");
        Console.WriteLine($"{Bold}{Cyan}  ╚═════╝ ╚═════╝ ╚═════╝ ╚══════╝╚═╝     ╚═╝╚═╝  ╚═╝   ╚═╝   ╚══════╝{Reset}");
        Console.WriteLine();
        Console.WriteLine($"  {Gray}Local AI Coding Assistant  •  No API Key Required  •  100% Offline{Reset}");
        Console.WriteLine();
    }

    public static void PrintSmallBanner()
    {
        Console.WriteLine($"{Bold}{Cyan}Code-Cli{Reset} {Gray}— Local AI Coding Assistant{Reset}");
        Console.WriteLine();
    }

    // ── Status messages ───────────────────────────────────────────────────────

    public static void Info(string message)    => Console.WriteLine($"{Cyan}ℹ  {Reset}{message}");
    public static void Success(string message) => Console.WriteLine($"{Green}✔  {Reset}{message}");
    public static void Warning(string message) => Console.WriteLine($"{Yellow}⚠  {Reset}{message}");
    public static void Error(string message)   => Console.WriteLine($"{Red}✖  {Reset}{message}");

    public static void Prompt(string label) =>
        Console.Write($"\n{Bold}{Blue}{label}{Reset} ");

    public static void AssistantPrefix() =>
        Console.Write($"{Bold}{Green}Code-Cli{Reset} {Gray}▶{Reset} ");

    public static void UserPrefix() =>
        Console.Write($"{Bold}{Magenta}You{Reset}     {Gray}▶{Reset} ");

    public static void SectionHeader(string title)
    {
        Console.WriteLine();
        var line = new string('─', Math.Min(Console.WindowWidth - 4, 70));
        Console.WriteLine($"{Bold}{Yellow}  {title}{Reset}");
        Console.WriteLine($"{Gray}  {line}{Reset}");
        Console.WriteLine();
    }

    public static void Separator()
    {
        var width = Math.Min(Console.WindowWidth - 2, 72);
        Console.WriteLine($"\n{Gray}{new string('─', width)}{Reset}\n");
    }

    // ── Thinking spinner ──────────────────────────────────────────────────────

    public static async Task<T> WithSpinnerAsync<T>(
        string message,
        Func<Task<T>> action,
        CancellationToken ct = default)
    {
        var frames = new[] { "⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷" };
        var idx = 0;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var spinnerTask = Task.Run(async () =>
        {
            Console.CursorVisible = false;
            while (!cts.Token.IsCancellationRequested)
            {
                Console.Write($"\r{Cyan}{frames[idx++ % frames.Length]}{Reset}  {Gray}{message}...{Reset}  ");
                await Task.Delay(80, cts.Token).ContinueWith(_ => { });
            }
            Console.Write($"\r{new string(' ', message.Length + 20)}\r");
            Console.CursorVisible = true;
        }, cts.Token);

        T result = await action();
        await cts.CancelAsync();
        try { await spinnerTask; } catch { }
        return result;
    }

    // ── Stream output with syntax color hints ─────────────────────────────────

    private static bool _inCodeBlock = false;

    public static void StreamToken(string token)
    {
        // Detect code block boundaries
        if (token.Contains("```"))
            _inCodeBlock = !_inCodeBlock;

        if (_inCodeBlock)
            Console.Write($"{White}{token}{Reset}");
        else
            Console.Write(token);
    }

    public static void ResetStreamState() => _inCodeBlock = false;

    // ── Table helpers ─────────────────────────────────────────────────────────

    public static void PrintModelTable(List<string> models)
    {
        Console.WriteLine($"  {Bold}{"Model Name",-40} {"Status",-12}{Reset}");
        Console.WriteLine($"  {Gray}{new string('─', 54)}{Reset}");
        foreach (var m in models)
        {
            var isDefault = m.Contains("qwen2.5-coder") || m.Contains("codellama") || m.Contains("deepseek-coder");
            var tag = isDefault ? $"{Green}[recommended]{Reset}" : "";
            Console.WriteLine($"  {Cyan}{m,-40}{Reset} {Green}installed{Reset} {tag}");
        }
        Console.WriteLine();
    }

    public static void PrintHelp()
    {
        PrintSmallBanner();
        Console.WriteLine($"  {Bold}USAGE{Reset}");
        Console.WriteLine($"    {Cyan}code-cli{Reset} {Yellow}<command>{Reset} [options]");
        Console.WriteLine();
        Console.WriteLine($"  {Bold}COMMANDS{Reset}");

        var commands = new (string cmd, string args, string desc)[]
        {
            ("chat",    "",                         "Start an interactive coding chat session"),
            ("ask",     "<question>",               "Ask a single coding question"),
            ("write",   "<description>",            "Generate production-ready code"),
            ("fix",     "<file> [--error <msg>]",   "Fix bugs in a source file"),
            ("review",  "<file>",                   "Full production-readiness code review"),
            ("explain", "<file>",                   "Get a detailed explanation of code"),
            ("models",  "",                         "List installed Ollama models"),
            ("config",  "",                         "Show / edit current configuration"),
        };

        foreach (var (cmd, args, desc) in commands)
        {
            Console.WriteLine($"    {Green}{cmd,-10}{Reset} {Yellow}{args,-35}{Reset} {desc}");
        }

        Console.WriteLine();
        Console.WriteLine($"  {Bold}OPTIONS{Reset}");
        Console.WriteLine($"    {Yellow}--model   <name>{Reset}     Override the AI model (default: qwen2.5-coder:7b)");
        Console.WriteLine($"    {Yellow}--host    <url>{Reset}      Ollama host (default: http://localhost:11434)");
        Console.WriteLine($"    {Yellow}--runtime <type>{Reset}     Ollama runtime: local or docker");
        Console.WriteLine($"    {Yellow}--output  <file>{Reset}     Save response to file");
        Console.WriteLine($"    {Yellow}--no-stream{Reset}          Wait for full response before printing");
        Console.WriteLine();
        Console.WriteLine($"  {Bold}EXAMPLES{Reset}");
        Console.WriteLine($"    {Gray}code-cli chat{Reset}");
        Console.WriteLine($"    {Gray}code-cli ask \"How do I implement a generic repository in C#?\"{Reset}");
        Console.WriteLine($"    {Gray}code-cli write \"REST API with JWT auth in ASP.NET Core\"{Reset}");
        Console.WriteLine($"    {Gray}code-cli fix MyService.cs --error \"NullReferenceException at line 42\"{Reset}");
        Console.WriteLine($"    {Gray}code-cli review Controllers/AuthController.cs{Reset}");
        Console.WriteLine($"    {Gray}code-cli explain Program.cs{Reset}");
        Console.WriteLine($"    {Gray}code-cli models{Reset}");
        Console.WriteLine($"    {Gray}code-cli ask \"Explain LINQ joins\" --runtime docker{Reset}");
        Console.WriteLine();
        Console.WriteLine($"  {Bold}SETUP{Reset}");
        Console.WriteLine($"    1. Local: install Ollama from {Cyan}https://ollama.ai{Reset}");
        Console.WriteLine($"    2. Docker: install Docker Desktop / Docker Engine");
        Console.WriteLine($"    3. Pull a model:     {Cyan}ollama pull qwen2.5-coder:7b{Reset} {Gray}(local){Reset}");
        Console.WriteLine($"    4. Or use Docker:    {Cyan}code-cli chat --runtime docker{Reset}");
        Console.WriteLine();
    }
}
