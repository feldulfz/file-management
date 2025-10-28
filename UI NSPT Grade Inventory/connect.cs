using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace UI_NSPT_Grade_Inventory
{
    internal class connect
    {
        public MySqlConnection con;
        public MySqlCommand cmd;
        public MySqlDataReader reader;
        public void connection()
        {
            try
            {
                //con = new MySqlConnection("datasource=localhost; database=pdf_inventory_trial_1; port=3306; username=root; password=password123");
                con = new MySqlConnection("datasource=localhost; database=pdf_inventory_trial_2; port=3306; username=nstp; password=b7y{IEw#X0!xG5H");
                con.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PDF INVENTORY TRIAL 1", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void closeConnection()
        {
            con.Close();
        }

        public void datasend(string sql) // for adding data
        {
            connection();
            cmd = new MySqlCommand(sql, con);
            cmd.ExecuteNonQuery(); // for SQL commands that modify data or perform actions on the 
            con.Close();
        }

        public void dataupdate(string sql) // for updating data
        {
            connection();
            cmd = new MySqlCommand(sql, con);
            reader = cmd.ExecuteReader();  // for SQL commands that retrieve data from the database.
            con.Close();
        }

        public void datadelete(string sql) // for deleting data
        {
            connection();
            cmd = new MySqlCommand(sql, con);
            reader = cmd.ExecuteReader();  // for SQL commands that retrieve data from the database.
            con.Close();
        }
    }
}
