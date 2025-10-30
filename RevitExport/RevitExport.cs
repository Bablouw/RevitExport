
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitExport.Models;
using RevitExport.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace RevitExport
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RevitExportCommand : IExternalCommand
    {
        private static int RVTversion { get; set; }

        static AddInId addinId = new AddInId(new Guid("D57F4F16-01B6-486D-B12E-494A12E24452"));
        public Result Execute (ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var commandId = RevitCommandId.LookupPostableCommandId(PostableCommand.ExitRevit);
            commandData.Application.PostCommand(commandId);
            return Result.Succeeded;
        }
        public Result ExecuteScript(Application app)
        {
            
            //UIDocument uidoc = commandData.Application.ActiveUIDocument;
            //Document doc = uidoc.Document;
            ParseDBService parseDBService = new ParseDBService();
            LinkService linkService = new LinkService();
            ModelService modelService = new ModelService();
#if R2021
            RVTversion = 2021;
            string filePath = "C:\\RevitExportTest\\exportRVT2021.txt";
            if (!File.Exists(filePath))
            {
                LogService.LogError((!File.Exists(filePath)).ToString());
                return Result.Succeeded;
            }
            File.Delete(filePath);
#elif R2022
    RVTversion = 2022;
    string filePath = @"C:\RevitExportTest\exportRVT2022.txt";  // Verbatim, clean
    LogService.LogError(RVTversion.ToString());
    
    if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));  // Ensure dir
    
    if (!File.Exists(filePath))
    {
        LogService.LogError($"Файл не найден: {filePath}");  // Cleaner log
        return Result.Succeeded;
    }
    
    bool deleted = false;
    for (int attempt = 0; attempt < 3; attempt++)  // Фикс: retry loop
    {
        try
        {
            File.Delete(filePath);
            deleted = true;
            LogService.LogError("Файл удалён успешно");
            break;
        }
        catch (IOException ioEx)
        {
            LogService.LogError($"Попытка {attempt + 1} failed (IO lock): {ioEx.Message}. PID Revit: {System.Diagnostics.Process.GetCurrentProcess().Id}");
            if (attempt < 2) System.Threading.Thread.Sleep(100);  // Фикс: sleep, дай GC/Revit release
            else
            {
                // Force: try move to temp (если lock read-only)
                string tempPath = Path.GetTempFileName();
                try { File.Move(filePath, tempPath); File.Delete(tempPath); deleted = true; } catch { /* ignore */ }
            }
        }
        catch (Exception ex)
        {
            LogService.LogError($"Generic delete ex: {ex.Message}");
        }
    }
    
    if (!deleted)
    {
        LogService.LogError("Delete failed after retries — continue anyway?");  // Или return Failed
    }
#elif R2023
            RVTversion = 2023;
            string filePath = "C:\\RevitExportTest\\exportRVT2023.txt";
            if (!File.Exists(filePath))
            {
            LogService.LogError((!File.Exists(filePath)).ToString());
                return Result.Succeeded;
            }
            File.Delete(filePath);
#elif R2024
            RVTversion = 2024;
            string filePath = "C:\\RevitExportTest\\exportRVT2024.txt";
            if (!File.Exists(filePath))
            {
                return Result.Succeeded;
            }
            File.Delete(filePath);
#endif

            Document doc;
            // 1 Получить список модели из БД
            List<NewDocument> models = new List<NewDocument>(parseDBService.GetExportModels(RVTversion));
            parseDBService.ReturnZeroExport();
            if(models.Count<1)
            {
                Process.GetCurrentProcess().Kill();
                return Result.Succeeded;
            }
            LogService.LogError($"{RVTversion} количество моделей {models.Count}");
            // 2 Цикл запуска модели 
            foreach (NewDocument model in models)
            {
                try
                {
                    //2.1 Преобразовываем пути
                    ModelPath modelPath;
                    ModelPath modelPathExport;
                    LogService.LogError($"Путь к модели : {model.model_path}");
                    modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(model.model_path);
                    modelPathExport = ModelPathUtils.ConvertUserVisiblePathToModelPath(model.export_path + model.rvt_model_name);
                    LogService.LogError("2.1");

                    if (app == null)
                    {
                        LogService.LogError("app null");
                    }
                    LogService.LogError($"{modelPath}");
                    if (modelPath == null)
                    {
                        LogService.LogError("Modelpath- пусто ");
                    }
                    //2.2 Открытие отсоединенной модели
                    doc = modelService.OpenDocumentDetach(app, modelPath);
                    if (doc == null)
                    { continue; }
                    LogService.LogError("2.2");

                    //2.3 Удалить связи
                    bool purgeAllLinks = modelService.PurgeLinks(doc);
                    LogService.LogError("2.3");

                    //2.4 Удалить виды кроме Navisworks
                    bool purgeViews = modelService.PurgeViews(doc);
                    LogService.LogError("2.4");

                    //2.5 Удалить листы 
                    bool purgeSheets = modelService.PurgeSheets(doc);
                    LogService.LogError("2.5");

                    //2.6 Удалить облрасти видимости
                    bool purgeScope = modelService.PurgeScope(doc);
                    LogService.LogError("2.6");

                    //2.7 Удалить неиспользуемое
                    bool purheUnused = modelService.PurgeUnused(doc);
                    LogService.LogError("2.7");

                    //2.8 Сохранить документ 
                    LogService.LogError(model.export_path + "\\" + model.rvt_model_name);
                    bool saveNewDoc = modelService.SaveNewDoc(doc, modelPathExport);
                    if (saveNewDoc == false) LogService.LogError($"ошибка сохранения , путь {modelPathExport}");
                    LogService.LogError("2.8");

                    //2.9 Создаем список опций для освобождения
                    RelinquishOptions relinquishOptions = new RelinquishOptions(true);
                    relinquishOptions.UserWorksets = true; // освободить рабочие наборы
                    relinquishOptions.CheckedOutElements = true; // освободить выгруженные элементы
                    relinquishOptions.StandardWorksets = true; // освободить стандартные рабочие наборы
                    LogService.LogError("2.9");

                    //2.10  Выполняем освобождение
                    TransactWithCentralOptions transactOptions = new TransactWithCentralOptions();
                    WorksharingUtils.RelinquishOwnership(doc, relinquishOptions, transactOptions);
                    LogService.LogError("2.10");

                    //2.11 Закрыть документ
                    doc.Close();
                    doc = null;
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        LogService.LogError(ex.ToString());
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex}");
                    //_router.MessageAsync(errorMessage);
                    //doc = null;
                }
            }

            Process.GetCurrentProcess().Kill();
            return Result.Succeeded;
            
        }
    }
}
