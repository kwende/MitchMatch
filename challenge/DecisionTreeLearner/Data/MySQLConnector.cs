using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace DecisionTreeLearner.Data
{
    public class MySQLConnector : IDisposable
    {
        private MySqlConnection _connection;

        ~MySQLConnector()
        {
            InnerDispose(true); 
        }

        public static MySQLConnector Connect()
        {
            MySqlConnectionStringBuilder connString = new MySqlConnectionStringBuilder();
            connString.Server = "www.ben-rush.net";
            connString.Database = "MitchMatch";
            connString.UserID = "mitchmatch";
            connString.Password = "lucasbutthole";

            MySQLConnector connector = new MySQLConnector();
            connector._connection = new MySqlConnection(connString.ToString());
            connector._connection.Open();

            return connector;
        }

        private void InnerDispose(bool fromGC)
        {
            if(!fromGC)
            {
                GC.SuppressFinalize(this); 
            }

            _connection.Close(); 
        }

        public void Dispose()
        {
            InnerDispose(false); 
        }
    }
}
