using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins
{
    public class OpenControlPanel : IPlugin
    {
        private IHostControl _hostControl;

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.OpenControlPanel.Name"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.OpenControlPanel.Description"); }
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
            get { return "GestureSign"; }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object Icon
        {
            get { return IconSource.GestureSign; }
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
            if (_hostControl == null || _hostControl.TrayManager == null)
                return false;

            _hostControl.TrayManager.OpenControlPanel();
            return true;
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
