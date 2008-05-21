//
// WidgetBuilderOptionPanel.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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


using System;
using System.Collections.Specialized;
using System.Collections;
using Gtk;
using Gdk;
using Glade;
	
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Components;
using MonoDevelop.Projects.Gui.Dialogs;


namespace MonoDevelop.GtkCore.Dialogs
{
	class WidgetBuilderOptionPanel: ItemOptionsPanel
	{
		class WidgetBuilderOptionPanelWidget : GladeWidgetExtract
		{
			[Glade.Widget] protected Gtk.TreeView tree;
			[Glade.Widget] protected Gtk.CheckButton checkWidgetLib;
			[Glade.Widget] protected Gtk.CheckButton checkGettext;
			[Glade.Widget] protected Gtk.CheckButton checkGtkEnabled;
			[Glade.Widget] protected Gtk.Entry entryGettext;
			[Glade.Widget] protected Gtk.Notebook notebook;
			[Glade.Widget] protected Gtk.ComboBox comboVersions;
			
			ListStore store;
			TreeViewColumn column;
			
			Project project;
			ArrayList selection;
			GtkDesignInfo designInfo;
			
			public WidgetBuilderOptionPanelWidget (Project project) : base ("gui.glade", "WidgetBuilderOptions")
			{
				store = new ListStore (typeof(bool), typeof(Pixbuf), typeof(string), typeof(object));
				tree.Model = store;
				tree.HeadersVisible = false;
				this.project = project;
				
				column = new TreeViewColumn ();
			
				CellRendererToggle crtog = new CellRendererToggle ();
				crtog.Activatable = true;
				crtog.Toggled += new ToggledHandler (OnToggled);
				column.PackStart (crtog, false);
				column.AddAttribute (crtog, "active", 0);
				
				CellRendererPixbuf pr = new CellRendererPixbuf ();
				column.PackStart (pr, false);
				column.AddAttribute (pr, "pixbuf", 1);
				
				CellRendererText crt = new CellRendererText ();
				column.PackStart (crt, true);
				column.AddAttribute (crt, "text", 2);
				
				tree.AppendColumn (column);
				
				if (!GtkCoreService.SupportsGtkDesigner (project)) {
					notebook.RemovePage (1);
				}
					
				designInfo = GtkCoreService.GetGtkInfo (project);
				
				selection = new ArrayList ();

				if (designInfo != null)
					selection.AddRange (designInfo.ExportedWidgets);
				
				foreach (IClass cls in GtkCoreService.GetExportableClasses (project)) {
					bool exported = designInfo != null && designInfo.IsExported (cls.FullyQualifiedName);
					string icon = IdeApp.Services.Icons.GetIcon (cls);
					Pixbuf pic = IdeApp.Services.Resources.GetIcon (icon);
					store.AppendValues (exported, pic, cls.FullyQualifiedName, cls);
				}
				
				checkGtkEnabled.Active = designInfo != null;
				checkWidgetLib.Active = designInfo != null && designInfo.IsWidgetLibrary;
				checkGettext.Active = designInfo == null || designInfo.GenerateGettext;
				entryGettext.Text = designInfo != null ? designInfo.GettextClass : "Mono.Unix.Catalog";
				tree.Sensitive = checkWidgetLib.Active;
				notebook.Sensitive = checkGtkEnabled.Active;
				entryGettext.Sensitive = checkGettext.Active;
				
				comboVersions.RemoveText (0);
				foreach (string v in GtkCoreService.SupportedGtkVersions)
					comboVersions.AppendText (v);

				comboVersions.Active = (designInfo != null)
					? Array.IndexOf (GtkCoreService.SupportedGtkVersions, designInfo.TargetGtkVersion)
					: 0;
				
				checkWidgetLib.Clicked += delegate {
					tree.Sensitive = checkWidgetLib.Active;
				};
				
				checkGtkEnabled.Clicked += delegate {
					notebook.Sensitive = checkGtkEnabled.Active;
				};
				
				checkGettext.Clicked += delegate {
					entryGettext.Sensitive = checkGettext.Active;
					if (checkGettext.Active)
						entryGettext.Text = "Mono.Unix.Catalog";
				};
			}
			
			void OnToggled (object o, ToggledArgs args)
			{
				TreeIter it;
				if (store.GetIter (out it, new TreePath (args.Path))) {
					bool sel = !(bool) store.GetValue (it, 0);
					store.SetValue (it, 0, sel);
					string txt = (string) store.GetValue (it, 2);
					if (sel)
						selection.Add (txt);
					else
						selection.Remove (txt);
				}
			}
		
			public void Store ()
			{
				if (checkGtkEnabled.Active) {
					if (designInfo == null)
						designInfo = GtkCoreService.EnableGtkSupport (project);
						
					// Save selected widgets
					if (checkWidgetLib.Active) {
						if (selection.Count > 0 || designInfo != null) {
							designInfo.IsWidgetLibrary = true;
							designInfo.ExportedWidgets = (string[]) selection.ToArray (typeof(string));
							GtkCoreService.UpdateObjectsFile (project);
						}
					} else
						designInfo.IsWidgetLibrary = false;
					
					designInfo.GenerateGettext = checkGettext.Active;
					designInfo.GettextClass = entryGettext.Text;
					designInfo.TargetGtkVersion = comboVersions.ActiveText;
						
					designInfo.UpdateGtkFolder ();
					designInfo.ForceCodeGenerationOnBuild ();
				}
				else {
					GtkCoreService.DisableGtkSupport (project);
				}
			}
		}
		
		WidgetBuilderOptionPanelWidget widget;

		public override Widget CreatePanelWidget()
		{
			return (widget = new WidgetBuilderOptionPanelWidget (ConfiguredProject));
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
	
}
