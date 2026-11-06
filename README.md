# Share Bridge 🌉

> **Universal Windows 11 Share Target Bridge** — seamlessly relay files, links, and text from Windows 11 File Explorer directly to **Telegram Desktop**, **Clipboard**, and **Browser App / PWA Shortcuts** (WhatsApp Web, Gmail, etc.) without third-party cloud services or telemetry.

---

## ✨ Features

- **🚀 Native Windows 11 Integration**:
  - Implements the Windows OS `windows.shareTarget` contract to appear natively inside the Windows 11 Share Sheet.
  - Native cascading File Explorer context menu: Right-click any file $\rightarrow$ **Share Bridge ▶** $\rightarrow$ choose destination.
- **✈️ Instant Telegram Desktop Relay**:
  - Detects official Telegram Desktop client.
  - Dispatches files directly using Telegram's native `-sendpath` command with zero clicks.
- **📋 Multi-Format Clipboard Staging**:
  - Stages files as native `FileDropList` (`CF_HDROP`), allowing instant `Ctrl + V` into WhatsApp Web, Slack, web forms, and desktop apps.
- **💬 Browser App & PWA Shortcuts**:
  - Automatically discovers installed Chrome and Edge Web Apps (WhatsApp Web, Gmail, etc.).
- **🎨 Modern WinUI 3 & Mica Alt Design**:
  - Built with .NET 10 LTS and Windows App SDK 2.6.x.
  - Apple-inspired motion tokens, fluid spring physics, and system dark/light theme awareness.
- **🔒 100% Offline & Private**:
  - Zero external network requests, zero telemetry, zero intermediate cloud relays.

---

## 🛠️ Architecture

```
ShareBridge/
├── src/
│   ├── ShareBridge.Core/         # Business models, contracts, and services
│   │   ├── Contracts/            # ITargetAdapter, IShareDataExtractor, IAdapterRegistry
│   │   ├── Models/               # SharePayload, AdapterResult, TargetMetadata
│   │   └── Services/             # AdapterRegistry, TempFileManager, ContextMenuInstaller
│   ├── ShareBridge.Adapters/     # Target implementations
│   │   ├── Telegram/             # Official Qt Telegram client handler (-sendpath)
│   │   ├── Clipboard/            # Multi-format Windows clipboard handler
│   │   └── BrowserApps/          # Chrome / Edge PWA shortcut detector & launcher
│   └── ShareBridge/              # WinUI 3 desktop application & MSIX packaging
│       ├── Views/                # SharePickerWindow (Mica Alt flyout), MainPage
│       └── Assets/               # Adaptive icons, badges, and splash images
└── tests/
    └── ShareBridge.Tests/        # 24 unit tests verifying registry, detection, & payload extraction
```

---

## 🚀 Getting Started

### Prerequisites
- Windows 11 (build 22000 or higher)
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Windows App SDK Build Tools](https://learn.microsoft.com/windows/apps/windows-app-sdk/)

### Build & Run Tests
```powershell
# Restore and build
dotnet build ShareBridge.slnx

# Run all 24 unit tests
dotnet test ShareBridge.slnx
```

### Install Context Menu
Run the included registration script or registry file:
```powershell
reg.exe import sharebridge_menu.reg
```

---

## 📄 License
MIT License. Free and open source.
