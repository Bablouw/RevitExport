using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkManager.Services
{
    public class ParseDBService
    {
        private const string ConnectionString = @"Data Source=C:\Test\ModelsDataTest.db";

        public string GetModelMappingPath(string modelName)
        {
            SqliteConnection connection = null;
            SqliteCommand command = null;
            try
            {
                connection = new SqliteConnection(ConnectionString);
                connection.Open();

                const string query = "SELECT model_mapping_path FROM revit_model WHERE model_name = @modelName;";
                command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@modelName", modelName);

                object result = command.ExecuteScalar();

                return result?.ToString(); // Вернёт null, если строка не найдена или значение null
            }
            catch (SqliteException ex)
            {
                LogService.LogError("Не удалось подключиться к БД" + ex);
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogService.LogError("Неизвестная ошибка");
                return string.Empty;
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                if (connection != null)
                    connection.Dispose();
            }
        }

        public string GetModelServerPath(string modelName)
        {
            SqliteConnection connection = null;
            SqliteCommand command = null;
            try
            {
                connection = new SqliteConnection(ConnectionString);
                connection.Open();
                const string query = "SELECT model_serverpath FROM revit_model WHERE model_name = @modelName;";
                command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@modelName", modelName);
                object result = command.ExecuteScalar();
                return result?.ToString();
            }
            catch (SqliteException ex)
            {
                LogService.LogError("Не удалось подключиться к БД" + ex);
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogService.LogError("Неизвестная ошибка");
                return string.Empty;
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                if (connection != null)
                    connection.Dispose();
            }
        }
    }
}
