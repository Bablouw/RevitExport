using Autodesk.Revit.DB;
using LinkManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkManager.Services
{
    public class LinkService
    {
        public List<LinkModel> GetRevitLinkModelsFromDoc(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<RevitLinkInstance> revitLinks = new List<RevitLinkInstance>();
            List<LinkModel> revitLinkModels = new List<LinkModel>();
            revitLinks = collector.
                OfCategory(BuiltInCategory.OST_RvtLinks)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .ToList();

            foreach (RevitLinkInstance link in revitLinks)
            {

                revitLinkModels.Add(new LinkModel
                {
                    linkInstance = link,
                    linkName = link.Name,
                    workShareName = link.LookupParameter("Рабочий набор").AsValueString()
                });
            }
            return revitLinkModels;
        }

        public void LoadLinks(Document doc, List<LinkModel> links)
        {
            if (doc == null || links == null)
            {
                LogService.LogError($"Ошибка доступа к документу"); ;
            }
            foreach (LinkModel link in links)
            {
                TryLoadLink(doc, link);
            }
        }
        private void TryLoadLink(Document doc, LinkModel link)
        {
            LogService.LogError($"Попытка загрузить модель {link.linkName}");
            using (Transaction trans = new Transaction(doc, "Load Revit Links"))
            {
                trans.Start();
                try
                {
                    if (string.IsNullOrEmpty(link.serverPath))
                    {
                        trans.RollBack();
                    }
                    ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(link.serverPath);
                    RevitLinkOptions linkOption = new RevitLinkOptions(false);
                    LinkLoadResult linkLoadResult = RevitLinkType.Create(doc, modelPath, linkOption);
                    RevitLinkInstance.Create(doc, linkLoadResult.ElementId, ImportPlacement.Shared);
                    LogService.LogError($"Модель {link.linkName} загружена");
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    LogService.LogError($"Ошибка при загрузке модели {link.linkName}\n {ex}");
                    trans.RollBack();
                }
            }
        }

    }
}
