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
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Dialogs;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Project
{
	partial class CodeGenerationPanelWidget : Gtk.Bin 
	{
		const int DEBUG_FULL = 0;
		const int DEBUG_PDB_ONLY = 1;
		const int DEBUG_PDB_PORTABLE = 2;
		const int DEBUG_PDB_EMBEDDED = 3;
		const int DEBUG_NONE = 4;

		DotNetProjectConfiguration configuration;
		CSharpCompilerParameters compilerParameters = null;
		
		public CodeGenerationPanelWidget ()
		{
			Build ();
			xmlDocsEntry.DisplayAsRelativePath = true;

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			generateOverflowChecksCheckButton.SetCommonAccessibilityAttributes ("CompilerOptions.OverflowChecks", "", 
			                                                                    GettextCatalog.GetString ("Check this to enable overflow checking"));
			enableOptimizationCheckButton.SetCommonAccessibilityAttributes ("CompilerOptions.Optimizations", "",
			                                                                GettextCatalog.GetString ("Check this to enable optimizations"));
			generateXmlOutputCheckButton.SetCommonAccessibilityAttributes ("CompilerOptions.XmlDoc", "",
			                                                               GettextCatalog.GetString ("Check this to generate XML documentation"));
			xmlDocsEntry.EntryAccessible.Name = "CompilerOptions.XmlEntry";
			xmlDocsEntry.EntryAccessible.SetLabel (GettextCatalog.GetString ("XML Filename"));
			xmlDocsEntry.EntryAccessible.Description = GettextCatalog.GetString ("Enter the filename for the generated XML documentation");

			comboDebug.SetCommonAccessibilityAttributes ("CompilerOptions.DebugCombo", label2,
			                                             GettextCatalog.GetString ("Select the level of debugging information to be generated"));

			symbolsEntry.SetCommonAccessibilityAttributes ("CompilerOptions.SymbolsEntry", label87,
			                                               GettextCatalog.GetString ("Enter the symbols the compiler should define"));

			comboPlatforms.SetCommonAccessibilityAttributes ("CompilerOptions.Platforms", label1,
			                                                 GettextCatalog.GetString ("Select the platform to target"));

			warningLevelSpinButton.SetCommonAccessibilityAttributes ("CompilerOptions.WarningsLevel", label85,
			                                                         GettextCatalog.GetString ("Select the warning level to use"));

			ignoreWarningsEntry.SetCommonAccessibilityAttributes ("CompilerOptions.IgnoreWarnings", label86,
			                                                      GettextCatalog.GetString ("Enter the warning numbers separated by a comma that the compile should ignore"));

			warningsAsErrorsCheckButton.SetCommonAccessibilityAttributes ("CompilerOptions.WarningsAsErrors", "",
			                                                              GettextCatalog.GetString ("Check to treat warnings as errors"));
		}

		//doing just original.Split(whitespaces).Distinct().Join(";") would make modifications to .csproj with " "(space) seperator
		internal static string RemoveDuplicateDefinedSymbols (string original)
		{
			var whitespaces = new char [] { ';', ',', ' ', '\t' };
			var symbols = new List<string> ();
			string currentSymbol = "";
			bool symbolStarted = false;
			bool eatWhitespaces = false;
			foreach (var c in original) {
				var isWhitespace = whitespaces.Contains (c);
				if (eatWhitespaces && !isWhitespace) {
					symbols.Add (currentSymbol);
					currentSymbol = "";
					symbolStarted = true;
					eatWhitespaces = false;
				} else if (symbolStarted && isWhitespace) {
					eatWhitespaces = true;
				} else if (!symbolStarted && !isWhitespace) {
					symbolStarted = true;
				}
				currentSymbol += c;
			}
			if (currentSymbol.Length > 0)
				symbols.Add (currentSymbol);

			string result = "";
			var symbolDuplicates = new HashSet<string> ();
			//since .targets files are adding new symbols and their seperaters at begining
			//we want to remove symbols and seperators at begining of string and not at end
			//to lower chance of changing seperators inside .csproj
			for (int i = symbols.Count - 1; i >= 0; i--) {
				if (symbolDuplicates.Add (symbols [i].Trim (whitespaces)))
					result = symbols [i] + result;
			}
			return result;
		}

		public void Load (DotNetProjectConfiguration configuration)
		{
			this.configuration = configuration;
			compilerParameters = (CSharpCompilerParameters) configuration.CompilationParameters;
			
			symbolsEntry.Text                          = RemoveDuplicateDefinedSymbols (compilerParameters.DefineSymbols);
			generateXmlOutputCheckButton.Active        = !string.IsNullOrEmpty (compilerParameters.DocumentationFile);
			enableOptimizationCheckButton.Active       = compilerParameters.Optimize;
			generateOverflowChecksCheckButton.Active   = compilerParameters.GenerateOverflowChecks;
			warningsAsErrorsCheckButton.Active         = compilerParameters.TreatWarningsAsErrors;
			warningLevelSpinButton.Value               = compilerParameters.WarningLevel;
			ignoreWarningsEntry.Text                   = compilerParameters.NoWarnings;
			
			int i = CSharpProject.SupportedPlatforms.IndexOf (compilerParameters.PlatformTarget);
			comboPlatforms.Active = i != -1 ? i : 0;

			if (!configuration.DebugSymbols || string.Equals ("none", configuration.DebugType, StringComparison.OrdinalIgnoreCase)) {
				comboDebug.Active = DEBUG_NONE;
			} else if (string.Equals ("pdbonly", configuration.DebugType, StringComparison.OrdinalIgnoreCase)) {
				comboDebug.Active = DEBUG_PDB_ONLY;
			} else if (string.Equals ("portable", configuration.DebugType, StringComparison.OrdinalIgnoreCase)) {
				comboDebug.Active = DEBUG_PDB_PORTABLE;
			} else if (string.Equals ("embedded", configuration.DebugType, StringComparison.OrdinalIgnoreCase)) {
				comboDebug.Active = DEBUG_PDB_EMBEDDED;
			} else {
				comboDebug.Active = DEBUG_FULL;
			}

			xmlDocsEntry.DefaultPath = configuration.OutputDirectory;

			xmlDocsEntry.Path = string.IsNullOrEmpty (compilerParameters.DocumentationFile)
				? configuration.CompiledOutputName.ChangeExtension (".xml")
				: compilerParameters.DocumentationFile;
		}

		public void Store ()
		{
			if (compilerParameters == null)
				throw new ApplicationException ("Code generation panel wasn't loaded !");
			
			compilerParameters.DefineSymbols          = symbolsEntry.Text;
			compilerParameters.DocumentationFile      = generateXmlOutputCheckButton.Active? xmlDocsEntry.Path : null;
			compilerParameters.Optimize               = enableOptimizationCheckButton.Active;
			compilerParameters.GenerateOverflowChecks = generateOverflowChecksCheckButton.Active;
			compilerParameters.TreatWarningsAsErrors  = warningsAsErrorsCheckButton.Active;
			compilerParameters.WarningLevel           = warningLevelSpinButton.ValueAsInt;
			compilerParameters.NoWarnings             = ignoreWarningsEntry.Text;
			compilerParameters.PlatformTarget         = CSharpProject.SupportedPlatforms [comboPlatforms.Active];

			switch (comboDebug.Active) {
			case DEBUG_FULL:
				configuration.DebugSymbols = true;
				if (!string.Equals (configuration.DebugType, "full", StringComparison.OrdinalIgnoreCase)) {
					configuration.DebugType = "full";
				}
				break;
			case DEBUG_PDB_ONLY:
				configuration.DebugSymbols = true;
				if (!string.Equals (configuration.DebugType, "pdbonly", StringComparison.OrdinalIgnoreCase)) {
					configuration.DebugType = "pdbonly";
				}
				break;
			case DEBUG_PDB_PORTABLE:
				configuration.DebugSymbols = true;
				if (!string.Equals (configuration.DebugType, "portable", StringComparison.OrdinalIgnoreCase)) {
					configuration.DebugType = "portable";
				}
				break;
			case DEBUG_PDB_EMBEDDED:
				configuration.DebugSymbols = true;
				if (!string.Equals (configuration.DebugType, "embedded", StringComparison.OrdinalIgnoreCase)) {
					configuration.DebugType = "embedded";
				}
				break;
			case DEBUG_NONE:
				configuration.DebugSymbols = false;
				if (!string.Equals (configuration.DebugType, "none", StringComparison.OrdinalIgnoreCase)) {
					configuration.DebugType = "";
				}
				break;
			}
		}
	}
	
	class CodeGenerationPanel : MultiConfigItemOptionsPanel
	{
		CodeGenerationPanelWidget widget;
		
		public override Control CreatePanelWidget()
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
