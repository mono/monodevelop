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
	
using MonoDevelop.Core.Properties;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Components;
using MonoDevelop.Core.Gui.Dialogs;


namespace MonoDevelop.GtkCore.Dialogs
{
	public class WidgetBuilderOptionPanel: AbstractOptionPanel
	{
		class WidgetBuilderOptionPanelWidget : GladeWidgetExtract
		{
			[Glade.Widget] Gtk.TreeView tree;
			ListStore store;
			TreeViewColumn column;
			
			Project project;
			ArrayList selection;
			GtkDesignInfo designInfo;
			
			public WidgetBuilderOptionPanelWidget (IProperties customizationObject) : base ("gui.glade", "WidgetBuilderOptions")
			{
				store = new ListStore (typeof(bool), typeof(Pixbuf), typeof(string), typeof(object));
				tree.Model = store;
				tree.HeadersVisible = false;
				
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
				
				this.project = (Project)((IProperties)customizationObject).GetProperty("Project");
				designInfo = GtkCoreService.GetGtkInfo (project);
				
				selection = new ArrayList ();

				if (designInfo != null)
					selection.AddRange (designInfo.ExportedWidgets);
				
				foreach (IClass cls in GtkCoreService.GetExportableClasses (project)) {
					bool exported = designInfo != null && designInfo.IsExported (cls);
					string icon = IdeApp.Services.Icons.GetIcon (cls);
					Pixbuf pic = IdeApp.Services.Resources.GetIcon (icon);
					store.AppendValues (exported, pic, cls.FullyQualifiedName, cls);
				}
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
		
			public void Store (IProperties customizationObject)
			{
				if (selection.Count == 0 && (designInfo == null || designInfo.ExportedWidgets.Length == 0))
					return;
				
				if (designInfo == null)
					designInfo = GtkCoreService.EnableGtkSupport (project);
				designInfo.ExportedWidgets = (string[]) selection.ToArray (typeof(string));
				GtkCoreService.UpdateObjectsFile (project);
			}
		}
		
		WidgetBuilderOptionPanelWidget widget;

		public override void LoadPanelContents()
		{
			try {
				Add (widget = new WidgetBuilderOptionPanelWidget ((IProperties) CustomizationObject));
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
		
		public override bool StorePanelContents()
		{
			widget.Store ((IProperties) CustomizationObject);
 			return true;
		}
	}
	
}
