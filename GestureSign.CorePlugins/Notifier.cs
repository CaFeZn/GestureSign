using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using System.Windows.Controls;

namespace GestureSign.CorePlugins
{
    public class Notifier : IPlugin
    {
        private const int DefaultTimeout = 3000;

        private NotifierSettings _settings = new NotifierSettings();
        private NotifierControl _gui;

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.Notifier.Name"); }
        }

        public string Description
        {
            get
            {
                string message = string.IsNullOrWhiteSpace(_settings.Message)
                    ? LocalizationProvider.Instance.GetTextValue("CorePlugins.Notifier.DefaultMessage")
                    : _settings.Message;
                return string.Format(LocalizationProvider.Instance.GetTextValue("CorePlugins.Notifier.Description"), message);
            }
        }

        public object GUI
        {
            get
            {
                if (_gui == null)
                    _gui = new NotifierControl { Settings = _settings };

                return _gui;
            }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public string Category
        {
            get { return "Common"; }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object Icon => IconSource.GestureSign;

        public IHostControl HostControl { get; set; }

        public void Initialize()
        {
        }

        public bool Gestured(PointInfo actionPoint)
        {
            if (HostControl?.TrayManager == null)
                return false;

            NotifierSettings settings = _settings ?? new NotifierSettings();
            string title = string.IsNullOrWhiteSpace(settings.Title)
                ? LocalizationProvider.Instance.GetTextValue("CorePlugins.Notifier.DefaultTitle")
                : settings.Title;
            string message = string.IsNullOrWhiteSpace(settings.Message)
                ? LocalizationProvider.Instance.GetTextValue("CorePlugins.Notifier.DefaultMessage")
                : settings.Message;

            HostControl.TrayManager.ShowNotification(title, message, settings.Timeout);
            return true;
        }

        public bool Deserialize(string serializedData)
        {
            bool result = PluginHelper.DeserializeSettings(serializedData, out _settings);
            NormalizeSettings();
            return result;
        }

        public string Serialize()
        {
            if (_gui != null)
                _settings = _gui.Settings;

            NormalizeSettings();
            return PluginHelper.SerializeSettings(_settings);
        }

        private void NormalizeSettings()
        {
            if (_settings == null)
                _settings = new NotifierSettings();

            _settings.Title = (_settings.Title ?? string.Empty).Trim();
            _settings.Message = (_settings.Message ?? string.Empty).Trim();
            if (_settings.Timeout <= 0)
                _settings.Timeout = DefaultTimeout;
        }

        private class NotifierSettings
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public int Timeout { get; set; } = DefaultTimeout;
        }

        private class NotifierControl : UserControl
        {
            private readonly TextBox _titleTextBox;
            private readonly TextBox _messageTextBox;
            private readonly TextBox _timeoutTextBox;

            public NotifierControl()
            {
                StackPanel panel = new StackPanel();

                panel.Children.Add(CreateLabel("CorePlugins.Notifier.Title"));
                _titleTextBox = new TextBox { Margin = new System.Windows.Thickness(0, 8, 0, 12) };
                panel.Children.Add(_titleTextBox);

                panel.Children.Add(CreateLabel("CorePlugins.Notifier.Message"));
                _messageTextBox = new TextBox
                {
                    AcceptsReturn = true,
                    MaxLines = 4,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Margin = new System.Windows.Thickness(0, 8, 0, 12)
                };
                panel.Children.Add(_messageTextBox);

                panel.Children.Add(CreateLabel("CorePlugins.Notifier.Timeout"));
                _timeoutTextBox = new TextBox
                {
                    Width = 80,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    Margin = new System.Windows.Thickness(0, 8, 0, 0)
                };
                panel.Children.Add(_timeoutTextBox);

                Content = panel;
            }

            public NotifierSettings Settings
            {
                get
                {
                    int timeout;
                    if (!int.TryParse(_timeoutTextBox.Text, out timeout) || timeout <= 0)
                        timeout = DefaultTimeout;

                    return new NotifierSettings
                    {
                        Title = _titleTextBox.Text.Trim(),
                        Message = _messageTextBox.Text.Trim(),
                        Timeout = timeout
                    };
                }
                set
                {
                    NotifierSettings settings = value ?? new NotifierSettings();
                    _titleTextBox.Text = settings.Title ?? string.Empty;
                    _messageTextBox.Text = settings.Message ?? string.Empty;
                    _timeoutTextBox.Text = (settings.Timeout <= 0 ? DefaultTimeout : settings.Timeout).ToString();
                }
            }

            private static TextBlock CreateLabel(string key)
            {
                return new TextBlock
                {
                    Text = LocalizationProvider.Instance.GetTextValue(key),
                    TextWrapping = System.Windows.TextWrapping.Wrap
                };
            }
        }
    }
}
