// BuildPanel.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal class BuildPanel : OptionsPanel
	{
		BuildPanelWidget widget;

		public override Control CreatePanelWidget ()
		{
			return (widget = new  BuildPanelWidget ());
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
	
	internal partial class BuildPanelWidget :  Gtk.Bin 
	{
		public BuildPanelWidget ()
		{
			Build ();
			BeforeCompileAction action = IdeApp.Preferences.BeforeBuildSaveAction;
			saveChangesRadioButton.Active = action == BeforeCompileAction.SaveAllFiles;
			promptChangesRadioButton.Active = action == BeforeCompileAction.PromptForSave;
			noSaveRadioButton.Active = action == BeforeCompileAction.Nothing;
			runWithWarningsCheckBox.Active = IdeApp.Preferences.RunWithWarnings;
			buildBeforeRunCheckBox.Active = IdeApp.Preferences.BuildBeforeExecuting;
			verbosityCombo.Active = (int)IdeApp.Preferences.MSBuildVerbosity.Value;
			buildBeforeTestCheckBox.Active = IdeApp.Preferences.BuildBeforeRunningTests;
			parallelBuildCheckbox.Active = MonoDevelop.Core.Runtime.Preferences.ParallelBuild.Value;
		}
		
		public void Store ()
		{
			IdeApp.Preferences.RunWithWarnings.Value = runWithWarningsCheckBox.Active;
			IdeApp.Preferences.BuildBeforeExecuting.Value = buildBeforeRunCheckBox.Active;
			IdeApp.Preferences.MSBuildVerbosity.Value = (MSBuildVerbosity) verbosityCombo.Active;
			IdeApp.Preferences.BuildBeforeRunningTests.Value = buildBeforeTestCheckBox.Active;
			MonoDevelop.Core.Runtime.Preferences.ParallelBuild.Value = parallelBuildCheckbox.Active;
			if (saveChangesRadioButton.Active)
				IdeApp.Preferences.BeforeBuildSaveAction.Value = BeforeCompileAction.SaveAllFiles;
			else if (promptChangesRadioButton.Active)
				IdeApp.Preferences.BeforeBuildSaveAction.Value = BeforeCompileAction.PromptForSave;
			else if (noSaveRadioButton.Active)
				IdeApp.Preferences.BeforeBuildSaveAction.Value = BeforeCompileAction.Nothing;
		}
	}
}
