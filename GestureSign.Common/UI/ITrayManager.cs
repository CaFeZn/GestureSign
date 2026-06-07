using System;
namespace GestureSign.Common.UI
{
	public interface ITrayManager
	{
		//void EnterUserDefinedMode();
		void ToggleDisableGestures();
		void OpenControlPanel();
        void ShowNotification(string title, string message, int timeout);
	}
}
