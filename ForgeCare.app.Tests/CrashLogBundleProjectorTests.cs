using System.Text;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class CrashLogBundleProjectorTests
{
    private const string LegacyRecord = """
        ============================================================
        ForgeCare exception · 2026-08-30 14:32:10
        Context: WPF DispatcherUnhandledException
        Machine: SECRET-WORKSTATION
        User: Alice
        OS: Microsoft Windows 11
        .NET: .NET 10.0.0

        System.IO.IOException: customer-specific failure at C:\Users\Alice\Documents\CustomerA\case.txt
           at ForgeCare.App.MainWindow.Export() in C:\dev\Mindforge\ForgeCare\MainWindow.xaml.cs:line 42
           at D:\Clients\SecretProject\Support.Run()
        Bearer SUPER_SECRET_TOKEN
        API_KEY_TEST_VALUE
        sk-test-secret
        """;

    [TestMethod]
    public async Task RealisticLegacyRecordProjectsUsefulFieldsWithoutSensitiveContent()
    {
        using var temp = new TemporaryDirectory();
        string source = Path.Combine(temp.Path, "crash.log");
        string target = Path.Combine(temp.Path, "projected.log");
        await File.WriteAllTextAsync(source, LegacyRecord);

        CrashLogBundleProjector.Project(source, target);

        string projected = await File.ReadAllTextAsync(target);
        StringAssert.Contains(projected, "2026-08-30 14:32:10");
        StringAssert.Contains(projected, "Context: WPF DispatcherUnhandledException");
        StringAssert.Contains(projected, "Failure type: System.IO.IOException");
        AssertSensitiveValuesAbsent(projected);
    }

    [TestMethod]
    public async Task RepeatedAndPartialRecordsRemainBounded()
    {
        using var temp = new TemporaryDirectory();
        string source = Path.Combine(temp.Path, "crash.log");
        string target = Path.Combine(temp.Path, "projected.log");
        await File.WriteAllTextAsync(
            source,
            LegacyRecord + Environment.NewLine +
            "============================================================\n" +
            "ForgeCare diagnostic issue · 2026-08-30 15:00:00 UTC\n" +
            "Version: 1.1.0-beta.1\n" +
            "Context: Debug bundle export\n" +
            "Failure type: IOException\n" +
            "Machine: SECOND-SECRET");

        CrashLogBundleProjector.Project(source, target);

        string projected = await File.ReadAllTextAsync(target);
        Assert.AreEqual(2, CountOccurrences(projected, "Failure type:"));
        StringAssert.Contains(projected, "Version: 1.1.0-beta.1");
        Assert.IsFalse(projected.Contains("SECOND-SECRET", StringComparison.Ordinal));
        AssertSensitiveValuesAbsent(projected);
    }

    [TestMethod]
    public async Task EmptyAndRandomInputCannotLeakRawContent()
    {
        using var temp = new TemporaryDirectory();
        string source = Path.Combine(temp.Path, "crash.log");
        string target = Path.Combine(temp.Path, "projected.log");
        await File.WriteAllTextAsync(source, string.Empty);

        CrashLogBundleProjector.Project(source, target);
        StringAssert.Contains(await File.ReadAllTextAsync(target), "No crash diagnostics were recorded.");

        await File.WriteAllTextAsync(source, "Alice C:\\Users\\Alice Bearer SUPER_SECRET_TOKEN");
        CrashLogBundleProjector.Project(source, target);
        string projected = await File.ReadAllTextAsync(target);
        StringAssert.Contains(projected, "Unstructured crash diagnostic content was omitted.");
        AssertSensitiveValuesAbsent(projected);
    }

    [TestMethod]
    public async Task LargeInputProducesBoundedProjection()
    {
        using var temp = new TemporaryDirectory();
        string source = Path.Combine(temp.Path, "crash.log");
        string target = Path.Combine(temp.Path, "projected.log");
        string record = "============================================================\nContext: Test\nSystem.IO.IOException: secret C:\\Users\\Alice\\x\n";
        await File.WriteAllTextAsync(source, string.Concat(Enumerable.Repeat(record, 40_000)));

        CrashLogBundleProjector.Project(source, target);

        string projected = await File.ReadAllTextAsync(target);
        Assert.IsLessThan(150_000, projected.Length);
        StringAssert.Contains(projected, "omitted by the bundle size limit");
        AssertSensitiveValuesAbsent(projected);
    }

    private static int CountOccurrences(string value, string search)
    {
        int count = 0;
        int offset = 0;
        while ((offset = value.IndexOf(search, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += search.Length;
        }
        return count;
    }

    internal static void AssertSensitiveValuesAbsent(string output)
    {
        foreach (string sensitive in new[]
        {
            "SECRET-WORKSTATION",
            "User: Alice",
            "C:\\Users\\Alice",
            "C:\\dev\\Mindforge",
            "D:\\Clients\\SecretProject",
            "customer-specific failure",
            "SUPER_SECRET_TOKEN",
            "API_KEY_TEST_VALUE",
            "sk-test-secret",
            "MainWindow.xaml.cs"
        })
        {
            Assert.IsFalse(output.Contains(sensitive, StringComparison.OrdinalIgnoreCase), sensitive);
        }
    }
}
