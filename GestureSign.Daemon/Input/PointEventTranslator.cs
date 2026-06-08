using System;
using System.Collections.Generic;
using System.Linq;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using ManagedWinapi.Hooks;

namespace GestureSign.Daemon.Input
{
    public class PointEventTranslator
    {
        private int _lastPointsCount;
        private readonly HashSet<int> _activeTouchContacts = new HashSet<int>();
        private readonly Dictionary<int, int> _touchContactIdMap = new Dictionary<int, int>();
        private HashSet<MouseActions> _pressedMouseButton;
        private IntPtr _sourceDeviceHandle;

        internal Devices SourceDevice { get; private set; }
        internal IntPtr SourceDeviceHandle { get; private set; }
        internal MouseActions CurrentDrawingButton { get; private set; }

        internal PointEventTranslator(InputProvider inputProvider)
        {
            _pressedMouseButton = new HashSet<MouseActions>();
            inputProvider.PointsIntercepted += TranslateTouchEvent;
            inputProvider.LowLevelMouseHook.MouseDown += LowLevelMouseHook_MouseDown;
            inputProvider.LowLevelMouseHook.MouseMove += LowLevelMouseHook_MouseMove;
            inputProvider.LowLevelMouseHook.MouseUp += LowLevelMouseHook_MouseUp;
        }

        #region Custom Events

        public event EventHandler<InputPointsEventArgs> PointDown;

        protected virtual void OnPointDown(InputPointsEventArgs args)
        {
            if (!IsMatchingSource(args, allowPenTransition: true)) return;
            SourceDevice = args.PointSource;
            _sourceDeviceHandle = args.DeviceHandle;
            SourceDeviceHandle = args.DeviceHandle;
            PointDown?.Invoke(this, args);
        }

        public event EventHandler<InputPointsEventArgs> PointUp;

        protected virtual void OnPointUp(InputPointsEventArgs args)
        {
            if (!IsMatchingSource(args)) return;

            PointUp?.Invoke(this, args);

            SourceDevice = Devices.None;
            _sourceDeviceHandle = IntPtr.Zero;
            SourceDeviceHandle = IntPtr.Zero;
        }

        public event EventHandler<InputPointsEventArgs> PointMove;

        protected virtual void OnPointMove(InputPointsEventArgs args)
        {
            if (!IsMatchingSource(args)) return;
            PointMove?.Invoke(this, args);
        }

        #endregion

        #region Private Methods

        private void LowLevelMouseHook_MouseUp(LowLevelMouseMessage mouseMessage, ref bool handled)
        {
            var button = (MouseActions)mouseMessage.Button;
            if (button == CurrentDrawingButton)
            {
                var args = new InputPointsEventArgs(new List<InputPoint>(new[] { new InputPoint(1, mouseMessage.Point) }), Devices.Mouse);
                OnPointUp(args);
                handled = args.Handled;
                CurrentDrawingButton = MouseActions.None;
            }
            _pressedMouseButton.Remove(button);
        }

        private void LowLevelMouseHook_MouseMove(LowLevelMouseMessage mouseMessage, ref bool handled)
        {
            var args = new InputPointsEventArgs(new List<InputPoint>(new[] { new InputPoint(1, mouseMessage.Point) }), Devices.Mouse);
            OnPointMove(args);
        }

        private void LowLevelMouseHook_MouseDown(LowLevelMouseMessage mouseMessage, ref bool handled)
        {
            var button = (MouseActions)mouseMessage.Button;
            if (IsDrawingButton(button) && _pressedMouseButton.Count == 0)
            {
                CurrentDrawingButton = button;
                var args = new InputPointsEventArgs(new List<InputPoint>(new[] { new InputPoint(1, mouseMessage.Point) }), Devices.Mouse);
                OnPointDown(args);
                handled = args.Handled;
            }
            _pressedMouseButton.Add(button);
        }

        private static bool IsDrawingButton(MouseActions button)
        {
            var drawingButtons = GetEffectiveDrawingButtons();
            return button != MouseActions.None && (drawingButtons & button) == button;
        }

        private static MouseActions GetEffectiveDrawingButtons()
        {
            if (AppConfig.DrawingButton != MouseActions.None)
                return AppConfig.DrawingButton;

            return PointCapture.Instance.Mode == CaptureMode.Training
                ? MouseActions.Right
                : MouseActions.None;
        }

