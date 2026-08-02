using System.Collections.Concurrent;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

internal static class Phase3TempPathRegistry
{
    private static readonly ConcurrentBag<string> Paths = new();
    private static int Hooked;

    public static void Register(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Paths.Add(path);
        EnsureHooked();
    }

    private static void EnsureHooked()
    {
        if (Interlocked.Exchange(ref Hooked, 1) == 0)
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Cleanup();
        }
    }

    private static void Cleanup()
    {
        while (Paths.TryTake(out var path))
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
