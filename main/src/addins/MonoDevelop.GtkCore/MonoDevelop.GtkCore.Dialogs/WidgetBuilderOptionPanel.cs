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


using System;
using System.Collections.Specialized;
using System.Collections;
using Gtk;
using Gdk;
using Glade;
	
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Components;
using MonoDevelop.Projects.Gui.Dialogs;


namespace MonoDevelop.GtkCore.Dialogs
{
	class WidgetBuilderOptionPanel: ItemOptionsPanel
	{
		class WidgetBuilderOptionPanelWidget : Gtk.VBox
		{
			Gtk.CheckButton checkGettext;
			Gtk.Entry entryGettext;
			Gtk.ComboBox comboVersions;
			
			GtkDesignInfo designInfo;
			
			public WidgetBuilderOptionPanelWidget (Project project) : base (false, 6)
			{
				designInfo = GtkDesignInfo.FromProject (project);

				Gtk.HBox box = new Gtk.HBox (false, 3);
				Gtk.Label lbl = new Gtk.Label (GettextCatalog.GetString ("Target Gtk# version:"));
				box.PackStart (lbl, false, false, 0);
				comboVersions = ComboBox.NewText ();
				box.PackStart (comboVersions, false, false, 0);
				box.ShowAll ();
				PackStart (box, false, false, 0);

				HSeparator sep = new HSeparator ();
				sep.Show ();
				PackStart (sep, false, false, 0);
				
				checkGettext = new CheckButton (GettextCatalog.GetString ("Enable gettext support"));
				checkGettext.Active = designInfo.GenerateGettext;
				checkGettext.Show ();
				PackStart (checkGettext, false, false, 0);
				box = new Gtk.HBox (false, 3);
				box.PackStart (new Label (GettextCatalog.GetString ("Gettext class:")), false, false, 0);
				entryGettext = new Gtk.Entry ();
				entryGettext.Text = designInfo.GettextClass;
				entryGettext.Sensitive = checkGettext.Active;
				box.PackStart (entryGettext, false, false, 0);
				box.ShowAll ();
				PackStart (box, false, false, 0);
				
				
				foreach (string v in GtkCoreService.SupportedGtkVersions)
					comboVersions.AppendText (v);

				comboVersions.Active = Array.IndexOf (GtkCoreService.SupportedGtkVersions, designInfo.TargetGtkVersion);
				
				checkGettext.Clicked += delegate {
					box.Sensitive = checkGettext.Active;
					if (checkGettext.Active)
						entryGettext.Text = "Mono.Unix.Catalog";
				};
			}
			
			public void Store ()
			{
				designInfo.GenerateGettext = checkGettext.Active;
				designInfo.GettextClass = entryGettext.Text;
				designInfo.TargetGtkVersion = comboVersions.ActiveText;
			}
		}
		
		WidgetBuilderOptionPanelWidget widget;

		public override Widget CreatePanelWidget()
		{
			return (widget = new WidgetBuilderOptionPanelWidget (ConfiguredProject));
		}
		
		public override bool IsVisible () 
		{
			return GtkDesignInfo.FromProject (DataObject as Project).SupportsDesigner;
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
