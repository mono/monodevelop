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
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.AnalysisCore.Gui
{
	public class AnalysisOptionsPanel : OptionsPanel
	{
		AnalysisOptionsWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return widget = new AnalysisOptionsWidget () {
				AnalysisEnabled = AnalysisOptions.AnalysisEnabled,
				UnitTestIntegrationEnabled = AnalysisOptions.EnableUnitTestEditorIntegration
			};
		}
		
		public override void ApplyChanges ()
		{
			AnalysisOptions.AnalysisEnabled.Set (widget.AnalysisEnabled);
			AnalysisOptions.EnableUnitTestEditorIntegration.Set (widget.UnitTestIntegrationEnabled);
		}
	}
	
	class AnalysisOptionsWidget : VBox
	{
		CheckButton enabledCheck;
		CheckButton enabledTest;

		public AnalysisOptionsWidget ()
		{
			enabledCheck = new CheckButton (GettextCatalog.GetString ("Enable source analysis of open files"));
			PackStart (enabledCheck, false, false, 0);
			enabledTest = new CheckButton (GettextCatalog.GetString ("Enable text editor unit test integration"));
			PackStart (enabledTest, false, false, 0);

			ShowAll ();
		}
		
		public bool AnalysisEnabled {
			get { return enabledCheck.Active; }
			set { enabledCheck.Active = value; }
		}

		public bool UnitTestIntegrationEnabled {
			get { return enabledTest.Active; }
			set { enabledTest.Active = value; }
		}
	}
}

