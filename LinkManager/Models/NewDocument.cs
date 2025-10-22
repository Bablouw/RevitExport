using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Creation;

namespace RevitModelСleaning
{
    internal class NewDocument
    {

        private Document Document { get; set; }
        private string WorkSharingListStatus { get; set; } 
        private string WorkSharingListClosStatus { get; set; }
        private string OpenDocStatus { get; set; }
        private string PurgeLinksStatus { get; set; }
        private string PurgeViewsStatus {  get; set; }
        private string PurgeSheetsStatus {  get; set; }
        private string PurgeScopeStatus { get; set; }
        private string PurgeUnusedStatus { get; set; }
        private string SaveNewDocStatus { get; set; }   
        private string ResultFoDoc {  get; set; }

    }
}
