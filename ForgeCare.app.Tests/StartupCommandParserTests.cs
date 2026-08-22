using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupCommandParserTests
{
    private readonly StartupCommandParser _parser = new(value =>
        value.Replace("%TESTROOT%", @"C:\TestRoot", StringComparison.OrdinalIgnoreCase));

    [TestMethod]
    public void ParsesQuotedExecutableAndArguments()
    {
        const string command = "\"C:\\Program Files\\Vendor\\App.exe\" --background";

        StartupCommandResolution result = _parser.Parse(
            command,
            StartupSourceKind.CurrentUserRegistry);

        Assert.AreEqual(StartupCommandResolutionStatus.DirectExecutable, result.Status);
        Assert.AreEqual(@"C:\Program Files\Vendor\App.exe", result.ResolvedPath);
        Assert.AreEqual("--background", result.Arguments);
        Assert.AreEqual(command, result.OriginalCommand);
    }

    [TestMethod]
    public void ParsesSimpleUnquotedExecutable()
    {
        StartupCommandResolution result = _parser.Parse(
            @"C:\Tools\Agent.exe",
            StartupSourceKind.LocalMachineRegistry);

        Assert.AreEqual(StartupCommandResolutionStatus.DirectExecutable, result.Status);
        Assert.AreEqual(@"C:\Tools\Agent.exe", result.ResolvedPath);
        Assert.IsNull(result.Arguments);
    }

    [TestMethod]
    public void ExpandsEnvironmentPathForInspection()
    {
        StartupCommandResolution result = _parser.Parse(
            @"%TESTROOT%\Agent.exe",
            StartupSourceKind.CurrentUserRegistry);

        Assert.AreEqual(StartupCommandResolutionStatus.DirectExecutable, result.Status);
        Assert.AreEqual(@"C:\TestRoot\Agent.exe", result.ResolvedPath);
        Assert.IsTrue(result.EnvironmentExpansionApplied);
        Assert.AreEqual(@"%TESTROOT%\Agent.exe", result.OriginalCommand);
    }

    [TestMethod]
    public void EmptyWhitespaceMalformedAndAmbiguousRemainUnresolved()
    {
        Assert.AreEqual(
            StartupCommandResolutionStatus.Empty,
            _parser.Parse(string.Empty, StartupSourceKind.Unknown).Status);
        Assert.AreEqual(
            StartupCommandResolutionStatus.Empty,
            _parser.Parse("   ", StartupSourceKind.Unknown).Status);
        Assert.AreEqual(
            StartupCommandResolutionStatus.Malformed,
            _parser.Parse("\"C:\\Program Files\\App.exe", StartupSourceKind.Unknown).Status);
        Assert.AreEqual(
            StartupCommandResolutionStatus.Ambiguous,
            _parser.Parse(@"C:\Program Files\Vendor\App.exe --silent", StartupSourceKind.Unknown).Status);
    }

    [TestMethod]
    [DataRow("cmd.exe /c payload", "cmd.exe")]
    [DataRow("cmd /c payload", "cmd")]
    [DataRow("powershell.exe -File task.ps1", "powershell.exe")]
    [DataRow("powershell -File task.ps1", "powershell")]
    [DataRow("pwsh.exe -File task.ps1", "pwsh.exe")]
    [DataRow("rundll32.exe sample.dll,Entry", "rundll32.exe")]
    [DataRow("regsvr32 /s sample.dll", "regsvr32")]
    [DataRow("wscript.exe task.vbs", "wscript.exe")]
    [DataRow("cscript task.vbs", "cscript")]
    public void RecognizesLauncherForms(string command, string launcher)
    {
        StartupCommandResolution result = _parser.Parse(command, StartupSourceKind.Unknown);

        Assert.AreEqual(StartupCommandResolutionStatus.LauncherMediated, result.Status);
        Assert.AreEqual(launcher, result.LauncherName);
        Assert.IsNull(result.ResolvedPath);
    }

    [TestMethod]
    public void StartupFolderShortcutIsNotTreatedAsExecutable()
    {
        StartupCommandResolution result = _parser.Parse(
            @"C:\Users\Technician\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\Vendor.lnk",
            StartupSourceKind.UserStartupFolder);

        Assert.AreEqual(StartupCommandResolutionStatus.ShortcutNotResolved, result.Status);
        Assert.IsNull(result.ResolvedPath);

        result = _parser.Parse(
            "\"C:\\Users\\Technician\\Startup\\Vendor.lnk\"",
            StartupSourceKind.UserStartupFolder);

        Assert.AreEqual(StartupCommandResolutionStatus.ShortcutNotResolved, result.Status);
        Assert.IsNull(result.ResolvedPath);
    }

    [TestMethod]
    public void UnsupportedFileTypeIsExplicit()
    {
        StartupCommandResolution result = _parser.Parse(
            @"C:\Tools\Readme.txt",
            StartupSourceKind.Unknown);

        Assert.AreEqual(StartupCommandResolutionStatus.Unsupported, result.Status);
        Assert.IsFalse(result.HasConfidentDirectPath);
    }

    [TestMethod]
    public void MapsOnlyCurrentScannerSourceLabels()
    {
        Assert.AreEqual(StartupSourceKind.CurrentUserRegistry, StartupCommandParser.MapSource("Current User Registry"));
        Assert.AreEqual(StartupSourceKind.LocalMachineRegistry, StartupCommandParser.MapSource("Local Machine Registry"));
        Assert.AreEqual(StartupSourceKind.UserStartupFolder, StartupCommandParser.MapSource("User Startup Folder"));
        Assert.AreEqual(StartupSourceKind.CommonStartupFolder, StartupCommandParser.MapSource("Common Startup Folder"));
        Assert.AreEqual(StartupSourceKind.Unknown, StartupCommandParser.MapSource("RunOnce"));
    }
}
