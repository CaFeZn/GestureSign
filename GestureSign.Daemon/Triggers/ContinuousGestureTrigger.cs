using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using GestureSign.Daemon.Native;
using GestureSign.PointPatterns;
using ManagedWinapi.Hooks;
using System;
using System.Collections.Generic;
using GestureSign.Common.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace GestureSign.Daemon.Triggers
{
    class ContinuousGestureTrigger : Trigger
    {
        private Point _startPoint;
        private Stopwatch _stopwatch = new Stopwatch();
        private List<Point> _lastPoints;
        private bool _continuousGestureFired;

        public ContinuousGestureTrigger()
        {
            PointCapture.Instance.CaptureStarted += PointCapture_CaptureStarted;
            PointCapture.Instance.PointCaptured += PointCapture_PointCaptured;
            PointCapture.Instance.BeforePointsCaptured += PointCapture_BeforePointsCaptured;
            PointCapture.Instance.CaptureEnded += PointCapture_CaptureEnded;
            PointCapture.Instance.CaptureCanceled += PointCapture_CaptureCanceled;
        }

        private void PointCapture_CaptureStarted(object sender, PointsCapturedEventArgs e)
        {
            _continuousGestureFired = false;
        }

        private void PointCapture_BeforePointsCaptured(object sender, PointsCapturedEventArgs e)
        {
            if (!_continuousGestureFired)
                return;

            e.Cancel = true;
            _continuousGestureFired = false;
        }

        private void PointCapture_CaptureEnded(object sender, System.EventArgs e)
        {
            Reset();
        }

        private void PointCapture_CaptureCanceled(object sender, PointsCapturedEventArgs e)
        {
            Reset();
        }

        private void Reset()
        {
            _stopwatch.Stop();
            _lastPoints = null;
            _continuousGestureFired = false;
        }

        private void PointCapture_PointCaptured(object sender, PointsCapturedEventArgs e)
        {
            if (PointCapture.Instance.State != CaptureState.Capturing || e.Points.Count == 0 || e.FirstCapturedPoints.Count == 0)
                return;
            var actionsWithContinuousGesture = ApplicationManager.Instance.GetRecognizedDefinedAction(IsSafeContinuousGestureAction);
            if (actionsWithContinuousGesture == null || actionsWithContinuousGesture.Count == 0)
                return;
            if (_lastPoints == null || _lastPoints.Count != e.FirstCapturedPoints.Count)
            {
                _startPoint = e.FirstCapturedPoints[0];
                _lastPoints = e.FirstCapturedPoints;
                _stopwatch.Restart();
                return;
            }

            int deltaX = 0, deltaY = 0;
            for (int i = 0; i < _lastPoints.Count; i++)
            {
                deltaX += e.FirstCapturedPoints[i].X - _lastPoints[i].X;
                deltaY += e.FirstCapturedPoints[i].Y - _lastPoints[i].Y;
            }
            deltaX /= _lastPoints.Count;
            deltaY /= _lastPoints.Count;
            int deltaXAbs = Math.Abs(deltaX);
            int deltaYAbs = Math.Abs(deltaY);
            bool isHorizontal = deltaXAbs > deltaYAbs;
            if (isHorizontal)
            {
                var rate = GetRateOfFire(deltaXAbs, e.FirstCapturedPoints[0]);
                if (rate >= 1)
                {
                    FireContinuousGesture(rate, deltaX > 0 ? Gestures.Right : Gestures.Left);
                    _stopwatch.Restart();
                    _lastPoints = e.FirstCapturedPoints;
                }
            }
            else
            {
                var rate = GetRateOfFire(deltaYAbs, e.FirstCapturedPoints[0]);
                if (rate >= 1)
                {
                    FireContinuousGesture(rate, deltaY > 0 ? Gestures.Down : Gestures.Up);
                    _stopwatch.Restart();
                    _lastPoints = e.FirstCapturedPoints;
                }
            }
        }

        private void FireContinuousGesture(double rate, Gestures gesture)
        {
            int fireCount = (int)Math.Floor(rate);
            for (int i = 0; i < fireCount; i++)
            {
                OnGesturerRecognized(_lastPoints.Count, gesture);
            }
        }

        private void OnGesturerRecognized(int contactCount, Gestures gesture)
        {
            var actions = ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.ContinuousGesture != null &&
            a.ContinuousGesture.ContactCount == contactCount &&
            a.ContinuousGesture.Gesture == gesture &&
            IsSafeSingleFingerTouchPadAction(a, contactCount));
            if (actions.Count > 0)
            {
                _continuousGestureFired = true;
                OnTriggerFired(new TriggerFiredEventArgs(actions, _startPoint));
            }
        }

        private static bool IsSafeContinuousGestureAction(IAction action)
        {
            return action != null &&
                action.ContinuousGesture != null &&
                IsSafeSingleFingerTouchPadAction(action, action.ContinuousGesture.ContactCount);
        }

        private static bool IsSafeSingleFingerTouchPadAction(IAction action, int contactCount)
        {
            return PointCapture.Instance.SourceDevice != Devices.TouchPad ||
                contactCount != 1 ||
                !string.IsNullOrWhiteSpace(action.Condition);
        }

        private double GetRateOfFire(int distance, Point referencePoint)
        {
            var deltaTime = _stopwatch.ElapsedMilliseconds;
            if (deltaTime < 2)
                return 0;

            var velocity = distance / (double)deltaTime;
            var motionThreshold = GetMotionThreshold(referencePoint);
            if (velocity < 3)
            {
                return distance / motionThreshold;
            }
            else
            {
                if (velocity > 16)
                    velocity = 16;
                return distance / ((-0.0023 * velocity * velocity + 0.0096 * velocity + 0.89) * motionThreshold);
            }
        }

        private static float GetMotionThreshold(Point referencePoint)
        {
            return Math.Max(1, AppConfig.ContinuousGestureDistance) * DpiHelper.GetScreenDpi(referencePoint) / 96f;
        }
    }
}
