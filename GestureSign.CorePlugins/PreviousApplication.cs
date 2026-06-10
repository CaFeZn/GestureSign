using System;
using GestureSign.Common.Plugins;
using GestureSign.Common.Localization;
using System.Windows.Forms;

namespace GestureSign.CorePlugins
{
    public class PreviousApplication : IPlugin
    {
        #region Private Variables

        IHostControl _HostControl = null;

        #endregion

        #region IAction Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.PreviousApplication.Name"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.PreviousApplication.Description"); }
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

        #region IAction Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo ActionPoint)
        {
            try
            {
                KeyboardHelper.SwitchToPreviousApplication();
                KeyboardHelper.ResetKeyStatePreserveForegroundWindow(Keys.LMenu, Keys.RMenu, Keys.LShiftKey, Keys.RShiftKey);
            }
            catch (Exception)
            {
                KeyboardHelper.ResetKeyStatePreserveForegroundWindow(Keys.LMenu, Keys.RMenu, Keys.LShiftKey, Keys.RShiftKey);

                return false;
            }
            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            return true;
            // Nothing to deserialize
        }

        public string Serialize()
        {
            // Nothing to serialize, send empty string
            return "";
        }

        public void ShowGUI(bool IsNew)
        {
            // Nothing to do here
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
