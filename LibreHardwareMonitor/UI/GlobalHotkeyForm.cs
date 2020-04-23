using System;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI
{
    public partial class GlobalHotkeyForm : Form
    {
        private readonly GlobalHotkey _globalHotkey;

        public GlobalHotkeyForm()
        {
            InitializeComponent();
        }

        public GlobalHotkeyForm(GlobalHotkey globalHotkey) : this()
        {
            _globalHotkey = globalHotkey ?? throw new ArgumentNullException(nameof(globalHotkey));
        }
    }
}
