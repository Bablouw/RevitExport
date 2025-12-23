using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Creation;

namespace RevitExport.Models
{
    public class NewDocument
    {
        public int is_export_revit {  get; set; }
        public int is_export_navis { get; set; }
        public int purify { get; set; }
        public string rvt_model_name { get; set; }
        public int rvt_version { get; set; }
        public string export_path { get; set; }
        public string model_path { get; set; }

    }
}
