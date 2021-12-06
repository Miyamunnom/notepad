using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace notepad
{
    class DBMySQLUtils
    {
        public static MySqlConnection GetDBConnection(string host, int port, string database, string username, string password)
        {
            //Строка соединения (Проще говоря ссылка, как обращаться к MySQL)
            String connString = "Server=" + host + ";Database=" + database
                + ";port=" + port + ";User Id=" + username + ";password=" + password;

            //Создание соединения
            MySqlConnection conn = new MySqlConnection(connString);

            //отправка этого соединения при вызове
            return conn;
        }
    }
}
