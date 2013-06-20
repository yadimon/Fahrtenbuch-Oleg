using System;
using System.Collections.Generic;

using FahrtenbuchHelper.Utils;

namespace FahrtenbuchHelper.Intervals
{
    public class DayPath
    {
        //private static OnePoint m_HomePoint = new OnePoint("home", "home", null);
        private List<OnePoint> m_Points = new List<OnePoint>();
        private List<OnePath> m_Paths = new List<OnePath>();
        private int m_BenzinIntervalSinceLast = -1;

        private DateTime m_Date;

        public int BenzinIntervalSinceLast
        {
            get { return m_BenzinIntervalSinceLast; }
        }

        public void BenzinIntervalAdd(int value)
        {
            if (m_BenzinIntervalSinceLast == -1)
            {
                m_BenzinIntervalSinceLast = value;
            }
            else
            {
                m_BenzinIntervalSinceLast += value;
            }
        }

        public List<OnePath> DayPaths
        {
            get { return m_Paths; }
        }

        public DayPath(DateTime date)
        {
            m_Date = date;
        }

        private OnePoint GetHomePoint()
        {
            //if (m_Date > new DateTime(2012,1,25))
            //{
            //    return new OnePoint("home", "home2", null, WayType.Normal);
            //}
            return new OnePoint("home", "home", null, WayType.Normal);
        }

        public void addNewPoint(string _firmaName, string _smallAddress, MyTime _time)
        {
            m_Points.Add(new OnePoint(_firmaName, _smallAddress, _time, WayType.Normal));
        }

        public void addNewPoint(string _firmaName, string _smallAddress, MyTime _time, WayType oneWay)
        {
            m_Points.Add(new OnePoint(_firmaName, _smallAddress, _time, oneWay));
        }

        public void CalculatePathsFromPoints()
        {
            m_Points = SortByTime(m_Points);

            bool oneWayTo = false;
            bool oneWayBack = false;

            for (int n = 0; n < m_Points.Count; n++)
            {
                if (m_Points[n].OneWay == WayType.OneWayTo)
                {
                    oneWayTo = true;
                }
                if (m_Points[n].OneWay == WayType.OneWayBack)
                {
                    oneWayBack = true;
                }
            }

            for (int n = 0; n < m_Points.Count; n++)
            {
                if (n == 0)
                {
                    if (!oneWayBack)
                    {
                        m_Paths.Add(new OnePath(GetHomePoint(), m_Points[n]));
                    }
                }
                else if (AddressDatabaseManager.getFullAddress(m_Points[n].Address) != AddressDatabaseManager.getFullAddress(m_Points[n - 1].Address))
                {
                    m_Paths.Add(new OnePath(m_Points[n - 1], m_Points[n]));
                }

                if (n == m_Points.Count - 1)
                {
                    if (!oneWayTo)
                    {
                        m_Paths.Add(new OnePath(m_Points[n], GetHomePoint()));
                    }
                }
            }
        }

        private List<OnePoint> SortByTime(List<OnePoint> _points)
        {
            for (int n = 0; n < _points.Count; n++)
            {
                for (int m = 1; m < _points.Count; m++)
                {
                    if (_points[m - 1].PointTime.MinutesCount() > _points[m].PointTime.MinutesCount())
                    {
                        SwapPoints(ref _points, m, m - 1);
                    }
                }
            }
            return _points;
        }

        private void SwapPoints(ref List<OnePoint> points, int index1, int index2)
        {
            OnePoint pointTemp = points[index1];
            points[index1] = points[index2];
            points[index2] = pointTemp;
        }

        public string CheckErrors()
        { 
            foreach(OnePath path in m_Paths)
            {
                string firstErrors = path.CheckErrors();
                if (!String.IsNullOrEmpty(firstErrors))
                {
                    return firstErrors;
                }
            }
            return "";
        }
    }
}

