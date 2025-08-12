using Avalonia;
using System;

namespace AvaloniaChat;

internal abstract class Program
{
    public const int TimeoutSeconds = 30;

   
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);


    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
