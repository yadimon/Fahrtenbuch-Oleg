using System;
using System.Windows.Forms;

namespace FahrtenbuchHelper.Utils
{
    internal static class HeaderHelper
    {
        private static int ms_checksum = -1;
        private static String ms_FirmaName = "";
        private static String ms_Address = "";
        private static int ms_StartKm = -1;
        private static int ms_RestKm = 0;
        private static double m_KmForLitr = 0; 

        public static void initHeader(string line)
        {
            try
            {
                line = line.Trim();
                string[] strHeader = line.Split('/');
                if (!line.StartsWith("checksum"))
                {
                    throw new Exception("Cant read checksum");
                }
                ms_checksum = int.Parse(strHeader[1]);
                //if (strHeader.Length > 2)
                {
                    ms_FirmaName = strHeader[2];
                }
                //if (strHeader.Length > 3)
                {
                    ms_Address = strHeader[3];
                }
                if (strHeader.Length > 4)
                {
                    ms_StartKm = int.Parse(strHeader[4]);
                }
                if (strHeader.Length > 5)
                {
                    ms_RestKm = int.Parse(strHeader[5]);
                }
                if (strHeader.Length > 6)
                {
                    m_KmForLitr = double.Parse(strHeader[6]);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static String getFirmaName()
        {
            return ms_FirmaName;
        }

        public static String getFullAdress()
        {
            return ms_Address;
        }

        public static int getStartKm()
        {
            return ms_StartKm;
        }

        public static int getRestKm()
        {
            return ms_RestKm;
        }

        public static double getKmForLitr()
        {
            return m_KmForLitr;
        }

        public static void decChecksum()
        {
            ms_checksum--;
        }

        public static void checkChecksum()
        {
            if (ms_checksum != 0)
            {
                MessageBox.Show("Wrong checksum, difference is: " + ms_checksum + " " + ms_FirmaName);
            }
        }      

    }
}

