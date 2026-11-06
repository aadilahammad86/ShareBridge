using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareBridge.Core.Models;
using ShareBridge.Core.Services;

namespace ShareBridge.Tests;

[TestClass]
public sealed class ShareBridgeCoreTests
{
    private string _testTempRoot = null!;

    [TestInitialize]
    public void Setup()
    {
        _testTempRoot = Path.Combine(Path.GetTempPath(), "ShareBridgeTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testTempRoot);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testTempRoot))
        {
            try
            {
                Directory.Delete(_testTempRoot, recursive: true);
            }
            catch
            {
                // Ignored
            }
        }
    }

    [TestMethod]
    public void TempFileManager_CreateSessionDirectory_CreatesDirectoryOnDisk()
    {
        var manager = new TempFileManager(_testTempRoot);
        var sessionDir = manager.CreateSessionDirectory();

        Assert.IsTrue(Directory.Exists(sessionDir));
        Assert.IsTrue(sessionDir.StartsWith(_testTempRoot, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TempFileManager_CleanSession_DeletesSessionDirectory()
    {
        var manager = new TempFileManager(_testTempRoot);
        var sessionDir = manager.CreateSessionDirectory();
        Assert.IsTrue(Directory.Exists(sessionDir));

        manager.CleanSession(sessionDir);
        Assert.IsFalse(Directory.Exists(sessionDir));
    }

    [TestMethod]
    public void TempFileManager_CleanStaleSessions_DeletesOldDirectoriesOnly()
    {
        var manager = new TempFileManager(_testTempRoot);
        var staleDir = manager.CreateSessionDirectory();
        var freshDir = manager.CreateSessionDirectory();

        // Simulate stale directory
        Directory.SetLastWriteTimeUtc(staleDir, DateTime.UtcNow.AddHours(-2));

        int cleaned = manager.CleanStaleSessions(TimeSpan.FromHours(1));

        Assert.AreEqual(1, cleaned);
        Assert.IsFalse(Directory.Exists(staleDir));
        Assert.IsTrue(Directory.Exists(freshDir));
    }

    [TestMethod]
    public void SharePayload_Dispose_DeletesSessionDirectoryWhenAutoCleanupEnabled()
    {
        var manager = new TempFileManager(_testTempRoot);
        var sessionDir = manager.CreateSessionDirectory();

        var payload = new SharePayload
        {
            SessionDirectory = sessionDir,
            AutoCleanupOnDispose = true
        };

        payload.Dispose();

        Assert.IsFalse(Directory.Exists(sessionDir));
    }

    [TestMethod]
    public void SharePayload_Dispose_PreservesSessionDirectoryWhenAutoCleanupDisabled()
    {
        var manager = new TempFileManager(_testTempRoot);
        var sessionDir = manager.CreateSessionDirectory();

        var payload = new SharePayload
        {
            SessionDirectory = sessionDir,
            AutoCleanupOnDispose = false
        };

        payload.Dispose();

        Assert.IsTrue(Directory.Exists(sessionDir));
    }

    [TestMethod]
    public void AdapterResult_Factories_CreateExpectedRecords()
    {
        var ok = AdapterResult.Ok();
        Assert.IsTrue(ok.Success);
        Assert.AreEqual(AdapterErrorKind.None, ok.ErrorKind);

        var fail = AdapterResult.Fail(AdapterErrorKind.AppNotFound, "Not found");
        Assert.IsFalse(fail.Success);
        Assert.AreEqual(AdapterErrorKind.AppNotFound, fail.ErrorKind);
        Assert.AreEqual("Not found", fail.ErrorMessage);
    }
}
