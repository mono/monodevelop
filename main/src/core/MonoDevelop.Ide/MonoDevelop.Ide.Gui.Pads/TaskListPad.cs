//
// TaskListPad.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2006 David Makovský
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
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Projects;

using Gtk;
using Gdk;
using MonoDevelop.Components.Docking;

namespace MonoDevelop.Ide.Gui.Pads
{
	
	public class TaskListPad : IPadContent
	{
		Widget control;
		ITaskListView activeView;
		
		//toolbar
		DockItemToolbar toolbar;
		ComboBox switcherCombo;
		ListStore switcherComboList;
		VSeparator separator;
		
		//content view
		ScrolledWindow sw;
		
		public Gtk.Widget Control {
			get {
				return control;
			}
		}

		public string Id {
			get { return "MonoDevelop.Ide.Gui.Pads.TaskListPad"; }
		}
		
		public string DefaultPlacement {
			get { return "Bottom"; }
		}
		
		public void RedrawContent()
		{
			control.QueueDraw ();
		}
		
		public void Dispose ()
		{
		}
		
		public TaskListPad ()
		{	
			VBox vbox = new VBox ();
			
			switcherComboList = new ListStore (typeof (string), typeof (ITaskListView), typeof (string));
			try
			{
				ExtensionNodeList viewCodons = AddinManager.GetExtensionNodes ("/MonoDevelop/Ide/TaskList/View", typeof (TaskListViewCodon));
				foreach (TaskListViewCodon codon in viewCodons)
				{
					switcherComboList.AppendValues (codon.Label, codon.View, codon.Class);
				}
			}
			catch (Exception e) // no codons loaded
			{
				LoggingService.LogError ("Loading of Tasks Views failed: {0}", e.ToString ());
			}
			
			switcherCombo = new ComboBox (switcherComboList);
			CellRenderer cr = new CellRendererText ();
			switcherCombo.PackStart (cr, true);
			switcherCombo.AddAttribute (cr, "text", 0);
			
			sw = new MonoDevelop.Components.CompactScrolledWindow ();
			sw.ShadowType = ShadowType.None;
			
			vbox.Add (sw);
			
			control = vbox;
			control.ShowAll ();
			
			// Load from preferences which one was used last time
			string className =(string)PropertyService.Get ("Monodevelop.TaskList.ActiveView", "");
			int pos = 0, i = 0;
			foreach (object[] row in switcherComboList)
			{
				if ((string)row[2] == className)
				{
					pos = i;
					break;
				}
				i++;
			}
			switcherCombo.Active = pos; 
		}
		
		void IPadContent.Initialize (IPadWindow window)
		{
			toolbar = window.GetToolbar (PositionType.Top);
			toolbar.Add (switcherCombo);
			toolbar.ShowAll ();
			switcherCombo.Changed += new EventHandler (OnContentSwitched);
			OnContentSwitched (null, null);
			
			separator = new VSeparator ();
		}
		
		void OnContentSwitched (object obj, EventArgs e)
		{
			TreeIter iter;
			if (switcherCombo.GetActiveIter (out iter))
			{
				if (sw.Children.Length > 0)
					sw.Remove (sw.Children[0]);
				ITaskListView view = (ITaskListView)switcherCombo.Model.GetValue (iter, 1);
				sw.Add (view.Content);
				
				if (activeView != null && activeView.ToolBarItems != null && activeView.ToolBarItems.Length > 0)
				{
					foreach (Widget w in activeView.ToolBarItems)
						toolbar.Remove (w);
					toolbar.Remove (separator);
				}
				
				if (view != null && view.ToolBarItems != null && view.ToolBarItems.Length > 0)
				{
					toolbar.Add (separator);
					foreach (Widget w in view.ToolBarItems)
						toolbar.Add (w);
				}
				if (toolbar != null)
					toolbar.ShowAll ();
				
				activeView = view;
				control.ShowAll ();
				
				string className = (string)switcherCombo.Model.GetValue (iter, 2);
				PropertyService.Set ("Monodevelop.TaskList.ActiveView", className);
			}
		}
	}
}
