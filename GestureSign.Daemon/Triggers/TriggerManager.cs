using GestureSign.Common.Applications;
using GestureSign.Common.Plugins;
using GestureSign.Daemon.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace GestureSign.Daemon.Triggers
{
    class TriggerManager
    {
        #region Private Variables

        private List<Trigger> _triggerList = new List<Trigger>(3);

        #endregion

        #region Constructors

        static TriggerManager()
        {
            Instance = new TriggerManager();
        }

        #endregion

        #region Public Instance Properties

        public static TriggerManager Instance { get; }

        #endregion

        #region Public Methods

        public void Load()
        {
            AddTrigger(new HotKeyManager());
            AddTrigger(new MouseTrigger());
            AddTrigger(new ContinuousGestureTrigger());
        }

        #endregion


        #region Private Methods

        private void AddTrigger(Trigger newTrigger)
        {
            newTrigger.TriggerFired += Trigger_TriggerFired;
            _triggerList.Add(newTrigger);
        }

        private void Trigger_TriggerFired(object sender, TriggerFiredEventArgs e)
        {
            if (e.FiredActions == null || e.FiredActions.Count == 0) return;
            var point = new List<Point>(new[] { e.FiredPoint });
            var points = new List<List<Point>>(new[] { point });
            var inputContacts = PointCapture.Instance.InputContacts;
            var conditionPoints = new List<List<Point>>(inputContacts.Count);
            var conditionContactIdentifiers = new List<int>(inputContacts.Count);

            foreach (var inputContact in inputContacts)
            {
                if (inputContact.Points.Count == 0)
                    continue;

                conditionPoints.Add(new List<Point>(inputContact.Points));
                conditionContactIdentifiers.Add(inputContact.ContactIdentifier);
            }
            if (conditionPoints.Count == 0)
            {
                conditionPoints.Add(point);
                conditionContactIdentifiers.Add(1);
            }

            PluginManager.Instance.ExecuteAction(
                e.FiredActions,
                PointCapture.Instance.Mode,
                e.SourceDevice != GestureSign.Common.Input.Devices.None ? e.SourceDevice : PointCapture.Instance.SourceDevice,
                new List<int>(new[] { 1 }),
                point,
                points,
                conditionContactIdentifiers,
                conditionPoints);
        }

        #endregion
    }
}
