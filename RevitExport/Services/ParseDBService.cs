using Microsoft.Data.Sqlite;
using RevitExport.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitExport.Services
{
    public class ParseDBService
    {
        private static string ConnectionString = null;
        public static void Initialize()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbTxtPath = Path.Combine(folder, "DBPath.txt");
            string dbPath = File.ReadAllText(dbTxtPath);
            LogService.LogError($"текст из файла:{dbPath}, а вмсе вместе получается:Data Source={dbPath}");
            ConnectionString = $"Data Source={dbPath}";
        }

        public List<NewDocument> GetExportModels(int RVTversion)
        {
            List<NewDocument> models = new List<NewDocument>();

            SqliteConnection connection = null;
            SqliteCommand command = null;
            SqliteDataReader reader = null;

            try
            {
                connection = new SqliteConnection(ConnectionString);
                connection.Open();

                const string query = @"
        SELECT is_export_revit, is_export_navis, rvt_model_name, rvt_version, export_path, model_path 
        FROM revit_export
        WHERE rvt_version = @RVTversion";

                command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@RVTversion", RVTversion); // ✅ добавляем после создания команды

                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var document = new NewDocument
                    {
                        is_export_revit = reader.GetInt32(reader.GetOrdinal("is_export_revit")),
                        is_export_navis = reader.GetInt32(reader.GetOrdinal("is_export_navis")),
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
                LogService.LogError("Не удалось подключиться к БД: " + ex);
                return new List<NewDocument>();
            }
            catch (Exception ex)
            {
                LogService.LogError($"Неизвестная ошибка: {ex}");
                return new List<NewDocument>();
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (command != null)
                    command.Dispose();
                if (connection != null)
                    connection.Dispose();
            }
        }


        public void ReturnZeroExport()
        {
            SqliteConnection connection = null;
            SqliteTransaction transaction = null;
            try
            {
                connection = new SqliteConnection(ConnectionString);
                connection.Open();

                transaction = connection.BeginTransaction();

                // 1. Сбросить все is_export = 0
                string resetQuery = "UPDATE revit_export SET is_export_revit = 0, is_export_navis = 0";
                using (SqliteCommand resetCmd = new SqliteCommand(resetQuery, connection, transaction))
                {
                    resetCmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch (SqliteException ex)
            {
                if (transaction != null)
                    transaction.Rollback();
                LogService.LogError("Не удалось подключиться к БД" + ex);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    transaction.Rollback();
                LogService.LogError($"Неизвестная ошибка: {ex}");
            }
            finally
            {
                transaction?.Dispose();
                connection?.Dispose();
            }
        }
    }
}
