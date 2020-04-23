using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LibreHardwareMonitor.Utilities;
using Open.WinKeyboardHook;

namespace LibreHardwareMonitor.UI
{
    public class GlobalHotkeyItem
    {
        public string UniqueId { get; }

        public Action OnAction { get; }

        public bool Enabled { get; set; }

        public Keys HotKey { get; set; }

        public GlobalHotkeyItem(string uniqueId, Action action)
        {
            UniqueId = uniqueId ?? throw new ArgumentNullException(nameof(uniqueId));
            OnAction = action ?? throw new ArgumentNullException(nameof(action));
        }

        public bool TryHandle(Keys keyData, bool pre)
        {
            if (!Enabled || HotKey != keyData)
                return false;

            if (!pre)
            {
                OnAction();
            }

            return true;
        }
    }

    public sealed class GlobalHotkeys
    {
        private readonly PersistentSettings _settings;
        private readonly IKeyboardInterceptor _interceptor;
        private readonly List<GlobalHotkeyItem> _hotkeyItems = new List<GlobalHotkeyItem>();

        public IReadOnlyCollection<GlobalHotkeyItem> Items
        {
            get => _hotkeyItems;
        }

        public GlobalHotkeys(PersistentSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _interceptor = new KeyboardInterceptor();
            _interceptor.KeyUp += OnGlobalHotKey;
            _interceptor.KeyDown += OnPreGlobalHotKey;
        }

        private void OnPreGlobalHotKey(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = _hotkeyItems.Any(i => i.TryHandle(e.KeyData, true));
        }

        private void OnGlobalHotKey(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = _hotkeyItems.Any(i => i.TryHandle(e.KeyData, false));
        }

        public void SetHotKeys(IEnumerable<(string UniqueId, Action Action)> items)
        {
            foreach (var tuple in items)
            {
                if (_hotkeyItems.Any(i => i.UniqueId == tuple.UniqueId))
                {
                    throw new InvalidOperationException("Save global hotkey already registered!");
                }

                GlobalHotkeyItem globalHotkeyItem = new GlobalHotkeyItem(tuple.UniqueId, tuple.Action);

                globalHotkeyItem.Enabled = _settings.GetValue(SettingKey(globalHotkeyItem) + "Enabled", false);
                globalHotkeyItem.HotKey =
                    (Keys)_settings.GetValue(SettingKey(globalHotkeyItem) + "HotKey", (int)Keys.None);

                _hotkeyItems.Add(globalHotkeyItem);
            }
        }

        public void Start()
        {
            _interceptor.StartCapturing();
        }

        public void Stop()
        {
            _interceptor.StopCapturing();
        }

        public void SaveCurrentSettings()
        {
            foreach (GlobalHotkeyItem globalHotkeyItem in _hotkeyItems)
            {
                _settings.SetValue(SettingKey(globalHotkeyItem) + "Enabled", globalHotkeyItem.Enabled);
                _settings.SetValue(SettingKey(globalHotkeyItem) + "HotKey", (int)globalHotkeyItem.HotKey);
            }
        }

        private static string SettingKey(GlobalHotkeyItem item)
        {
            return $"GlobalHotkeys.{item.UniqueId}.";
        }
    }
}
