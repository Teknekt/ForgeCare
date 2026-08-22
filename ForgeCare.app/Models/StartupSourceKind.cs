namespace ForgeCare.App.Models;

public enum StartupSourceKind
{
    CurrentUserRegistry,
    LocalMachineRegistry,
    UserStartupFolder,
    CommonStartupFolder,
    Unknown
}
