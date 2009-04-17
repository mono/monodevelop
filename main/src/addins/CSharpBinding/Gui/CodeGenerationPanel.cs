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

namespace CSharpBinding
{
	partial class CodeGenerationPanelWidget : Gtk.Bin 
	{
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
			generateDebugInformationCheckButton.Active = configuration.DebugMode;
			generateXmlOutputCheckButton.Active        = compilerParameters.GenerateXmlDocumentation;
			enableOptimizationCheckButton.Active       = compilerParameters.Optimize;
			generateOverflowChecksCheckButton.Active   = compilerParameters.GenerateOverflowChecks;
			warningsAsErrorsCheckButton.Active         = compilerParameters.TreatWarningsAsErrors;
			warningLevelSpinButton.Value               = compilerParameters.WarningLevel;
			additionalArgsEntry.Text                   = compilerParameters.AdditionalArguments;
			ignoreWarningsEntry.Text                   = compilerParameters.NoWarnings;
		}

		public void Store ()
		{
			if (compilerParameters == null)
				throw new ApplicationException ("Code generation panel wasn't loaded !");
			
			compilerParameters.DefineSymbols            = symbolsEntry.Text;
			configuration.DebugMode                     = generateDebugInformationCheckButton.Active;
			compilerParameters.GenerateXmlDocumentation = generateXmlOutputCheckButton.Active;
			compilerParameters.Optimize                 = enableOptimizationCheckButton.Active;
			compilerParameters.GenerateOverflowChecks   = generateOverflowChecksCheckButton.Active;
			compilerParameters.TreatWarningsAsErrors    = warningsAsErrorsCheckButton.Active;
			compilerParameters.WarningLevel             = warningLevelSpinButton.ValueAsInt;
			compilerParameters.AdditionalArguments      = additionalArgsEntry.Text;
			compilerParameters.NoWarnings               = ignoreWarningsEntry.Text;
		}
	}
	
	public class CodeGenerationPanel : MonoDevelop.Projects.Gui.Dialogs.MultiConfigItemOptionsPanel
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
