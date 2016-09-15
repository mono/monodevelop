//
// RunWithCustomParametersDialog.cs
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
using System.Linq;
using Xwt;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Cairo;
using MonoDevelop.Core.Execution;
using System.Collections.Generic;
using System.Data;

namespace MonoDevelop.Ide.Execution
{
	class RunWithCustomParametersDialog: Dialog
	{
		RunConfigurationEditor editor;
		Project project;
		ProjectRunConfiguration runConfig;
		ComboBox modeCombo;
		DialogButton runButton;

		public RunWithCustomParametersDialog (Project project)
		{
			this.project = project;
			runConfig = project.CreateRunConfiguration ("Custom");

			Title = GettextCatalog.GetString ("Custom Parameters");

			Width = 650;
			Height = 400;

			editor = RunConfigurationService.CreateEditorForConfiguration (runConfig);
			editor.Load (project, runConfig);

			var box = new VBox ();
			Content = box;
			var c = editor.CreateControl ().GetNativeWidget<Gtk.Widget> ();
			box.PackStart (box.Surface.ToolkitEngine.WrapWidget (c, NativeWidgetSizing.DefaultPreferredSize), true);

			box.PackStart (new HSeparator ());

			var hbox = new HBox ();
			hbox.PackStart (new Label ("Run Action: "));
			hbox.PackStart (modeCombo = new ComboBox ());
			box.PackStart (hbox);

			runButton = new DialogButton (new Command ("run", GettextCatalog.GetString ("Run")));

			Buttons.Add (Command.Cancel);
			Buttons.Add (runButton);

			LoadModes ();
			UpdateStatus ();

			editor.Changed += Editor_Changed;
			modeCombo.SelectionChanged += (s,a) => UpdateStatus ();
		}

		void LoadModes ()
		{
			var currentMode = modeCombo.SelectedItem;
			modeCombo.Items.Clear ();
			var modes = GetExecutionConfigurations ();
			foreach (var mode in modes) {
				string label = /*modes.Count (m => m.ModeSet == mode.ModeSet) == 1 ? mode.ModeSet.Name :*/ mode.ModeSet.Name + " – " + mode.Mode.Name;
				modeCombo.Items.Add (mode, label);
			}
			modeCombo.SelectedItem = currentMode;
			if (modeCombo.SelectedIndex == -1)
				modeCombo.SelectedIndex = 0;
		}

		void Editor_Changed (object sender, EventArgs e)
		{
			LoadModes ();
			UpdateStatus ();
		}

		void UpdateStatus ()
		{
			runButton.Sensitive = editor.Validate () && modeCombo.SelectedItem != null;
			if (SelectedExecutionModeSet != null)
				runButton.Label = SelectedExecutionModeSet.Name;
		}

		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd == runButton.Command)
				editor.Save ();
			base.OnCommandActivated (cmd);
		}

		List<ExecutionConfiguration> GetExecutionConfigurations ()
		{
			var ctx = new CommandExecutionContext (project, h => project.CanExecute (new ExecutionContext (h, null, IdeApp.Workspace.ActiveExecutionTarget), IdeApp.Workspace.ActiveConfiguration, runConfig));
			var res = new List<ExecutionConfiguration> ();
			foreach (var modeSet in Runtime.ProcessService.GetExecutionModes ()) {
				foreach (var mode in modeSet.ExecutionModes) {
					if (ctx.CanExecute (mode.ExecutionHandler))
						res.Add (new ExecutionConfiguration (runConfig, modeSet, mode));
				}
			}
			return res;
		}

		public RunConfiguration SelectedConfiguration {
			get {
				return runConfig;
			}
		}

		public IExecutionMode SelectedExecutionMode {
			get {
				var c = (ExecutionConfiguration)modeCombo.SelectedItem;
				return c?.Mode;
			}
		}

		public IExecutionModeSet SelectedExecutionModeSet {
			get {
				var c = (ExecutionConfiguration)modeCombo.SelectedItem;
				return c?.ModeSet;
			}
		}
	}
}

