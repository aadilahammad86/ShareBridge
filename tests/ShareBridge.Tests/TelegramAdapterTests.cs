using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareBridge.Adapters.Telegram;
using ShareBridge.Core.Models;

namespace ShareBridge.Tests;

[TestClass]
public sealed class TelegramAdapterTests
{
    [TestMethod]
    public void TelegramAdapter_CanHandle_RecognizesFilesTextAndLinks()
    {
        var adapter = new TelegramAdapter();

        var emptyPayload = new SharePayload();
        Assert.IsFalse(adapter.CanHandle(emptyPayload));

        var textPayload = new SharePayload
        {
            Type = PayloadType.Text,
            Text = "Hello from Windows 11"
        };
        Assert.IsTrue(adapter.CanHandle(textPayload));

        var linkPayload = new SharePayload
        {
            Type = PayloadType.WebLink,
            WebLink = new Uri("https://github.com/microsoft/winappCli")
        };
        Assert.IsTrue(adapter.CanHandle(linkPayload));

        var filePayload = new SharePayload
        {
            Type = PayloadType.Files,
            FilePaths = new[] { @"C:\test\sample.png" }
        };
        Assert.IsTrue(adapter.CanHandle(filePayload));
    }

    [TestMethod]
    public void TelegramAdapter_BuildLaunchInfo_SingleFileGeneratesCorrectSendpath()
    {
        var adapter = new TelegramAdapter();
        var payload = new SharePayload
        {
            Type = PayloadType.Files,
            FilePaths = new[] { @"C:\Users\test\Pictures\vacation.jpg" }
        };

        var startInfo = adapter.BuildLaunchInfo(payload, @"C:\bin\Telegram.exe");

        Assert.AreEqual(@"C:\bin\Telegram.exe", startInfo.FileName);
        Assert.AreEqual(@"-sendpath ""C:\Users\test\Pictures\vacation.jpg""", startInfo.Arguments);
        Assert.IsFalse(startInfo.UseShellExecute);
    }

    [TestMethod]
    public void TelegramAdapter_BuildLaunchInfo_MultipleFilesGeneratesQuotedSendpath()
    {
        var adapter = new TelegramAdapter();
        var payload = new SharePayload
        {
            Type = PayloadType.Files,
            FilePaths = new[] { @"C:\docs\report 1.pdf", @"C:\docs\invoice 2.xlsx" }
        };

        var startInfo = adapter.BuildLaunchInfo(payload, @"C:\bin\Telegram.exe");

        Assert.AreEqual(@"C:\bin\Telegram.exe", startInfo.FileName);
        Assert.AreEqual(@"-sendpath ""C:\docs\report 1.pdf"" ""C:\docs\invoice 2.xlsx""", startInfo.Arguments);
        Assert.IsFalse(startInfo.UseShellExecute);
    }

    [TestMethod]
    public void TelegramAdapter_BuildLaunchInfo_WebLinkGeneratesTgMsgUrlUri()
    {
        var adapter = new TelegramAdapter();
        var payload = new SharePayload
        {
            Type = PayloadType.WebLink,
            WebLink = new Uri("https://github.com"),
            Text = "Check this out"
        };

        var startInfo = adapter.BuildLaunchInfo(payload);

        Assert.IsTrue(startInfo.UseShellExecute);
        Assert.AreEqual("tg://msg_url?url=https%3A%2F%2Fgithub.com%2F&text=Check%20this%20out", startInfo.FileName);
    }

    [TestMethod]
    public void TelegramAdapter_BuildLaunchInfo_TextGeneratesTgMsgUri()
    {
        var adapter = new TelegramAdapter();
        var payload = new SharePayload
        {
            Type = PayloadType.Text,
            Text = "Meeting at 3 PM & bring notes"
        };

        var startInfo = adapter.BuildLaunchInfo(payload);

        Assert.IsTrue(startInfo.UseShellExecute);
        Assert.AreEqual("tg://msg?text=Meeting%20at%203%20PM%20%26%20bring%20notes", startInfo.FileName);
    }

    [TestMethod]
    public void TelegramAdapter_FindExecutablePath_FindsInstalledTelegramOnThisSystem()
    {
        var adapter = new TelegramAdapter();
        var exe = adapter.FindExecutablePath();

        // On this machine, Telegram Desktop is installed in AppData
        Assert.IsNotNull(exe, "Telegram Desktop should be discovered on this system.");
        Assert.IsTrue(exe.EndsWith("Telegram.exe", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(adapter.IsAvailable());
    }
}
