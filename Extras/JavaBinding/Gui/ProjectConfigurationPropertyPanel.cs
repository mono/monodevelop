//  ProjectConfigurationPropertyPanel.cs
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
using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace JavaBinding
{
	public class ProjectConfigurationPropertyPanel : AbstractOptionPanel
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

	partial class CodeGenerationPanelWidget : Gtk.Bin 
	{
		JavaCompilerParameters compilerParameters = null;
		DotNetProjectConfiguration configuration;
		
		public CodeGenerationPanelWidget (Properties CustomizationObject)
		{	
			Build ();
			
			configuration = ((Properties)CustomizationObject).Get<DotNetProjectConfiguration> ("Config");
			compilerParameters = (JavaCompilerParameters) configuration.CompilationParameters;
			
			ListStore store = new ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Executable"));
			store.AppendValues (GettextCatalog.GetString ("Library"));
			compileTargetCombo.Model = store;
			CellRendererText cr = new CellRendererText ();
			compileTargetCombo.PackStart (cr, true);
			compileTargetCombo.AddAttribute (cr, "text", 0);
			compileTargetCombo.Active = (int) configuration.CompileTarget;

			if (compilerParameters.Compiler == JavaCompiler.Javac)
				compilerJavacButton.Active = true;
			else
				compilerGcjButton.Active = true;
				
			compilerJavacButton.Toggled += new EventHandler (OnCompilerToggled);
			compilerGcjButton.Toggled += new EventHandler (OnCompilerToggled);

			enableOptimizationCheckButton.Active = compilerParameters.Optimize;
			generateDebugInformationCheckButton.Active = configuration.DebugMode;
			deprecationCheckButton.Active = compilerParameters.Deprecation;
			generateWarningsCheckButton.Active = compilerParameters.GenWarnings;
			warningsAsErrorsCheckButton.Active = !configuration.RunWithWarnings;
			
			compilerEntry.Text = compilerParameters.CompilerPath;
			classPathEntry.Text = compilerParameters.ClassPath;				
			mainClassEntry.Text = compilerParameters.MainClass;				
			symbolsEntry.Text = compilerParameters.DefineSymbols;	
			OnCompilerToggled (null, null);
		}
		
		void OnCompilerToggled (object o, EventArgs args)
		{
			if (compilerJavacButton.Active)
				compilerEntry.Text = "javac";
			else
				compilerEntry.Text = "gcj";
		}
		
		public bool Store ()
		{
			if (compilerParameters == null)
				return true;

			if (compilerJavacButton.Active)
				compilerParameters.Compiler = JavaCompiler.Javac;
			else
				compilerParameters.Compiler = JavaCompiler.Gcj;

			configuration.CompileTarget = (CompileTarget) compileTargetCombo.Active;
			compilerParameters.GenWarnings = generateWarningsCheckButton.Active;			
			compilerParameters.Deprecation = deprecationCheckButton.Active;			
			configuration.DebugMode = generateDebugInformationCheckButton.Active;			
			compilerParameters.Optimize = enableOptimizationCheckButton.Active;
			configuration.RunWithWarnings = !warningsAsErrorsCheckButton.Active;
			
			compilerParameters.CompilerPath = compilerEntry.Text;
			compilerParameters.ClassPath = classPathEntry.Text;
			compilerParameters.MainClass = mainClassEntry.Text;
			compilerParameters.DefineSymbols = symbolsEntry.Text;
			return true;
		}
	}

}
