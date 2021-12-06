using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace notepad
{
    class DBUtils
    {
        //Метод для создание соединения с БД
        public static MySqlConnection GetDBConnection()
        {
            //Адрес где находится БД (У меня на компьютере, поэтому localhost)
            string host = "localhost";
            //Порт (Не трогать, это порт MySQL)
            int port = 3306;
            //Название БД, где находятся таблицы
            string database = "notepad";
            //Название пользователя, у которого есть права для этой БД
            string username = "root";
            //Пароль от пользователя
            string password = "";

            //Создание соединения и его возвращение по вызову (Когда вызывается этот метод, будет отдано соединение с БД)
            return DBMySQLUtils.GetDBConnection(host, port, database, username, password);
        }
    }
}
