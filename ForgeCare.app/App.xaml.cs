using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ForgeCare.App.Services;

namespace ForgeCare.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        Startup +=
            OnStartup;

        Exit +=
            OnExit;
    }

    private static void OnStartup(
        object sender,
        StartupEventArgs e)
    {
        AppLifecycleRecoveryService.BeginSession();
    }

    private static void OnExit(
        object sender,
        ExitEventArgs e)
    {
        AppLifecycleRecoveryService.MarkCleanShutdown();
    }

    private static void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        CrashLogService.Record(e.Exception, "WPF DispatcherUnhandledException");
        e.Handled = false;
    }

    private static void OnDomainUnhandledException(
        object? sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            CrashLogService.Record(ex, "AppDomain.UnhandledException");
    }

    private static void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        CrashLogService.Record(e.Exception, "TaskScheduler.UnobservedTaskException");
    }
}
