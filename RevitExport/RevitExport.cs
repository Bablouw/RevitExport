
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitExport.Models;
using RevitExport.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace RevitExport
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RevitExportCommand
    {
        private static int RVTversion { get; set; }

        static AddInId addinId = new AddInId(new Guid("D57F4F16-01B6-486D-B12E-494A12E24452"));

        public Result ExecuteScript(Application app)
        {
            
            //UIDocument uidoc = commandData.Application.ActiveUIDocument;
            //Document doc = uidoc.Document;
            ParseDBService parseDBService = new ParseDBService();
            ParseDBService.Initialize();
            string exportPath = "";
            LinkService linkService = new LinkService();
            ModelService modelService = new ModelService();
#if R2021
            RVTversion = 2021;
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "exportRVT2021.txt";
            if (!File.Exists(filePath))
            {
                LogService.LogError((!File.Exists(filePath)).ToString());
                return Result.Succeeded;
            }
            File.Delete(filePath);
#elif R2022
            RVTversion = 2022;
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "exportRVT2022.txt";
            if (!File.Exists(filePath))
            {
            LogService.LogError((!File.Exists(filePath)).ToString());
                return Result.Succeeded;
            }
            File.Delete(filePath);
#elif R2023
            RVTversion = 2023;
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "exportRVT2023.txt";
            if (!File.Exists(filePath))
            {
            LogService.LogError((!File.Exists(filePath)).ToString());
                return Result.Succeeded;
            }
            File.Delete(filePath);
#elif R2024
            RVTversion = 2024;
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "exportRVT2024.txt";
            if (!File.Exists(filePath))
            {
            LogService.LogError((!File.Exists(filePath)).ToString());
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
                    modelPathExport = ModelPathUtils.ConvertUserVisiblePathToModelPath((model.export_path + "/") + model.rvt_model_name);
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
                    LogService.LogError($"2.3 {purgeAllLinks.ToString()}");

                    //2.4 Удалить виды кроме Navisworks
                    if (model.is_export_revit == 1)
                    {
                        bool purgeViews = modelService.PurgeViews(doc);
                        LogService.LogError("2.4");
                    }

                    //2.5 Удалить листы 
                    if (model.is_export_revit == 1)
                    {
                        bool purgeSheets = modelService.PurgeSheets(doc);
                        LogService.LogError("2.5");
                    }

                    //2.6 Удалить облрасти видимости
                    if (model.is_export_revit == 1)
                    {
                        bool purgeScope = modelService.PurgeScope(doc);
                        LogService.LogError("2.6");
                    }

                    //2.7 Удалить неиспользуемое
                    if (model.is_export_revit == 1)
                    {
                        bool purheUnused = modelService.PurgeUnused(doc);
                        LogService.LogError("2.7");
                    }

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
                    LogService.LogError("2.11");
                    if(model.is_export_navis == 0) continue;
                    //2.12 Открыть очищеный документ
                    doc = modelService.OpenDocumentWithWorksets(app, modelPathExport);
                    LogService.LogError("2.12");
                    // 2.13 Выгрузить NWC
                    LogService.LogError($"путь: {model.export_path} имя файла:{model.rvt_model_name} ");
                    bool exportNwc = modelService.ExportNWC(doc, model.export_path, model.rvt_model_name);
                    LogService.LogError("2.13");
                    // 2.14 Закрыть документ
                    doc.Close(false);
                    doc = null;
                    //2.15 Удалить Backup'ы
                    foreach (var dir in Directory.GetDirectories(model.export_path, "*backup*", SearchOption.TopDirectoryOnly)
                        .Where(d => d.ToLower().Contains("backup")))
                    {
                        Directory.Delete(dir, true);
                    }
                    if (model.is_export_revit == 0)
                    {
                        File.Delete(model.export_path + "\\" + model.rvt_model_name);
                    }
                   exportPath = model.export_path;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex}");
                    //_router.MessageAsync(errorMessage);
                    //doc = null;
                }
            }
            //2.16 Создать nwd
            //modelService.CreateNWD(exportPath);
            //2.17 Закрыть ревит
            Process.GetCurrentProcess().Kill();
            return Result.Succeeded;
            
        }

        
    }
}
