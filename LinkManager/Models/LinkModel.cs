using Autodesk.Revit.DB;


namespace LinkManager.Models
{
    public class LinkModel
    {
        public string linkName {  get; set; }
        public string workShareName { get; set; }
        public string serverPath { get; set; }
        public RevitLinkInstance linkInstance { get; set; }
    }
}
