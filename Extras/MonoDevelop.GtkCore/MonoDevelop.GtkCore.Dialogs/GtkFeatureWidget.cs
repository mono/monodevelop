
using System;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Core;
using MonoDevelop.Ide.Projects;
using Gtk;

namespace MonoDevelop.GtkCore.Dialogs
{
	class GtkFeatureWidget : Gtk.VBox
	{
		CheckButton libCheck;
		
		public GtkFeatureWidget (MSBuildProject project)
		{
			Spacing = 6;
			Label lab = new Label (GettextCatalog.GetString ("Enables support for the Gtk# designer."));
			PackStart (lab, false, false, 0);

// TODO: Project Conversion 
//			DotNetProjectConfiguration conf = project.ActiveConfiguration as DotNetProjectConfiguration;
//			if (conf != null && conf.CompileTarget == CompileTarget.Library || conf.CompiledOutputName.EndsWith (".dll")) {
//				GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
//				libCheck = new CheckButton (GettextCatalog.GetString ("This assembly is a widget library"));
//				libCheck.Active = info != null && info.IsWidgetLibrary;
//				PackStart (libCheck, false, false, 0);
//			}

			ShowAll ();
		}
		
		public bool IsWidgetLibrary {
			get { return libCheck != null && libCheck.Active; }
		}
	}

// TODO: Project Conversion
//	class GtkProjectFeature: ICombineEntryFeature
//	{
//		public string Title {
//			get { return GettextCatalog.GetString ("Gtk# Support"); }
//		}
//
//		public bool SupportsCombineEntry (Solution parentCombine, IProject entry)
//		{
//			return entry is MSBuildProject;
//		}
//		
//		public Widget CreateFeatureEditor (Solution parentCombine, IProject entry)
//		{
//			return new GtkFeatureWidget ((MSBuildProject) entry);
//		}
//
//		public void ApplyFeature (Solution parentCombine, IProject entry, Widget editor)
//		{
//			GtkDesignInfo info = GtkCoreService.EnableGtkSupport ((DotNetProject) entry);
//			info.IsWidgetLibrary = ((GtkFeatureWidget)editor).IsWidgetLibrary;
//		}
//		
//		public string Validate (Solution parentCombine, IProject entry, Gtk.Widget editor)
//		{
//			return null;
//		}
//		
//		public bool IsEnabled (Solution parentCombine, IProject entry) 
//		{
//			return GtkCoreService.GetGtkInfo ((Project)entry) != null;
//		}
//	}
}
