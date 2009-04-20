// 
// MonoRuntimePanelWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	class MonoRuntimePanel : OptionsPanel
	{
		MonoRuntimePanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return widget = new MonoRuntimePanelWidget ();
		}
		
		public override void ApplyChanges ()
		{
			widget.Store();
		}
	}
	
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class MonoRuntimePanelWidget : Gtk.Bin
	{
		ListStore store;
		List<MonoRuntimeInfo> newInfos = new List<MonoRuntimeInfo> ();
		List<TargetRuntime> removedRuntimes = new List<TargetRuntime> ();
		TreeIter defaultIter;
		TreeIter runningIter;
		
		public MonoRuntimePanelWidget()
		{
			this.Build();
			
			labelRunning.Markup = GettextCatalog.GetString ("MonoDevelop is currently running on <b>{0}</b>.", Runtime.SystemAssemblyService.CurrentRuntime.DisplayName);
			store = new ListStore (typeof(string), typeof(object));
			tree.Model = store;
			
			CellRendererText crt = new CellRendererText ();
			tree.AppendColumn ("Runtime", crt, "markup", 0);
			TargetRuntime defRuntime = IdeApp.Preferences.DefaultTargetRuntime;
			
			foreach (TargetRuntime tr in Runtime.SystemAssemblyService.GetTargetRuntimes ()) {
				string name = tr.DisplayName;
				TreeIter it;
				if (tr == defRuntime) {
					name = "<b>" + name + " (Default)</b>";
					defaultIter = it = store.AppendValues (name, tr);
				} else
					it = store.AppendValues (name, tr);
				if (tr.IsRunning)
					runningIter = it;
			}
			
			tree.Selection.Changed += HandleChanged;
			UpdateButtons ();
		}

		void HandleChanged(object sender, EventArgs e)
		{
			UpdateButtons ();
		}
		
		public void Store ()
		{
			object ob = store.GetValue (defaultIter, 1);
			MonoRuntimeInfo newDefaultInfo = ob as MonoRuntimeInfo;
			if (ob is TargetRuntime)
				IdeApp.Preferences.DefaultTargetRuntime = (TargetRuntime)ob;
			
			foreach (MonoRuntimeInfo rinfo in newInfos) {
				TargetRuntime tr = MonoTargetRuntime.RegisterRuntime (rinfo);
				if (rinfo == newDefaultInfo)
					IdeApp.Preferences.DefaultTargetRuntime = tr;
			}
			foreach (TargetRuntime tr in removedRuntimes)
				Runtime.SystemAssemblyService.UnregisterRuntime (tr);
			
		}
		
		protected virtual void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			FolderDialog fd = new FolderDialog (GettextCatalog.GetString ("Select the mono installation prefix"));
			fd.SetFilename ("/usr");
			
			int response = fd.Run ();
			
			if (response != (int) ResponseType.Ok) {
				fd.Hide ();
				return;
			}
			fd.Hide ();
			
			MonoRuntimeInfo rinfo = new MonoRuntimeInfo (fd.Filename);
			if (!rinfo.IsValidRuntime) {
				MessageService.ShowError (GettextCatalog.GetString ("Mono runtime not found"), GettextCatalog.GetString ("Please provide a valid directory prefix where mono is installed (for example, /usr)"));
				return;
			}
			newInfos.Add (rinfo);
			store.AppendValues (rinfo.DisplayName, rinfo);
		}
	
		protected virtual void OnButtonRemoveClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (tree.Selection.GetSelected (out it)) {
				object ob = store.GetValue (it, 1);
				if (ob is MonoRuntimeInfo)
					newInfos.Remove ((MonoRuntimeInfo)ob);
				else {
					TargetRuntime tr = (TargetRuntime) ob;
					if (tr.IsRunning)
						return;
					removedRuntimes.Add (tr);
				}
				if (it.Equals (defaultIter)) {
					defaultIter = runningIter;
					UpdateRow (defaultIter);
				}
				store.Remove (ref it);
			}
		}
	
		protected virtual void OnButtonDefaultClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (tree.Selection.GetSelected (out it)) {
				TreeIter oldDefault = defaultIter;
				defaultIter = it;
				UpdateRow (oldDefault);
				UpdateRow (defaultIter);
			}
		}
		
		void UpdateRow (TreeIter it)
		{
			object ob = store.GetValue (it, 1);
			string text;
			if (ob is MonoRuntimeInfo)
				text = ((MonoRuntimeInfo)ob).DisplayName;
			else
				text = ((TargetRuntime)ob).DisplayName;
			if (it.Equals (defaultIter))
				text = "<b>" + text + " (Default)</b>";
			store.SetValue (it, 0, text);
		}
		
		void UpdateButtons ()
		{
			TreeIter it;
			if (tree.Selection.GetSelected (out it)) {
				object ob = store.GetValue (it, 1);
				buttonRemove.Sensitive = !(ob is TargetRuntime && ((TargetRuntime)ob).IsRunning);
				buttonDefault.Sensitive = true;
			} else {
				buttonRemove.Sensitive = false;
				buttonDefault.Sensitive = false;
			}
		}
	}
}
