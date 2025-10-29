using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitExport.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Application = Autodesk.Revit.ApplicationServices.Application;
using View = Autodesk.Revit.DB.View;

namespace RevitExport
{
    public class ModelCleaner : IExternalApplication
    {
        string successMessage = "";
        string errorMessage = "";
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
            //Document doc = null;
            LogService.Initialize("C:\\Test_download");
            LogService.LogError("инициализация прошла");
            RevitExportCommand revitExport = new RevitExportCommand();
            revitExport.Execute(app);
        }

    }
    public class WarnigResolver : IFailuresPreprocessor
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
                { Guid.Parse("c3aa4692-975c-4720-8cb4-389532cf43a4"), (a,f) => a.DeleteWarning(f) } // Мониторинг координации
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

    //public static class TransactionHandler
    //{
    //    public static void SetWarningResolver(Transaction transaction)
    //    {
    //        FailureHandlingOptions failOptions = transaction.GetFailureHandlingOptions();
    //        failOptions.SetFailuresPreprocessor(new WarnigResolver());
    //        transaction.SetFailureHandlingOptions(failOptions);
    //    }
    //}
}
