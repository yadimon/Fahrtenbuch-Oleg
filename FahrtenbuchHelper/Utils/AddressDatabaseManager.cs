using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FahrtenbuchHelper.Utils
{
    public static class AddressDatabaseManager
    {
        private static DataTable ms_AddressTable = null;
        private static DataTable ms_DistanceTable = null;
        //private static List<String> ms_CityDictionary = new List<String>();

        private static string ms_Directory = null; 

        public static void initDatabaseManager(string directory)
        {
            ms_Directory = directory;

            ms_AddressTable = new DataTable("Addresses");
            ms_AddressTable.Columns.Add("name", typeof(string));
            ms_AddressTable.Columns.Add("fullname", typeof(string));

            ms_DistanceTable = new DataTable("Distances");
            ms_DistanceTable.Columns.Add("address1", typeof(string));
            ms_DistanceTable.Columns.Add("address2", typeof(string));
            ms_DistanceTable.Columns.Add("distance", typeof(int));
            ms_DistanceTable.Columns.Add("time", typeof(string)); //dd:hh:mm

            addNewAddress("home", "Zerzabelshofstr 29", true);
            addNewAddress("home2", "Zerzabelshofstr 29", true);
            loadData(ms_Directory);

            //ms_CityDictionary.Add("nürnberg");
            //ms_CityDictionary.Add("erlangen");
            //ms_CityDictionary.Add("regensburg");
            //ms_CityDictionary.Add("darmstadt");


        }

        public static bool addNewAddress(string name, string fullname, bool showMessage)
        {
            DataRow[] rows = ms_AddressTable.Select("name='" + name + "'");
            if (rows.Length == 0)
            {
                ms_AddressTable.Rows.Add(name, fullname);
            }
            else
            {
                if (showMessage)
                {
                    MessageBox.Show("Name: " + fullname + " already exists");
                }
                return false;
            }
            return true;
        }

        public static string getFullAddress(string name)
        {
            DataRow[] rows = ms_AddressTable.Select("name='" + name + "'");
            if (rows.Length == 1)
            {
                return rows[0].ItemArray[1].ToString();
            }
            else
            {
                MessageBox.Show("Error in database, row count: " + rows.Length + ". Name: " + name);
            }
            return null;
        }

        /*private static bool ContainsCity(String adress)
        {
            foreach (String city in ms_CityDictionary)
            {
                if (adress.ToLower().Contains(city))
                {
                    return true;
                }
            }
            return false;
        }*/

        public static string getFullAddressForPrint(string name)
        {
            string fullName = getFullAddress(name);
            if (fullName.Contains("[") &&
                fullName.Contains("]"))
            {
                string tempName = fullName.Replace("[", "").Replace("]", "").Replace("Nürnberg", "");
                return tempName;
            }
            return fullName;
        }

        public static string getFullAddressForRoute(string name)
        {
            string fullName = getFullAddress(name);
            if (fullName.Contains("[") &&
                fullName.Contains("]"))
            {
                string tempName = fullName.Replace("[", "").Replace("]", "");
                return tempName;
            }
            else
            {
                return fullName + " Nürnberg";
            }
        }

        private static void addNewDistance(string address1, string address2, int distance, string time)
        {
            DataRow[] rows = ms_DistanceTable.Select("address1='" + address1 + "' and address2='" + address2 + "'");
            if (rows.Length == 0)
            {
                ms_DistanceTable.Rows.Add(address1, address2, distance, time);
            }
            else
            {
                MessageBox.Show("Distance between " + address1 + " and " + address2 + " already exists");
            }
        }

        public static void getDistance(string address1, string address2, out int outDistance, out string outTime)
        {
            outDistance = -1;
            outTime = "";
            DataRow[] rows = ms_DistanceTable.Select("address1='" + address1 + "' and address2='" + address2 + "'");
            if (rows.Length == 1)
            {
                outDistance = int.Parse(rows[0].ItemArray[2].ToString());
                outTime = rows[0].ItemArray[3].ToString();
            }
            else if (rows.Length == 0)
            {
                string outStrDistance = "";
                string outStrTime = "";
                if (GetRouteInformation(address1, address2, out outStrDistance, out outStrTime))
                {
                    outDistance = IntParserHelper(outStrDistance);
                    outTime = IntParserTimeHelper(outStrTime);
                    addNewDistance(address1, address2, outDistance, outTime);
                }
            }
            else
            {
                MessageBox.Show("Error in database, row count: " + rows.Length + ". Names: " + address1 + ", " + address2);
            }
        }

        private static bool GetRouteInformation(string address1, string address2, out string outDistance, out string outTime)
        { 
            XDocument doc = XDocument.Load(String.Format(@"http://maps.google.com/maps/api/directions/xml?origin={0}&destination={1}&sensor=false",
                                               getFullAddressForRoute(address1),
                                               getFullAddressForRoute(address2)));
            XElement dirResp = doc.Element(XName.Get("DirectionsResponse"));  
            XElement eStatus = dirResp.Element(XName.Get("status"));  
            if (eStatus.Value == "OK")
            {
                XElement eRoute = dirResp.Element(XName.Get("route"));
                XElement eLeg = eRoute.Element(XName.Get("leg"));
                XElement eDuration = eLeg.Element(XName.Get("duration"));
                XElement eDistance = eLeg.Element(XName.Get("distance"));
                outDistance = eDistance.Element(XName.Get("text")).Value;
                outTime = eDuration.Element(XName.Get("text")).Value;
                return true;
            }
            else
            {
                string text = eStatus.Value + ". Addresses: " + getFullAddress(address1) + ", " + getFullAddress(address2);
                DialogResult dlg = MessageBox.Show(text, "No rute found. Try again?", MessageBoxButtons.YesNo);
                if (dlg == DialogResult.Yes)
                {
                    return GetRouteInformation(address1, address2, out outDistance, out outTime);    
                }
                
                saveData();
                Environment.Exit(0);
            }
            outDistance = "";
            outTime = "";
            return false;
        }

        private static int IntParserHelper(string text)
        {
            text = text.Trim();
            int pos = text.IndexOf(' ');
            text = text.Substring(0,pos);
            CultureInfo cult = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            cult.NumberFormat.NumberDecimalSeparator = ".";
            double valueDouble = double.Parse(text, cult.NumberFormat);
            return (int)Math.Ceiling(valueDouble);
        }

        private static string IntParserTimeHelper(string text)
        {
            return Regex.Replace(text, "[s*[A-Za-z]]s*", ":");
        }

        private static void loadData(string directory)
        {
            string fileName = Path.Combine(directory,"distance.xml");
            if (File.Exists(fileName))
            {
                ms_DistanceTable.ReadXml(fileName);
            }
        }

        public static void saveData()
        {
            string fileName = Path.Combine(ms_Directory, "distance.xml");
            ms_DistanceTable.WriteXml(fileName);
        }
    }
}

