using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using ManagedWinapi;
using System.Collections.Generic;
using System.Linq;

namespace GestureSign.CorePlugins.HotKey
{
    public class HotKeyPlugin : IPlugin
    {
        #region Private Variables

        private HotKey _GUI;
        private HotKeySettings _Settings;
        private const string User32 = "user32.dll";
        private readonly string _exceptionWindow = "Microsoft Edge";

        #endregion

        #region PInvoke Declarations

        [DllImport(User32)]
        private static extern bool LockWorkStation();

        [DllImport(User32)]
        private static extern int GetKeyNameText(int lParam, [Out] StringBuilder lpString, int nSize);

        [DllImport(User32)]
        private static extern int MapVirtualKey(int uCode, int uMapType);

        #endregion


        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.Name"); }
        }

        public string Description
        {
            get { return GetDescription(_Settings); }
        }

        public object GUI
        {
            get { return _GUI ?? (_GUI = CreateGUI()); }
        }

        public bool ActivateWindowDefault
        {
            get { return true; }
        }

        public HotKey TypedGUI
        {
            get { return (HotKey)GUI; }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.Category"); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object Icon => IconSource.Keyboard;

        #endregion

        #region Public Methods

        public static string GetKeyName(Keys key)
        {
            bool extended;
            switch (key)
            {
                case Keys.LControlKey:
                    return "Left Ctrl";
                case Keys.RControlKey:
                    return "Right Ctrl";
                case Keys.LShiftKey:
                    return "Left Shift";
                case Keys.RShiftKey:
                    return "Right Shift";
                case Keys.LMenu:
                    return "Left Alt";
                case Keys.RMenu:
                    return "Right Alt";
                case Keys.LWin:
                    return "Left Windows";
                case Keys.RWin:
                    return "Right Windows";
                case Keys.VolumeDown:
                case Keys.VolumeMute:
                case Keys.VolumeUp:
                case Keys.MediaNextTrack:
                case Keys.MediaPlayPause:
                case Keys.MediaPreviousTrack:
                case Keys.MediaStop:
                case Keys.BrowserBack:
                case Keys.BrowserForward:
                case Keys.BrowserHome:
                case Keys.BrowserRefresh:
                case Keys.BrowserSearch:
                case Keys.BrowserStop:
                    return key.ToString();
                case Keys.Insert:
                case Keys.Delete:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    extended = true;
                    break;
                default:
                    extended = false;
                    break;
            }
            StringBuilder sb = new StringBuilder(64);
            int scancode = MapVirtualKey((int)key, 0);
            if (extended)
                scancode += 0x100;
            GetKeyNameText(scancode << 16, sb, sb.Capacity);
            if (sb.Length == 0)
            {
                switch (key)
                {
                    case Keys.BrowserBack:
                        sb.Append("Back");
                        break;
                    case Keys.BrowserForward:
                        sb.Append("Forward");
                        break;
                    case (Keys)19:
                        sb.Append("Break");
                        break;
                    case Keys.Apps:
                        sb.Append("ContextMenu");
                        break;
                    case Keys.LControlKey:
                        sb.Append("Left Ctrl");
                        break;
                    case Keys.RControlKey:
                        sb.Append("Right Ctrl");
                        break;
                    case Keys.LShiftKey:
                        sb.Append("Left Shift");
                        break;
                    case Keys.RShiftKey:
                        sb.Append("Right Shift");
                        break;
                    case Keys.LMenu:
                        sb.Append("Left Alt");
                        break;
                    case Keys.RMenu:
                        sb.Append("Right Alt");
                        break;
                    case Keys.LWin:
                        sb.Append("Left Windows");
                        break;
                    case Keys.RWin:
                        sb.Append("Right Windows");
                        break;
                    case Keys.PrintScreen:
                        sb.Append("PrintScreen");
                        break;
                }
            }
            return sb.ToString();
        }
        public void Initialize()
        {

        }

        public bool Gestured(PointInfo ActionPoint)
        {
            try
            {
                if (_Settings == null)
                    return false;
                if (_Settings.Windows &&
                  _Settings.KeyCode.Count != 0 && _Settings.KeyCode[0] == Keys.L)
                {
                    LockWorkStation();
                    return true;
                }

                if (ActionPoint.Window.Title.Contains(_exceptionWindow))
                {
                    SendKeysSeparately(_Settings);
                }
                else
                    SendShortcutKeys(_Settings);
            }
            catch (Exception)
            {
                var keyList = new List<Keys>();
                if (_Settings.Shift)
                    keyList.Add(Keys.LShiftKey);
                if (_Settings.Alt)
                    keyList.Add(Keys.LMenu);
                if (_Settings.Control)
                    keyList.Add(Keys.LControlKey);
                if (_Settings.Windows)
                    keyList.Add(Keys.LWin);

                keyList.AddRange(_Settings.KeyCode);

                KeyboardHelper.ResetKeyState(ActionPoint.Window, keyList.ToArray());
            }
            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            return PluginHelper.DeserializeSettings(SerializedData, out _Settings);
        }

        public string Serialize()
        {
            if (_GUI != null)
                _Settings = _GUI.Settings;

            if (_Settings == null)
                _Settings = new HotKeySettings();

            return PluginHelper.SerializeSettings(_Settings);
        }

        #endregion

        #region Private Methods

        private HotKey CreateGUI()
        {
            HotKey newGUI = new HotKey();

            newGUI.Loaded += (o, e) =>
            {
                TypedGUI.Settings = _Settings;
                TypedGUI.HostControl = HostControl;
            };

            return newGUI;
        }

        public static string GetDescription(HotKeySettings Settings)
        {
            if (Settings == null || Settings.KeyCode == null)
                return LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.Description");

            // Create string to store key combination and final output description
            string strKeyCombo = "";
            string strFormattedOutput = LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.SpecificDescription");

            // Build output string
            if (Settings.Windows)
                strKeyCombo = "Win + ";

            if (Settings.Control)
                strKeyCombo += "Ctrl + ";

            if (Settings.Alt)
                strKeyCombo += "Alt + ";

            if (Settings.Shift)
                strKeyCombo += "Shift + ";
            if (Settings.KeyCode.Count != 0)
            {
                foreach (var k in Settings.KeyCode)
                    strKeyCombo += GetKeyName(k) + " + ";
            }
            strKeyCombo = strKeyCombo.TrimEnd(' ', '+');

            // Return final formatted string
            return String.Format(strFormattedOutput, strKeyCombo);
        }

        private void SendKeysSeparately(HotKeySettings settings)
        {
            InputSimulator simulator = new InputSimulator();
            List<VirtualKeyCode> modifiedKeys;
            List<VirtualKeyCode> keys;
            SplitShortcutKeys(settings, out modifiedKeys, out keys);

            PressModifiers(simulator, modifiedKeys);
            try
            {
                foreach (var key in keys)
                    simulator.Keyboard.KeyPress(key).Sleep(30);
            }
            finally
            {
                ReleaseModifiers(simulator, modifiedKeys);
            }
        }

        private void SendShortcutKeys(HotKeySettings settings)
        {
            if (settings.SendByKeybdEvent)
            {
                List<VirtualKeyCode> ignoredModifiedKeys;
                List<VirtualKeyCode> ignoredKeys;
                SplitShortcutKeys(settings, out ignoredModifiedKeys, out ignoredKeys);
                var modifiedKeys = ignoredModifiedKeys.Select(k => new KeyboardKey((Keys)k)).ToList();
                var keys = ignoredKeys.Select(k => new KeyboardKey((Keys)k)).ToList();

                foreach (var modifierKey in modifiedKeys)
                    modifierKey.Press();

                try
                {
                    foreach (var key in keys)
                        if (!String.IsNullOrEmpty(key.KeyName))
                            key.PressAndRelease();
                }
                finally
                {
                    for (int i = modifiedKeys.Count - 1; i >= 0; i--)
                        modifiedKeys[i].Release();
                }
            }
            else
            {
                InputSimulator simulator = new InputSimulator();
                List<VirtualKeyCode> modifiedKeys = new List<VirtualKeyCode>();
                List<VirtualKeyCode> keys = new List<VirtualKeyCode>();
                SplitShortcutKeys(settings, out modifiedKeys, out keys);

                if (modifiedKeys.Count == 0)
                {
                    if (keys.Count != 0)
                    {
                        simulator.Keyboard.KeyPress(keys.ToArray()).Sleep(30);
                    }
                }
                else
                {
                    if (keys.Count != 0)
                    {
                        simulator.Keyboard.ModifiedKeyStroke(modifiedKeys, keys).Sleep(30);
                    }
                    else
                    {
                        PressModifiers(simulator, modifiedKeys);
                        ReleaseModifiers(simulator, modifiedKeys);
                    }
                }
            }
        }

        private static void SplitShortcutKeys(HotKeySettings settings, out List<VirtualKeyCode> modifiedKeys, out List<VirtualKeyCode> keys)
        {
            modifiedKeys = new List<VirtualKeyCode>();
            keys = new List<VirtualKeyCode>();

            if (settings.Windows)
                AddDistinct(modifiedKeys, VirtualKeyCode.LWIN);
            if (settings.Control)
                AddDistinct(modifiedKeys, VirtualKeyCode.LCONTROL);
            if (settings.Alt)
                AddDistinct(modifiedKeys, VirtualKeyCode.LMENU);
            if (settings.Shift)
                AddDistinct(modifiedKeys, VirtualKeyCode.LSHIFT);

            if (settings.KeyCode == null)
                return;

            foreach (var k in settings.KeyCode)
            {
                if (!Enum.IsDefined(typeof(VirtualKeyCode), k.GetHashCode()))
                    continue;

                var key = (VirtualKeyCode)k;
                if (IsModifierKey(key))
                    AddDistinct(modifiedKeys, key);
                else
                    keys.Add(key);
            }
        }

        private static bool IsModifierKey(VirtualKeyCode key)
        {
            return key == VirtualKeyCode.LWIN || key == VirtualKeyCode.RWIN ||
                key == VirtualKeyCode.LCONTROL || key == VirtualKeyCode.RCONTROL ||
                key == VirtualKeyCode.LMENU || key == VirtualKeyCode.RMENU ||
                key == VirtualKeyCode.LSHIFT || key == VirtualKeyCode.RSHIFT;
        }

        private static void AddDistinct(List<VirtualKeyCode> keys, VirtualKeyCode key)
        {
            if (!keys.Contains(key))
                keys.Add(key);
        }

        private static void PressModifiers(InputSimulator simulator, List<VirtualKeyCode> modifiedKeys)
        {
            foreach (var key in modifiedKeys)
                simulator.Keyboard.KeyDown(key).Sleep(30);
        }

        private static void ReleaseModifiers(InputSimulator simulator, List<VirtualKeyCode> modifiedKeys)
        {
            for (int i = modifiedKeys.Count - 1; i >= 0; i--)
                simulator.Keyboard.KeyUp(modifiedKeys[i]).Sleep(30);
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}
