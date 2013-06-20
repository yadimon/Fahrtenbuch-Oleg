using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FahrtenbuchHelper.Utils
{
    public class MyTime
    {
        private string m_OriginalTime = null;

        private int m_Hours = -1;
        private int m_Minutes = -1;
 
        public int Minutes
        {
            get{ return m_Minutes;}
            set{ m_Minutes = value;}
        }

        public int Hours
        {
            get { return m_Hours; }
            set { m_Hours = value; }
        }

        public MyTime(string _time)
        {
            _time = _time.Replace(" ", "");
            _time = _time.Replace(".", ":");
            _time = _time.ToLower();
            if (_time.Contains(":"))
            {
                string[] hoursAndMinutes = _time.Split(':');
                m_Hours = int.Parse(hoursAndMinutes[0]);
                m_Minutes = int.Parse(hoursAndMinutes[1]);
                m_OriginalTime = _time;
            }
            else
            { 
                
                Match match = Regex.Match(_time, @"[0-9]+h");
                if (match.Success)
                {
                    string hours = match.Groups[0].Value;
                    hours = hours.Replace("h", "");
                    m_Hours = int.Parse(hours);
                }
                else
                {
                    m_Hours = 0;
                }

                match = Regex.Match(_time, @"[0-9]+m");
                if (match.Success)
                {
                    string minutes = match.Groups[0].Value;
                    minutes = minutes.Replace("m", "");
                    m_Minutes = int.Parse(minutes);
                }
                else
                {
                    m_Minutes = 0;
                }
                RewriteTime();
            }
        }

        public void RewriteTime()
        {
            m_OriginalTime = m_Hours.ToString("00") + ":" + m_Minutes.ToString("00");
        }

        public override string ToString()
        {
            return m_OriginalTime;
        }

        public int MinutesCount()
        {
            if (m_Hours < 0 || m_Minutes < 0)
            {
                throw new Exception("Hours or Minutes are wrong calculated");
            }
            return m_Hours*60 + m_Minutes;
        }

        public static MyTime Add(MyTime _time1, MyTime _time2)
        {
            MyTime time = new MyTime(_time1.ToString());
            int minutes = time.MinutesCount() + _time2.MinutesCount();
            time.Hours = minutes / 60;
            time.Minutes = minutes % 60;

            time.RewriteTime();
            return time;
        }

        public static MyTime Subtract(MyTime _time1, MyTime _time2)
        {
            MyTime time = new MyTime(_time1.ToString());
            int minutes = time.MinutesCount() - _time2.MinutesCount();
            time.Hours = minutes / 60;
            time.Minutes = minutes % 60;

            time.RewriteTime();
            return time;
        }

        public static MyTime AbsSubtract(MyTime _time1, MyTime _time2)
        {
            //MyTime time = new MyTime(this.ToString());
            if (_time1.MinutesCount() > _time2.MinutesCount())
            {
                return MyTime.Subtract(_time1,_time2);
            }

            return MyTime.Subtract(_time2, _time1);
        }
    }
}

