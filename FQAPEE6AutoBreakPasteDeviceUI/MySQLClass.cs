using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace FQAPEE6AutoBreakPasteDeviceUI
{
    public class MySQLClass 
    {
        public string StrMySQL = "Server=10.86.16.109;Database=dnc;Uid=dnc;Pwd=qhddnc*168";
        public string FindResult(string barcode)
        {
            MySqlConnection conn = null;
            MySqlDataReader rdr = null;
            string result = "";
            try
            {
                conn = new MySqlConnection(StrMySQL);
                conn.Open();

                string stm = "SELECT TOTAL_DEVICE_LAYOUT FROM dnc.SPI_INI WHERE  BARCODE_ID ='" + barcode + "'";
                MySqlCommand cmd = new MySqlCommand(stm, conn);
                rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    result = rdr.GetString(0);
                }
                else
                {
                    result = "Empty";
                }
            }
            catch (MySqlException ex)
            {

                Console.WriteLine("Error: {0}", ex.ToString());
                result = "Error";
            }
            finally
            {
                if (rdr != null)
                {
                    rdr.Close();
                }
                if (conn != null)
                {
                    conn.Close();
                }
            }
            return result;
        }
        

    }
}
