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
        private const string ConnectionString = @"Data Source=R:\01_Database\01_Soft\03_MEP\DB\ModelsDataTest.db";

        public string GetModelMappingPath(string modelName)
        {
            SqliteConnection connection = null;
            SqliteCommand command = null;
            try
            {
                connection = new SqliteConnection(ConnectionString);
                connection.Open();

                const string query = "SELECT ws_mapping_path FROM revit_model WHERE model_name = @modelName;";
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
                LogService.LogError($"Неизвестная ошибка: {ex}");
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
            const string ConnectionString = @"Data Source=R:\01_Database\01_Soft\03_MEP\DB\ModelsDataTest.db"; // Замени на реальный, или сделай параметром/полем класса

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                try
                {
                    const string query1 = "SELECT model_serverpath FROM revit_model WHERE model_name = @modelName;";
                    using (SqliteCommand command = new SqliteCommand(query1, connection))
                    {
                        command.Parameters.AddWithValue("@modelName", modelName);
                        object result1 = command.ExecuteScalar();

                        const string query2 = "SELECT server_host FROM revit_model WHERE model_name = @modelName;";
                        command.CommandText = query2; // Переиспользуем command
                        command.Parameters.Clear(); // Очищаем параметры, чтобы избежать ошибок
                        command.Parameters.AddWithValue("@modelName", modelName);
                        object result2 = command.ExecuteScalar();

                        string path = result1?.ToString() ?? string.Empty;
                        string host = result2?.ToString() ?? string.Empty;
                        return string.IsNullOrEmpty(path) || string.IsNullOrEmpty(host) ? string.Empty : $"RSN://{host}/{path}";
                    }
                }
                catch (SqliteException ex)
                {
                    LogService.LogError($"Не удалось подключиться к БД: {ex.Message}");
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    LogService.LogError($"Неизвестная ошибка: {ex.Message}");
                    return string.Empty;
                }
            }
        }
    }
}
