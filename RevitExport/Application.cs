using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitExport.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using Application = Autodesk.Revit.ApplicationServices.Application;
using View = Autodesk.Revit.DB.View;

namespace RevitExport
{
    public class ModelCleaner : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {

            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {

            application.ControlledApplication.ApplicationInitialized += ApplicationInitialized;
            return Result.Succeeded;
        }

        public void ApplicationInitialized(object sender, EventArgs e)
        {
            Application app = sender as Application;
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbTxtPath = Path.Combine(folder, "DBPath.txt");
            string content = File.ReadAllText(dbTxtPath);
            content = content.Replace("\\ExportDB.db", "");
            //Document doc = null;
            LogService.Initialize(content);
            LogService.LogError("инициализация прошла");


            //LogService.LogError($"имя файла - {dbPath}");

            RevitExportCommand revitExport = new RevitExportCommand();
            revitExport.ExecuteScript(app);
            
            
        }

    }
    public class WarningResolver : IFailuresPreprocessor
    {
        // Словарь известных ошибок (GUID -> действие)
        private static readonly Dictionary<Guid, Action<FailuresAccessor, FailureMessageAccessor>> KnownFailures =
            new Dictionary<Guid, Action<FailuresAccessor, FailureMessageAccessor>>
            {
                { Guid.Parse("ce3275c6-1c51-402e-8de3-df3a3d566f5c"), (a,f) => a.DeleteWarning(f) }, // Область неправильно окружена
                { Guid.Parse("4f0bba25-e17f-480a-a763-d97d184be18a"), (a,f) => a.DeleteWarning(f) }, // Марка помещения вне элемента
                { Guid.Parse("83d4a67c-818c-4291-adaf-f2d33064fea8"), (a,f) => a.DeleteWarning(f) }, // Несколько помещений
                { Guid.Parse("a359f2bb-aaa4-47dc-b33b-177a073698a8"), (a,f) => a.DeleteWarning(f) }, // Удалён сегмент размера
                { Guid.Parse("8695a52f-2a88-4ca2-bedc-3676d5857af6"), (a,f) => a.DeleteWarning(f) }, // Выделенные перекрытия пересекаются
                { Guid.Parse("249aaf1d-3f4b-4f27-a67a-c45a9d888cf7"), (a,f) => a.DeleteWarning(f) },// Расчеты Помещение удовлетворительны только без учета следующих параметров
                { Guid.Parse("c3aa4692-975c-4720-8cb4-389532cf43a4"), (a,f) => a.DeleteWarning(f) },
                { Guid.Parse("f654a048-6a1d-4456-ab83-1ec1b5203f7a"), (a,f) => a.DeleteWarning(f) },// какая то ошибка в эом
                { Guid.Parse("cbd5deb4-c4a3-4b95-abfe-7eeb9dc3f06c"), (a,f) => a.DeleteWarning(f) }, // Помещение не окржуено
                { Guid.Parse("a22de05c-4c92-4bdc-9ce3-a965d2cf316c"), (a,f) => a.DeleteWarning(f) }, //Перекрытие объемов Помещение. Скорректируйте свойства "Верхний предел" и "Смещение предела" Помещения.
                { Guid.Parse("b44c8ba0-7a86-44c1-bbf1-2de8e2017266"), (a,f) => a.DeleteWarning(f) }, //Один или несколько опорных элементов размеров сейчас некорректны.
                { Guid.Parse("8a9ff20d-fdc2-4f98-87e6-2aa8b71b0c83"), (a,f) => a.DeleteWarning(f) }, //Один или несколько опорных элементов размеров сейчас некорректны.
                { Guid.Parse("e431b9e1-9a4d-469f-81b3-51432f904a65"), (a,f) => a.DeleteWarning(f) } // Выделенная геометрия больше не описывает плоскость
                

            };

        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            try
            {
                // Получаем сообщения один раз
                var failures = accessor.GetFailureMessages();
                if (failures == null || failures.Count == 0)
                    return FailureProcessingResult.Continue;

                // Группируем по типу ошибки
                var groupedFailures = failures
                    .GroupBy(f => f?.GetFailureDefinitionId())
                    .Where(g => g.Key != null);

                foreach (var group in groupedFailures)
                {
                    var firstFailure = group.First();
                    var id = group.Key;
                    var guid = id.Guid;

                    // Для неизвестных ошибок показываем информацию
                    if (!KnownFailures.ContainsKey(guid))
                    {
                        ShowUnknownFailure(firstFailure, id);
                        continue;
                    }

                    // Обрабатываем всю группу
                    foreach (var failure in group)
                    {
                        try
                        {
                            KnownFailures[guid](accessor, failure);
                        }
                        catch
                        {
                            // Пропускаем проблемные элементы
                            continue;
                        }
                    }
                }

                return FailureProcessingResult.Continue;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Ошибка обработчика: {e.Message}");
                return FailureProcessingResult.Continue;
            }
        }

        private void ShowUnknownFailure(FailureMessageAccessor failure, FailureDefinitionId id)
        {
            string typeName = GetBuiltInFailureName(id);
            string description = failure.GetDescriptionText();

            Autodesk.Revit.UI.TaskDialog.Show("Новый тип ошибки",
                $"GUID: {id.Guid}\n" +
                $"Тип: {typeName}\n" +
                $"Описание: {description}\n\n" +
                $"Добавьте этот GUID в словарь KnownFailures");

            LogService.LogError($"Добавьте этот GUID в словарь {id.Guid}");
            //// Логирование в файл
            //try
            //{
            //    File.AppendAllText("RevitUnknownErrors.log",
            //        $"[{DateTime.Now}] {id.Guid} - {typeName}\n{description}\n\n");
            //}
            //catch { /* Игнорируем ошибки записи */ }
        }

        public static string GetBuiltInFailureName(FailureDefinitionId id)
        {
            try
            {
                Guid targetGuid = id.Guid;
                var builtInFailuresType = typeof(BuiltInFailures);

                foreach (var nestedType in builtInFailuresType.GetNestedTypes(BindingFlags.Public))
                {
                    foreach (var field in nestedType.GetFields(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (field.FieldType == typeof(FailureDefinitionId))
                        {
                            var value = (FailureDefinitionId)field.GetValue(null);
                            if (value.Guid == targetGuid)
                            {
                                return $"BuiltInFailures.{nestedType.Name}.{field.Name}";
                            }
                        }
                    }
                }
            }
            catch { }

            return "Unknown (Custom Error)";
        }
    }

}
