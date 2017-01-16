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
using System.Linq;
using System.Collections.Generic;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	class MonoRuntimePanel : OptionsPanel
	{
		MonoRuntimePanelWidget widget;
		
		public override Control CreatePanelWidget ()
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
		readonly List<TargetRuntime> removedRuntimes = new List<TargetRuntime> ();
		readonly List<MonoRuntimeInfo> newInfos = new List<MonoRuntimeInfo> ();
		readonly ListStore store;
		TreeIter defaultIter;
		TreeIter runningIter;
		
		public MonoRuntimePanelWidget()
		{
			this.Build();
			
			textview1.SetMarkup (textview1.Buffer.Text);
			
			labelRunning.Markup = GettextCatalog.GetString (
				"{0} is currently running on <b>{1}</b>.",
				BrandingService.ApplicationName,
				Runtime.SystemAssemblyService.CurrentRuntime.DisplayName
			);
			store = new ListStore (typeof(string), typeof(object));
			tree.Model = store;
			tree.SearchColumn = -1; // disable the interactive search

			CellRendererText crt = new CellRendererText ();
			tree.AppendColumn (GettextCatalog.GetString ("Runtime"), crt, "markup", 0);
			TargetRuntime defRuntime = IdeApp.Preferences.DefaultTargetRuntime;
			
			foreach (TargetRuntime tr in Runtime.SystemAssemblyService.GetTargetRuntimes ()) {
				string name = tr.DisplayName;
				TreeIter it;
				if (tr == defRuntime) {
					name = string.Format ("<b>{0} {1}</b>", name, GettextCatalog.GetString ("(Default)"));
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
				IdeApp.Preferences.DefaultTargetRuntime.Value = (TargetRuntime)ob;

			foreach (var rinfo in newInfos) {
				TargetRuntime tr = MonoTargetRuntime.RegisterRuntime (rinfo);
				if (rinfo == newDefaultInfo)
					IdeApp.Preferences.DefaultTargetRuntime.Value = tr;
			}

			foreach (var tr in removedRuntimes.OfType<MonoTargetRuntime> ())
				MonoTargetRuntime.UnregisterRuntime (tr);
			
		}

		// ProgramFilesX86 is broken on 32-bit WinXP, this is a workaround
		static string GetProgramFilesX86 ()
		{
			return Environment.GetFolderPath (IntPtr.Size == 8?
				Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles);
		}
		
		protected virtual void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			var dlg = new SelectFolderDialog (GettextCatalog.GetString ("Select the mono installation prefix")) {
				TransientFor = this.Toplevel as Gtk.Window,
			};
			
			//set a platform-dependent default folder for the dialog if possible
			if (Platform.IsWindows) {
				// ProgramFilesX86 is broken on 32-bit WinXP
				string programFilesX86 = GetProgramFilesX86 ();
				if (!string.IsNullOrEmpty (programFilesX86) && System.IO.Directory.Exists (programFilesX86))
					dlg.CurrentFolder = programFilesX86;
			} else {
				if (System.IO.Directory.Exists ("/usr"))
					dlg.CurrentFolder = "/usr";
			}
			
			if (!dlg.Run ())
				return;
			
			var rinfo = new MonoRuntimeInfo (dlg.SelectedFile);
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
				if (store.GetPath (it).Equals (store.GetPath (defaultIter))) {
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
			if (store.GetPath (it).Equals (store.GetPath (defaultIter)))
				text = string.Format ("<b>{0} {1}</b>", text, GettextCatalog.GetString ("(Default)"));
			store.SetValue (it, 0, text);
		}
		
		void UpdateButtons ()
		{
			TreeIter it;
			if (tree.Selection.GetSelected (out it)) {
				object ob = store.GetValue (it, 1);
				MonoTargetRuntime tr = ob as MonoTargetRuntime;
				buttonRemove.Sensitive = (tr != null && tr.UserDefined) || ob is MonoRuntimeInfo;
				buttonDefault.Sensitive = true;
			} else {
				buttonRemove.Sensitive = false;
				buttonDefault.Sensitive = false;
			}
		}
	}
}
