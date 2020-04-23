using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI
{
    public partial class GlobalHotkeyForm : Form
    {
        private readonly KeysConverter _keysConverter = new KeysConverter();

        private readonly GlobalHotkey _globalHotkey;

        public GlobalHotkeyForm()
        {
            InitializeComponent();

            Font = SystemFonts.MessageBoxFont;
        }

        public GlobalHotkeyForm(GlobalHotkey globalHotkey) : this()
        {
            _globalHotkey = globalHotkey ?? throw new ArgumentNullException(nameof(globalHotkey));

            _globalHotkey.Stop();

            foreach (GlobalHotkeyItem hotkeyItem in _globalHotkey.Items)
            {
                (Keys modifiers, Keys code) = SplitKeyData(hotkeyItem.HotKey);

                if (hotkeyItem.UniqueId == "ShowHideHotKey")
                {
                    chkShowHideHotKey.Checked = hotkeyItem.Enabled;
                    txtShowHideHotKey.Tag = hotkeyItem.HotKey;
                    txtShowHideHotKey.Text = ToHumanReadable(modifiers, code);

                }
                else if (hotkeyItem.UniqueId == "RtssHotKey")
                {
                    chkEnableRtssServiceHotKey.Checked = hotkeyItem.Enabled;
                    txtEnableRtssServiceHotKey.Tag = hotkeyItem.HotKey;
                    txtEnableRtssServiceHotKey.Text = ToHumanReadable(modifiers, code);
                }
            }
        }

        private void GlobalHotkeyFormClosed(object sender, FormClosedEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                foreach (GlobalHotkeyItem hotkeyItem in _globalHotkey.Items)
                {
                    if (hotkeyItem.UniqueId == "ShowHideHotKey")
                    {
                        hotkeyItem.Enabled = chkShowHideHotKey.Checked;
                        hotkeyItem.HotKey = (Keys)txtShowHideHotKey.Tag;

                    }
                    else if (hotkeyItem.UniqueId == "RtssHotKey")
                    {
                        hotkeyItem.Enabled = chkEnableRtssServiceHotKey.Checked;
                        hotkeyItem.HotKey = (Keys)txtEnableRtssServiceHotKey.Tag;
                    }
                }
            }

            _globalHotkey.Start();
        }

        private void ShowHideHotKeyCheckedChanged(object sender, EventArgs e)
        {
            txtShowHideHotKey.Enabled = chkShowHideHotKey.Checked;
        }

        private void EnableRtssServiceHotKeyCheckedChanged(object sender, EventArgs e)
        {
            txtEnableRtssServiceHotKey.Enabled = chkEnableRtssServiceHotKey.Checked;
        }

        private void GlobalHotkeyFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey ||
                e.KeyCode == Keys.ShiftKey ||
                e.KeyCode == Keys.Menu ||
                e.KeyCode == Keys.Apps ||
                e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin
                )
            {
                e.SuppressKeyPress = true;
                return;
            }

            if (FindFocusedControl(this) is TextBox textBox && textBox.Enabled)
            {
                Keys modifiers = e.Modifiers;
                Keys keyCode = e.KeyCode;

                textBox.Tag = e.KeyData;
                textBox.Text = ToHumanReadable(modifiers, keyCode);

                e.SuppressKeyPress = true;
            }
        }

        private (Keys Modifiers, Keys KeyCode) SplitKeyData(Keys keyData)
        {
            var mod = 
                (keyData & Keys.Control) |
                (keyData & Keys.Alt) |
                (keyData & Keys.Shift);

            var key = keyData & ~Keys.Control & ~Keys.Alt & ~Keys.Shift;

            return (mod, key);
        }

        private string ToHumanReadable(Keys modifiers, Keys keyCode)
        {
            // Ctrl+Alt+Shift+Key like

            string r = string.Empty;

            if (modifiers.HasFlag(Keys.Control))
                r += "Ctrl+";

            if (modifiers.HasFlag(Keys.Alt))
                r += "Alt+";

            if (modifiers.HasFlag(Keys.Shift))
                r += "Shift+";

            if (keyCode == Keys.Space)
            {
                r += "Space";
            }
            else
            {
                char scanCode = (char)MapVirtualKey((uint)keyCode, MapVirtualKeyMapTypes.MAPVK_VK_TO_CHAR);

                if (char.IsControl(scanCode))
                {
                    r += _keysConverter.ConvertToString(keyCode);
                }
                else
                {
                    r += ToUnicode(keyCode).ToUpper();
                }
            }

            return r;
        }

        private static Control FindFocusedControl(Control control)
        {
            ContainerControl container = control as ContainerControl;
            while (container != null)
            {
                control = container.ActiveControl;
                container = control as ContainerControl;
            }
            return control;
        }

        private static string ToUnicode(Keys keyCode)
        {
            StringBuilder charPressed = new StringBuilder(256);
            IntPtr inputLocaleIdentifier = GetKeyboardLayout(0);

            uint virtualKeyCode = (uint)keyCode;
            uint scanCode = MapVirtualKey(virtualKeyCode, MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC);

            byte[] keyboardState = new byte[0];
            ToUnicode(virtualKeyCode, scanCode, keyboardState, charPressed, charPressed.Capacity, 0); // , inputLocaleIdentifier
            return charPressed.ToString();
        }

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, MapVirtualKeyMapTypes uMapType);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        /// <summary>
        /// The set of valid MapTypes used in MapVirtualKey
        /// </summary>
        public enum MapVirtualKeyMapTypes : uint
        {
            /// <summary>
            /// uCode is a virtual-key code and is translated into a scan code.
            /// If it is a virtual-key code that does not distinguish between left- and
            /// right-hand keys, the left-hand scan code is returned.
            /// If there is no translation, the function returns 0.
            /// </summary>
            MAPVK_VK_TO_VSC = 0x00,

            /// <summary>
            /// uCode is a scan code and is translated into a virtual-key code that
            /// does not distinguish between left- and right-hand keys. If there is no
            /// translation, the function returns 0.
            /// </summary>
            MAPVK_VSC_TO_VK = 0x01,

            /// <summary>
            /// uCode is a virtual-key code and is translated into an unshifted
            /// character value in the low-order word of the return value. Dead keys (diacritics)
            /// are indicated by setting the top bit of the return value. If there is no
            /// translation, the function returns 0.
            /// </summary>
            MAPVK_VK_TO_CHAR = 0x02,

            /// <summary>
            /// Windows NT/2000/XP: uCode is a scan code and is translated into a
            /// virtual-key code that distinguishes between left- and right-hand keys. If
            /// there is no translation, the function returns 0.
            /// </summary>
            MAPVK_VSC_TO_VK_EX = 0x03,
        }
    }
}
