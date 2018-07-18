// 
// AnalysisOptionsPanel.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Ide.Gui.Dialogs;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.AnalysisCore.Gui
{
	public class AnalysisOptionsPanel : OptionsPanel
	{
		AnalysisOptionsWidget widget;
		
		public override Control CreatePanelWidget ()
		{
			return widget = new AnalysisOptionsWidget (LanguageNames.CSharp) {
				AnalysisEnabled = AnalysisOptions.AnalysisEnabled,
				UnitTestIntegrationEnabled = AnalysisOptions.EnableUnitTestEditorIntegration,
				FullAnalysisEnabled = IdeApp.Preferences.Roslyn.For (LanguageNames.CSharp).SolutionCrawlerClosedFileDiagnostic && IdeApp.Preferences.Roslyn.FullSolutionAnalysisRuntimeEnabled,
			};
		}
		
		public override void ApplyChanges ()
		{
			AnalysisOptions.AnalysisEnabled.Set (widget.AnalysisEnabled);
			AnalysisOptions.EnableUnitTestEditorIntegration.Set (widget.UnitTestIntegrationEnabled);
			IdeApp.Preferences.Roslyn.For (LanguageNames.CSharp).SolutionCrawlerClosedFileDiagnostic.Value = widget.FullAnalysisEnabled;
		}
	}
	
	class AnalysisOptionsWidget : VBox
	{
		CheckButton enabledCheck;
		CheckButton enabledFullCheck;
		CheckButton enabledTest;

		public AnalysisOptionsWidget (string languageName)
		{
			enabledCheck = new CheckButton (GettextCatalog.GetString ("Enable source analysis of open files"));
			PackStart (enabledCheck, false, false, 0);
			enabledFullCheck = new CheckButton (GettextCatalog.GetString ("Enable source analysis of whole solution"));
			PackStart (enabledFullCheck, false, false, 0);
			enabledTest = new CheckButton (GettextCatalog.GetString ("Enable text editor unit test integration"));
			PackStart (enabledTest, false, false, 0);

			ShowAll ();
		}

		public bool AnalysisEnabled {
			get { return enabledCheck.Active; }
			set {
				enabledCheck.Active = value;

				enabledFullCheck.Sensitive = value;
				if (!value) {
					enabledFullCheck.Active = false;
				}
			}
		}

		public bool FullAnalysisEnabled {
			get { return enabledFullCheck.Active; }
			set { enabledFullCheck.Active = value; }
		}

		public bool UnitTestIntegrationEnabled {
			get { return enabledTest.Active; }
			set { enabledTest.Active = value; }
		}
	}
}

