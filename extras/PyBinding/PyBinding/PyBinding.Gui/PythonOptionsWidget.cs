// PythonOptionsWidget.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using Gtk;

using MonoDevelop.Gettext;
using MonoDevelop.Ide.Gui;

using PyBinding.Runtime;

namespace PyBinding.Gui
{
	public partial class PythonOptionsWidget : Gtk.Bin
	{
		ListStore m_PathsListStore;
		ListStore m_RuntimeListStore;
		
		public PythonOptionsWidget ()
		{
			this.Build();
			
			// Python paths
			m_PathsListStore = new ListStore (typeof (string));
			m_PathsTreeView.Model = this.m_PathsListStore;
			m_PathsTreeView.HeadersVisible = false;
			TreeViewColumn column = new TreeViewColumn ();
			CellRendererText ctext = new CellRendererText ();
			column.PackStart (ctext, true);
			column.AddAttribute (ctext, "text", 0);
			m_PathsTreeView.AppendColumn (column);
			m_PathsTreeView.Selection.Changed += delegate {
				this.m_RemovePathButton.Sensitive = m_PathsTreeView.Selection.CountSelectedRows () == 1;
			};
			
			// Setup Python Runtime Version
			m_RuntimeListStore = new ListStore (typeof (string), typeof (Type));
			m_RuntimeCombo.Model = this.m_RuntimeListStore;
			m_RuntimeListStore.AppendValues ("Python 2.5", typeof (Python25Runtime));
			m_RuntimeListStore.AppendValues ("Python 2.6", typeof (Python26Runtime));
		}
		
		public string DefaultModule {
			get {
				return this.m_ModuleEntry.Text;
			}
			set {
				this.m_ModuleEntry.Text = value;
			}
		}
		
		public bool Optimize {
			get {
				return this.m_OptimizeCheckBox.Active;
			}
			set {
				this.m_OptimizeCheckBox.Active = value;
			}
		}
		
		public string PythonOptions {
			get {
				return m_PythonOptions.Text;
			}
			set {
				m_PythonOptions.Text = value;
			}
		}
		
		public string[] PythonPaths {
			get {
				List<string> paths = new List<string> ();
				TreeIter iter;
				
				if (m_PathsListStore.GetIterFirst (out iter)) {
					do {
						var path = (string)m_PathsListStore.GetValue (iter, 0);
						paths.Add (path);
					} while (m_PathsListStore.IterNext (ref iter));
				}
				
				return paths.ToArray ();
			}
			set {
				m_PathsListStore.Clear ();
				
				foreach (var path in value) {
					m_PathsListStore.AppendValues (path);
				}
			}
		}
		
		public IPythonRuntime Runtime {
			get {
				Type runtimeType;
				Gtk.TreeIter iter;
				
				if (!this.m_RuntimeCombo.GetActiveIter (out iter))
					throw new Exception ("No selected runtime!");
				
				runtimeType = this.m_RuntimeListStore.GetValue (iter, 1) as Type;
				return Activator.CreateInstance (runtimeType) as IPythonRuntime;
			}
			set {
				Gtk.TreeIter iter;
				
				if (this.m_RuntimeListStore.GetIterFirst (out iter)) {
					do {
						Type t = this.m_RuntimeListStore.GetValue (iter, 1) as Type;
						if (t == value.GetType ()) {
							this.m_RuntimeCombo.SetActiveIter (iter);
							break;
						}
					} while (m_RuntimeListStore.IterNext (ref iter));
				}
			}
		}

		protected virtual void AddPath_Clicked (object sender, System.EventArgs e)
		{
			var dialog = new FileChooserDialog ("Add Path",
			                                    IdeApp.Workbench.RootWindow,
			                                    FileChooserAction.SelectFolder,
			                                    Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
			                                    Gtk.Stock.Open, Gtk.ResponseType.Ok);
			
			if (dialog.Run () == (int)Gtk.ResponseType.Ok) {
				m_PathsListStore.AppendValues (dialog.Filename);
			}
			
			dialog.Destroy ();
		}

		protected virtual void RemovePath_Clicked (object sender, System.EventArgs e)
		{
			TreeModel model;
			TreeIter iter;
			
			if (m_PathsTreeView.Selection.GetSelected (out model, out iter)) {
				(model as ListStore).Remove (ref iter);
			}
		}
	}
}
