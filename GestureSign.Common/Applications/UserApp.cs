namespace GestureSign.Common.Applications
{
    public class UserApp : ApplicationBase
    {
        private int _blockTouchInputThreshold;
        private int _limitNumberOfFingers;

        public int LimitNumberOfFingers
        {
            get { return _limitNumberOfFingers < 1 ? _limitNumberOfFingers = 2 : _limitNumberOfFingers; }
            set { _limitNumberOfFingers = value; }
        }

        public int BlockTouchInputThreshold
        {
            get { return _blockTouchInputThreshold >= 2 ? _blockTouchInputThreshold : 0; }
            set { _blockTouchInputThreshold = value >= 2 ? value : 0; }
        }
    }
}
