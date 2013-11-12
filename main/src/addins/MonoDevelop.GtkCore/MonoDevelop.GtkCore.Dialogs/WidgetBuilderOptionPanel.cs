//
// WidgetBuilderOptionPanel.cs
//
// Author:
//   Lluis Sanchez Gual
//   Mike Kestner
//
// Copyright (C) 2006, 2008  Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using Gtk;
	
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.GtkCore.Dialogs
{
	class WidgetBuilderOptionPanel: ItemOptionsPanel
	{
		class WidgetBuilderOptionPanelWidget : VBox
		{
			readonly CheckButton checkGettext;
			readonly Entry entryGettext;
			readonly ComboBox comboVersions;
			
			readonly DotNetProject project;
			
			public WidgetBuilderOptionPanelWidget (Project project) : base (false, 6)
			{
				this.project = project as DotNetProject;

				var box = new HBox (false, 3);
				var lbl = new Label (GettextCatalog.GetString ("Target Gtk# version:"));
				box.PackStart (lbl, false, false, 0);
				comboVersions = ComboBox.NewText ();
				var refmgr = new ReferenceManager (project as DotNetProject);
				foreach (string v in refmgr.SupportedGtkVersions)
					comboVersions.AppendText (v);
				comboVersions.Active = refmgr.SupportedGtkVersions.IndexOf (refmgr.GtkPackageVersion);
				refmgr.Dispose ();
				box.PackStart (comboVersions, false, false, 0);
				box.ShowAll ();
				PackStart (box, false, false, 0);

				var sep = new HSeparator ();
				sep.Show ();
				PackStart (sep, false, false, 0);
				
				if (!GtkDesignInfo.HasDesignedObjects (project))
					return;

				GtkDesignInfo designInfo = GtkDesignInfo.FromProject (project);
				checkGettext = new CheckButton (GettextCatalog.GetString ("Enable gettext support"));
				checkGettext.Active = designInfo.GenerateGettext;
				checkGettext.Show ();
				PackStart (checkGettext, false, false, 0);
				box = new HBox (false, 3);
				box.PackStart (new Label (GettextCatalog.GetString ("Gettext class:")), false, false, 0);
				entryGettext = new Entry ();
				entryGettext.Text = designInfo.GettextClass;
				entryGettext.Sensitive = checkGettext.Active;
				box.PackStart (entryGettext, false, false, 0);
				box.ShowAll ();
				PackStart (box, false, false, 0);
				
				checkGettext.Clicked += delegate {
					box.Sensitive = checkGettext.Active;
					if (checkGettext.Active)
						entryGettext.Text = "Mono.Unix.Catalog";
				};
			}
			
			public void Store ()
			{
				var refmgr = new ReferenceManager (project);
				if (!string.IsNullOrEmpty (comboVersions.ActiveText))
					refmgr.GtkPackageVersion = comboVersions.ActiveText;
				if (GtkDesignInfo.HasDesignedObjects (project)) {
					GtkDesignInfo info = GtkDesignInfo.FromProject (project);
					info.GenerateGettext = checkGettext.Active;
					info.GettextClass = entryGettext.Text;
					info.GuiBuilderProject.SteticProject.TargetGtkVersion = comboVersions.ActiveText;
					info.GuiBuilderProject.SaveProject (false);
				}
				refmgr.Dispose ();
			}
		}
		
		WidgetBuilderOptionPanelWidget widget;

		public override Widget CreatePanelWidget()
		{
			return (widget = new WidgetBuilderOptionPanelWidget (ConfiguredProject));
		}
		
		public override bool IsVisible () 
		{
			return GtkDesignInfo.SupportsDesigner (DataObject as Project);
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
