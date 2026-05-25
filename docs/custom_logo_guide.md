# Custom Logo & Icon Guide: WakeyWakey

Having a custom logo makes the application look highly professional, polished, and premium. For a Windows WPF application like **WakeyWakey**, custom images are used in **two main contexts**, each requiring specific formats and setups.

Below is a detailed breakdown of the image formats, the custom multi-resolution logo, the exact file integrations, and the clean build results.

---

## 1. The Custom WakeyWakey Logo & Icon

To match the sleek dark-mode aesthetic of **WakeyWakey** (with its deep indigos, neon purple, and hot pink accent palette), the following custom assets are located inside your project:
1. **Source Graphic:** A premium, modern high-tech PNG app icon at `src\WakeyWakey\Resources\logo.png`.
2. **True Windows Icon (.ico):** A fully compliant, multi-resolution Windows Icon file at `src\WakeyWakey\Resources\logo.ico`.

---

## 2. File Integration & Configuration

### A. Project Assembly & Executable Icon (.csproj)
The multi-resolution `.ico` has been linked as the native assembly executable icon so that it is embedded directly in `WakeyWakey.exe` and shown on the desktop or in Windows Explorer:

```xml
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <!-- Links the logo.ico as the primary assembly executable icon -->
    <ApplicationIcon>Resources\logo.ico</ApplicationIcon>
  </PropertyGroup>

  <!-- Clean, deduplicated list of compiled resources -->
  <ItemGroup>
    <Resource Include="Resources\logo.png" />
    <Resource Include="Resources\logo.ico" />
  </ItemGroup>
```

### B. Window Title Bar & Taskbar Icon (XAML)
`MainWindow.xaml` is configured to load your custom multi-resolution `.ico` container file using WPF's absolute **Pack URI** scheme. This allows Windows to extract the exact resolution (like `16x16` or `32x32`) it needs at runtime depending on window state and display scale:

* **MainWindow.xaml (Window Tag):**
  ```xml
  <Window x:Class="WakeyWakey.MainWindow"
          ...
          Title="WakeyWakey Dashboard" Height="620" Width="900"
          Icon="pack://application:,,,/Resources/logo.ico">
  ```

* **MainWindow.xaml (Sidebar branding):**
  The vector-like PNG is still used here for its crisp transparency over the sidebar's dark background gradient:
  ```xml
  <Image Source="pack://application:,,,/Resources/logo.png" Width="36" Height="36" Margin="0,0,10,0" VerticalAlignment="Center"/>
  ```

---

## 3. Deployment & Distribution

### 🚀 Official Final Distribution
The official final distribution build of the application is located at:
`C:\learm\wakeywakey\dist`

> [!NOTE]
> Developers can safely **ignore** the deep `src\WakeyWakey\bin` and `src\WakeyWakey\obj` folders. Those are internal build artifacts.
> The primary, fully compiled final application to run and distribute is **`C:\learm\wakeywakey\dist\WakeyWakey.exe`**.

### Clean Publication Steps
The final distribution is built using:
1. **Clean Command Executed:** `Remove-Item -Recurse -Force C:\learm\wakeywakey\dist\*` (removes old cached builds).
2. **Publish Command Executed:**
   ```powershell
   dotnet publish C:\learm\wakeywakey\src\WakeyWakey\WakeyWakey.csproj -c Release -o C:\learm\wakeywakey\dist
   ```
3. **Executable Verification:** Compiled successfully with **0 warnings** and **0 errors**. The resulting `dist\WakeyWakey.exe` carries your custom embedded icon frame!

---

## 4. Note on Windows Icon Cache
Because Windows Explorer aggressively caches executable icons by filename, `WakeyWakey.exe` may continue to display the default generic icon locally on your computer until you either clear your system's icon cache database (located under `%localappdata%\IconCache.db`) or restart your machine. However, the binary itself is successfully compiled with the custom icon and will display correctly on other machines or once the local system cache is refreshed!
