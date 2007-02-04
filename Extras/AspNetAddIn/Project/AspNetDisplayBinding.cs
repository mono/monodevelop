
using System;

using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;

namespace AspNetAddIn
{
	
	
	public class AspNetDisplayBinding : IDisplayBinding
	{
		
		public string DisplayName {
			get { return "ASP.NET Source"; }
		}
		

		public bool CanCreateContentForFile(string fileName)
		{			
			WebSubtype type = AspNetAppProject.DetermineWebSubtype (System.IO.Path.GetExtension (fileName));
			
			switch (type) {
				case WebSubtype.WebForm:
				case WebSubtype.WebControl:
				case WebSubtype.WebHandler:
				case WebSubtype.WebService:
				case WebSubtype.MasterPage:	
					IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingForMimeType ("text");
					return (db != null);
				default:
					return false;
			}
		}

		public bool CanCreateContentForMimeType (string mimetype)
		{
			switch (mimetype) {
				case "application/x-aspx":
				case "application/x-ascx":
				case "application/x-asmx":
				case "application/x-ashx":
				case "application/x-asax":
					return true;
				default:
					return false;
			}
		}
		
		public IViewContent CreateContentForFile (string fileName)
		{
			IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingForMimeType ("text/plain");
			return db.CreateContentForFile (fileName);
		}
		
		public IViewContent CreateContentForMimeType (string mimeType, System.IO.Stream content)
		{
			//SourceEditor AddIn won't open won't the web application mimetypes directly
			IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingForMimeType ("text/plain");
			return db.CreateContentForMimeType ("text/plain", content);
		}
		
		
	}
}
