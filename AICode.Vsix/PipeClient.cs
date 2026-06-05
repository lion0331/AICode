using System.IO.Pipes;
using System.Text;

namespace AICode.Services;

public class PipeClient
{
    private readonly string _pipeName = "AICode_Pipe";

    public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken cancellationToken)
    {
        using var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipeClient.ConnectAsync(5000, cancellationToken);

        var request = new
        {
            type = "generate_stream",
            prompt = prompt
        };

        var requestJson = System.Text.Json.JsonSerializer.Serialize(request);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        await pipeClient.WriteAsync(requestBytes, cancellationToken);
        await pipeClient.FlushAsync(cancellationToken);

        var buffer = new byte[4096];
        var sb = new StringBuilder();

        while (true)
        {
            int bytesRead = await pipeClient.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0) break;

            var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            sb.Append(chunk);

            if (sb.ToString().EndsWith("[END]"))
            {
                var result = sb.ToString().Substring(0, sb.Length - 5);
                if (!string.IsNullOrEmpty(result))
                {
                    yield return result;
                }
                break;
            }

            yield return chunk;
        }
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        var result = string.Empty;
        await foreach (var chunk in GenerateStreamAsync(prompt, cancellationToken))
        {
            result += chunk;
        }
        return result;
    }
}