        private void TranslateTouchEvent(object sender, RawPointsDataMessageEventArgs e)
        {
            if ((e.SourceDevice & Devices.TouchDevice) != 0)
            {
                TranslateTouchDeviceEvent(e);
            }
            else if (e.SourceDevice == Devices.Pen)
            {
                var penSetting = AppConfig.PenGestureButton;
                bool drawByTip = (penSetting & DeviceStates.Tip) != 0;
                bool drawByHover = (penSetting & DeviceStates.InRange) != 0;
                bool hasActivationButton = (penSetting & (DeviceStates.Invert | DeviceStates.RightClickButton)) != 0;
                bool activationPressed = (e.RawData[0].State & (DeviceStates.Invert | DeviceStates.RightClickButton)) != 0;
                bool inRange = (e.RawData[0].State & DeviceStates.InRange) != 0;
                bool tip = (e.RawData[0].State & (DeviceStates.Eraser | DeviceStates.Tip)) != 0;
                bool active = hasActivationButton
                    ? activationPressed && inRange
                    : (drawByHover && inRange) || (drawByTip && tip);
                bool release = !active;

                if (release)
                {
                    OnPointUp(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                    _lastPointsCount = 0;
                    return;
                }

                if (drawByHover && drawByTip)
                {
                    if (_lastPointsCount == 1 && SourceDevice == Devices.Pen)
                    {
                        OnPointMove(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                    }
                    else if (_lastPointsCount >= 0)
                    {
                        _lastPointsCount = 1;
                        OnPointDown(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                    }
                }
                else if (drawByTip)
                {
                    if (!tip)
                    {
                        if (SourceDevice == Devices.Pen)
                        {
                            OnPointUp(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                            _lastPointsCount = 0;
                        }
                        return;
                    }

                    if (_lastPointsCount == 1 && SourceDevice == Devices.Pen)
                    {
                        OnPointMove(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                    }
                    else if (_lastPointsCount >= 0)
                    {
                        _lastPointsCount = 1;
                        OnPointDown(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                    }
                }
                else if (drawByHover)
                {
                    if (_lastPointsCount == 1 && SourceDevice == Devices.Pen)
                    {
                        OnPointMove(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                    }
                    else if (_lastPointsCount >= 0)
                    {
                        _lastPointsCount = 1;
                        OnPointDown(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                    }
                }
            }
        }

        private void TranslateTouchDeviceEvent(RawPointsDataMessageEventArgs e)
        {
            var activeRawData = e.RawData.Where(IsActiveTouchContact).ToList();
            var activeContactIdentifiers = new HashSet<int>(activeRawData.Select(rd => rd.ContactIdentifier));
            var releasedContactIdentifiers = e.RawData
                .Where(rd => !IsActiveTouchContact(rd))
                .Select(rd => rd.ContactIdentifier)
                .ToList();
            bool hadActiveTouchContacts = _activeTouchContacts.Count != 0;
            bool releasedTrackedContacts = releasedContactIdentifiers.Any(id => _activeTouchContacts.Contains(id));

            if (_activeTouchContacts.Count != 0 && releasedContactIdentifiers.Count != 0)
            {
                _activeTouchContacts.ExceptWith(releasedContactIdentifiers);
                foreach (var releasedContactIdentifier in releasedContactIdentifiers)
                    _touchContactIdMap.Remove(releasedContactIdentifier);
            }

            if (activeRawData.Count != 0 && hadActiveTouchContacts && releasedTrackedContacts && _activeTouchContacts.Count == 0)
            {
                if ((e.SourceDevice & Devices.TouchDevice) != 0 &&
                    TryContinueTouchGestureAfterContactIdRollover(activeRawData, out var remappedActiveRawData, out var rawToStableContactMap))
                {
                    if (IsCurrentSource(e))
                    {
                        SetActiveTouchContacts(activeContactIdentifiers, remappedActiveRawData.Count);
                        SetTouchContactIdMap(rawToStableContactMap);
                    }
                    OnPointMove(new InputPointsEventArgs(remappedActiveRawData, e.SourceDevice, e.DeviceHandle));
                    return;
                }

                // Rapid taps can report the previous release and the next press in one raw frame.
                OnPointUp(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                ClearActiveTouchContacts();

                OnPointDown(new InputPointsEventArgs(activeRawData, e.SourceDevice, e.DeviceHandle));
                if (IsCurrentSource(e))
                {
                    SetActiveTouchContacts(activeContactIdentifiers, activeRawData.Count);
                    SetTouchContactIdMapIdentity(activeContactIdentifiers);
                }
                return;
            }

            if (activeRawData.Count == 0)
            {
                if (_activeTouchContacts.Count == 0 && IsCurrentSource(e))
                    OnPointUp(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                _lastPointsCount = _activeTouchContacts.Count;
                return;
            }

            if (_activeTouchContacts.Count == 0)
            {
                OnPointDown(new InputPointsEventArgs(activeRawData, e.SourceDevice, e.DeviceHandle));
                if (IsCurrentSource(e))
                {
                    SetActiveTouchContacts(activeContactIdentifiers, activeRawData.Count);
                    SetTouchContactIdMapIdentity(activeContactIdentifiers);
                }
                return;
            }

            var trackedActiveRawData = activeRawData
                .Where(rd => _activeTouchContacts.Contains(rd.ContactIdentifier))
                .ToList();

            if (trackedActiveRawData.Count == 0)
            {
                if ((e.SourceDevice & Devices.TouchDevice) != 0 &&
                    TryContinueTouchGestureAfterContactIdRollover(activeRawData, out var remappedActiveRawData, out var rawToStableContactMap))
                {
                    if (IsCurrentSource(e))
                    {
                        SetActiveTouchContacts(activeContactIdentifiers, remappedActiveRawData.Count);
                        SetTouchContactIdMap(rawToStableContactMap);
                    }
                    OnPointMove(new InputPointsEventArgs(remappedActiveRawData, e.SourceDevice, e.DeviceHandle));
                    return;
                }

                OnPointUp(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                ClearActiveTouchContacts();

                OnPointDown(new InputPointsEventArgs(activeRawData, e.SourceDevice, e.DeviceHandle));
                if (IsCurrentSource(e))
                {
                    SetActiveTouchContacts(activeContactIdentifiers, activeRawData.Count);
                    SetTouchContactIdMapIdentity(activeContactIdentifiers);
                }
                return;
            }

            bool hasNewContacts = activeRawData.Count > _lastPointsCount ||
                activeContactIdentifiers.Any(id => !_activeTouchContacts.Contains(id));

            if (releasedTrackedContacts &&
                hasNewContacts &&
                PointCapture.Instance.State == CaptureState.CapturingInvalid)
            {
                OnPointUp(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                ClearActiveTouchContacts();

                OnPointDown(new InputPointsEventArgs(activeRawData, e.SourceDevice, e.DeviceHandle));
                if (IsCurrentSource(e))
                {
                    SetActiveTouchContacts(activeContactIdentifiers, activeRawData.Count);
                    SetTouchContactIdMapIdentity(activeContactIdentifiers);
                }
                return;
            }

            if (hasNewContacts &&
                (e.SourceDevice & Devices.TouchDevice) != 0 &&
                TryContinueTouchGestureAfterContactIdRollover(activeRawData, out var continuedActiveRawData, out var continuedRawToStableContactMap))
            {
                if (IsCurrentSource(e))
                {
                    SetActiveTouchContacts(activeContactIdentifiers, continuedActiveRawData.Count);
                    SetTouchContactIdMap(continuedRawToStableContactMap);
                }
                _lastPointsCount = continuedActiveRawData.Count;
                OnPointMove(new InputPointsEventArgs(continuedActiveRawData, e.SourceDevice, e.DeviceHandle));
                return;
            }

            if (hasNewContacts && !PointCapture.Instance.InputPoints.Any(p => p.Count > 10))
            {
                OnPointUp(new InputPointsEventArgs(e.RawData, e.SourceDevice, e.DeviceHandle));
                ClearActiveTouchContacts();

                OnPointDown(new InputPointsEventArgs(activeRawData, e.SourceDevice, e.DeviceHandle));
                if (IsCurrentSource(e))
                {
                    SetActiveTouchContacts(activeContactIdentifiers, activeRawData.Count);
                    SetTouchContactIdMapIdentity(activeContactIdentifiers);
                }
                return;
            }

            _activeTouchContacts.IntersectWith(activeContactIdentifiers);
            _lastPointsCount = trackedActiveRawData.Count;
            OnPointMove(new InputPointsEventArgs(MapRawDataContactIdentifiers(trackedActiveRawData), e.SourceDevice, e.DeviceHandle));
        }

        private static bool TryContinueTouchGestureAfterContactIdRollover(List<RawData> activeRawData, out List<RawData> remappedActiveRawData, out Dictionary<int, int> rawToStableContactMap)
        {
            remappedActiveRawData = null;
            rawToStableContactMap = null;
            if (activeRawData == null ||
                PointCapture.Instance.State != CaptureState.Capturing)
            {
                return false;
            }

            var existingPoints = PointCapture.Instance.InputPoints;
            var existingIdentifiers = PointCapture.Instance.InputContactIdentifiers;
            if (existingPoints == null ||
                existingIdentifiers == null ||
                existingPoints.Length != activeRawData.Count ||
                existingIdentifiers.Count != activeRawData.Count ||
                !existingPoints.Any(points => points != null && points.Count > 1))
            {
                return false;
            }

            int maxDelta = Math.Max(160, AppConfig.MinimumPointDistance * 40);
            long maxDistanceSquared = (long)maxDelta * maxDelta;
            var unmatchedContacts = new List<RawData>(activeRawData);
            var remappedContacts = new List<RawData>(existingIdentifiers.Count);
            var rawToStableMap = new Dictionary<int, int>(existingIdentifiers.Count);

            for (int i = 0; i < existingIdentifiers.Count; i++)
            {
                var stroke = existingPoints[i];
                if (stroke == null || stroke.Count == 0)
                    return false;

                var previousPoint = stroke.Last();
                int bestIndex = -1;
                long bestDistanceSquared = long.MaxValue;

                for (int j = 0; j < unmatchedContacts.Count; j++)
                {
                    long deltaX = unmatchedContacts[j].RawPoints.X - previousPoint.X;
                    long deltaY = unmatchedContacts[j].RawPoints.Y - previousPoint.Y;
                    long distanceSquared = deltaX * deltaX + deltaY * deltaY;
                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        bestIndex = j;
                    }
                }

                if (bestIndex < 0 || bestDistanceSquared > maxDistanceSquared)
                    return false;

                var matchedContact = unmatchedContacts[bestIndex];
                remappedContacts.Add(new RawData(matchedContact.State, existingIdentifiers[i], matchedContact.RawPoints));
                rawToStableMap[matchedContact.ContactIdentifier] = existingIdentifiers[i];
                unmatchedContacts.RemoveAt(bestIndex);
            }

            remappedActiveRawData = remappedContacts;
            rawToStableContactMap = rawToStableMap;
            return true;
        }

        private List<RawData> MapRawDataContactIdentifiers(List<RawData> rawData)
        {
            if (rawData == null || rawData.Count == 0 || _touchContactIdMap.Count == 0)
                return rawData;

            return rawData
                .Select(rd => _touchContactIdMap.TryGetValue(rd.ContactIdentifier, out var mappedContactIdentifier)
                    ? new RawData(rd.State, mappedContactIdentifier, rd.RawPoints)
                    : rd)
                .ToList();
        }

        private static bool IsActiveTouchContact(RawData rawData)
        {
            return rawData.State != DeviceStates.None;
        }

        private void SetActiveTouchContacts(IEnumerable<int> contactIdentifiers, int pointCount)
        {
            _activeTouchContacts.Clear();
            _activeTouchContacts.UnionWith(contactIdentifiers);
            _lastPointsCount = pointCount;
        }

        private void SetTouchContactIdMapIdentity(IEnumerable<int> contactIdentifiers)
        {
            _touchContactIdMap.Clear();
            foreach (var contactIdentifier in contactIdentifiers)
                _touchContactIdMap[contactIdentifier] = contactIdentifier;
        }

        private void SetTouchContactIdMap(Dictionary<int, int> rawToStableContactMap)
        {
            _touchContactIdMap.Clear();
            if (rawToStableContactMap == null)
                return;

            foreach (var pair in rawToStableContactMap)
                _touchContactIdMap[pair.Key] = pair.Value;
        }

        private void ClearActiveTouchContacts()
        {
            _activeTouchContacts.Clear();
            _touchContactIdMap.Clear();
            _lastPointsCount = 0;
        }

        internal void ResetTouchDeviceSource()
        {
            if ((SourceDevice & Devices.TouchDevice) == 0)
                return;

            ClearActiveTouchContacts();
            SourceDevice = Devices.None;
            _sourceDeviceHandle = IntPtr.Zero;
            SourceDeviceHandle = IntPtr.Zero;
        }

        private bool IsMatchingSource(InputPointsEventArgs args, bool allowPenTransition = false)
        {
            if (SourceDevice == Devices.None)
                return true;

            if (allowPenTransition && args.PointSource == Devices.Pen)
                return SourceDevice == Devices.TouchScreen || IsSameSource(args);

            return IsSameSource(args);
        }

        private bool IsCurrentSource(RawPointsDataMessageEventArgs args)
        {
            return SourceDevice == args.SourceDevice &&
                (_sourceDeviceHandle == IntPtr.Zero || args.DeviceHandle == IntPtr.Zero || _sourceDeviceHandle == args.DeviceHandle);
        }

        private bool IsSameSource(InputPointsEventArgs args)
        {
            return SourceDevice == args.PointSource &&
                (_sourceDeviceHandle == IntPtr.Zero || args.DeviceHandle == IntPtr.Zero || _sourceDeviceHandle == args.DeviceHandle);
        }

        #endregion
    }
}
