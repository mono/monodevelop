//
// DotNetRunConfigurationEditor.cs
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
using MonoDevelop.Components;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Projects;
using Xwt;
using MonoDevelop.Core;
using System.IO;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	class AssemblyRunConfigurationEditor: RunConfigurationEditor
	{
		DotNetRunConfigurationEditorWidget widget;

		public AssemblyRunConfigurationEditor ()
		{
			widget = new DotNetRunConfigurationEditorWidget ();
		}

		public override Control CreateControl ()
		{
			return new XwtControl (widget);
		}

		public override void Load (Project project, SolutionItemRunConfiguration config)
		{
			widget.Load (project, (AssemblyRunConfiguration)config);
			widget.Changed += (sender, e) => NotifyChanged ();
		}

		public override void Save ()
		{
			widget.Save ();
		}

		public override bool Validate ()
		{
			return widget.Validate ();
		}
	}

	public class DotNetRunConfigurationEditorWidget: Notebook
	{
		RadioButton radioStartProject;
		RadioButton radioStartApp;
		Xwt.FileSelector appEntry;
		TextEntry argumentsEntry;
		FolderSelector workingDir;
		EnvironmentVariableCollectionEditor envVars;
		AssemblyRunConfiguration config;
		CheckBox externalConsole;
		CheckBox pauseConsole;
		ComboBox runtimesCombo;
		TextEntry monoSettingsEntry;
		InformationPopoverWidget appEntryInfoIcon;

		public DotNetRunConfigurationEditorWidget ()
			: this (true)
		{

		}

		public DotNetRunConfigurationEditorWidget (bool includeAdvancedTab)
		{
			VBox mainBox = new VBox ();

			mainBox.Margin = 12;
			mainBox.PackStart (new Label { Markup = GettextCatalog.GetString ("Start Action") });
			var table = new Table ();
			
			table.Add (radioStartProject = new RadioButton (GettextCatalog.GetString ("Start project")), 0, 0);
			table.Add (radioStartApp = new RadioButton (GettextCatalog.GetString ("Start external program:")), 0, 1);
			table.Add (appEntry = new Xwt.FileSelector (), 1, 1, hexpand: true);
			table.Add (appEntryInfoIcon = new InformationPopoverWidget (), 2, 1);
			appEntryInfoIcon.Hide ();
			radioStartProject.Group = radioStartApp.Group;
			table.MarginLeft = 12;
			mainBox.PackStart (table);

			mainBox.PackStart (new HSeparator () { MarginTop = 8, MarginBottom = 8 });
			table = new Table ();

			table.Add (new Label (GettextCatalog.GetString ("Arguments:")), 0, 0);
			table.Add (argumentsEntry = new TextEntry (), 1, 0, hexpand:true);

			table.Add (new Label (GettextCatalog.GetString ("Run in directory:")), 0, 1);
			table.Add (workingDir = new FolderSelector (), 1, 1, hexpand: true);
		
			mainBox.PackStart (table);

			mainBox.PackStart (new HSeparator () { MarginTop = 8, MarginBottom = 8 });

			mainBox.PackStart (new Label (GettextCatalog.GetString ("Environment Variables")));
			envVars = new EnvironmentVariableCollectionEditor ();

			mainBox.PackStart (envVars, true);

			mainBox.PackStart (new HSeparator () { MarginTop = 8, MarginBottom = 8 });

			HBox cbox = new HBox ();
			cbox.PackStart (externalConsole = new CheckBox (GettextCatalog.GetString ("Run on external console")));
			cbox.PackStart (pauseConsole = new CheckBox (GettextCatalog.GetString ("Pause console output")));
			mainBox.PackStart (cbox);

			Add (mainBox, GettextCatalog.GetString ("General"));

			var adBox = new VBox ();
			adBox.Margin = 12;

			table = new Table ();
			table.Add (new Label (GettextCatalog.GetString ("Execute in .NET Runtime:")), 0, 0);
			table.Add (runtimesCombo = new ComboBox (), 1, 0, hexpand:true);

			table.Add (new Label (GettextCatalog.GetString ("Mono runtime settings:")), 0, 1);

			var box = new HBox ();
			Button monoSettingsButton = new Button (GettextCatalog.GetString ("..."));
			box.PackStart (monoSettingsEntry = new TextEntry { PlaceholderText = GettextCatalog.GetString ("Default settings")}, true);
			box.PackStart (monoSettingsButton);
			monoSettingsEntry.ReadOnly = true;
			table.Add (box, 1, 1, hexpand: true);
			adBox.PackStart (table);

			if (includeAdvancedTab)
				Add (adBox, GettextCatalog.GetString ("Advanced"));

			monoSettingsButton.Clicked += EditRuntimeClicked;
			radioStartProject.ActiveChanged += (sender, e) => UpdateStatus ();
			externalConsole.Toggled += (sender, e) => UpdateStatus ();

			LoadRuntimes ();

			appEntry.FileChanged += (sender, e) => NotifyChanged ();
			argumentsEntry.Changed += (sender, e) => NotifyChanged ();
			workingDir.FolderChanged += (sender, e) => NotifyChanged ();
			envVars.Changed += (sender, e) => NotifyChanged ();
			externalConsole.Toggled += (sender, e) => NotifyChanged ();
			pauseConsole.Toggled += (sender, e) => NotifyChanged ();
			runtimesCombo.SelectionChanged += (sender, e) => NotifyChanged ();
			monoSettingsEntry.Changed += (sender, e) => NotifyChanged ();

		}

		void LoadRuntimes ()
		{
			runtimesCombo.Items.Add ("", GettextCatalog.GetString ("(Default runtime)"));
			foreach (var r in Runtime.SystemAssemblyService.GetTargetRuntimes ())
				runtimesCombo.Items.Add (r.Id, r.DisplayName);
		}

		void EditRuntimeClicked (object sender, EventArgs e)
		{
			using (var dlg = new Xwt.Dialog ()) {
				dlg.Title = GettextCatalog.GetString ("Mono Runtime Settings");
				dlg.Width = 700;
				dlg.Height = 500;
				var w = new MonoExecutionParametersWidget ();
				var mparams = config.MonoParameters.Clone ();
				w.Load (mparams);
				w.ShowAll ();
				dlg.Content = Surface.ToolkitEngine.WrapWidget (w);
				dlg.Buttons.Add (Command.Ok, Command.Cancel);
				dlg.TransientFor = ParentWindow;
				if (dlg.Run () == Command.Ok) {
					config.MonoParameters = mparams;
					monoSettingsEntry.Text = config.MonoParameters.GenerateDescription ();
				}
			}
		}

		public void Load (Project project, AssemblyRunConfiguration config)
		{
			this.config = config;
			if (config.StartAction == AssemblyRunConfiguration.StartActions.Project)
				radioStartProject.Active = true;
			else
				radioStartApp.Active = true;
			
			appEntry.FileName = config.StartProgram.ToString ();
			appEntry.CurrentFolder = project.BaseDirectory;
			argumentsEntry.Text = config.StartArguments;
			workingDir.Folder = config.StartWorkingDirectory;
			workingDir.CurrentFolder = project.BaseDirectory;
			envVars.LoadValues (config.EnvironmentVariables);
			externalConsole.Active = config.ExternalConsole;
			pauseConsole.Active = config.PauseConsoleOutput;
			runtimesCombo.SelectedItem = config.TargetRuntimeId;
			monoSettingsEntry.Text = config.MonoParameters.GenerateDescription ();
			UpdateStatus ();
		}

		void UpdateStatus ()
		{
			appEntry.Sensitive = radioStartApp.Active;
			pauseConsole.Sensitive = externalConsole.Active;
		}

		public bool Validate ()
		{
/*			if (radioStartApp.Active && string.IsNullOrEmpty (appEntry.FileName)) {
				appEntryInfoIcon.Severity = Tasks.TaskSeverity.Error;
				appEntryInfoIcon.Message = GettextCatalog.GetString ("Application file not provided");
				appEntryInfoIcon.Show ();
				return false;
			}*/
			appEntryInfoIcon.Hide ();
			return true;
		}

		public void Save ()
		{
			if (radioStartProject.Active)
				config.StartAction = AssemblyRunConfiguration.StartActions.Project;
			else if (radioStartApp.Active)
				config.StartAction = AssemblyRunConfiguration.StartActions.Program;
			config.StartProgram = appEntry.FileName;
			config.StartArguments = argumentsEntry.Text;
			config.StartWorkingDirectory = workingDir.Folder;
			config.ExternalConsole = externalConsole.Active;
			config.PauseConsoleOutput = pauseConsole.Active;
			config.TargetRuntimeId = (string) runtimesCombo.SelectedItem;
			envVars.StoreValues (config.EnvironmentVariables);
		}

		protected void NotifyChanged ()
		{
			Changed?.Invoke (this, EventArgs.Empty);
		}

		public event EventHandler Changed;
	}
}

