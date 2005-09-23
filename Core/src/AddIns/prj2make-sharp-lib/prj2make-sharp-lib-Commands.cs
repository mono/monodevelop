using System;
using System.IO;

using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Prj2Make;
using MonoDevelop.Prj2Make.Schema.Prjx;
using MonoDevelop.Prj2Make.Schema.Csproj;
using MonoDevelop.Commands;

namespace MonoDevelop.Prj2Make
{
	public enum Commands
	{
		ImportSolution
	}

	public class ImportPrj : CommandHandler
	{
		static PropertyService PropertyService = (PropertyService)ServiceManager.GetService (typeof (PropertyService));
		
		protected override void Run()
		{
			using (FileSelector fs = new FileSelector (GettextCatalog.GetString ("File to Open"))) {
				bool conversionSuccessfull = false;
				SlnMaker slnMkObj = null;
				int response = fs.Run ();
				string name = fs.Filename;
				fs.Hide ();
				IProjectService proj = null;

				if (response == (int)Gtk.ResponseType.Ok) {
					switch (Path.GetExtension(name).ToUpper()) {
						case ".SLN": 
							slnMkObj = new SlnMaker();
							// Load the sln and parse it
							slnMkObj.MsSlnToCmbxHelper(name);
							conversionSuccessfull = true;
							name = slnMkObj.CmbxFileName;
							break;
						case ".CSPROJ":
							slnMkObj = new SlnMaker();
							// Load the csproj and parse it
							slnMkObj.CreatePrjxFromCsproj(name);
							conversionSuccessfull = true;
							name = slnMkObj.PrjxFileName;
							break;
						default:
							IMessageService messageService =(IMessageService)ServiceManager.GetService(typeof(IMessageService));
							messageService.ShowError(String.Format (GettextCatalog.GetString ("Can't open file {0} as project"), name));
							break;
					}
					if (conversionSuccessfull == true) {
						try {
							proj = (IProjectService)ServiceManager.GetService (typeof (IProjectService));
							proj.OpenCombine(name);
						} catch (Exception ex) {
							Console.WriteLine(ex.Message);
						}
					}
				}
			}
		}
	}

}
