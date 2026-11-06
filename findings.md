# Findings & Technical Knowledge Base

## Telegram Desktop
- **Executable location**: `%APPDATA%\Telegram Desktop\Telegram.exe`
- **Registry protocol key**: `HKCU:\Software\Classes\tg\shell\open\command` -> `"C:\Users\sysadmin\AppData\Roaming\Telegram Desktop\Telegram.exe" -- "%1"`
- **CLI `-sendpath` confirmed**: In `Telegram/SourceFiles/core/launcher.cpp`:
  ```cpp
  { "-sendpath" , KeyFormat::AllLeftValues }
  ```
  Launches Telegram with recipient selection modal pre-loaded with the file(s).
- **Text / WebLink URIs**:
  - `tg://msg?text=<encoded_text>`
  - `tg://msg_url?url=<encoded_url>&text=<encoded_text>`

## Browser Shortcuts & PWAs
- **Chrome App shortcuts**: `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Chrome Apps\`
- **Installed on this system**: `WhatsApp Web.lnk` (App ID: `hnpfjngllnobngcgfapefoaidbinmjnm`)
- **PWA Command**: `chrome_proxy.exe --profile-directory=Default --app-id=<app_id>`
- **Files limitation**: PWAs cannot receive file paths on CLI due to sandbox; solution is assisted clipboard staging + focusing PWA window.

## Windows Share Target Contract
- **Category**: `windows.shareTarget` declared in `Package.appxmanifest`
- **Activation**: In WinUI 3, routed via `AppInstance.GetCurrent().GetActivatedEventArgs()` where `Kind == ExtendedActivationKind.ShareTarget`
- **Signal requirements**: MUST call `ShareOperation.ReportStarted()` immediately, and `ReportCompleted()` on handoff
- **Process model**: Multi-instanced by default; each share spawns a lightweight instance which handles handoff and exits cleanly.

## Design Engineering Tokens
- **Motion**: 180ms ease-out (`cubic-bezier(0.16, 1, 0.3, 1)`), press scale `0.98` with spring damping (`stiffness: 300, damping: 24`).
- **Surface**: Windows 11 Mica Alt with 1px border contrast (`#FFFFFF` 8% in dark, `#000000` 6% in light).
