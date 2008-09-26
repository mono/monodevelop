//  CodeGenerationPanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.IO;
using System.Drawing;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui;

using Gtk;
using MonoDevelop.Components;

namespace CSharpBinding
{
	partial class CodeGenerationPanelWidget : Gtk.Bin 
	{
		//Project project;
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
				return;
			
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
	

	public class CodeGenerationPanel : MultiConfigItemOptionsPanel
	{
		CodeGenerationPanelWidget widget;
		
		public override Widget CreatePanelWidget()
		{
			return (widget = new  CodeGenerationPanelWidget ());
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
