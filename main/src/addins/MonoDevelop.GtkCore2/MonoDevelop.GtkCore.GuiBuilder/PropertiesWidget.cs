//
// GuiBuilderPropertiesPad.cs
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	class PropertiesWidget: Gtk.VBox
	{
		public static PropertiesWidget Instance;
		
		Stetic.SignalsEditor signalsEditor;
		
		static PropertiesWidget ()
		{
			Instance = new PropertiesWidget ();
		}
		
		public PropertiesWidget ()
		{
			Stetic.WidgetPropertyTree grid = GuiBuilderService.SteticApp.PropertiesWidget;
			
			Notebook tabs = new Notebook ();
			
			tabs.AppendPage (grid, new Label (GettextCatalog.GetString ("Properties")));
			
			signalsEditor = GuiBuilderService.SteticApp.SignalsWidget;
			signalsEditor.SignalActivated += new EventHandler (OnSignalActivated);
			tabs.AppendPage (signalsEditor, new Label (GettextCatalog.GetString ("Signals")));
			
			Gtk.EventBox infoBox = new Gtk.EventBox ();
			tabs.AppendPage (infoBox, new Gtk.Label (""));
			
			PackStart (tabs, true, true, 0);
			
			ShowAll ();
			infoBox.Hide ();
			
			tabs.Page = 0;
		}
		
		void OnSignalActivated (object s, EventArgs a)
		{
			GuiBuilderService.JumpToSignalHandler (signalsEditor.SelectedSignal);
		}
	}
	
/*	public class GuiBuilderPropertiesPad: AbstractPadContent
	{
		Stetic.WidGetTree grid;
		Stetic.SignalsEditor signalsEditor;
		Gtk.EventBox infoBox;
		Gtk.Widget widget;
		Stetic.Wrapper.Action currentAction;
		Notebook tabs;
		
		public GuiBuilderPropertiesPad (): base ("")
		{
			grid = GuiBuilderService.SteticApp.PropertiesWidget;
			
			DefaultPlacement = "MonoDevelop.GtkCore.GuiBuilder.GuiBuilderPalettePad/bottom; right";
			
			tabs = new Notebook ();
			
			tabs.AppendPage (grid, new Label (GettextCatalog.GetString ("Properties")));
			
			signalsEditor = GuiBuilderService.SteticApp.SignalsWidget;
			signalsEditor.SignalActivated += new EventHandler (OnSignalActivated);
			tabs.AppendPage (signalsEditor, new Label (GettextCatalog.GetString ("Signals")));
			
			infoBox = new Gtk.EventBox ();
			tabs.AppendPage (infoBox, new Gtk.Label (""));
			
			widget = tabs;
			
			widget.ShowAll ();
			infoBox.Hide ();
			
			tabs.Page = 0;
		}
		
		public override Gtk.Widget Control {
			get { return widget; }
		}
		
		void OnSignalActivated (object s, EventArgs a)
		{
			GuiBuilderService.JumpToSignalHandler (signalsEditor.SelectedSignal);
		}
		
		public object TargetObject { 
			get {
				return grid.TargetObject;
			}
			set {
				Stetic.Wrapper.Action action = Stetic.ObjectWrapper.Lookup (value) as Stetic.Wrapper.Action;
				if (action != null) {
					// Don't allow editing of global actions
					if (grid.Project != null && grid.Project.ActionGroups.IndexOf (action.ActionGroup) != -1) {
						if (infoBox.Child != null)
							infoBox.Remove (infoBox.Child);
						infoBox.Add (CreateGlobalActionInfo (action));
						infoBox.ShowAll ();
						tabs.Page = 2;
						tabs.ShowTabs = false;
						grid.Hide ();
						signalsEditor.Hide ();
						return;
					}
				}
				
				if (!grid.Visible) {
					tabs.ShowTabs = true;
					grid.Show ();
					signalsEditor.Show ();
					tabs.Page = 0;
					infoBox.Hide ();
				}
						
				grid.TargetObject = value;
				signalsEditor.TargetObject = value;
			}
		}
		
		Gtk.Widget CreateGlobalActionInfo (Stetic.Wrapper.Action action)
		{
			currentAction = action;
			
			Gtk.HBox hbox = new Gtk.HBox ();
			hbox.BorderWidth = 12;
			Gtk.Image img = new Gtk.Image (Gtk.Stock.DialogInfo, Gtk.IconSize.Menu);
			img.Yalign = 0;
			hbox.PackStart (img, false, false, 0);
			
			Gtk.VBox box = new Gtk.VBox ();
			Gtk.Label info = new Gtk.Label (GettextCatalog.GetString ("The action '{0}' belongs to the global action group '{1}'. To modify it, open the action group file.", action.MenuLabel, action.ActionGroup.Name));
			info.Xalign = 0;
			info.WidthRequest = 200;
			info.LineWrap = true;
			box.PackStart (info, false, false, 0);
			
			HBox bb = new HBox ();
			Gtk.Button but = new Gtk.Button (GettextCatalog.GetString ("Open Action Group"));
			but.Clicked += new EventHandler (OnOpenGroup);
			bb.PackStart (but, false, false, 0);
			box.PackStart (bb, false, false, 12);
			
			hbox.PackStart (box, true, true, 12);
			hbox.ShowAll ();
			return hbox;
		}
		
		void OnOpenGroup (object s, EventArgs args)
		{
			Project prj = GetProjectFromDesign (currentAction.ActionGroup);
			if (prj != null) {
				ActionGroupView view = GuiBuilderService.OpenActionGroup (prj, currentAction.ActionGroup);
				if (view != null)
					view.SelectAction (currentAction);
			}
		}


		public static Project GetProjectFromDesign (Stetic.Wrapper.ActionGroup group)
		{
			if (IdeApp.ProjectOperations.CurrentOpenCombine == null)
				return null;
				
			foreach (Project prj in IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects ()) {
				GtkDesignInfo info = GtkCoreService.GetGtkInfo (prj);
				if (info != null && info.GuiBuilderProject != null && info.GuiBuilderProject.SteticProject.ActionGroups.IndexOf (group) != -1)
					return prj;
			}
			return null;
		}

	}
*/	
}
