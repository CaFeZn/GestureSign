using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;

namespace GestureSign.CorePlugins
{
    public class OpenClipboardText : IPlugin
    {
        private const string SearchUrlPrefix = "https://www.google.com/search?q=";
        private static readonly Regex DomainLikePattern = new Regex(
            @"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z][a-z0-9-]{1,62}(?::\d{1,5})?(?:[/?#].*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.OpenClipboardText.Name"); }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.OpenClipboardText.Category"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.OpenClipboardText.Description"); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object GUI
        {
            get { return null; }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public object Icon => IconSource.Browser;

        public IHostControl HostControl { get; set; }

        public void Initialize()
        {
        }

        public bool Gestured(PointInfo actionPoint)
        {
            string clipboardText = GetClipboardText(actionPoint);
            if (string.IsNullOrWhiteSpace(clipboardText))
                return false;

            try
            {
                string url = BuildUrl(clipboardText);
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return true;
            }
            catch
            {
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

        private static string GetClipboardText(PointInfo actionPoint)
        {
            string text = string.Empty;
            try
            {
                actionPoint.Invoke(() =>
                {
                    if (Clipboard.ContainsText())
                        text = Clipboard.GetText();
                });
            }
            catch
            {
            }

            return text == null ? string.Empty : text.Trim();
        }

        private static string BuildUrl(string text)
        {
            text = text.Trim();
            Uri uri;
            if (TryGetWebUri(text, out uri))
                return uri.AbsoluteUri;

            return SearchUrlPrefix + Uri.EscapeDataString(text);
        }

        private static bool TryGetWebUri(string text, out Uri uri)
        {
            if (Uri.TryCreate(text, UriKind.Absolute, out uri) && IsHttpOrHttps(uri))
                return true;

            if (DomainLikePattern.IsMatch(text) && Uri.TryCreate("https://" + text, UriKind.Absolute, out uri))
                return true;

            uri = null;
            return false;
        }

        private static bool IsHttpOrHttps(Uri uri)
        {
            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }
    }
}
