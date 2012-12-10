// 
// CustomExecutionModeDialog.cs
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
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Projects;
using MonoDevelop.Core;


namespace MonoDevelop.Ide.Execution
{
	internal partial class CustomExecutionModeDialog : Gtk.Dialog
	{
		IExecutionMode mode;
		Dictionary<object,IExecutionConfigurationEditor> currentEditors = new Dictionary<object, IExecutionConfigurationEditor> ();
		CommandExecutionContext ctx;
		CustomExecutionMode data;
		bool editMode = true;
		bool nameChanged;
		
		public CustomExecutionModeDialog ()
		{
			this.Build ();
			TransientFor = IdeApp.Workbench.RootWindow;
		}
		
		public void Initialize (CommandExecutionContext ctx, IExecutionMode modeToExecute, CustomExecutionMode data)
		{
			this.ctx = ctx;
			
			this.data = data;
			if (this.data == null)
				this.data = new CustomExecutionMode ();
			
			if (modeToExecute != null) {
				// The user is running the project and the selected mode requires arguments
				comboTargetMode.Load (ctx, true, false);
				comboTargetMode.SelectedMode = modeToExecute;
				mode = modeToExecute;
				editMode = false;
				buttonOk.Label = Gtk.Stock.Execute;
			}
			else if (data != null) {
				// Editing an existing custom mode
				comboTargetMode.Load (ctx, true, false);
				mode = comboTargetMode.SelectedMode = data.Mode;
				checkSave.Visible = false;
				checkPrompt.Visible = boxName.Visible = true;
				checkPrompt.Active = data.PromptForParameters;
				entryModeName.Text = data.Name;
				comboStore.Active = (int) data.Scope;
			}
			else {
				// Creating a new mode
				comboTargetMode.Load (ctx, true, false);
				comboTargetMode.SelectedMode = null;
				checkSave.Visible = false;
				boxName.Visible = true;
				checkPrompt.Visible = true;
				buttonOk.Sensitive = false; // Until name is intered
			}
			LoadEditors ();
			nameChanged = false;
		}
	
		void LoadEditors ()
		{
			Dictionary<object,object> oldData = new Dictionary<object, object> ();
			foreach (KeyValuePair<object,IExecutionConfigurationEditor> editor in currentEditors) {
				object data = editor.Value.Save ();
				oldData [editor.Key] = data;
			}
			
			foreach (Gtk.Widget w in notebook.Children) {
				notebook.Remove (w);
				w.Destroy ();
			}
			
			if (mode == null)
				return;
			
			currentEditors.Clear ();
			
			foreach (ExecutionCommandCustomizer customizer in ExecutionModeCommandService.GetExecutionCommandCustomizers (ctx)) {
				IExecutionConfigurationEditor e = customizer.CreateEditor ();
				currentEditors.Add (customizer, e);
				object cdata;
				if (!oldData.TryGetValue (customizer, out cdata))
					cdata = data.GetCommandData (customizer.Id);
				Gtk.Widget w = e.Load (ctx, cdata);
				w.Show ();
				notebook.AppendPage (w, new Gtk.Label (GettextCatalog.GetString (customizer.Name)));
			}
			
			ParameterizedExecutionHandler handler = mode.ExecutionHandler as ParameterizedExecutionHandler;
			if (handler != null) {
				IExecutionConfigurationEditor e = handler.CreateEditor ();
				currentEditors.Add (mode, e);
				object cdata;
				if (!oldData.TryGetValue (mode, out cdata))
					cdata = data.Data;
				Gtk.Widget w = e.Load (ctx, data.Data);
				w.Show ();
				notebook.AppendPage (w, new Gtk.Label (mode.Name));
			}
			notebook.ShowTabs = notebook.ShowBorder = currentEditors.Count > 1;
			hseparator.Visible = !notebook.ShowTabs;
		}
		
		public CustomExecutionMode GetConfigurationData ()
		{
			CustomExecutionMode cmode = new CustomExecutionMode ();
			cmode.Mode = mode;
			foreach (KeyValuePair<object,IExecutionConfigurationEditor> editor in currentEditors) {
				if (editor.Key is IExecutionMode)
					cmode.Data = editor.Value.Save ();
				else {
					ExecutionCommandCustomizer customizer = (ExecutionCommandCustomizer) editor.Key;
					cmode.SetCommandData (customizer.Id, editor.Value.Save ());
				}
			}
			cmode.Name = entryModeName.Text;
			cmode.Scope = (CustomModeScope) comboStore.Active;
			cmode.PromptForParameters = checkPrompt.Active;
			cmode.Id = data.Id;
			return cmode;
		}
		
		public IExecutionMode TargetMode {
			get { return mode; }
		}
		
		public bool Save {
			get { return checkSave.Active; }
		}
		
		protected virtual void OnCheckSaveToggled (object sender, System.EventArgs e)
		{
			boxName.Visible = checkPrompt.Visible = checkSave.Active;
			
			if (checkSave.Active && entryModeName.Text.Length == 0) {
				if (!nameChanged)
					SuggestName ();
			}
			else if (!checkSave.Active)
				buttonOk.Sensitive = true;
		}
		
		void SuggestName ()
		{
			if (mode == null)
				return;
			
			string baseName = mode.Name;
			int count = 1;
			
			for (int n=1; n<100; n++) {
				if (baseName.EndsWith (GetNamePosfix (n))) {
					count = n + 1;
					baseName = baseName.Substring (0, baseName.Length - GetNamePosfix (n).Length);
				}
			}
			
			string name = baseName + GetNamePosfix (count);
			bool found;
			do {
				found = false;
				foreach (IExecutionMode m in ExecutionModeCommandService.GetExecutionModes (ctx)) {
					if (m.Name == name) {
						found = true;
						break;
					}
				}
				if (found)
					name = baseName + GetNamePosfix (count++);
			} while (found);
			
			entryModeName.Text = name;
			nameChanged = false;
		}
		
		string GetNamePosfix (int n)
		{
			if (n < 2)
				return " " + GettextCatalog.GetString ("(Custom)");
			else
				return " " + GettextCatalog.GetString ("(Custom {0})", n);
		}

		protected virtual void OnEntryModeNameChanged (object sender, System.EventArgs e)
		{
			buttonOk.Sensitive = entryModeName.Text.Length > 0;
			nameChanged = true;
		}

		protected virtual void OnComboTargetModeSelectionChanged (object sender, System.EventArgs e)
		{
			mode = comboTargetMode.SelectedMode;

			if (!editMode)
				return;

			LoadEditors ();
			if (!nameChanged)
				SuggestName ();
		}
	}
}
