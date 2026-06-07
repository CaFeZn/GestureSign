using GestureSign.Common.Localization;
using GestureSign.Common.Applications;
using ManagedWinapi;
using ManagedWinapi.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace GestureSign.CorePlugins
{
    public class KeyboardHelper
    {
        public static void SwitchToNextApplication()
        {
            SendAltTab(false);
        }

        public static void SwitchToPreviousApplication()
        {
            SendAltTab(true);
        }

        public static void SwitchToNextDesktop()
        {
            SendVirtualDesktopSwitch(Keys.Right);
        }

        public static void SwitchToPreviousDesktop()
        {
            SendVirtualDesktopSwitch(Keys.Left);
        }

        private static void SendVirtualDesktopSwitch(Keys directionKey)
        {
            KeyboardKey winKey = new KeyboardKey(Keys.LWin);
            KeyboardKey controlKey = new KeyboardKey(Keys.LControlKey);
            KeyboardKey arrowKey = new KeyboardKey(directionKey);
            bool winPressed = false;
            bool controlPressed = false;

            try
            {
                winKey.Press();
                winPressed = true;
                Thread.Sleep(30);

                controlKey.Press();
                controlPressed = true;
                Thread.Sleep(30);

                arrowKey.PressAndRelease();
                Thread.Sleep(30);
            }
            finally
            {
                if (controlPressed)
                {
                    controlKey.Release();
                    Thread.Sleep(30);
                }

                if (winPressed)
                {
                    winKey.Release();
                    Thread.Sleep(30);
                }
            }
        }

        private static void SendAltTab(bool shift)
        {
            KeyboardKey altKey = new KeyboardKey(Keys.LMenu);
            KeyboardKey shiftKey = new KeyboardKey(Keys.LShiftKey);
            KeyboardKey tabKey = new KeyboardKey(Keys.Tab);
            bool altPressed = false;
            bool shiftPressed = false;

            try
            {
                altKey.Press();
                altPressed = true;
                Thread.Sleep(30);

                if (shift)
                {
                    shiftKey.Press();
                    shiftPressed = true;
                    Thread.Sleep(30);
                }

                tabKey.PressAndRelease();
                Thread.Sleep(30);
            }
            finally
            {
                if (shiftPressed)
                {
                    shiftKey.Release();
                    Thread.Sleep(30);
                }

                if (altPressed)
                {
                    altKey.Release();
                    Thread.Sleep(30);
                }
            }
        }

        public static void ResetKeyState(SystemWindow targetWindow, params Keys[] keys)
        {
            if (keys == null || keys.Length == 0)
                return;

            var shieldWindow = new NativeWindow();
            List<KeyboardKey> keyList = new List<KeyboardKey>(keys.Select(k => new KeyboardKey(k)));
            List<KeyboardKey> failureList = new List<KeyboardKey>(keyList);

            try
            {
                shieldWindow.CreateHandle(new CreateParams() { ExStyle = (int)(WindowExStyleFlags.TOOLWINDOW | WindowExStyleFlags.LAYERED) });

                InputSimulator simulator = new InputSimulator();
                SystemWindow.ForegroundWindow = new SystemWindow(shieldWindow.Handle)
                {
                    WindowState = FormWindowState.Normal
                };

                for (int i = 0; i < keys.Length; i++)
                {
                    if (!Enum.IsDefined(typeof(VirtualKeyCode), keys[i].GetHashCode())) continue;
                    simulator.Keyboard.KeyUp((VirtualKeyCode)keys[i]).Sleep(10);

                    if (keyList[i].IsGloballyPressed)
                    {
                        keyList[i].Release();
                        Thread.Sleep(10);
                        if (!keyList[i].IsGloballyPressed)
                            failureList.Remove(keyList[i]);
                    }
                    else failureList.Remove(keyList[i]);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (targetWindow != null && !ApplicationManager.IsShellUiWindow(targetWindow))
                    SystemWindow.ForegroundWindow = targetWindow;
                shieldWindow.DestroyHandle();

                if (failureList.Count != 0)
                {
                    string keysName = string.Join("+ ", keyList.Select(k => k.KeyName));
                    string message = string.Format(LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.FailureMessage"), keysName);

                    string failurekeysName = string.Join(", ", failureList.Select(k => k.KeyName));
                    message += "\r\n" + string.Format(LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.ResetFailure"), failurekeysName);

                    MessageBox.Show(message, LocalizationProvider.Instance.GetTextValue("Messages.Error"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }

        public static void ReleaseKeyState(params Keys[] keys)
        {
            if (keys == null || keys.Length == 0)
                return;

            InputSimulator simulator = new InputSimulator();
            foreach (var key in keys)
            {
                if (!Enum.IsDefined(typeof(VirtualKeyCode), key.GetHashCode()))
                    continue;

                simulator.Keyboard.KeyUp((VirtualKeyCode)key).Sleep(10);
                var keyboardKey = new KeyboardKey(key);
                if (keyboardKey.IsGloballyPressed)
                {
                    keyboardKey.Release();
                    Thread.Sleep(10);
                }
            }
        }
    }
}
