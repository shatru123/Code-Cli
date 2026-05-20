namespace CodeCli.Providers;

public interface IModelProvider
{
    Task<string> GenerateAsync(string prompt);

    IAsyncEnumerable<string> StreamAsync(string prompt);
}
