using System;
using System.Diagnostics;
using System.IO;
using LibreHardwareMonitor.Utilities;
using Microsoft.Win32;

namespace LibreHardwareMonitor.Rtss
{
    public class RtssService
    {
        private readonly PersistentSettings _settings;

        public bool IsRunning
        {
            get => Process.GetProcessesByName("RTSS").Length > 0;
        }

        public bool IsAvailable
        {
            get => !string.IsNullOrWhiteSpace(_rtssServiceLocation) && File.Exists(_rtssServiceLocation);
        }

        private string _rtssServiceLocation;

        public string RtssServiceLocation
        {
            get { return _rtssServiceLocation; }
            set
            {
                _rtssServiceLocation = value;
                _settings.SetValue("RtssService.RtssServiceLocation", value);
            }
        }

        public RtssService(PersistentSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _rtssServiceLocation = _settings.GetValue("RtssService.RtssServiceLocation", FindDefaultLocation());
        }

        private static string FindDefaultLocation()
        {
            // check registry

            string installPath = string.Empty;

            try
            {
                // SOFTWARE\WOW6432Node\Unwinder\RTSS
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\WOW6432Node\\Unwinder\\RTSS"))
                {
                    if (key != null)
                    {
                        object o = key.GetValue("InstallPath");
                        if (o != null)
                        {
                            installPath = o as string;  //"as" because it's REG_SZ...otherwise ToString() might be safe(r)
                        }
                    }
                }

                // SOFTWARE\Unwinder\RTSS
                if (string.IsNullOrWhiteSpace(installPath))
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Unwinder\\RTSS"))
                    {
                        if (key != null)
                        {
                            object o = key.GetValue("InstallPath");
                            if (o != null)
                            {
                                installPath = o as string;  //"as" because it's REG_SZ...otherwise ToString() might be safe(r)
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            if (string.IsNullOrWhiteSpace(installPath) || !File.Exists(installPath))
            {
                // check default installation path
                //C:\Program Files (x86)\RivaTuner Statistics Server\RTSS.exe
                //C:\Program Files\RivaTuner Statistics Server\RTSS.exe

                if (File.Exists(@"C:\Program Files (x86)\RivaTuner Statistics Server\RTSS.exe"))
                    installPath = @"C:\Program Files (x86)\RivaTuner Statistics Server\RTSS.exe";
                else if (File.Exists(@"C:\Program Files\RivaTuner Statistics Server\RTSS.exe"))
                    installPath = @"C:\Program Files\RivaTuner Statistics Server\RTSS.exe";
            }

            return installPath;
        }

        public bool TryRun()
        {
            if (IsRunning)
                return true;

            if (!IsAvailable)
                return false;

            try
            {
                Process proc = new Process
                {
                    StartInfo =
                    {
                        FileName = RtssServiceLocation,
                        UseShellExecute = false,
                        Verb = "runas"
                    }
                };
                proc.Start();

                return IsRunning;
            }
            catch (Exception) { }

            return false;
        }
    }
}
