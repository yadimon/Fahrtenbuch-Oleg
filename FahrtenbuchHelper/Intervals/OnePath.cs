using System;
using System.Collections.Generic;
using System.Windows.Forms;

using FahrtenbuchHelper.Utils;

namespace FahrtenbuchHelper.Intervals
{
    public class OnePath
    {
        static Random ms_Rand = new Random();
        int m_KmInterval = -1;
        MyTime m_TimeDiff = null;

        OnePoint m_startPoint;
        OnePoint m_endPoint;

        String m_Errors = "";

        public OnePoint StartPoint
        {
            get { return m_startPoint; }
        }

        public OnePoint EndPoint
        {
            get { return m_endPoint; }
        }

        public MyTime TimeDiff
        {
            get { return m_TimeDiff; }
        }

        public int KmInterval
        {
            get { return m_KmInterval; }
        }

        public OnePath(OnePoint _startPoint, OnePoint _endPoint)
        {
            m_startPoint = _startPoint;
            m_endPoint = _endPoint;

            string timeDiff = "";
            AddressDatabaseManager.getDistance(_startPoint.Address, _endPoint.Address, out m_KmInterval, out timeDiff);
            m_TimeDiff = new MyTime(timeDiff);

            MyTime timeDiffPlus10 = new MyTime(m_TimeDiff.ToString());
            if (timeDiffPlus10.MinutesCount() < 5)
            {
                timeDiffPlus10 = new MyTime("00:05");
            }
            else if (timeDiffPlus10.MinutesCount() >= 5 && timeDiffPlus10.MinutesCount() <= 15)
            {
                timeDiffPlus10 = MyTime.Add(m_TimeDiff, new MyTime("00:05"));
            }
            else if (timeDiffPlus10.MinutesCount() > 15)
            {
                timeDiffPlus10 = MyTime.Add(m_TimeDiff, new MyTime("00:08"));
            }

            if (m_startPoint.PointTime == null && m_endPoint.PointTime == null)
            {
                throw new Exception("Both parts of path are null. You must input some time! This shoudnot happen!");
            }
            else if (m_startPoint.PointTime == null)
            {
                m_startPoint.PointTime = MyTime.Subtract(m_endPoint.PointTime, timeDiffPlus10);
            }
            else if (m_endPoint.PointTime == null)
            {
                m_endPoint.PointTime = MyTime.Add(m_startPoint.PointTime, timeDiffPlus10);
            }
            
            //get 10% from m_KmInterval;
            int randDiff = m_KmInterval / 10;

            m_KmInterval = ms_Rand.Next(m_KmInterval, m_KmInterval + randDiff);

            CheckTimes();
        }

        public string CheckErrors()
        {
            return m_Errors;
        }

        private void CheckTimes()
        {
            if (m_endPoint.PointTime.MinutesCount() < m_startPoint.PointTime.MinutesCount())
            {
                m_Errors += "/ starttime is bigger then endtime ";
                return;
            }
            MyTime timeDiff = MyTime.Subtract(m_endPoint.PointTime,m_startPoint.PointTime);

            //timeDiff = MyTime.AbsSubtract(m_TimeDiff, timeDiff);
            if (m_TimeDiff.MinutesCount() > timeDiff.MinutesCount())
            {
                m_Errors += "/ Warning: time difference is too small. It is " +
                    timeDiff.MinutesCount() + " minutes and must be more then " + m_TimeDiff.MinutesCount() + " minutes.";
            }
            else if (timeDiff.MinutesCount() > (m_TimeDiff.MinutesCount() + 30))
            {
                m_Errors += "/ Warning: time difference is too big. It is " +
                    timeDiff.MinutesCount() + " minutes and must be less then " + (m_TimeDiff.MinutesCount() + 30) + " minutes.";
            }
        }
    }
}

