//
// ExecutionModeSelectorDialog.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using Xwt;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Execution;
using System.Linq;
using MonoDevelop.Ide.Projects.OptionPanels;

namespace MonoDevelop.Ide.Execution
{
	class ExecutionModeSelectorDialog: Dialog
	{
		IRunTarget item;

		DialogButton runButton;
		RunConfigurationsList listConfigs;
		TreeView treeModes;
		
		TreeStore storeModes;

		DataField<string> configNameField = new DataField<string> ();
		DataField<RunConfiguration> configField = new DataField<RunConfiguration> ();
		DataField<string> modeNameField = new DataField<string> ();
		DataField<IExecutionMode> modeField = new DataField<IExecutionMode> ();
		DataField<IExecutionModeSet> modeSetField = new DataField<IExecutionModeSet> ();

		public ExecutionModeSelectorDialog ()
		{
			Title = GettextCatalog.GetString ("Execution Mode Selector");
			
			Width = 500;
			Height = 400;

			var box = new VBox ();
			Content = box;

			box.PackStart (new Label (GettextCatalog.GetString ("Run Configurations:")));
		
			listConfigs = new RunConfigurationsList ();
			box.PackStart (listConfigs, true);

			box.PackStart (new Label (GettextCatalog.GetString ("Execution Modes:")));

			storeModes = new TreeStore (modeNameField, modeField, modeSetField);
			treeModes = new TreeView (storeModes);
			treeModes.HeadersVisible = false;
			treeModes.Columns.Add (GettextCatalog.GetString ("Name"), modeNameField);
			box.PackStart (treeModes, true);

			runButton = new DialogButton (new Command ("run", GettextCatalog.GetString ("Run")));

			Buttons.Add (Command.Cancel);
			Buttons.Add (runButton);

			listConfigs.SelectionChanged += (sender, e) => LoadModes ();
			treeModes.SelectionChanged += OnModeChanged;
		}

		public void Load (IRunTarget item)
		{
			this.item = item;
			var configs = item.GetRunConfigurations ().ToArray ();
			listConfigs.Fill (configs);
			listConfigs.SelectedConfiguration = configs.FirstOrDefault ();
			LoadModes ();
		}

		void LoadModes ()
		{
			storeModes.Clear ();
			var currentMode = SelectedExecutionMode;
			bool nodeSelected = false;
			var ctx = new CommandExecutionContext (item, h => item.CanExecute (new ExecutionContext (h, null, IdeApp.Workspace.ActiveExecutionTarget), IdeApp.Workspace.ActiveConfiguration, SelectedConfiguration));
			foreach (var modeSet in Runtime.ProcessService.GetExecutionModes ()) {
				TreeNavigator setNode = null;
				foreach (var mode in modeSet.ExecutionModes) {
					if (ctx.CanExecute (mode.ExecutionHandler)) {
						if (setNode == null) {
							setNode = storeModes.AddNode ();
							setNode.SetValue (modeNameField, modeSet.Name);
							setNode.SetValue (modeField, mode);
							setNode.SetValue (modeSetField, modeSet);
							if (mode.Id == currentMode?.Id) {
								treeModes.SelectRow (setNode.CurrentPosition);
								nodeSelected = true;
							}
						}
						var node = storeModes.AddNode (setNode.CurrentPosition);
						node.SetValue (modeNameField, mode.Name);
						node.SetValue (modeField, mode);
						node.SetValue (modeSetField, modeSet);
						if (!nodeSelected && mode.Id == currentMode?.Id) {
							treeModes.SelectRow (node.CurrentPosition);
							nodeSelected = true;
						}
					}
				}
				// If the mode only has one child, remove it, we don't need to show it
				if (setNode != null && setNode.MoveToChild ()) {
					var pos = setNode.Clone ();
					if (!setNode.MoveNext ())
						pos.Remove ();
				}
			}
			if (!nodeSelected && storeModes.GetFirstNode ()?.CurrentPosition != null)
				treeModes.SelectRow (storeModes.GetFirstNode ().CurrentPosition);
		}

		void OnModeChanged (object sender, EventArgs e)
		{
			UpdateButtons ();
		}

		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd.Id == "run") {
				Respond (cmd);
				return;
			}
			base.OnCommandActivated (cmd);
		}

		void UpdateButtons ()
		{
			runButton.Sensitive = SelectedConfiguration != null && SelectedExecutionMode != null;
			if (SelectedExecutionMode != null)
				runButton.Label = SelectedExecutionModeSet.Name;
			else
				runButton.Label = GettextCatalog.GetString ("Run");
		}

		public RunConfiguration SelectedConfiguration {
			get {
				return listConfigs.SelectedConfiguration;
			}
		}

		public IExecutionMode SelectedExecutionMode {
			get {
				var n = treeModes.SelectedRow;
				return n != null ? storeModes.GetNavigatorAt (n).GetValue (modeField) : null;
			}
		}

		public IExecutionModeSet SelectedExecutionModeSet {
			get {
				var n = treeModes.SelectedRow;
				return n != null ? storeModes.GetNavigatorAt (n).GetValue (modeSetField) : null;
			}
		}
	}
}

