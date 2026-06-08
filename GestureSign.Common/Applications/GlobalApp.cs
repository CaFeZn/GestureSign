using GestureSign.Common.Localization;

namespace GestureSign.Common.Applications
{
    public class GlobalApp : ApplicationBase
    {
        private int _limitNumberOfFingers;

        public int LimitNumberOfFingers
        {
            get
            {
                if (_limitNumberOfFingers < 1)
                    _limitNumberOfFingers = 1;
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

        #region IApplication Properties

        public override string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("Common.GlobalActions"); ; }
            //set { /* Set only exists for deserialization purposes */ }
        }

        public override MatchUsing MatchUsing
        {
            get { return MatchUsing.All; }
            //	set { /* Set only exists for deserialization purposes */ }
        }

        public override bool MatchActivated { get => false; }

        #endregion
    }
}
