
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitExport.Models;
using RevitExport.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace RevitExport
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RevitExportCommand 
    {
        static AddInId addinId = new AddInId(new Guid("D57F4F16-01B6-486D-B12E-494A12E24452"));
        public Result Execute(Application app)
        {
            //UIDocument uidoc = commandData.Application.ActiveUIDocument;
            //Document doc = uidoc.Document;
            ParseDBService parseDBService = new ParseDBService();
            LinkService linkService = new LinkService();
            ModelService modelService = new ModelService();
            
            Document doc;
            // 1 Получить список модели из БД
            List<NewDocument> models = new List<NewDocument>(parseDBService.GetExportModels());
            // 2 Цикл запуска модели 
            foreach (NewDocument model in models)
            {
                try
                {
                    //2.1 Преобразовываем пути
                    ModelPath modelPath;
                    ModelPath modelPathExport;
                    modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(model.model_path);
                    modelPathExport = ModelPathUtils.ConvertUserVisiblePathToModelPath(model.export_path + model.rvt_model_name);
                    LogService.LogError("2.1");
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
                    LogService.LogError(model.export_path + model.rvt_model_name);
                    bool saveNewDoc = modelService.SaveNewDoc(doc, modelPathExport);
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex}");
                    //_router.MessageAsync(errorMessage);
                    //doc = null;
                }
            }

            return Result.Succeeded;
        }
    }
}
