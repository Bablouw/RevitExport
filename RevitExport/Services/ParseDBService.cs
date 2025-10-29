using Microsoft.Data.Sqlite;
using RevitExport.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitExport.Services
{
    public class ParseDBService
    {
        private const string ConnectionString = @"Data Source=C:\RevitExportTest\ExportDB.db";

        public List<NewDocument> GetExportModels ()
        {
            List <NewDocument> models = new List <NewDocument> ();

            SqliteConnection connection = null;
            SqliteCommand command = null;
            SqliteDataReader reader = null;
            try
            {
                connection = new SqliteConnection(ConnectionString);
                connection.Open();

                const string query = @"
                    SELECT is_export, rvt_model_name, rvt_version, export_path, model_path 
                    FROM revit_export
                    WHERE is_export = 1";
                command = new SqliteCommand(query, connection);
                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var document = new NewDocument
                    {
                        IsExport = reader.GetInt32(reader.GetOrdinal("is_export")),
                        rvt_model_name = reader.GetString(reader.GetOrdinal("rvt_model_name")),
                        rvt_version = reader.GetInt32(reader.GetOrdinal("rvt_version")),
                        export_path = reader.IsDBNull(reader.GetOrdinal("export_path"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("export_path")),
                        model_path = reader.IsDBNull(reader.GetOrdinal("model_path"))
                           ? null
                           : reader.GetString(reader.GetOrdinal("model_path"))
                    };
                    models.Add(document);
                }
                return models; 
            }
            catch (SqliteException ex)
            {
                LogService.LogError("Не удалось подключиться к БД" + ex);
                return new List<NewDocument>();
            }
            catch (Exception ex)
            {
                LogService.LogError($"Неизвестная ошибка: {ex}");
                return new List<NewDocument>();
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
