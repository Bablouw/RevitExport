using Autodesk.Revit.DB;
using LinkManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkManager.Services
{
    public class ModelService
    {
        public string GetModelName(Document doc)
        {
            // Получить путь центральной модели
            ModelPath centralModelPath = doc.GetWorksharingCentralModelPath();
            if (centralModelPath == null || centralModelPath.Empty)
            {
                //TaskDialog.Show("Ошибка", "Центральная модель не найдена (возможно, модель detached).");
                return string.Empty;
            }

            // Конвертировать ModelPath в строку пути
            string centralFullPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(centralModelPath);
            if (string.IsNullOrEmpty(centralFullPath))
            {
               // TaskDialog.Show("Ошибка", "Не удалось получить путь центральной модели.");
                return string.Empty;
            }

            // Извлечь имя файла
            string centralFileName = Path.GetFileName(centralFullPath);

            return centralFileName;
        }
        public void SetCorrectWorkshare(Document doc, List<LinkModel> parsedLinks)
        {
            if (!doc.IsWorkshared)
            {
                throw new Exception("Проект не использует рабочие наборы.");
            }

            // Собираем все RevitLinkInstance
            FilteredElementCollector linkCollector = new FilteredElementCollector(doc);
            List<RevitLinkInstance> revitLinks = linkCollector
                .OfCategory(BuiltInCategory.OST_RvtLinks)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .ToList();

            // Собираем все рабочие наборы
            WorksetTable worksetTable = doc.GetWorksetTable();
            FilteredWorksetCollector worksetCollector = new FilteredWorksetCollector(doc);
            List<Workset> worksets = worksetCollector
                .OfKind(WorksetKind.UserWorkset) // Только пользовательские рабочие наборы
                .Cast<Workset>()
                .ToList();


            // Начинаем транзакцию для изменения модели
            using (Transaction trans = new Transaction(doc, "Set Workshare for Links"))
            {
                trans.Start();

                foreach (LinkModel link in parsedLinks)
                {
                    // Находим RevitLinkInstance по имени файла
                    RevitLinkInstance targetLink = null;
                    foreach (RevitLinkInstance revitLink in revitLinks)
                    {
                        if (revitLink.Name.Contains(link.linkName))
                            targetLink = revitLink;
                    }

                    if (targetLink == null)
                    {
                        continue;
                    }

                    // Находим рабочий набор по имени
                    Workset targetWorkset = worksets.FirstOrDefault(w => w.Name.Equals(link.workShareName, StringComparison.OrdinalIgnoreCase));
                    if (targetWorkset == null)
                    {
                        try
                        {
                            targetWorkset = Workset.Create(doc, link.workShareName);
                            // Добавляем новый рабочий набор в список (для последующих итераций, если нужно)
                            worksets.Add(targetWorkset);
                        }
                        catch (Exception ex)
                        {
                            continue; // Или throw ex, в зависимости от нужной логики
                        }
                    }

                    Parameter worksetParam = targetLink.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                    if (worksetParam != null)
                    {
                        worksetParam.Set(targetWorkset.Id.IntegerValue);
                    }
                }

                trans.Commit();
            }
        }

        public void PinAllGrids(Document doc)
        {
            using (Transaction trans = new Transaction(doc, "Pin All Grids"))
            {
                trans.Start();
                try
                {
                    FilteredElementCollector collector = new FilteredElementCollector(doc);

                    List<Grid> grids = collector
                        .OfClass(typeof(Grid))
                        .WhereElementIsNotElementType()
                        .Where(k => k.Pinned is false)
                        .Cast<Grid>()
                        .ToList();
                    foreach (Grid grid in grids)
                    {
                        if (!grid.Pinned)
                        {
                            grid.Pinned = true;
                        }
                    }
                    LogService.LogError($"Закреплено {grids.Count} осей");
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    LogService.LogError("Ошибка закрепления Осей");
                    trans.RollBack();
                }
            }
        }

        public void PinAllLevels(Document doc)
        {
            using (Transaction trans = new Transaction(doc, "Pin All levels"))
            {
                trans.Start();
                try
                {
                    FilteredElementCollector collector = new FilteredElementCollector(doc);

                    List<Level> levels = collector
                        .OfClass(typeof(Level))
                        .WhereElementIsNotElementType()
                        .Where(k => k.Pinned is false)
                        .Cast<Level>()
                        .ToList();
                    foreach (Level level in levels)
                    {
                        if (!level.Pinned)
                        {
                            level.Pinned = true;
                        }
                    }
                    LogService.LogError($"Закреплено {levels.Count} уровней");
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    LogService.LogError("Ошибка закрепления уровней");
                    trans.RollBack();
                }
            }
        }
        public void PinAllLinks(Document doc)
        {
            using (Transaction trans = new Transaction(doc, "Pin All links"))
            {
                trans.Start();
                try
                {
                    FilteredElementCollector collector = new FilteredElementCollector(doc);

                    List<RevitLinkInstance> links = collector
                        .OfClass(typeof(RevitLinkInstance))
                        .WhereElementIsNotElementType()
                        .Where(k => k.Pinned is false)
                        .Cast<RevitLinkInstance>()
                        .ToList();
                    foreach (RevitLinkInstance link in links)
                    {
                        if (!link.Pinned)
                        {
                            link.Pinned = true;
                        }
                    }
                    LogService.LogError($"Закреплено {links.Count} связей");
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    LogService.LogError("Ошибка закрепления связей");
                    trans.RollBack();
                }
            }
        }
    }
}
