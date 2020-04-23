// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Drawing;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI
{
    public partial class PortForm : Form
    {
        private readonly HttpServer _server;
        private readonly string _localIp;

        public PortForm()
        {
            InitializeComponent();

            CancelButton = portCancelButton;

            Font = SystemFonts.MessageBoxFont;
        }

        public PortForm(HttpServer server) : this()
        {
            _localIp = GetLocalIP();
            _server = server;
        }

        private void PortNumericUpDnValueChanged(object sender, EventArgs e)
        {
            string url = "http://" + _localIp + ":" + portNumericUpDn.Value + "/";
            webServerLinkLabel.Text = url;
            webServerLinkLabel.Links.Remove(webServerLinkLabel.Links[0]);
            webServerLinkLabel.Links.Add(0, webServerLinkLabel.Text.Length, url);
        }

        private void PortOKButtonClick(object sender, EventArgs e)
        {
            _server.ListenerPort = (int)portNumericUpDn.Value;

            Close();
        }

        private void PortCancelButtonClick(object sender, EventArgs e)
        {
            Close();
        }

        private void PortFormLoad(object sender, EventArgs e)
        {
            portNumericUpDn.Value = _server.ListenerPort;
            PortNumericUpDnValueChanged(null, null);
        }

        private void WebServerLinkLabelLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Link.LinkData.ToString()));
            }
            catch { }
        }

        private static string GetLocalIP()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return "?";
        }
    }
}
