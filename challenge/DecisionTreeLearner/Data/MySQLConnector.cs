using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using DecisionTreeLearner.DataTypes.MySQL;
using DecisionTreeLearner.DataTypes;

namespace DecisionTreeLearner.Data
{
    public class MySQLConnector : IDisposable
    {
        private MySqlConnection _connection;

        ~MySQLConnector()
        {
            InnerDispose(true);
        }

        public Record GetRecordById(int recordId)
        {
            Record ret = new Record();

            using (MySqlCommand command = _connection.CreateCommand())
            {
                command.CommandText = "select * from app_record where id = @id";
                command.Parameters.AddWithValue("id", recordId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();

                    ret = ReaderToObject.ReaderToRecord(reader);
                }
            }

            return ret;
        }

        public List<Record> GetSetFromSetMember(Record setMember)
        {
            List<Record> ret = new List<Record>();

            List<Record> recordIds = new List<Record>();
            using (MySqlCommand command = _connection.CreateCommand())
            {
                command.CommandText = "select * from app_record where id in " +
                    "(select recordid_id from app_setmember where setid_id = " +
                        "(select setid_id from app_setmember where recordid_id = " +
                            "(select id from app_record where enterpriseid = @enterpriseId)))";
                command.Parameters.AddWithValue("enterpriseId", setMember.EnterpriseId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(ReaderToObject.ReaderToRecord(reader));
                    }
                }
            }

            return ret;
        }

        public List<Record> GetSetFromId(int setId)
        {
            List<Record> ret = new List<Record>();

            List<int> recordIds = new List<int>();
            using (MySqlCommand command = _connection.CreateCommand())
            {
                command.CommandText = "select RecordId_id from app_setmember where SetId_id = @setId";
                command.Parameters.AddWithValue("setId", setId);

                using (MySqlDataReader setReader = command.ExecuteReader())
                {
                    while (setReader.Read())
                    {
                        int recordId = (int)setReader["RecordId_id"];
                        recordIds.Add(recordId);
                        //ret.Add(GetRecordById(recordId));
                    }
                }
            }

            foreach (int recordId in recordIds)
            {
                ret.Add(GetRecordById(recordId));
            }

            return ret;
        }

        public List<SetWithPossibleOthers> GetSetsWithPossibleOthers()
        {
            List<SetWithPossibleOthers> ret = new List<SetWithPossibleOthers>();

            Dictionary<int, List<int>> setsWithOthers = new Dictionary<int, List<int>>();
            using (MySqlCommand command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT CorrespondingRecord_id, CorrespondingSet_id FROM " +
                    "MitchMatch.app_mlfoundextrasetmember where ReviewedStatus = 1";
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int recordId = (int)reader["CorrespondingRecord_id"];
                        int setId = (int)reader["CorrespondingSet_id"];

                        if (!setsWithOthers.ContainsKey(setId))
                        {
                            setsWithOthers.Add(setId, new List<int>());
                        }

                        setsWithOthers[setId].Add(recordId);
                    }
                }
            }

            foreach (KeyValuePair<int, List<int>> setWithOthers in setsWithOthers)
            {
                SetWithPossibleOthers setWithPossibleOthers = new SetWithPossibleOthers();
                setWithPossibleOthers.OriginalSet = GetSetFromId(setWithOthers.Key);
                setWithPossibleOthers.PossibleMatches = new List<Record>();
                foreach (int recordId in setWithOthers.Value)
                {
                    setWithPossibleOthers.PossibleMatches.Add(GetRecordById(recordId));
                }

                ret.Add(setWithPossibleOthers);
            }

            return ret;
        }

        public static MySQLConnector Connect()
        {
            MySqlConnectionStringBuilder connString = new MySqlConnectionStringBuilder();
            connString.Server = "www.ben-rush.net";
            connString.Database = "MitchMatch";
            connString.UserID = "mitchmatch";
            connString.Password = "lucasbutthole";
            connString.SslMode = MySqlSslMode.None;
            connString.AllowBatch = true;

            MySQLConnector connector = new MySQLConnector();
            connector._connection = new MySqlConnection(connString.ToString());
            connector._connection.Open();

            return connector;
        }

        private void InnerDispose(bool fromGC)
        {
            if (!fromGC)
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
