// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using LibreHardwareMonitor.UI;

namespace LibreHardwareMonitor
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            if (!AllRequiredFilesAvailable())
            {
                Environment.Exit(0);
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Application.ThreadException += ApplicationOnThreadException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (MainForm form = new MainForm())
            {
                form.FormClosed += delegate
                {
                    Application.Exit();
                };
                Application.Run();
            }
        }

        private static void ApplicationOnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show("Unexpected error occurred." + "\nError details:\n" + RenderException(e.Exception), "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                string errorDetails = string.Format("Unhandled exception occurred! IsTerminating: {0}, AppDomainId={1}.", e.IsTerminating, AppDomain.CurrentDomain.Id);
                MessageBox.Show(errorDetails + "\nError details:\n" + RenderException(exception), "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                string errorDetails = string.Format("CurrentDomain_UnhandledException! Error: {0}, IsTerminating: {1}, AppDomainId={2}.", e.ExceptionObject, e.IsTerminating, AppDomain.CurrentDomain.Id);
                MessageBox.Show(errorDetails, "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string RenderException(Exception e)
        {
            return e.Message;
        }

        private static bool IsFileAvailable(string fileName)
        {
            string path = Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar;
            if (!File.Exists(path + fileName))
            {
                MessageBox.Show("The following file could not be found: " + fileName + "\nPlease extract all files from the archive.", "Error",
                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private static bool AllRequiredFilesAvailable()
        {
            if (!IsFileAvailable("Aga.Controls.dll"))
                return false;

            if (!IsFileAvailable("LibreHardwareMonitorLib.dll"))
                return false;

            if (!IsFileAvailable("OxyPlot.dll"))
                return false;

            if (!IsFileAvailable("OxyPlot.WindowsForms.dll"))
                return false;

            return true;
        }
    }
}
