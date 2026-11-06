using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareBridge.Adapters.BrowserApps;
using ShareBridge.Core.Models;

namespace ShareBridge.Tests;

[TestClass]
public sealed class BrowserAppAdapterTests
{
    [TestMethod]
    public void BrowserAppDetector_ClassifyApp_IdentifiesWhatsAppAndGmail()
    {
        Assert.AreEqual(BrowserAppKind.WhatsApp, BrowserAppDetector.ClassifyApp("WhatsApp Web", "hnpfjngllnobngcgfapefoaidbinmjnm"));
        Assert.AreEqual(BrowserAppKind.WhatsApp, BrowserAppDetector.ClassifyApp("WhatsApp", "customid"));
        Assert.AreEqual(BrowserAppKind.Gmail, BrowserAppDetector.ClassifyApp("Gmail", "fhihpiojkbmbpdjeoajapmgkhlnakfjf"));
        Assert.AreEqual(BrowserAppKind.GenericPwa, BrowserAppDetector.ClassifyApp("Twitter / X", "genericid"));
    }

    [TestMethod]
    public void BrowserAppDetector_ExtractAppId_ExtractsCorrectHash()
    {
        var uninstallStr = "\"C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe\" --profile-directory=Default --uninstall-app-id=hnpfjngllnobngcgfapefoaidbinmjnm";
        var appId = BrowserAppDetector.ExtractAppId(uninstallStr);

        Assert.AreEqual("hnpfjngllnobngcgfapefoaidbinmjnm", appId);
    }

    [TestMethod]
    public void BrowserAppDetector_DetectInstalledApps_DetectsWhatsAppWebOnThisSystem()
    {
        var detector = new BrowserAppDetector();
        var apps = detector.DetectInstalledApps();

        Assert.IsNotNull(apps);
        // On this PC, WhatsApp Web was confirmed installed
        var wa = apps.FirstOrDefault(a => a.Kind == BrowserAppKind.WhatsApp);
        Assert.IsNotNull(wa, "WhatsApp Web should be detected on this machine.");
        Assert.AreEqual("hnpfjngllnobngcgfapefoaidbinmjnm", wa.AppId);
    }

    [TestMethod]
    public void BrowserAppAdapter_Properties_MatchAppInfo()
    {
        var info = new BrowserAppInfo(
            AppId: "hnpfjngllnobngcgfapefoaidbinmjnm",
            DisplayName: "WhatsApp Web",
            Kind: BrowserAppKind.WhatsApp
        );

        var adapter = new BrowserAppAdapter(info);

        Assert.AreEqual("browser-app-hnpfjngllnobngcgfapefoaidbinmjnm", adapter.Id);
        Assert.AreEqual("WhatsApp Web", adapter.DisplayName);
        Assert.IsTrue(adapter.IsAvailable());

        var payload = new SharePayload
        {
            Type = PayloadType.Text,
            Text = "Hello from Windows 11"
        };
        Assert.IsTrue(adapter.CanHandle(payload));
    }
}
