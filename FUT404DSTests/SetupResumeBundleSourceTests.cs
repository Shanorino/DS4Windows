using FUT404DS.Installation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.AccessControl;

namespace FUT404DSTests;

[TestClass]
public sealed class SetupResumeBundleSourceTests
{
    private const string BundleId = "{B9989EB0-A33C-4604-ABAB-BBD165F8A40E}";
    private const string Root = @"C:\ProgramData\Package Cache";
    private static string BundlePath => Path.Combine(Root, BundleId, "FUT404DS_VIIPERRC4.6.1_Setup_x64.exe");

    [TestMethod]
    [TestCategory("InstalledBundle")]
    public void OptInInstalledBundleResolvesWithoutMutatingRegistryCacheOrLaunchingCode()
    {
        string id = Environment.GetEnvironmentVariable("FUT404DS_RESUME_BUNDLE_ID");
        if (string.IsNullOrWhiteSpace(id))
            Assert.Inconclusive("Opt in with FUT404DS_RESUME_BUNDLE_ID for a read-only installed Burn cache check.");
        string source = SetupResumeBundleSource.Resolve(id);
        Assert.IsTrue(File.Exists(source));
        Assert.AreEqual(Guid.Parse(id), Guid.Parse(Path.GetFileName(Path.GetDirectoryName(source))));
        StartupSetupRecovery.RequireProtectedPath(StartupSetupRecovery.NativeProgramFiles,
            StartupSetupRecovery.NativeProgramFiles);
    }

    [TestMethod]
    public void ExactCurrentBundleRegistrationSelectsOnlyItsMachineCache()
    {
        Assert.AreEqual(BundlePath, SetupResumeBundleSource.ValidateRegistration(BundleId, BundleId,
            SetupResumeBundleSource.BundleTag, new[] { SetupResumeBundleSource.UpgradeCode }, BundlePath, Root));
    }

    [DataTestMethod]
    [DataRow("provider")]
    [DataRow("tag")]
    [DataRow("upgrade")]
    [DataRow("path")]
    [DataRow("otherBundleCache")]
    [DataRow("nested")]
    [DataRow("traversal")]
    [DataRow("unc")]
    [DataRow("executable")]
    public void ForeignOrRetargetedRegistrationsCannotSelectAResumeSource(string changed)
    {
        string provider = changed == "provider" ? "{0083D13B-244D-4724-8D88-6078F34148BE}" : BundleId;
        string tag = changed == "tag" ? "OtherBundle" : SetupResumeBundleSource.BundleTag;
        string upgrade = changed == "upgrade" ? provider : SetupResumeBundleSource.UpgradeCode;
        string path = changed switch
        {
            "path" => @"C:\Users\Public\FUT404DS_Test_Setup_x64.exe",
            "otherBundleCache" => Path.Combine(Root, "{0083D13B-244D-4724-8D88-6078F34148BE}",
                "FUT404DS_Test_Setup_x64.exe"),
            "nested" => Path.Combine(Root, BundleId, "nested", "FUT404DS_Test_Setup_x64.exe"),
            "traversal" => Path.Combine(Root, BundleId, "child", "..", "FUT404DS_Test_Setup_x64.exe"),
            "unc" => @"\\server\share\FUT404DS_Test_Setup_x64.exe",
            "executable" => Path.Combine(Root, BundleId, "unrelated.exe"),
            _ => BundlePath,
        };
        Assert.ThrowsException<InvalidDataException>(() => SetupResumeBundleSource.ValidateRegistration(BundleId,
            provider, tag, new[] { upgrade }, path, Root));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("../other")]
    [DataRow("B9989EB0-A33C-4604-ABAB-BBD165F8A40E")]
    public void BundleIdentityCannotBecomeARegistryPath(string id)
    {
        Assert.ThrowsException<InvalidDataException>(() => SetupResumeBundleSource.ValidateRegistration(id,
            BundleId, SetupResumeBundleSource.BundleTag, new[] { SetupResumeBundleSource.UpgradeCode }, BundlePath, Root));
    }

    [TestMethod]
    public void WindowsProtectedCacheAclAndHarmlessAncestorCreationRightsAreAccepted()
    {
        byte[] cache = Descriptor("O:BAD:PAI(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)(A;;FRFX;;;WD)");
        StartupSetupRecovery.RequireProtectedSecurity(cache, ancestor: false);
        byte[] programData = Descriptor("O:SYD:PAI(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)(A;;FW;;;BU)(A;OICIIO;GA;;;CO)");
        StartupSetupRecovery.RequireProtectedSecurity(programData, ancestor: true);
    }

    [DataTestMethod]
    [DataRow("O:BUD:(A;;FA;;;BA)(A;;FRFX;;;BU)", false)]
    [DataRow("O:BAD:(A;;FA;;;BA)(A;;FW;;;BU)", false)]
    [DataRow("O:BAD:(A;;FA;;;BA)(A;;GA;;;WD)", false)]
    [DataRow("O:BAD:(A;;FA;;;BA)(A;;0x40;;;BU)", true)]
    [DataRow("O:BAD:(A;;FA;;;BA)(A;;WD;;;BU)", true)]
    [DataRow("O:BAD:(A;;FA;;;BA)(A;;WO;;;BU)", true)]
    [DataRow("O:BAD:NO_ACCESS_CONTROL", false)]
    public void WritableOwnersFilesAndAncestorsAreRejected(string sddl, bool ancestor)
    {
        Assert.ThrowsException<IOException>(() =>
            StartupSetupRecovery.RequireProtectedSecurity(Descriptor(sddl), ancestor));
    }

    private static byte[] Descriptor(string sddl)
    {
        var security = new RawSecurityDescriptor(sddl);
        var bytes = new byte[security.BinaryLength];
        security.GetBinaryForm(bytes, 0);
        return bytes;
    }
}
