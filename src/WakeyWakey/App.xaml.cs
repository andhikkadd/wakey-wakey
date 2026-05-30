using System;
using System.Linq;
using System.Threading;
using System.Windows;
using WakeyWakey.Views;
using WakeyWakey.Services;

namespace WakeyWakey
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private const string MutexName = "Global\\WakeyWakeyMutex";
        private static ApiService? _apiService;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool isTriggerMode = false;
            bool isTestMode = false;
            Guid alarmId = Guid.Empty;

            // Simple command line arguments parsing
            for (int i = 0; i < e.Args.Length; i++)
            {
                if (e.Args[i] == "--trigger-alarm" && i + 1 < e.Args.Length)
                {
                    isTriggerMode = true;
                    Guid.TryParse(e.Args[i + 1], out alarmId);
                }
                else if (e.Args[i] == "--test-alarm")
                {
                    isTestMode = true;
                    if (i + 1 < e.Args.Length)
                    {
                        Guid.TryParse(e.Args[i + 1], out alarmId);
                    }
                }
            }

            // Normal UI mode check - enforce single instance
            if (!isTriggerMode && !isTestMode)
            {
                _mutex = new Mutex(true, MutexName, out bool createdNew);
                if (!createdNew)
                {
                    // An instance is already running, activate it and exit
                    NativeMethods.BringExistingInstanceToFront();
                    Shutdown();
                    return;
                }
            }

            base.OnStartup(e);

            if (isTriggerMode || isTestMode)
            {
                // Run in Alarm trigger mode
                var alarmWindow = new AlarmWindow(alarmId, isTestMode);
                alarmWindow.Show();
            }
            else
            {
                // Run in Normal Dashboard mode
                _apiService = new ApiService();
                _apiService.Start();

                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _apiService?.Stop();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
