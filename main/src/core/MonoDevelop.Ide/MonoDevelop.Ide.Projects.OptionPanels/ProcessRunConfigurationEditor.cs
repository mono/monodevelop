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

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	class ProcessRunConfigurationEditor: RunConfigurationEditor
	{
		ProcessRunConfigurationEditorWidget widget;

		public ProcessRunConfigurationEditor ()
		{
			widget = new ProcessRunConfigurationEditorWidget ();
			widget.Changed += (sender, e) => NotifyChanged ();
		}

		public override Control CreateControl ()
		{
			return new XwtControl (widget);
		}

		public override void Load (Project project, SolutionItemRunConfiguration config)
		{
			widget.Load ((ProcessRunConfiguration)config);
		}

		public override void Save ()
		{
			widget.Save ();
		}
	}

	class ProcessRunConfigurationEditorWidget : VBox
	{
		TextEntry argumentsEntry;
		FolderSelector workingDir;
		EnvironmentVariableCollectionEditor envVars;
		ProcessRunConfiguration config;
		CheckBox externalConsole;
		CheckBox pauseConsole;

		public ProcessRunConfigurationEditorWidget ()
		{
			VBox mainBox = this;

			mainBox.Margin = 12;
			var table = new Table ();

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

			argumentsEntry.Changed += (s, a) => NotifyChanged ();
			workingDir.FolderChanged += (s, a) => NotifyChanged ();
			envVars.Changed += (s, a) => NotifyChanged ();
			externalConsole.Toggled += (s, a) => NotifyChanged ();
			pauseConsole.Toggled += (s, a) => NotifyChanged ();
		}

		public void Load (ProcessRunConfiguration config)
		{
			this.config = config;
			argumentsEntry.Text = config.StartArguments;
			workingDir.Folder = config.StartWorkingDirectory;
			envVars.LoadValues (config.EnvironmentVariables);
			externalConsole.Active = config.ExternalConsole;
			pauseConsole.Active = config.PauseConsoleOutput;
			UpdateStatus ();
		}

		void UpdateStatus ()
		{
			pauseConsole.Sensitive = externalConsole.Active;
		}

		public void Save ()
		{
			config.StartArguments = argumentsEntry.Text;
			config.StartWorkingDirectory = workingDir.Folder;
			config.ExternalConsole = externalConsole.Active;
			config.PauseConsoleOutput = pauseConsole.Active;
			envVars.StoreValues (config.EnvironmentVariables);
		}

		void NotifyChanged ()
		{
			Changed?.Invoke (this, EventArgs.Empty);
		}

		public event EventHandler Changed;
	}
}

