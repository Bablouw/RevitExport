using Autodesk.Revit.UI;
using Autodesk.Windows;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;

using RibbonPanel = Autodesk.Revit.UI.RibbonPanel;

namespace ListManager
{
    internal class Application : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            SetUpRibbonPanel(application);

            return Result.Succeeded;
        }

        private void SetUpRibbonPanel(UIControlledApplication application)
        {
            var ribbonTabName = "UNITools Common";
            //to adjust
            var ribbonPanelName = "Docs"; ;

            RibbonTabCollection ribbonTabs = ComponentManager.Ribbon.Tabs;

            if (ribbonTabs.All(x => x.Name != ribbonTabName))
            {
                application.CreateRibbonTab(ribbonTabName);
            }

            List<RibbonPanel> ribbonPanels = application.GetRibbonPanels(ribbonTabName);

            RibbonPanel ribbonPanel = ribbonPanels.All(x => x.Name != ribbonPanelName) 
                ? application.CreateRibbonPanel(ribbonTabName, ribbonPanelName)
                : ribbonPanels.FirstOrDefault(x => x.Name == ribbonPanelName);

            var assembly = Assembly.GetExecutingAssembly();

            //to adjust
            var pushButtonData1 = new PushButtonData(
                "LinkManager",
                "Связи по РН",
                assembly.Location,
                typeof(LinkManager.LinkManagerCommand).FullName);
            pushButtonData1.LongDescription = "Версия плагина: 0.01";
            //to adjust
            ribbonPanel.AddItem(pushButtonData1);
        }

        /*private void ApplyImageToButton(Bitmap imagePath, ButtonData pushButtonData)
        {
            BitmapImage image;

            using (var memory = new MemoryStream())
            {
                imagePath.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = memory;
                image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
            }

            pushButtonData.LargeImage = image; 
        }*/
    }
}
