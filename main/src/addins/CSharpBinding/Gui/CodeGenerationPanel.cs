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
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects.Parser;
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

		public 
		CodeGenerationPanelWidget (Properties CustomizationObject)
		{
			Build ();
			
			configuration = ((Properties)CustomizationObject).Get<DotNetProjectConfiguration> ("Config");
			//project = ((Properties)CustomizationObject).Get<Project> ("Project");
			compilerParameters = (CSharpCompilerParameters) configuration.CompilationParameters;
			
			symbolsEntry.Text                          = compilerParameters.DefineSymbols;
			generateDebugInformationCheckButton.Active = configuration.DebugMode;
			generateXmlOutputCheckButton.Active        = compilerParameters.GenerateXmlDocumentation;
			enableOptimizationCheckButton.Active       = compilerParameters.Optimize;
			generateOverflowChecksCheckButton.Active   = compilerParameters.GenerateOverflowChecks;
			warningsAsErrorsCheckButton.Active         = ! configuration.RunWithWarnings;
			warningLevelSpinButton.Value               = compilerParameters.WarningLevel;
			additionalArgsEntry.Text                   = compilerParameters.AdditionalArguments;
			ignoreWarningsEntry.Text                   = compilerParameters.NoWarnings;
		}

		public bool Store ()
		{
			if (compilerParameters == null) {
				return true;
			}
			
			compilerParameters.DefineSymbols            = symbolsEntry.Text;
			configuration.DebugMode                     = generateDebugInformationCheckButton.Active;
			compilerParameters.GenerateXmlDocumentation = generateXmlOutputCheckButton.Active;
			compilerParameters.Optimize                 = enableOptimizationCheckButton.Active;
			compilerParameters.GenerateOverflowChecks   = generateOverflowChecksCheckButton.Active;
			configuration.RunWithWarnings               = ! warningsAsErrorsCheckButton.Active;
			compilerParameters.WarningLevel             = warningLevelSpinButton.ValueAsInt;
			compilerParameters.AdditionalArguments      = additionalArgsEntry.Text;
			compilerParameters.NoWarnings               = ignoreWarningsEntry.Text;
			
			return true;
		}
	}
	

	public class CodeGenerationPanel : AbstractOptionPanel
	{
		CodeGenerationPanelWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new  CodeGenerationPanelWidget ((Properties) CustomizationObject));
		}
		
		public override bool StorePanelContents()
		{
			bool result = true;
			result = widget.Store ();
 			return result;
		}
	}
}
