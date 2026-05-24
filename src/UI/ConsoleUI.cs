using System.Text;

namespace CodeCli.UI;

public static class ConsoleUI
{
    private const string Reset   = "\x1b[0m";
    private const string Bold    = "\x1b[1m";
    private const string Cyan    = "\x1b[96m";
    private const string Green   = "\x1b[92m";
    private const string Yellow  = "\x1b[93m";
    private const string Red     = "\x1b[91m";
    private const string Magenta = "\x1b[95m";
    private const string White   = "\x1b[97m";
    private const string Gray    = "\x1b[90m";

    static ConsoleUI()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var handle = GetStdHandle(-11);
                GetConsoleMode(handle, out uint mode);
                SetConsoleMode(handle, mode | 0x0004);
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch { }
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern nint GetStdHandle(int n);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern bool GetConsoleMode(nint h, out uint m);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern bool SetConsoleMode(nint h, uint m);

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
        Console.WriteLine($"  {Gray}Local AI Coding Assistant  •  Claude + Ollama  •  No subscription{Reset}");
        Console.WriteLine();
    }

    public static void PrintSmallBanner()
    {
        Console.WriteLine($"{Bold}{Cyan}Code-Cli{Reset} {Gray}v2.0 — AI Coding Assistant (Claude + Ollama){Reset}");
        Console.WriteLine();
    }

    // ── Status ────────────────────────────────────────────────────────────────

    public static void Info(string message)    => Console.WriteLine($"{Cyan}ℹ  {Reset}{message}");
    public static void Success(string message) => Console.WriteLine($"{Green}✔  {Reset}{message}");
    public static void Warning(string message) => Console.WriteLine($"{Yellow}⚠  {Reset}{message}");
    public static void Error(string message)   => Console.WriteLine($"{Red}✖  {Reset}{message}");

    public static void AssistantPrefix() =>
        Console.Write($"{Bold}{Green}Code-Cli{Reset} {Gray}▶{Reset} ");

    public static void UserPrefix() =>
        Console.Write($"{Bold}{Magenta}You{Reset}     {Gray}▶{Reset} ");

    public static void SectionHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"{Bold}{Yellow}  {title}{Reset}");
        Console.WriteLine($"{Gray}  {new string('─', Math.Max(10, Math.Min(Console.WindowWidth - 4, 70)))}{Reset}");
        Console.WriteLine();
    }

    public static void Separator()
    {
        Console.WriteLine($"\n{Gray}{new string('─', Math.Max(10, Math.Min(Console.WindowWidth - 2, 72)))}{Reset}\n");
    }

    // ── Spinner ───────────────────────────────────────────────────────────────

    public static async Task<T> WithSpinnerAsync<T>(
        string message,
        Func<Task<T>> action,
        CancellationToken ct = default)
    {
        var frames = new[] { "⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷" };
        var idx    = 0;
        var spinCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var spinnerTask = Task.Run(async () =>
        {
            Console.CursorVisible = false;
            while (!spinCts.Token.IsCancellationRequested)
            {
                Console.Write($"\r{Cyan}{frames[idx++ % frames.Length]}{Reset}  {Gray}{message}...{Reset}  ");
                await Task.Delay(80, spinCts.Token).ContinueWith(_ => { });
            }
            Console.Write($"\r{new string(' ', message.Length + 20)}\r");
            Console.CursorVisible = true;
        }, spinCts.Token);

        T result = await action();
        await spinCts.CancelAsync();
        try { await spinnerTask; } catch { }
        return result;
    }

    // ── Streaming ─────────────────────────────────────────────────────────────

    private static bool _inCodeBlock;

    public static void StreamToken(string token)
    {
        if (token.Contains("```")) _inCodeBlock = !_inCodeBlock;
        Console.Write(_inCodeBlock ? $"{White}{token}{Reset}" : token);
    }

    public static void ResetStreamState() => _inCodeBlock = false;

    // ── Tables ────────────────────────────────────────────────────────────────

    public static void PrintModelTable(IReadOnlyList<string> models, string? activeModel = null)
    {
        Console.WriteLine($"  {Bold}{"Model Name",-45} {"Status"}{Reset}");
        Console.WriteLine($"  {Gray}{new string('─', 60)}{Reset}");
        foreach (var m in models)
        {
            var active = m == activeModel ? $" {Yellow}[active]{Reset}" : "";
            var rec    = (m.Contains("qwen2.5-coder") || m.Contains("deepseek-coder") || m.Contains("sonnet"))
                       ? $" {Green}✅{Reset}" : "";
            Console.WriteLine($"  {Cyan}{m,-45}{Reset} {Green}available{Reset}{rec}{active}");
        }
        Console.WriteLine();
    }

    // ── Help ──────────────────────────────────────────────────────────────────

    public static void PrintHelp()
    {
        PrintSmallBanner();

        Console.WriteLine($"  {Bold}USAGE{Reset}");
        Console.WriteLine($"    {Cyan}code-cli{Reset} {Yellow}<command>{Reset} [options]");
        Console.WriteLine();

        Console.WriteLine($"  {Bold}COMMANDS{Reset}");
        var commands = new (string cmd, string args, string desc)[]
        {
            ("chat",          "",                                 "Interactive coding session"),
            ("ask",           "<question>",                       "One-off coding question"),
            ("write",         "<description>",                    "Generate production-ready code"),
            ("fix",           "<file> [--error <msg>]",           "Detect and fix all bugs"),
            ("review",        "<file>",                           "Full production-readiness audit"),
            ("explain",       "<file>",                           "Detailed code walkthrough"),
            ("refactor",      "<file> [--goal <goal>]",           "Refactor toward a goal"),
            ("test",          "<file> [--framework <fw>]",        "Generate unit tests"),
            ("analyse",       "[path] [--focus <pattern>]",       "Analyse file or whole project"),
            ("diagnose",      "[path]",                           "Diagnose repository issues"),
            ("optimize",      "[path]",                           "Suggest optimisations"),
            ("architecture",  "",                                 "Explain project architecture"),
            ("models",        "",                                 "List available AI models"),
            ("provider",      "",                                 "Show active provider status"),
            ("config",        "",                                 "Show / update configuration"),
        };
        foreach (var (cmd, a, desc) in commands)
            Console.WriteLine($"    {Green}{cmd,-14}{Reset} {Yellow}{a,-38}{Reset} {desc}");

        Console.WriteLine();
        Console.WriteLine($"  {Bold}OPTIONS{Reset}");
        Console.WriteLine($"    {Yellow}--provider <p>{Reset}     AI provider: claude | ollama | openai-compatible | llama.cpp");
        Console.WriteLine($"    {Yellow}--model    <n>{Reset}     Override the AI model");
        Console.WriteLine($"    {Yellow}--output   <f>{Reset}     Save response to file");
        Console.WriteLine($"    {Yellow}--goal     <g>{Reset}     Refactoring goal  (for refactor)");
        Console.WriteLine($"    {Yellow}--framework<f>{Reset}     Test framework    (for test)");
        Console.WriteLine($"    {Yellow}--focus    <p>{Reset}     File pattern      (for analyse)");
        Console.WriteLine($"    {Yellow}--error    <m>{Reset}     Error context     (for fix)");
        Console.WriteLine($"    {Yellow}--host     <u>{Reset}     Ollama host URL");
        Console.WriteLine($"    {Yellow}--runtime  <r>{Reset}     Ollama runtime: local | docker");
        Console.WriteLine($"    {Yellow}--verbose{Reset}          Show connection details");
        Console.WriteLine();

        Console.WriteLine($"  {Bold}QUICK SETUP — CLAUDE{Reset}");
        Console.WriteLine($"    {Gray}code-cli config --set-key sk-ant-...{Reset}");
        Console.WriteLine($"    {Gray}code-cli chat{Reset}");
        Console.WriteLine();

        Console.WriteLine($"  {Bold}QUICK SETUP — OLLAMA{Reset}");
        Console.WriteLine($"    {Gray}ollama pull qwen2.5-coder:7b{Reset}");
        Console.WriteLine($"    {Gray}code-cli chat --provider ollama{Reset}");
        Console.WriteLine();

        Console.WriteLine($"  {Bold}EXAMPLES{Reset}");
        Console.WriteLine($"    {Gray}code-cli ask How do I implement rate limiting in ASP.NET Core{Reset}");
        Console.WriteLine($"    {Gray}code-cli write \"Generic repository with EF Core + Unit of Work\"{Reset}");
        Console.WriteLine($"    {Gray}code-cli fix PaymentService.cs --error \"NullReferenceException line 42\"{Reset}");
        Console.WriteLine($"    {Gray}code-cli refactor OrderService.cs --goal \"extract CQRS handlers\"{Reset}");
        Console.WriteLine($"    {Gray}code-cli test Services/UserService.cs --framework xunit{Reset}");
        Console.WriteLine($"    {Gray}code-cli analyse . --focus Controllers --output report.md{Reset}");
        Console.WriteLine($"    {Gray}code-cli diagnose{Reset}");
        Console.WriteLine();
    }
}
