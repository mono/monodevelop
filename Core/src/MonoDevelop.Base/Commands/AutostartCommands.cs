// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ?Â¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;

using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Gui.ErrorHandlers;

using MonoDevelop.Internal.Parser;
using MonoDevelop.Services;
using SA = MonoDevelop.SharpAssembly.Assembly;

namespace MonoDevelop.Commands
{
	internal class InitializeWorkbenchCommand : AbstractCommand
	{
		public override void Run()
		{
			DefaultWorkbench w = new DefaultWorkbench();
			WorkbenchSingleton.Workbench = w;
			w.InitializeWorkspace();
			w.UpdateViews(null, null);
			WorkbenchSingleton.CreateWorkspace();
			((Gtk.Window)w).Visible = false;
		}
	}
	
	internal class StartWorkbenchCommand : AbstractCommand
	{
		const string workbenchMemento = "SharpDevelop.Workbench.WorkbenchMemento";
		
		public override void Run()
		{
			// register string tag provider (TODO: move to add-in tree :)
			Runtime.StringParserService.RegisterStringTagProvider(new MonoDevelop.Commands.SharpDevelopStringTagProvider());
			
			// load previous combine
			if ((bool)Runtime.Properties.GetProperty("SharpDevelop.LoadPrevProjectOnStartup", false)) {
				RecentOpen recentOpen = Runtime.FileService.RecentOpen;

				if (recentOpen.RecentProject != null && recentOpen.RecentProject.Length > 0) { 
					Runtime.ProjectService.OpenCombine(recentOpen.RecentProject[0].ToString());
				}
			}
			
			foreach (string file in SplashScreenForm.GetRequestedFileList()) {
				//FIXME: use mimetypes
				if (Runtime.ProjectService.IsCombineEntryFile (file)) {
					try {
						Runtime.ProjectService.OpenCombine (file);
					} catch (Exception e) {
						CombineLoadError.HandleError(e, file);
					}
				} else {
					try {
						Runtime.FileService.OpenFile (file);
					
					} catch (Exception e) {
						Runtime.LoggingService.InfoFormat("unable to open file {0} exception was :\n{1}", file, e.ToString());
					}
				}
			}
			
			((Gtk.Window)WorkbenchSingleton.Workbench).Show ();
			WorkbenchSingleton.Workbench.SetMemento ((IXmlConvertable)Runtime.Properties.GetProperty (workbenchMemento, WorkbenchSingleton.Workbench.CreateMemento()));
			((Gtk.Window)WorkbenchSingleton.Workbench).Visible = true;
			WorkbenchSingleton.Workbench.RedrawAllComponents ();
			((Gtk.Window)WorkbenchSingleton.Workbench).Present ();
		
			// finally run the workbench window ...
			Gtk.Application.Run ();
		}
	}
}
