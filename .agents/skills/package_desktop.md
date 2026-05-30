## Objective
Your goal as Desktop Release Engineer is to build, package, and run the native desktop application based on the chosen stack.

## Instructions
1. Stack Detection:
   Inspect production_artifacts/Technical_Specification.md and app_build/ to determine the native desktop stack.

2. Restore/Install Dependencies:
   If the project is C#/.NET, run:
   - dotnet restore
   - dotnet build
   If the project uses another native stack, use the appropriate native build command.

3. Native App Build:
   Build the app as a desktop executable, not a web server.
   For .NET WPF/WinUI, prefer:
   - dotnet publish -c Release -r win-x64 --self-contained false

4. Windows Integration Check:
   Verify that the app includes:
   - alarm trigger argument handling
   - Windows Task Scheduler registration code
   - wake-from-sleep documentation
   - audio volume/unmute integration
   - fullscreen alarm mode

5. Packaging:
   Prepare release artifacts in release_build/.
   Include:
   - executable or publish output
   - README.md
   - setup/build instructions
   - any required default alarm sound assets

6. Report:
   Do not output a localhost URL.
   Instead, report:
   - build success/failure
   - executable path
   - how to run the app
   - any Windows permissions/settings needed