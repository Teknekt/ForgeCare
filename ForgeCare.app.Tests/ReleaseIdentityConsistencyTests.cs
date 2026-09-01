using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ReleaseIdentityConsistencyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string AppRoot = Path.Combine(RepositoryRoot, "ForgeCare.app");

    [TestMethod]
    public void AuthoritativeIdentityMatchesBuildManifestAndReleaseMetadata()
    {
        XDocument props = XDocument.Load(Path.Combine(AppRoot, "ForgeCare.Release.props"));
        XElement propertyGroup = props.Root!.Element("PropertyGroup")!;
        string version = propertyGroup.Element("ForgeCareVersion")!.Value;
        string numericVersion = propertyGroup.Element("ForgeCareNumericVersion")!.Value;
        string channel = propertyGroup.Element("ForgeCareReleaseChannel")!.Value;
        string portableFile = propertyGroup.Element("ForgeCarePortableFileName")!.Value;
        string installerBaseName = propertyGroup.Element("ForgeCareInstallerBaseName")!.Value;

        Assert.AreEqual("1.1.0-beta.1", version);
        Assert.AreEqual("1.1.0.0", numericVersion);
        Assert.AreEqual("beta", channel);
        Assert.AreEqual($"ForgeCare-v{version}-win-x64-portable.zip", portableFile);
        Assert.AreEqual($"ForgeCare-v{version}-Setup", installerBaseName);

        XDocument project = XDocument.Load(Path.Combine(AppRoot, "ForgeCare.app.csproj"));
        Assert.AreEqual("ForgeCare.Release.props", project.Root!.Element("Import")!.Attribute("Project")!.Value);
        XElement buildIdentity = project.Root.Elements("PropertyGroup").Single(group => group.Element("Version") is not null);
        Assert.AreEqual("$(ForgeCareVersion)", buildIdentity.Element("Version")!.Value);
        Assert.AreEqual("$(ForgeCareNumericVersion)", buildIdentity.Element("AssemblyVersion")!.Value);
        Assert.AreEqual("$(ForgeCareNumericVersion)", buildIdentity.Element("FileVersion")!.Value);
        Assert.AreEqual("$(ForgeCareVersion)", buildIdentity.Element("InformationalVersion")!.Value);

        XDocument appManifest = XDocument.Load(Path.Combine(AppRoot, "app.manifest"));
        XNamespace assemblyNamespace = "urn:schemas-microsoft-com:asm.v1";
        Assert.AreEqual(numericVersion, appManifest.Root!.Element(assemblyNamespace + "assemblyIdentity")!.Attribute("version")!.Value);

        using JsonDocument releaseManifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppRoot, "release-manifest.template.json")));
        JsonElement root = releaseManifest.RootElement;
        Assert.AreEqual(version, root.GetProperty("version").GetString());
        Assert.AreEqual(numericVersion, root.GetProperty("numericVersion").GetString());
        Assert.AreEqual(channel, root.GetProperty("channel").GetString());
        Assert.AreEqual(portableFile, root.GetProperty("portable").GetProperty("file").GetString());
        Assert.AreEqual(installerBaseName + ".exe", root.GetProperty("installer").GetProperty("file").GetString());
        Assert.AreEqual("windows-11-x64", root.GetProperty("target").GetString());
        Assert.IsFalse(root.GetProperty("signed").GetBoolean());
    }

    [TestMethod]
    public void InstallerKeepsStableAppIdAndRequiresPipelineIdentity()
    {
        string installer = File.ReadAllText(Path.Combine(AppRoot, "installer", "ForgeCare.iss"));

        StringAssert.Contains(installer, "{0F34D1F2-0B94-4F4F-A63D-F0A15E7D11C7}");
        StringAssert.Contains(installer, "#ifndef MyAppVersion");
        StringAssert.Contains(installer, "#ifndef MyNumericVersion");
        StringAssert.Contains(installer, "#ifndef MyOutputBaseFilename");
        StringAssert.Contains(installer, "OutputBaseFilename={#MyOutputBaseFilename}");
    }

    [TestMethod]
    public void RuntimeAssemblyUsesAuthoritativeBetaIdentity()
    {
        string? informationalVersion = typeof(ReleaseIdentityService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        Assert.IsNotNull(informationalVersion);
        Assert.AreEqual("1.1.0-beta.1", informationalVersion.Split('+')[0]);
        Assert.AreEqual(new Version(1, 1, 0, 0), typeof(ReleaseIdentityService).Assembly.GetName().Version);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ForgeCare.slnx")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the ForgeCare repository root.");
    }
}
