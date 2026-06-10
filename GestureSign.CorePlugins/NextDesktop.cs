using System;
using System.Windows.Forms;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins
{
    public class NextDesktop : IPlugin
    {
        private IHostControl _hostControl;

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.NextDesktop.Name"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.NextDesktop.Description"); }
        }

        public object GUI
        {
            get { return null; }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public string Category
        {
            get { return "Windows"; }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object Icon
        {
            get { return IconSource.Window; }
        }

        public IHostControl HostControl
        {
            get { return _hostControl; }
            set { _hostControl = value; }
        }

        public void Initialize()
        {
        }

        public bool Gestured(PointInfo actionPoint)
        {
            try
            {
                KeyboardHelper.SwitchToNextDesktop();
                KeyboardHelper.ResetKeyStatePreserveForegroundWindow(Keys.LWin, Keys.RWin, Keys.LControlKey, Keys.RControlKey);
                return true;
            }
            catch (Exception)
            {
                KeyboardHelper.ResetKeyStatePreserveForegroundWindow(Keys.LWin, Keys.RWin, Keys.LControlKey, Keys.RControlKey);
                return false;
            }
        }

        public bool Deserialize(string serializedData)
        {
            return true;
        }

        public string Serialize()
        {
            return string.Empty;
        }
    }
}
