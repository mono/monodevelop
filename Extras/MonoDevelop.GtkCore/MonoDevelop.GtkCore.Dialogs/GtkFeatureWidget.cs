
using System;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Gtk;

namespace MonoDevelop.GtkCore.Dialogs
{
	class GtkFeatureWidget : Gtk.VBox
	{
		CheckButton libCheck;
		
		public GtkFeatureWidget (DotNetProject project)
		{
			Spacing = 6;
			Label lab = new Label (GettextCatalog.GetString ("Enables support for the Gtk# designer."));
			PackStart (lab, false, false, 0);
			
			DotNetProjectConfiguration conf = project.ActiveConfiguration as DotNetProjectConfiguration;
			if (conf != null && conf.CompileTarget == CompileTarget.Library || conf.CompiledOutputName.EndsWith (".dll")) {
				GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
				libCheck = new CheckButton (GettextCatalog.GetString ("This assembly is a widget library"));
				libCheck.Active = info != null && info.IsWidgetLibrary;
				PackStart (libCheck, false, false, 0);
			}

			ShowAll ();
		}
		
		public bool IsWidgetLibrary {
			get { return libCheck != null && libCheck.Active; }
		}
	}
	
	class GtkProjectFeature: ICombineEntryFeature
	{
		public string Title {
			get { return GettextCatalog.GetString ("Gtk# Support"); }
		}

		public bool SupportsCombineEntry (Combine parentCombine, CombineEntry entry)
		{
			return entry is DotNetProject;
		}
		
		public Widget CreateFeatureEditor (Combine parentCombine, CombineEntry entry)
		{
			return new GtkFeatureWidget ((DotNetProject) entry);
		}

		public void ApplyFeature (Combine parentCombine, CombineEntry entry, Widget editor)
		{
			GtkDesignInfo info = GtkCoreService.EnableGtkSupport ((DotNetProject) entry);
			info.IsWidgetLibrary = ((GtkFeatureWidget)editor).IsWidgetLibrary;
		}
		
		public string Validate (Combine parentCombine, CombineEntry entry, Gtk.Widget editor)
		{
			return null;
		}
		
		public bool IsEnabled (Combine parentCombine, CombineEntry entry) 
		{
			return GtkCoreService.GetGtkInfo ((Project)entry) != null;
		}
	}
}
