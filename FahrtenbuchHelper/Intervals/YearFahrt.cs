using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;

using FahrtenbuchHelper.Utils;

namespace FahrtenbuchHelper.Intervals
{
    public class YearPath
    {
        private bool m_DEBUG = false;

        //private const double m_KmForLitr = 100.0/11; 

        private string m_Year = null;
        private int m_YearStartKm = -1;
        //private int m_StartDay = -1;
        private DayPath[] m_DayPaths = null;
        //private int m_KmInThisYear = 0;
        private string m_WorkingPath = "";

        public bool DEBUG
        {
            //get { return m_DEBUG; }
            set { m_DEBUG = value; }
        }

        public string Year
        {
            get { return m_Year; }
        }

        public DayPath[] DayPaths
        {
            get { return m_DayPaths; }
        }

        public int YearStartKm
        {
            get { return m_YearStartKm; }
        }

        public string WorkingPath
        {
            get { return m_WorkingPath; }
        }

        public YearPath(string dataYearPath)
        {
            m_Year = dataYearPath;
            m_WorkingPath = Path.Combine(Directory.GetCurrentDirectory(), "..\\..\\data\\" + dataYearPath);
            AddressDatabaseManager.initDatabaseManager(m_WorkingPath);
            
            m_DayPaths = new DayPath[getDaysInAYear(dataYearPath)];
            DateTime dateTime = new DateTime(int.Parse(m_Year), 1, 1);
            for (int n = 0; n < m_DayPaths.Length; n++)
            {
                m_DayPaths[n] = new DayPath(dateTime);
                dateTime = dateTime.AddDays(1);
            }

            // load paths          
            string[] fileNames = Directory.GetFiles(m_WorkingPath);
            foreach (string fileName in fileNames)
            {
                string tempFileName = Path.GetFileName(fileName);
                if (tempFileName.StartsWith(dataYearPath))
                {
                    if (tempFileName.ToLower().Contains("benzin"))
                    {
                        loadAndFilterBenzinTable(fileName);
                    }
                    else if (tempFileName.ToLower().Contains("gespraeche"))
                    {
                        loadAndFilterGespraeche(fileName);
                    }
                    else if (tempFileName.ToLower().Contains("privat"))
                    {
                        loadAndFilterPrivat(fileName);
                    }
                    else
                    {
                        loadAndFilterWorkList(fileName);
                    }
                }
            }

            // calculate paths
            for (int n = 0; n < m_DayPaths.Length; n++)
            {
                try
                {
                    m_DayPaths[n].CalculatePathsFromPoints();
                }
                catch
                { 
                    
                }
            }
            AddressDatabaseManager.saveData();
        }

