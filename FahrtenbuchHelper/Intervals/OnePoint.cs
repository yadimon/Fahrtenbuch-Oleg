using System;
using System.Collections.Generic;

using FahrtenbuchHelper.Utils;

namespace FahrtenbuchHelper.Intervals
{
    public enum WayType
    {
        Normal = 0,
        OneWayTo = 1,
        OneWayBack = 2,
    }

    public class OnePoint
    {
        private string m_FirmaName = null;
        private string m_Address = null;
        private MyTime m_Time = null;
        private WayType m_OneWay = WayType.Normal;

        public string FirmaName
        {
            get { return m_FirmaName; }
        }

        public string Address
        {
            get { return m_Address; }
        }

        public WayType OneWay
        {
            get { return m_OneWay; }
        }

        public MyTime PointTime
        {
            get { return m_Time; }
            set { m_Time = value; }
        }

        public OnePoint(string _firmaName, string _address, MyTime _time, WayType oneWay)
        {
            m_Address = _address;
            m_FirmaName = _firmaName;
            m_Time = _time;
            m_OneWay = oneWay;
        }
    }
}

