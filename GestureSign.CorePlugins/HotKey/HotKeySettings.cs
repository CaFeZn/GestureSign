using System.Collections.Generic;
using System.Windows.Forms;
using GestureSign.Common.Localization;

namespace GestureSign.CorePlugins.HotKey
{
    public class HotKeySettings
    {
        #region Public Properties

        public bool Windows { get; set; }

        public bool Control { get; set; }

        public bool Shift { get; set; }

        public bool Alt { get; set; }

        public List<Keys> KeyCode { get; set; }

        public bool SendByKeybdEvent { get; set; }

        #endregion
    }

    public class ExtraKeysDescription
    {
        static ExtraKeysDescription()
        {
            DescriptionDict = new Dictionary<Keys, string>(14)
            {
                {Keys.None, LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.AddExtraKey")}
            };

            AddDescription(Keys.BrowserBack, Keys.BrowserForward, Keys.BrowserHome, Keys.BrowserRefresh,
                Keys.BrowserSearch, Keys.BrowserStop,
                Keys.MediaNextTrack, Keys.MediaPlayPause, Keys.MediaPreviousTrack,
                Keys.MediaStop, Keys.VolumeDown, Keys.VolumeMute, Keys.VolumeUp,
                Keys.PageDown, Keys.PageUp, Keys.PrintScreen, Keys.Scroll,
                Keys.LControlKey, Keys.RControlKey, Keys.LShiftKey, Keys.RShiftKey,
                Keys.LMenu, Keys.RMenu, Keys.LWin, Keys.RWin);

            AddKeys(Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6,
                Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12,
                Keys.F13, Keys.F14, Keys.F15, Keys.F16, Keys.F17, Keys.F18,
                Keys.F19, Keys.F20, Keys.F21, Keys.F22, Keys.F23, Keys.F24,
                Keys.Home, Keys.End, Keys.Insert, Keys.Pause);
        }
        public static Dictionary<Keys, string> DescriptionDict { get; private set; }

        private static void AddDescription(params Keys[] keys)
        {
            foreach (Keys code in keys)
                DescriptionDict.Add(code,
                    LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.ExtraKeys." + code));
        }

        private static void AddKeys(params Keys[] keys)
        {
            foreach (Keys key in keys)
                DescriptionDict.Add(key, key.ToString());
        }
    }
}
