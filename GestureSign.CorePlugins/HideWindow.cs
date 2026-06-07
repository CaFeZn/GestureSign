using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using ManagedWinapi.Windows;

namespace GestureSign.CorePlugins
{
    public class HideWindow : IPlugin
    {
        #region Private Variables

        IHostControl _HostControl = null;

        #endregion

        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.HideWindow.Name"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.HideWindow.Description"); }
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

        public object Icon => IconSource.Window;

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public void ShowGUI(bool IsNew)
        {
            // Nothing to do here
        }

        public bool Gestured(PointInfo ActionPoint)
        {
            try
            {
                var className = ActionPoint.Window.ClassName;
                // Don't attempt to hide shell or tool windows.
                if ("Windows.UI.Core.CoreWindow".Equals(className) ||
                   "ImmersiveBackgroundWindow".Equals(className) ||
                   "ImmersiveLauncher".Equals(className) ||
                   (ActionPoint.Window.ExtendedStyle & WindowExStyleFlags.TOOLWINDOW) == WindowExStyleFlags.TOOLWINDOW)
                    return false;

                ActionPoint.Window.VisibilityFlag = false;
            }
            catch { return false; }
            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            return true;
            // Nothing to do here
        }

        public string Serialize()
        {
            // Nothing to serialize
            return "";
        }

        #endregion

        #region Host Control

        public IHostControl HostControl
        {
            get { return _HostControl; }
            set { _HostControl = value; }
        }

        #endregion
    }
}
