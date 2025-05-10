using Avalonia;
using System;
using System.Threading.Tasks;
using CodeIngest.Desktop.Views;
using DTC.Core;

namespace CodeIngest.Desktop;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            Logger.Instance.SysInfo();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Logger.Instance.Info("Application ended cleanly.");
        }
        catch (Exception ex)
        {
            HandleFatalException(ex);
        }
        finally
        {
            Settings.Instance.Save();
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            HandleFatalException(ex);
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        Logger.Instance.Exception("Unobserved task exception.", e.Exception);
    }

    private static void HandleFatalException(Exception ex) =>
        Logger.Instance.Exception("A fatal error occurred.", ex);

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}