using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using FahrtenbuchHelper.Intervals;
using FahrtenbuchHelper.Utils;

using System.IO;

namespace FahrtenbuchHelper
{
    public partial class Form1 : Form
    {
        string m_year = "2012";
        int kmDiffMin = -50;
        int kmDiffMax = 200;

        public Form1()
        {
            InitializeComponent();
            
        }
       
        YearPath m_currentYearPath = null;
        private void button1_Click(object sender, EventArgs e)
        {
            m_currentYearPath = new YearPath("2012");

            drawYear(m_currentYearPath);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //m_currentYearPath = new YearPath("2010");
            if (m_currentYearPath == null)
            {
                MessageBox.Show("You must calculate results first press show results button");
            }
            else
            {
                printYear(m_currentYearPath);
            }
        }

        public void drawYear(YearPath _yearPath)
        {
            int selectedIndex = -1;
            listView1.Clear();
            listView1.Columns.Clear();
            listView1.Columns.Add("Date");
            listView1.Columns.Add("KmShould");
            listView1.Columns.Add("KmWas");
            listView1.Columns.Add("Diff");
            listView1.Columns.Add("Fahrts_a_Day");
            DateTime dateTime = new DateTime(int.Parse(_yearPath.Year), 1, 1);
            int yearKmInterval = 0;
            int yearKmBenzin = HeaderHelper.getRestKm();
            for (int n = 0; n < _yearPath.DayPaths.Length; n++)
            {
                DayPath dayPath = _yearPath.DayPaths[n];
                DateTime date = dateTime.AddDays(n);
                ListViewItem item = new ListViewItem(date.ToString("dd.MM.yy"));
                if (dayPath.BenzinIntervalSinceLast != -1)
                {
                    yearKmBenzin += dayPath.BenzinIntervalSinceLast;
                    item.SubItems.Add(yearKmBenzin.ToString() + "," + dayPath.BenzinIntervalSinceLast.ToString());
                }
                else
                {
                    item.SubItems.Add("");
                }
                if (dayPath.DayPaths != null && dayPath.DayPaths.Count > 0)
                {
                    foreach (OnePath onePath in dayPath.DayPaths)
                    {
                        if (onePath.KmInterval == -1)
                        {
                            throw new Exception("onePath.KmInterval == -1");
                        }
                        yearKmInterval += onePath.KmInterval;
                    }
                    item.SubItems.Add(yearKmInterval.ToString());
                }
                else
                {
                    item.SubItems.Add("");
                }

                if (yearKmBenzin - yearKmInterval > kmDiffMax ||
                    yearKmBenzin - yearKmInterval < kmDiffMin)
                {
                    item.SubItems.Add("*** " + (yearKmBenzin - yearKmInterval).ToString());
                    if (selectedIndex == -1)
                    {
                        selectedIndex = listView1.Items.Count;
                    }
                }
                else
                {
                    item.SubItems.Add((yearKmBenzin - yearKmInterval).ToString());
                }


                int pathCount = dayPath.DayPaths.Count;
                
                if (date.DayOfWeek == DayOfWeek.Sunday && pathCount > 2)
                {
                    item.SubItems.Add("sun ***" + pathCount.ToString());
                }
                else if (date.DayOfWeek == DayOfWeek.Sunday && pathCount > 0)
                {
                    item.SubItems.Add("sun " + pathCount.ToString());
                }
                else if (pathCount > 2)
                {
                    item.SubItems.Add("***" + pathCount.ToString());
                }
                else
                {
                    item.SubItems.Add("");
                }

                if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    item.BackColor = Color.LightPink;
                }

                if (!String.IsNullOrEmpty(dayPath.CheckErrors()))
                {
                    item.BackColor = Color.Red;
                    if (selectedIndex == -1)
                    {
                        selectedIndex = listView1.Items.Count;
                    }
                }

                listView1.Items.Add(item);
            }

            listView1.SelectedIndices.Clear();
            if (selectedIndex > 0)
            {
                listView1.Items[selectedIndex].EnsureVisible();
            }
        }