        public void loadAndFilterWorkList(string fileName)
        {
            StreamWriter sw = null;
            bool isSuccessed = false;
            if (File.Exists(fileName))
            {
                StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding(1252));
                if (sr != null)
                {
                    if (m_DEBUG)
                    {
                        sw = File.CreateText(fileName + "_debug.txt");
                    }
                    string line = "";
                    HeaderHelper.initHeader(sr.ReadLine()); // header and checksum 
                    AddressDatabaseManager.addNewAddress(HeaderHelper.getFirmaName(), HeaderHelper.getFullAdress(), true);
                    
                    while ((line = sr.ReadLine()) != null)
                    {
                        isSuccessed = false;
                        Match match = Regex.Match(line, @"[0-9]{2,2}\.[0-9]{2,2}\.[0-9]{4,4}(\s|\t)*\-*[a-zäöüß]*\-*(\s|\t)*[0-9]+\:[0-9]+(\s|\t)*[0-9]+\:[0-9]+", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            string linePart = match.Groups[0].Value;
                            match = Regex.Match(linePart, @"[0-9]{2,2}\.[0-9]{2,2}\.[0-9]{4,4}", RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                string dateStr = match.Groups[0].Value;
                                match = Regex.Match(line, @"[0-9]+\:[0-9]+", RegexOptions.IgnoreCase);
                                if (match.Success && match.NextMatch().Success)
                                {
                                    MyTime time1 = new MyTime(match.Groups[0].Value);
                                    string tempTime2 = match.NextMatch().Groups[0].Value;
                                    MyTime time2 = new MyTime(tempTime2);
                                    int posTime2 = line.LastIndexOf(tempTime2.ToString()) + tempTime2.ToString().Length;
                                    string restLine = line.Substring(posTime2).Trim();
                                    string[] rest = restLine.Split('/');
                                    
                                    WayType oneWay = WayType.Normal;
                                    if (rest.Length > 0)
                                    {
                                        if (rest[0].Trim().ToLower() == "onewayto")
                                        {
                                            oneWay = WayType.OneWayTo;
                                        }
                                        else if (rest[0].Trim().ToLower() == "onewayback")
                                        {
                                            oneWay = WayType.OneWayBack;
                                        }
                                    }

                                    DateTime date = DateTime.Parse(dateStr, CultureInfo.GetCultureInfo("de-DE"));
                                    if (oneWay != WayType.OneWayBack)
                                    {
                                        m_DayPaths[date.DayOfYear - 1].addNewPoint(HeaderHelper.getFirmaName(), HeaderHelper.getFirmaName(), time1, oneWay);
                                    }
                                    if (oneWay != WayType.OneWayTo)
                                    {
                                        m_DayPaths[date.DayOfYear - 1].addNewPoint(HeaderHelper.getFirmaName(), HeaderHelper.getFirmaName(), time2, oneWay);
                                    }

                                    HeaderHelper.decChecksum();
                                    isSuccessed = true;
                                }
                            }
                        }

                        if (m_DEBUG)
                        {
                            if (isSuccessed)
                            {
                                sw.WriteLine(line + "\t\t\t V");
                            }
                            else
                            {
                                sw.WriteLine(line);
                            }
                        }
                    }

                    sr.Close();
                }
                HeaderHelper.checkChecksum();
            }
           
        }

