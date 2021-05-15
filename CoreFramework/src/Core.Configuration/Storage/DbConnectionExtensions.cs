using System.Data;

namespace Core.Configuration.Storage
{
    public static class DbConnectionExtensions
    {
        public static int ExecuteNonQuery(this IDbConnection connection, string sql, params object[] sqlParams)
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            foreach (var param in sqlParams)
            {
                command.Parameters.Add(param);
            }
            return command.ExecuteNonQuery();
        }

        public static IDataReader ExecuteQuery(this IDbConnection connection, string sql, params object[] sqlParams)
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            foreach (var param in sqlParams)
            {
                command.Parameters.Add(param);
            }
            return command.ExecuteReader();
        }
    }
}