        public void printYear(YearPath _yearPath)
        {
            DateTime dateTime = new DateTime(int.Parse(_yearPath.Year), 1, 1);
            int yearKmInterval = _yearPath.YearStartKm;

            string fileName = Path.Combine(_yearPath.WorkingPath, "result.txt");
            //StreamWriter sw = File.CreateText();
            //sw.Close();
            StreamWriter sw = new StreamWriter(fileName,false, Encoding.GetEncoding(1252));

            for (int n = 0; n < _yearPath.DayPaths.Length; n++)
            {
                DayPath dayPath = _yearPath.DayPaths[n];
                DateTime date = dateTime.AddDays(n);
            
                if (dayPath.DayPaths != null && dayPath.DayPaths.Count > 0)
                {
                    foreach (OnePath onePath in dayPath.DayPaths)
                    {
                        if (onePath.KmInterval == -1)
                        {
                            throw new Exception("onePath.KmInterval == -1");
                        }

                        
                        sw.Write(date.ToString("dd.MM.yy") + "  ");
                        sw.Write(onePath.StartPoint.PointTime.ToString() + "  ");
                        sw.Write(onePath.EndPoint.PointTime.ToString() + "  ");
                        sw.Write(addSpaces(onePath.EndPoint.FirmaName, 30) );
                        sw.Write(addSpaces(AddressDatabaseManager.getFullAddressForPrint(onePath.EndPoint.Address), 40));
                        if (onePath.EndPoint.FirmaName.ToLower() == "privat")
                        {
                            sw.Write("privat      " + "  "); 
                        }
                        else
                        {
                            sw.Write("geschäftlich" + "  "); //TODO OLEG
                        }
                        sw.Write(addSpaces(yearKmInterval.ToString(),10));
                        sw.Write(addSpaces(onePath.KmInterval.ToString(),5));

                        yearKmInterval += onePath.KmInterval;

                        sw.Write(addSpaces(yearKmInterval.ToString(), 10));

                        sw.WriteLine("Orlov"); //TODO OLEG
                    }
                }

            }

            sw.Close();
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            int selectedIndex = -1;
            if (listView1.SelectedIndices.Count > 0 &&
                (selectedIndex = listView1.SelectedIndices[0]) > -1)
            {
                DateTime date = DateTime.Parse(listView1.Items[selectedIndex].Text);
                int day = date.DayOfYear - 1;
                List<OnePath> paths = m_currentYearPath.DayPaths[day].DayPaths;
                listView2.Clear();
                listView2.Columns.Clear();
                listView2.Columns.Add("Start address", (int)(listView2.Width / 4.2));
                listView2.Columns.Add("Start time", (int)(listView2.Width / 4.2));
                listView2.Columns.Add("End address", (int)(listView2.Width / 4.2));
                listView2.Columns.Add("End time", (int)(listView2.Width / 4.2));
                foreach (OnePath path in paths)
                {
                    ListViewItem item = new ListViewItem(AddressDatabaseManager.getFullAddress(path.StartPoint.Address));
                    item.SubItems.Add(path.StartPoint.PointTime.ToString());
                    item.SubItems.Add(AddressDatabaseManager.getFullAddress(path.EndPoint.Address));
                    item.SubItems.Add(path.EndPoint.PointTime.ToString());
                    
                    
                    
                    if (!String.IsNullOrEmpty(path.CheckErrors()))
                    {
                        item.BackColor = Color.Red;
                    }
                    item.Tag = path;
                    listView2.Items.Add(item);
                }
            }
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = -1;
            if (listView2.SelectedIndices.Count > 0 &&
                (selectedIndex = listView2.SelectedIndices[0]) > -1)
            {
                OnePath path = listView2.Items[selectedIndex].Tag as OnePath;

                textBox1.Text = path.CheckErrors();
            }
        }



        private static string addSpaces(string _text, int size)
        {
            string result = _text;
            int count = size - _text.Length;
            for (int n = 0; n < count; n++)
            {
                result += " ";
            }
            return result;
        }
    }
}
