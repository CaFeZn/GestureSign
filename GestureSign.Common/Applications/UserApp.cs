namespace GestureSign.Common.Applications
{
    public class UserApp : ApplicationBase
    {
        private int _blockTouchInputThreshold;
        private int _limitNumberOfFingers;

        public int LimitNumberOfFingers
        {
            get
            {
                if (_limitNumberOfFingers < 1)
                    _limitNumberOfFingers = 2;
                else if (_limitNumberOfFingers > 10)
                    _limitNumberOfFingers = 10;
                return _limitNumberOfFingers;
            }
            set
            {
                if (value < 1) value = 1;
                if (value > 10) value = 10;
                _limitNumberOfFingers = value;
            }
        }

        public int BlockTouchInputThreshold
        {
            get
            {
                if (_blockTouchInputThreshold < 2)
                    return 0;
                return _blockTouchInputThreshold > 10 ? 10 : _blockTouchInputThreshold;
            }
            set
            {
                if (value < 2)
                {
                    _blockTouchInputThreshold = 0;
                    return;
                }

                _blockTouchInputThreshold = value > 10 ? 10 : value;
            }
        }
    }
}
