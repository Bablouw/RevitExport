using Autodesk.Revit.DB;
using RevitExport.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = Autodesk.Revit.ApplicationServices.Application;
using View = Autodesk.Revit.DB.View;

namespace RevitExport.Services
{
    public class ModelService
    {
        public bool PurgeViews(Document doc)
        {
            var allViews = new FilteredElementCollector(doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .ToList();

            View viewToKeep = allViews.FirstOrDefault(view => view.Name.Contains("Navisworks"));

            // Собираем ID всех листов, КРОМЕ того, который оставляем
            List<ElementId> viewsToDelete = allViews
                .Where(view => view != viewToKeep) // Удаляем все, кроме нужного
                .Select(view => view.Id)
                .ToList();
            //MessageBox.Show($"Всего видов: {allViews.Count}, удаляем: {viewsToDelete.Count}");
            if (allViews.Count > 0)
            {
                using (UpdateProgressForm progressForm = new UpdateProgressForm())
                {
                    progressForm.Show();
                    try
                    {
                        List<ElementId> elements = new List<ElementId>();
                        HashSet<string> errors = new HashSet<string>();
                        using (Transaction t = new Transaction(doc, "Очистка видов"))
                        {
                            t.SetFailureHandlingOptions(t.GetFailureHandlingOptions().SetFailuresPreprocessor(new WarnigResolver()));
                            int completed = 0;
                            t.Start();
                            foreach (ElementId view in viewsToDelete)
                            {
                                progressForm.UpdateProgress(completed, viewsToDelete.Count);
                                try
                                {
                                    doc.Delete(view);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                                completed++;
                            }
                            t.Commit();
                            //MessageBox.Show($"Удалили виды, ошибки при удалении: {string.Join(", ", elements)}, ошибки: {string.Join("\n ", errors)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        progressForm.Close();
                        MessageBox.Show($"Ошибка при удалении видов: {ex.Message}");
                        return false;
                    }
                    progressForm.Close();
                }
            }
            return true;
        }

        public bool PurgeSheets(Document doc)
        {
            var allSheets = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .ToList();

            List<ElementId> sheetsToDelete = allSheets
                .Select(sheet => sheet.Id)
                .ToList();

            //MessageBox.Show($"Всего листов: {allSheets.Count}, удаляем: {sheetsToDelete.Count}");
            if (allSheets.Count > 0)
            {
                using (UpdateProgressForm progressForm = new UpdateProgressForm())
                {
                    progressForm.Show();
                    try
                    {
                        using (Transaction t = new Transaction(doc, "Очистка листов"))
                        {
                            t.SetFailureHandlingOptions(t.GetFailureHandlingOptions().SetFailuresPreprocessor(new WarnigResolver()));
                            int completed = 0;
                            t.Start();

                            foreach (ElementId sheet in sheetsToDelete)
                            {

                                progressForm.UpdateProgress(completed, sheetsToDelete.Count);
                                try
                                {
                                    doc.Delete(sheet);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                                completed++;
                            }
                            t.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении листов: {ex.Message}");
                        progressForm.Close();
                        return false;
                    }
                    progressForm.Close();
                }
            }
            return true;
        }

        public bool PurgeScope(Document doc)
        {
            var scopeFilter = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                .Cast<Element>()
                .ToList();

            List<ElementId> scopeToDelited = scopeFilter
                .Select(scope => scope.Id)
                .ToList();

            if (scopeToDelited.Count > 0)
            {
                using (UpdateProgressForm progressForm = new UpdateProgressForm())
                {
                    progressForm.Show();
                    try
                    {
                        List<ElementId> elements = new List<ElementId>();
                        HashSet<string> errors = new HashSet<string>();
                        using (Transaction t = new Transaction(doc, "Удладаление областей видимости"))
                        {
                            t.SetFailureHandlingOptions(t.GetFailureHandlingOptions().SetFailuresPreprocessor(new WarnigResolver()));
                            int completed = 0;
                            t.Start();
                            foreach (ElementId scope in scopeToDelited)
                            {
                                progressForm.UpdateProgress(completed, scopeToDelited.Count);
                                try
                                {
                                    doc.Delete(scope);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                                completed++;
                            }
                            t.Commit();
                            //MessageBox.Show($"Удалили области видимости, ошибки при удалении: {string.Join(", ", elements)}, ошибки: {string.Join("\n ", errors)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        progressForm.Close();
                        MessageBox.Show($"Ошибка при удалении областей видимости: {ex.Message}");
                        return false;
                    }
                    progressForm.Close();
                }
            }
            return true;
        }

        public bool PurgeLinks(Document doc)
        {
            List<ElementId> allLinks = new List<ElementId>();

            // CAD-ссылки (DWG, DXF, DGN и т.д.)
            allLinks.AddRange(new FilteredElementCollector(doc)
                .OfClass(typeof(ImportInstance))
                .Select(links => links.Id)
                .ToList());

            // Revit-ссылки (RVT)
            allLinks.AddRange(new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Select(links => links.Id)
                .ToList());

            //MessageBox.Show($"Нашли связей: {allLinks.Count}");
            if (allLinks.Count > 0)
            {
                try
                {
                    using (Transaction t = new Transaction(doc, "Очистка связей"))
                    {
                        t.SetFailureHandlingOptions(t.GetFailureHandlingOptions().SetFailuresPreprocessor(new WarnigResolver()));
                        t.Start();
                        foreach (ElementId link in allLinks)
                        {
                            try
                            {
                                doc.Delete(link);
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                        //MessageBox.Show($"Удалили листы");
                        t.Commit();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении связей: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        public bool PurgeUnused(Document doc)
        {
            try
            {
                using (Transaction t = new Transaction(doc, "Удление неиспользуемого"))
                {
                    t.Start();

                    // Константа GUID для правила очистки
                    const string PurgeGuid = "e8c63650-70b7-435a-9010-ec97660c1bda";
                    var performanceAdviserRuleIds = new List<PerformanceAdviserRuleId>();

                    // Находим нужное правило по GUID
                    foreach (PerformanceAdviserRuleId ruleId in PerformanceAdviser.GetPerformanceAdviser().GetAllRuleIds())
                    {
                        if (ruleId.Guid.ToString() == PurgeGuid)
                        {
                            performanceAdviserRuleIds.Add(ruleId);
                            break;
                        }
                    }

                    // Получаем элементы для удаления
                    List<ElementId> purgeableElementIds = null;
                    var failureMessages = PerformanceAdviser.GetPerformanceAdviser()
                        .ExecuteRules(doc, performanceAdviserRuleIds)
                        .ToList();

                    if (failureMessages.Count > 0)
                    {
                        purgeableElementIds = failureMessages[0].GetFailingElements().ToList();
                    }

                    // Удаляем элементы, если они есть
                    if (purgeableElementIds != null && purgeableElementIds.Count > 0)
                    {
                        doc.Delete(purgeableElementIds);
                    }

                    t.Commit();
                    return purgeableElementIds != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool SaveNewDoc(Document doc, ModelPath modelPathNew)
        {
            try
            {
                SaveAsOptions saveOptions = new SaveAsOptions
                {
                    OverwriteExistingFile = true,
                    MaximumBackups = 2,
                };
                WorksharingSaveAsOptions worksharingSaveAsOptions = new WorksharingSaveAsOptions
                {
                    SaveAsCentral = true,
                };

                saveOptions.SetWorksharingOptions(worksharingSaveAsOptions);

                doc.SaveAs(modelPathNew, saveOptions);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
                return false;
            }
        }

        public Document OpenDocumentDetach(Application app, ModelPath modelPath)
        {
            bool saveworkset = true;
            bool isWorkSharing = false;


            try
            {
                IList<WorksetPreview> workSharingList = WorksharingUtils.GetUserWorksetInfo(modelPath);
                isWorkSharing = workSharingList != null && workSharingList.Count > 0 ? true : false;
            }
            catch (Exception e)
            { MessageBox.Show($"Ошибка проверки наличия рабочих наборов:\n {e}"); }
                    ;

            if (isWorkSharing)
            {
                OpenOptions openOptions = new OpenOptions();
                openOptions.AllowOpeningLocalByWrongUser = true;
                openOptions.DetachFromCentralOption = DetachFromCentralOption.DetachAndDiscardWorksets;

                if (saveworkset)
                {
                    openOptions.DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets;
                }

                try //Попытка отключить рабочие наборы со связями для ускорения процесса
                {
                    IList<WorksetId> worksetIds = new List<WorksetId>();
                    IList<WorksetPreview> worksets = WorksharingUtils.GetUserWorksetInfo(modelPath);

                    foreach (WorksetPreview worksetPrev in worksets)
                    {
                        //if (!worksetPrev.Name.ToLower().Contains("link"))
                        //{
                        worksetIds.Add(worksetPrev.Id);
                        //}
                    }
                    WorksetConfiguration openConfig = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
                    openConfig.Close(worksetIds);
                    openOptions.SetOpenWorksetsConfiguration(openConfig);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Ошибка скрытия наборов в файле:\n {e}");
                }


                try
                {
                    Document doc = app.OpenDocumentFile(modelPath, openOptions);
                    //MessageBox.Show("Файл открыт");
                    return doc;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Ошибка открытия фйла:\n {e} ");
                    return null;
                }
            }
            else
            {
                try
                {
                    Document doc = app.OpenDocumentFile(modelPath, null);
                    //MessageBox.Show("Файл открыт");
                    return doc;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Ошибка открытия фйла:\n {e}");
                    return null;
                }
            }
        }

        public Document OpenDocumentWithWorksets(Application app, ModelPath modelPath)
        {
            bool saveworkset = true;
            bool isWorkSharing = false;


            try
            {
                IList<WorksetPreview> workSharingList = WorksharingUtils.GetUserWorksetInfo(modelPath);
                isWorkSharing = workSharingList != null && workSharingList.Count > 0 ? true : false;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Ошибка проверки наличия рабочих наборов:\n {e}");
            }
            ;

            if (isWorkSharing)
            {
                OpenOptions openOptions = new OpenOptions();
                openOptions.AllowOpeningLocalByWrongUser = true;
                openOptions.DetachFromCentralOption = DetachFromCentralOption.DetachAndDiscardWorksets;

                if (saveworkset)
                {
                    openOptions.DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets;
                }

                try //Попытка отключить рабочие наборы со связями для ускорения процесса
                {

                    WorksetConfiguration openConfig = new WorksetConfiguration(WorksetConfigurationOption.OpenAllWorksets);
                    openOptions.SetOpenWorksetsConfiguration(openConfig);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Ошибка скрытия наборов в файле:\n {e}");
                }


                try
                {
                    Document doc = app.OpenDocumentFile(modelPath, openOptions);
                    //MessageBox.Show("Файл открыт");
                    return doc;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Ошибка открытия фйла:\n {e} ");
                    return null;
                }
            }
            else
            {
                try
                {
                    Document doc = app.OpenDocumentFile(modelPath, null);
                    //MessageBox.Show("Файл открыт");
                    return doc;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Ошибка открытия фйла:\n {e}");
                    return null;
                }
            }
        }

        public bool ExportNWC(Document doc, string folder, string fileName)
        {
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D));

                View3D navisView = collector
                    .Cast<View3D>()
                    .FirstOrDefault(v => v.Name == "Navisworks");

                NavisworksExportOptions exportOptions = new NavisworksExportOptions();
                doc.Export(folder, CorrectNwcFileName(fileName), exportOptions);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}");
                LogService.LogError($"{ex.Message}");
                return false;
            }
        }

        public string CorrectNwcFileName(string fileName)
        {
            if (fileName.EndsWith(".rvt"))
            {
                fileName = fileName.Replace(".rvt", "");
                return fileName;
            }
            return fileName;
        }

    }
}
