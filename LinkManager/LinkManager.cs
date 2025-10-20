using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LinkManager.Models;
using LinkManager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkManager
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class LinkManagerCommand : IExternalCommand
    {
        static AddInId addinId = new AddInId(new Guid("E05C11B3-8333-49D0-A46C-09790E8D5387"));
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            ParseCsvService parseCsvService = new ParseCsvService();
            ParseDBService parseDBService = new ParseDBService();
            LinkService linkService = new LinkService();
            ModelService modelService = new ModelService();

            //1 Парсинг пути к CSV
            string modelName = modelService.GetModelName(doc);
            string mappingTablePath = parseDBService.GetModelMappingPath(modelName);
            LogService.Initialize(mappingTablePath);
            LogService.LogError($"Отчет о работе плагина с модель {modelName}");
            mappingTablePath =  mappingTablePath + "\\test.csv";
            //2 Парсинг списка моделей и РН из CSV
            List<LinkModel> parsedLinksModels = new List<LinkModel> (parseCsvService.ParseCsv(mappingTablePath));
            //3 Парсинг путей к моделям
            foreach (LinkModel parsedLinkModel in parsedLinksModels)
            {
                SetServerPath(parseDBService, parsedLinkModel);
            }
            //4 Нахождение недостающих связей
            List<LinkModel> existingLinks = new List<LinkModel>(linkService.GetRevitLinkModelsFromDoc(doc));
            List<LinkModel> missedLinks = new List<LinkModel> ();
            missedLinks = FindMissLinks(parsedLinksModels, existingLinks);
            //5 Подгрузка недостающих связей
            linkService.LoadLinks(doc, missedLinks);
            //6 Распределить по РН
            modelService.SetCorrectWorkshare(doc,parsedLinksModels);
            //7 Закрепление осей
            modelService.PinAllGrids(doc);
            //8 Закрепление уровней
            modelService.PinAllLevels(doc);
            //9 Закрепление связей
            modelService.PinAllLinks(doc);
            return Result.Succeeded;
        }

        

        private List<LinkModel> FindMissLinks(List<LinkModel> parsedLinksModels, List<LinkModel> existingLinks)
        {
            if (parsedLinksModels == null || existingLinks == null)
            {
                return new List<LinkModel>();
            }
            var existingLinkNames = existingLinks.Select(link => link.linkName.Split(':')[0].Trim()).ToList();
            List<LinkModel> result = parsedLinksModels
                .Where(link => !existingLinkNames.Contains(link.linkName))
                .ToList();
            return result;
        }

        private void SetServerPath(ParseDBService service, LinkModel link)
        {

             link.serverPath = service.GetModelServerPath(link.linkName);
        }

    }
}
