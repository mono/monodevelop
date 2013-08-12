//
// CodeGenerationPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects;
using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.CSharp.Project
{
	partial class CodeGenerationPanelWidget : Gtk.Bin 
	{
		const int DEBUG_FULL = 0;
		const int DEBUG_PDB_ONLY = 1;
		const int DEBUG_NONE = 2;

		DotNetProjectConfiguration configuration;
		CSharpCompilerParameters compilerParameters = null;
		
		public CodeGenerationPanelWidget ()
		{
			Build ();
		}
		
		public void Load (DotNetProjectConfiguration configuration)
		{
			this.configuration = configuration;
			compilerParameters = (CSharpCompilerParameters) configuration.CompilationParameters;
			
			symbolsEntry.Text                          = compilerParameters.DefineSymbols;
			generateXmlOutputCheckButton.Active        = compilerParameters.GenerateXmlDocumentation;
			enableOptimizationCheckButton.Active       = compilerParameters.Optimize;
			generateOverflowChecksCheckButton.Active   = compilerParameters.GenerateOverflowChecks;
			warningsAsErrorsCheckButton.Active         = compilerParameters.TreatWarningsAsErrors;
			warningLevelSpinButton.Value               = compilerParameters.WarningLevel;
			additionalArgsEntry.Text                   = compilerParameters.AdditionalArguments;
			ignoreWarningsEntry.Text                   = compilerParameters.NoWarnings;
			
			int i = CSharpLanguageBinding.SupportedPlatforms.IndexOf (compilerParameters.PlatformTarget);
			comboPlatforms.Active = i != -1 ? i : 0;

			if (!configuration.DebugMode || string.Equals ("none", compilerParameters.DebugType, StringComparison.OrdinalIgnoreCase)) {
				comboDebug.Active = DEBUG_NONE;
			} else if (string.Equals ("pdbonly", compilerParameters.DebugType, StringComparison.OrdinalIgnoreCase)) {
				comboDebug.Active = DEBUG_PDB_ONLY;
			} else {
				comboDebug.Active = DEBUG_FULL;
			}
		}

		public void Store ()
		{
			if (compilerParameters == null)
				throw new ApplicationException ("Code generation panel wasn't loaded !");
			
			compilerParameters.DefineSymbols            = symbolsEntry.Text;
			compilerParameters.GenerateXmlDocumentation = generateXmlOutputCheckButton.Active;
			compilerParameters.Optimize                 = enableOptimizationCheckButton.Active;
			compilerParameters.GenerateOverflowChecks   = generateOverflowChecksCheckButton.Active;
			compilerParameters.TreatWarningsAsErrors    = warningsAsErrorsCheckButton.Active;
			compilerParameters.WarningLevel             = warningLevelSpinButton.ValueAsInt;
			compilerParameters.AdditionalArguments      = additionalArgsEntry.Text;
			compilerParameters.NoWarnings               = ignoreWarningsEntry.Text;
			compilerParameters.PlatformTarget           = CSharpLanguageBinding.SupportedPlatforms [comboPlatforms.Active];

			switch (comboDebug.Active) {
			case DEBUG_FULL:
				configuration.DebugMode = true;
				if (!string.Equals (compilerParameters.DebugType, "full", StringComparison.OrdinalIgnoreCase)) {
					compilerParameters.DebugType = "";
				}
				break;
			case DEBUG_PDB_ONLY:
				configuration.DebugMode = true;
				compilerParameters.DebugType = "pdbonly";
				break;
			case DEBUG_NONE:
				configuration.DebugMode = false;
				if (!string.Equals (compilerParameters.DebugType, "none", StringComparison.OrdinalIgnoreCase)) {
					compilerParameters.DebugType = "";
				}
				break;
			}
		}
	}
	
	class CodeGenerationPanel : MultiConfigItemOptionsPanel
	{
		CodeGenerationPanelWidget widget;
		
		public override Widget CreatePanelWidget()
		{
			return widget = new  CodeGenerationPanelWidget ();
		}
		
		public override void LoadConfigData ()
		{
			widget.Load ((DotNetProjectConfiguration) CurrentConfiguration);
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
