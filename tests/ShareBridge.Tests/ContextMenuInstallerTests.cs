using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareBridge.Core.Services;

namespace ShareBridge.Tests;

[TestClass]
public class ContextMenuInstallerTests
{
    [TestMethod]
    public void Install_Then_Uninstall_ManagesRegistryCleanly()
    {
        var dummyExe = @"C:\Program Files\WindowsApps\ShareBridge.App_1.0.0.0_x64__rr33d9kzhb48w\ShareBridge.exe";

        // Test installation
        ContextMenuInstaller.Install(dummyExe);
        Assert.IsTrue(ContextMenuInstaller.IsInstalled(), "Context menu should be reported as installed.");

        // Test uninstallation
        ContextMenuInstaller.Uninstall();
        Assert.IsFalse(ContextMenuInstaller.IsInstalled(), "Context menu should be reported as not installed.");
    }
}
