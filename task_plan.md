# Task Plan: Windows 11 Share Bridge

## Overview
Share Bridge is a modern Windows 11 desktop app built with .NET 10 LTS and WinUI 3, packaged via single-project MSIX. It acts as a universal bridge allowing desktop apps (like Telegram Desktop) and browser shortcut PWAs (WhatsApp Web, Gmail) to receive shared files, text, and URLs via the native Windows 11 Share panel.

## Active Phase
- **Current Phase**: Implementation Complete & Verified
- **Status**: COMPLETED

## Phases & Tasks

### [x] Phase 0: Solution Scaffolding & Package Identity (COMPLETED)
- [x] Install WinUI 3 templates (`Microsoft.WindowsAppSDK.WinUI.CSharp.Templates`)
- [x] Create repository layout: `src/ShareBridge`, `src/ShareBridge.Core`, `src/ShareBridge.Adapters`, `tests/ShareBridge.Tests`
- [x] Configure solution and references with `net10.0-windows10.0.26100.0`
- [x] Configure `Package.appxmanifest` with `windows.shareTarget` extension for `StorageItems`, `Text`, `WebLink`, `Bitmap`
- [x] Verified build (`dotnet build`: 0 warnings, 0 errors) and test run (`dotnet test`: 1 passed)

### [x] Phase 1: Core Contracts & Data Extraction (COMPLETED)
- [x] Implement `SharePayload` model (`src/ShareBridge.Core/Models/SharePayload.cs`)
- [x] Implement `ITargetAdapter` contract & `AdapterResult` (`src/ShareBridge.Core/Contracts/ITargetAdapter.cs`)
- [x] Implement `TempFileManager` with isolated session cleanup (`src/ShareBridge.Core/Services/TempFileManager.cs`)
- [x] Implement `ShareDataExtractor` for `DataPackageView` (`src/ShareBridge.Core/Services/ShareDataExtractor.cs`)
- [x] Unit tests for data models, session lifecycle, and stale cleanup (`tests/ShareBridge.Tests`: 7 passed)

### [x] Phase 2: Telegram Desktop Adapter (COMPLETED)
- [x] Implement `TelegramAdapter` (`src/ShareBridge.Adapters/Telegram/TelegramAdapter.cs`)
- [x] Executable auto-discovery (Registry `HKCU\Software\Classes\tg`, `%APPDATA%\Telegram Desktop`, custom settings)
- [x] Command builder for `-sendpath` (files) and `tg://msg` / `tg://msg_url` (text/links)
- [x] Unit tests for argument formatting and encoding (`tests/ShareBridge.Tests`: 13 passed)

### [x] Phase 3: Clipboard Adapter (COMPLETED)
- [x] Implement `ClipboardAdapter` (`src/ShareBridge.Adapters/Clipboard/ClipboardAdapter.cs`)
- [x] Multi-format clipboard population (StorageItems, plain text, and web link)
- [x] Unit tests for clipboard formatting (`tests/ShareBridge.Tests`: 17 passed)

### [x] Phase 4: Browser App / PWA Adapter (COMPLETED)
- [x] Implement `BrowserAppDetector` (Chrome/Edge `.lnk` shortcuts and uninstall registry)
- [x] Implement `BrowserAppAdapter` for WhatsApp Web and Gmail
- [x] Direct compose URLs for text/links
- [x] Assisted clipboard file flow with toast instructions
- [x] Unit tests for shortcut parsing (`tests/ShareBridge.Tests`: 21 passed)

### [x] Phase 5: WinUI 3 User Interface & Polish (COMPLETED)
- [x] Implement `AdapterRegistry` to discover and manage registered adapters
- [x] Implement activation router in `App.xaml.cs` (`ExtendedActivationKind.ShareTarget` vs normal launch)
- [x] Implement `SharePickerWindow.xaml` (Mica Alt, Apple design spring curves, Emil Kowalski scale-on-press feedback)
- [x] Implement `MainWindow.xaml` and `MainPage.xaml` for management, adapter toggles, test share button, and offline assurance
- [x] Edge-case hardening: middle-ellipsis file truncation, keyboard accessibility (Tab, Enter, Esc)
- [x] Unit tests for adapter registry (23/23 tests passed)

### [x] Phase 6: Packaging, Signing & Live End-to-End Verification (COMPLETED)
- [x] Configured self-contained Windows App SDK (`<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`)
- [x] Created development signing certificate (`ShareBridge_Dev.cer` / `.pfx`)
- [x] Packaged `ShareBridge_1.0.0.0_x64.msix` and digitally signed with `signtool.exe`
- [x] Tested live execution of `ShareBridge.exe`
- [x] Created `Install-DevPackage.ps1` and `Uninstall-Package.ps1` scripts
- [x] Authored full walkthrough documentation in `walkthrough.md`
