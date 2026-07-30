using System.Net;
using System.Net.Sockets;
using ImageMagick;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

internal sealed class TestHttpFixtureServer : IAsyncDisposable
{
    private readonly HttpListener listener;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task loop;
    private readonly string fixtureHtml;

    private TestHttpFixtureServer(HttpListener listener, string fixtureHtml)
    {
        this.listener = listener;
        this.fixtureHtml = fixtureHtml;
        loop = Task.Run(ListenAsync);
    }

    public string BaseUrl { get; private init; } = "";

    public static Task<TestHttpFixtureServer> StartAsync(string fixturePath)
    {
        var port = GetFreePort();
        var listener = new HttpListener();
        var baseUrl = $"http://127.0.0.1:{port}/";
        listener.Prefixes.Add(baseUrl);
        listener.Start();
        var server = new TestHttpFixtureServer(listener, File.ReadAllText(fixturePath))
        {
            BaseUrl = baseUrl
        };
        return Task.FromResult(server);
    }

    public async ValueTask DisposeAsync()
    {
        cancellation.Cancel();
        listener.Stop();
        listener.Close();
        try
        {
            await loop;
        }
        catch (ObjectDisposedException)
        {
        }
        catch (HttpListenerException)
        {
        }
    }

    private async Task ListenAsync()
    {
        while (!cancellation.IsCancellationRequested)
        {
            var context = await listener.GetContextAsync();
            _ = Task.Run(() => RespondAsync(context), cancellation.Token);
        }
    }

    private async Task RespondAsync(HttpListenerContext context)
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";
            if (path is "/" or "/index.html")
            {
                await WriteAsync(context, "text/html; charset=utf-8", System.Text.Encoding.UTF8.GetBytes(fixtureHtml));
                return;
            }

            if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                await WriteAsync(context, "image/svg+xml", System.Text.Encoding.UTF8.GetBytes("""<svg xmlns="http://www.w3.org/2000/svg" width="320" height="180"><rect width="320" height="180" fill="#315c48"/></svg>"""));
                return;
            }

            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                await WriteAsync(context, "image/png", CreateImage(MagickFormat.Png, "#dbeafe"));
                return;
            }

            if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                await WriteAsync(context, "image/jpeg", CreateImage(MagickFormat.Jpeg, "#f3d19c"));
                return;
            }

            if (path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                await WriteAsync(context, "video/mp4", [0, 0, 0, 24, 102, 116, 121, 112]);
                return;
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }
        catch
        {
            if (context.Response.OutputStream.CanWrite)
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
        }
    }

    private static async Task WriteAsync(HttpListenerContext context, string contentType, byte[] bytes)
    {
        context.Response.ContentType = contentType;
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    private static byte[] CreateImage(MagickFormat format, string color)
    {
        using var image = new MagickImage(new MagickColor(color), 320, 180);
        image.Format = format;
        return image.ToByteArray();
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
