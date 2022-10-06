using Server.DAL.Interfaces;
using Server.FtpServer.DTO;
using Server.Util;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace Server.DAL
{
    public class DB : IDB
    {
        public IList<User> SelectRegisteredUsers()
        {
            var registeredUsers = new List<User>();
            using (var conn = new SqlConnection(ConnectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT * FROM FtpUsers;";
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var user = new User();
                        user.Id = DbHelper.Convert<int>(reader["Id"]);
                        user.Username = DbHelper.Convert<string>(reader["Username"]);
                        user.Hash = DbHelper.Convert<string>(reader["Hash"]);
                        registeredUsers.Add(user);
                    }
                }
            }
            return registeredUsers;
        }

        protected string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["TestDB"].ConnectionString;
            }
        }
    }
}
