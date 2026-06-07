using System;
using System.Collections.Generic;
using System.Linq;
using GestureSign.Common.Localization;
using ManagedWinapi.Hooks;

namespace GestureSign.ControlPanel.ViewModel
{
    public class MouseActionDescription
    {
        static MouseActionDescription()
        {
            DescriptionDict = new Dictionary<MouseActions, string>();
            DrawingDescription = new Dictionary<MouseActions, string>();

            foreach (MouseActions mouseAction in Enum.GetValues(typeof(MouseActions)))
            {
                string description = LocalizationProvider.Instance.GetTextValue("Gesture.MouseActions." + mouseAction);
                DescriptionDict.Add(mouseAction, description);
                DrawingDescription.Add(mouseAction, description);
            }

            DrawingDescription.Remove(MouseActions.None);
            DrawingDescription.Remove(MouseActions.WheelBackward);
            DrawingDescription.Remove(MouseActions.WheelForward);
        }

        public static Dictionary<MouseActions, string> DescriptionDict { get; }
        public static Dictionary<MouseActions, string> DrawingDescription { get; }

        public static string GetDescription(MouseActions mouseActions)
        {
            if (DescriptionDict.ContainsKey(mouseActions))
                return DescriptionDict[mouseActions];

            var descriptions = DescriptionDict
                .Where(pair => pair.Key != MouseActions.None && (mouseActions & pair.Key) == pair.Key)
                .Select(pair => pair.Value);

            return string.Join(" + ", descriptions);
        }
    }
}