        private void loadAndFilterGespraeche(string fileName)
        {
            if (File.Exists(fileName))
            {
                StreamReader sr = new StreamReader(fileName,Encoding.GetEncoding(1252));
                
                if (sr != null)
                {
                    string line = "";
                    HeaderHelper.initHeader(sr.ReadLine()); // header and checksum 
                    while ((line = sr.ReadLine()) != null)
                    {
                        Match match = Regex.Match(line, @"[0-9]{2,2}\.[0-9]{2,2}\.[0-9]{4,4}(\s|\t)*\-*[a-zäöüß]*\-*(\s|\t)*[0-9]+\:[0-9]+(\s|\t)*[0-9]+\:[0-9]+(\s|\t)*[a-zäöüß0-9(\s|\t)\.]*\/[a-zäöüß0-9(\s|\t)-]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                        // Here we check the Match instance.
                        if (match.Success)
                        {
                            // Finally, we get the Group value and display it.
                            string linePart = match.Groups[0].Value;
                            match = Regex.Match(linePart, @"[0-9]{2,2}\.[0-9]{2,2}\.[0-9]{4,4}", RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                string dateStr = match.Groups[0].Value;
                                line = line.Replace(dateStr, "");
                                match = Regex.Match(line, @"[0-9]+\:[0-9]+", RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    MyTime time1 = new MyTime(match.Groups[0].Value);
                                    string tempTime2 = match.NextMatch().Groups[0].Value;
                                    MyTime time2 = new MyTime(tempTime2);
                                    int posTime2 = line.LastIndexOf(tempTime2.ToString()) + tempTime2.ToString().Length;
                                    line = line.Substring(posTime2).Trim();
                                    string[] nameAndAdress = line.Split('/');

                                    WayType oneWay = WayType.Normal;
                                    if (nameAndAdress.Length < 2)
                                    {
                                        throw new Exception("Cant read name or address in: " + fileName);
                                    }
                                    else if (nameAndAdress.Length > 2)
                                    {
                                        if (nameAndAdress[2].Trim().ToLower() == "onewayto")
                                        {
                                            oneWay = WayType.OneWayTo;
                                        }
                                        else if (nameAndAdress[2].Trim().ToLower() == "onewayback")
                                        {
                                            oneWay = WayType.OneWayBack;
                                        }
                                    }

                                    string nameStr = nameAndAdress[0].Trim();
                                    string addressStr = nameAndAdress[1].Trim();
                                    
                                    // add new address
                                    string firmaName = nameStr;
                                    int counter = 1;
                                    while (!AddressDatabaseManager.addNewAddress(nameStr, addressStr,false))
                                    {
                                        nameStr = firmaName + counter.ToString();
                                        counter++;
                                    }

                                    // convert values
                                    DateTime date = DateTime.Parse(dateStr, CultureInfo.GetCultureInfo("de-DE"));
                                    if (oneWay != WayType.OneWayBack)
                                    {
                                        m_DayPaths[date.DayOfYear - 1].addNewPoint(firmaName, nameStr, time1, oneWay);
                                    }
                                    if (oneWay != WayType.OneWayTo)
                                    {
                                        m_DayPaths[date.DayOfYear - 1].addNewPoint(firmaName, nameStr, time2, oneWay);
                                    }
                                    HeaderHelper.decChecksum();
                                }
                            }
                        }
                    }
                    //load text data file

                    sr.Close();
                }
                HeaderHelper.checkChecksum();
            }
        }

        private void loadAndFilterPrivat(string fileName)
        {
            if (File.Exists(fileName))
            {
                StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding(1252));

                if (sr != null)
                {
                    string line = "";
                    HeaderHelper.initHeader(sr.ReadLine()); // header and checksum 
                    while ((line = sr.ReadLine()) != null)
                    {
                        Match match = Regex.Match(line, @"[0-9]{2,2}\.[0-9]{2,2}\.[0-9]{4,4}(\s|\t)*\-*[a-zäöüß]*\-*(\s|\t)*[0-9]+\:[0-9]+(\s|\t)*[0-9]+\:[0-9]+(\s|\t)*[a-zäöüß0-9(\s|\t)\.]*\/[a-zäöüß0-9(\s|\t)-]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                        // Here we check the Match instance.
                        if (match.Success)
                        {
                            // Finally, we get the Group value and display it.
                            string linePart = match.Groups[0].Value;
                            match = Regex.Match(linePart, @"[0-9]{2,2}\.[0-9]{2,2}\.[0-9]{4,4}", RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                string dateStr = match.Groups[0].Value;
                                line = line.Replace(dateStr, "");
                                match = Regex.Match(line, @"[0-9]+\:[0-9]+", RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    MyTime time1 = new MyTime(match.Groups[0].Value);
                                    string tempTime2 = match.NextMatch().Groups[0].Value;
                                    MyTime time2 = new MyTime(tempTime2);
                                    int posTime2 = line.LastIndexOf(tempTime2.ToString()) + tempTime2.ToString().Length;
                                    line = line.Substring(posTime2).Trim();
                                    string[] nameAndAdress = line.Split('/');

                                    WayType oneWay = WayType.Normal;

                                    if (nameAndAdress.Length > 2)
                                    {
                                        if (nameAndAdress[2].Trim().ToLower() == "onewayto")
                                        {
                                            oneWay = WayType.OneWayTo;
                                        }
                                        else if (nameAndAdress[2].Trim().ToLower() == "onewayback")
                                        {
                                            oneWay = WayType.OneWayBack;
                                        }
                                    }

                                    string nameStr = nameAndAdress[0].Trim();
                                    string addressStr = nameAndAdress[1].Trim();

                                    // add new address
                                    string firmaName = nameStr;
                                    if (!String.IsNullOrEmpty(addressStr))
                                    {
                                        int counter = 1;
                                        while (!AddressDatabaseManager.addNewAddress(nameStr, addressStr, false))
                                        {
                                            nameStr = firmaName + counter.ToString();
                                            counter++;
                                        }
                                    }

                                    // convert values
                                    DateTime date = DateTime.Parse(dateStr, CultureInfo.GetCultureInfo("de-DE"));
                                    if (oneWay != WayType.OneWayBack)
                                    {
                                        m_DayPaths[date.DayOfYear - 1].addNewPoint("privat", nameStr, time1, oneWay);
                                    }
                                    if (oneWay != WayType.OneWayTo)
                                    {
                                        m_DayPaths[date.DayOfYear - 1].addNewPoint("privat", nameStr, time2, oneWay);
                                    }
                                    HeaderHelper.decChecksum();
                                }
                            }
                        }
                    }
                    //load text data file

                    sr.Close();
                }
                HeaderHelper.checkChecksum();
            }
        }

        private void loadAndFilterBenzinTable(string fileName)
        {
            if (File.Exists(fileName))
            {
                StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding(1252));
                if (sr != null)
                {
                    string line = "";
                    HeaderHelper.initHeader(sr.ReadLine()); // header and checksum 
                    while ((line = sr.ReadLine()) != null)
                    {
                        Match match = Regex.Match(line, @"[0-9]{2,2}\.[0-9]{2,2}\.[0-9]{4,4}(\s|\t)+[0-9]+\,[0-9]+(\s|\t)+[0-9]{2,2}\:[0-9]{2,2}(\s|\t)+[a-zäöüß0-9]+(\s|\t)*[a-zäöüß0-9\s\t-]*", RegexOptions.IgnoreCase);

                        // Here we check the Match instance.
                        if (match.Success)
                        {
                            // Finally, we get the Group value and display it.
                            string linePart = match.Groups[0].Value;
                            match = Regex.Match(linePart, @"[0-9]{2,2}\.[0-9]{2,2}\.[0-9]{4,4}", RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                string dateStr = match.Groups[0].Value;
                                linePart = linePart.Replace(dateStr,"");
                                match = Regex.Match(linePart, @"[0-9]+\,[0-9]+", RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    string litrStr = match.Groups[0].Value;
                                    linePart = linePart.Replace(litrStr, "");
                                    match = Regex.Match(linePart, @"[0-9]{2,2}\:[0-9]{2,2}", RegexOptions.IgnoreCase);
                                    if (match.Success)
                                    {
                                        MyTime time1 = new MyTime(match.Groups[0].Value);
                                        linePart = linePart.Replace(time1.ToString(), "");
                                        match = Regex.Match(linePart, @"[a-zäöüß]+[0-9]*(\s|\t)*[a-zäöüß0-9\s\t-]*", RegexOptions.IgnoreCase);
                                        if (match.Success)
                                        {
                                            string addressStr = match.Groups[0].Value;
                                            int position = line.IndexOf(addressStr);
                                            addressStr = line.Substring(position, line.Length - position);
                                            // add new address
                                            if (addressStr.Length > 5)
                                            {
                                                addressStr = addressStr.Trim();
                                                int spacePos = addressStr.IndexOf(' ');
                                                string name = addressStr.Substring(0, spacePos);
                                                string fullName = addressStr.Substring(spacePos + 1, addressStr.Length - spacePos - 1).Trim();
                                                AddressDatabaseManager.addNewAddress(name, fullName, true);

                                                addressStr = name;
                                            }
                                            addressStr = addressStr.Trim();

                                            // convert values
                                            DateTime date = DateTime.Parse(dateStr, CultureInfo.GetCultureInfo("de-DE"));
                                            double benzinIntervalSinceLast = double.Parse(litrStr) * HeaderHelper.getKmForLitr();
                                            m_DayPaths[date.DayOfYear - 1].BenzinIntervalAdd((int)benzinIntervalSinceLast);
                                            // way to tank station
                                            m_DayPaths[date.DayOfYear - 1].addNewPoint(HeaderHelper.getFirmaName(), addressStr, time1);
                                            // second time when i left tank station
                                            m_DayPaths[date.DayOfYear - 1].addNewPoint(HeaderHelper.getFirmaName(), addressStr, MyTime.Add(time1, new MyTime("00:05")));

                                            HeaderHelper.decChecksum();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //load text data file

                    sr.Close();
                }
                m_YearStartKm = HeaderHelper.getStartKm();
                HeaderHelper.checkChecksum();
            }
            
        }

        private static int getDaysInAYear(string _year)
        {
            int year = int.Parse(_year);
            int days = 0;
            for (int i = 1; i <= 12; i++)
            {
                days += DateTime.DaysInMonth(year, i);
            }
            return days;
        }

        
    }
}

