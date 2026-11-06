using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareBridge.Adapters.Clipboard;
using ShareBridge.Adapters.Telegram;
using ShareBridge.Core.Models;
using ShareBridge.Core.Services;

namespace ShareBridge.Tests;

[TestClass]
public sealed class AdapterRegistryTests
{
    [TestMethod]
    public void AdapterRegistry_RegisterAdapter_AvoidsDuplicates()
    {
        var registry = new AdapterRegistry();
        var adapter1 = new ClipboardAdapter();
        var adapter2 = new ClipboardAdapter();

        registry.RegisterAdapter(adapter1);
        registry.RegisterAdapter(adapter2);

        var all = registry.GetAllAdapters();
        Assert.AreEqual(1, all.Count);
    }

    [TestMethod]
    public void AdapterRegistry_GetCompatibleAdapters_FiltersCorrectly()
    {
        var registry = new AdapterRegistry();
        registry.RegisterAdapter(new ClipboardAdapter());
        registry.RegisterAdapter(new TelegramAdapter());

        var emptyPayload = new SharePayload();
        var emptyCompatible = registry.GetCompatibleAdapters(emptyPayload);
        Assert.AreEqual(0, emptyCompatible.Count);

        var textPayload = new SharePayload { Type = PayloadType.Text, Text = "Hello" };
        var textCompatible = registry.GetCompatibleAdapters(textPayload);
        Assert.IsTrue(textCompatible.Count >= 2);
    }
}
