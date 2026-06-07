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
            _stopwatch.Stop();
            _lastPoints = null;
        }

        private void PointCapture_PointCaptured(object sender, PointsCapturedEventArgs e)
        {
            if (PointCapture.Instance.State != CaptureState.Capturing || e.Points.Count == 0 || e.FirstCapturedPoints.Count == 0)
                return;
            var actionsWithContinuousGesture = ApplicationManager.Instance.GetRecognizedDefinedAction(a => a != null && a.ContinuousGesture != null);
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
                var rate = GetRateOfFire(deltaXAbs);
                if (rate >= 1)
                {
                    for (int i = 1; i < rate; i++)
                    {
                        OnGesturerRecognized(_lastPoints.Count, deltaX > 0 ? Gestures.Right : Gestures.Left);
                    }
                    _stopwatch.Restart();
                    _lastPoints = e.FirstCapturedPoints;
                }
            }
            else
            {
                var rate = GetRateOfFire(deltaYAbs);
                if (rate >= 1)
                {
                    for (int i = 1; i < rate; i++)
                    {
                        OnGesturerRecognized(_lastPoints.Count, deltaY > 0 ? Gestures.Down : Gestures.Up);
                    }
                    _stopwatch.Restart();
                    _lastPoints = e.FirstCapturedPoints;
                }
            }
        }

        private void OnGesturerRecognized(int contactCount, Gestures gesture)
        {
            var actions = ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.ContinuousGesture != null &&
            a.ContinuousGesture.ContactCount == contactCount && a.ContinuousGesture.Gesture == gesture);
            if (actions.Count > 0)
            {
                _continuousGestureFired = true;
                OnTriggerFired(new TriggerFiredEventArgs(actions, _startPoint));
            }
        }

        private double GetRateOfFire(int distance)
        {
            var deltaTime = _stopwatch.ElapsedMilliseconds;
            if (deltaTime < 2)
                return 0;

            var velocity = distance / (double)deltaTime;
            var motionThreshold = GetMotionThreshold();
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

        private static float GetMotionThreshold()
        {
            return Math.Max(1, AppConfig.ContinuousGestureDistance) * DpiHelper.GetSystemDpi() / 96f;
        }
    }
}
