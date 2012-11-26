using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;

namespace YetAnotherRelogger
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
            
        }

        /// <summary>
        /// Add a custom Global Hotkey
        /// </summary>
        /// <param name="name">Hotkey name</param>
        /// <param name="modifier">modifiers</param>
        /// <param name="key">key</param>
        /// <returns>True on success</returns>
        public bool Add(string name, ModifierKeys modifier, Keys key)
        {
            // Create hotkey
            var hk = new Hotkey {Name = name, Modifier = modifier, Key = key, Id = _keyboardHook.RegisterHotKey(modifier, key)};
            // Add hotkey to settings
            Settings.Default.HotKeys.Add(hk);
            return true;
        }

        /// <summary>
        /// Remove a Global Hotkey
        /// </summary>
        /// <param name="id">Hotkey id</param>
        /// <returns></returns>
        public bool Remove(int id)
        {
            _keyboardHook.UnregisterHotkey(id);
            //Settings.Default.
            return true;
        }

        private void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            Debug.WriteLine("Hotkey pressed: " + e.Modifier.ToString() +"+ "+ e.Key.ToString());
            
        }

    }
    public class Hotkey
    {
        [XmlIgnore] public int Id { get; set; }
        public string Name { get; set; }
        public ModifierKeys Modifier { get; set; }
        public Keys Key { get; set; }
    }
}
