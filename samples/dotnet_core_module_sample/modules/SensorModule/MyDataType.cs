using System;

namespace SensorModule
{
    public class MyDataType
    {
        #region Public Constructors

        public MyDataType(DateTime time, long increment)
        {
            TimestampId = time;
            Value = increment;
        }

        #endregion Public Constructors

        #region Public Properties

        public DateTime TimestampId
        {
            get;
            set;
        }

        public long Value
        {
            get; set;
        }

        #endregion Public Properties
    }
}