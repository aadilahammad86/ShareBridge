using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareBridge.Adapters.Clipboard;
using ShareBridge.Core.Models;

namespace ShareBridge.Tests;

[TestClass]
public sealed class ClipboardAdapterTests
{
    [TestMethod]
    public void ClipboardAdapter_IsAvailable_AlwaysReturnsTrue()
    {
        var adapter = new ClipboardAdapter();
        Assert.IsTrue(adapter.IsAvailable());
        Assert.AreEqual("clipboard", adapter.Id);
    }

    [TestMethod]
    public void ClipboardAdapter_CanHandle_ValidatesSupportedPayloads()
    {
        var adapter = new ClipboardAdapter();

        Assert.IsFalse(adapter.CanHandle(new SharePayload()));

        var textPayload = new SharePayload { Type = PayloadType.Text, Text = "Copy me" };
        Assert.IsTrue(adapter.CanHandle(textPayload));

        var linkPayload = new SharePayload { Type = PayloadType.WebLink, WebLink = new Uri("https://microsoft.com") };
        Assert.IsTrue(adapter.CanHandle(linkPayload));

        var filePayload = new SharePayload { Type = PayloadType.Files, FilePaths = new[] { @"C:\data\file.txt" } };
        Assert.IsTrue(adapter.CanHandle(filePayload));
    }

    [TestMethod]
    public void ClipboardAdapter_FormatTextRepresentation_FormatsFilesCorrectly()
    {
        var adapter = new ClipboardAdapter();
        var payload = new SharePayload
        {
            Type = PayloadType.Files,
            FilePaths = new[] { @"C:\file1.txt", @"C:\file2.txt" }
        };

        var text = adapter.FormatTextRepresentation(payload);

        var expected = $"C:\\file1.txt{Environment.NewLine}C:\\file2.txt";
        Assert.AreEqual(expected, text);
    }

    [TestMethod]
    public void ClipboardAdapter_FormatTextRepresentation_FormatsLinkWithTextCorrectly()
    {
        var adapter = new ClipboardAdapter();
        var payload = new SharePayload
        {
            Type = PayloadType.WebLink,
            WebLink = new Uri("https://github.com"),
            Text = "GitHub Homepage"
        };

        var text = adapter.FormatTextRepresentation(payload);

        var expected = $"GitHub Homepage{Environment.NewLine}https://github.com/";
        Assert.AreEqual(expected, text);
    }
}
