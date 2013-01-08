using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger.Helpers.Hotkeys
{
    public sealed class GlobalHotkeys
    {
        #region singleton

        private static readonly GlobalHotkeys instance = new GlobalHotkeys();

        static GlobalHotkeys()
        {
        }

        private GlobalHotkeys()
        {
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyPressed += new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);
        }

        public static GlobalHotkeys Instance
        {
            get { return instance; }
        }

        #endregion

        private readonly KeyboardHook _keyboardHook;
        
        public void Load()
        {
            if (Settings.Default.HotKeys.Count == 0) return;
            Logger.Instance.WriteGlobal("## Loading Hotkeys ##");
            foreach (var hk in Settings.Default.HotKeys)
            {
                Logger.Instance.WriteGlobal("Register hotkey: {0} - {1}+{2}", hk.Name, hk.Modifier.ToString().Replace(", ", "+"),hk.Key);
                hk.HookId = _keyboardHook.RegisterHotKey(hk.Modifier, hk.Key);

                foreach (var action in hk.Actions)
                {
                    Debug.WriteLine("Initialize Hotkey: {0} {1}", action.Name, action.Version);
                    ActionContainer.GetAction(action.Name, action.Version).OnInitialize(hk);
                }
            }
        }

        /// <summary>
        /// Add a Global Hotkey
        /// </summary>
        /// <param name="hotkey">Hotkey</param>
        /// <returns>True on success</returns>
        public bool Add(Hotkey hotkey)
        {
            int id = 0;
            // Add hotkey to settings
            try
            {
                Settings.Default.HotKeys.Add(hotkey);
                Logger.Instance.WriteGlobal("Register hotkey: {0} - {1}+{2}", hotkey.Name,
                                            hotkey.Modifier.ToString().Replace(", ", "+"), hotkey.Key);
                id = _keyboardHook.RegisterHotKey(hotkey.Modifier, hotkey.Key);
                hotkey.HookId = id;

                foreach (var action in hotkey.Actions)
                {
                    Debug.WriteLine("Initialize Hotkey: {0} {1}", action.Name, action.Version);
                    ActionContainer.GetAction(action.Name, action.Version).OnInitialize(hotkey);
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal("Failed to register hotkey with message: " + ex.Message);
                DebugHelper.Exception(ex);
            }
            return id >= 1;
        }

        /// <summary>
        /// Remove a Global Hotkey
        /// </summary>
        /// <param name="id">Hotkey id</param>
        /// <returns></returns>
        public bool Remove(int id)
        {
            var hk = Settings.Default.HotKeys.FirstOrDefault(x => x.HookId == id);
            if (hk != null)
            {
                _keyboardHook.UnregisterHotkey(id);
                foreach (var action in hk.Actions)
                {
                    Debug.WriteLine("Dispose Hotkey: {0} {1}", action.Name, action.Version);
                    ActionContainer.GetAction(action.Name, action.Version).OnDispose();
                }
                return Settings.Default.HotKeys.Remove(hk);
            }
            return false;
        }

        private void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            Debug.WriteLine("Hotkey pressed: " + e.Modifier.ToString() +"+ "+ e.Key.ToString());
            var hk = Settings.Default.HotKeys.FirstOrDefault(x => x.Modifier == e.Modifier && x.Key == e.Key);
            if (hk != null)
            {
                foreach (var action in hk.Actions)
                {
                    Debug.WriteLine("Calling Hotkey Onpress Event for: {0} {1}", action.Name, action.Version);
                    ActionContainer.GetAction(action.Name, action.Version).OnPressed();
                }
            }
        }

        /// <summary>
        /// Translate Keys to ModifierKeys standard for GlobalHotkeys
        /// </summary>
        /// <param name="keys">Keys as ModifierKeys</param>
        /// <returns>ModifierKeys</returns>
        public static ModifierKeys KeysToModifierKeys(Keys keys)
        {
            var mfk = new ModifierKeys();
            if (keys.HasFlag(Keys.Shift))
                mfk |= ModifierKeys.Shift;
            if (keys.HasFlag(Keys.Control))
                mfk |= ModifierKeys.Control;
            if (keys.HasFlag(Keys.LWin | Keys.RWin))
                mfk |= ModifierKeys.Win;
            if (keys.HasFlag(Keys.Alt))
                mfk |= ModifierKeys.Alt;
            return mfk;
        }
    }
}
