// 
// InspectionOptions.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using MonoDevelop.Refactoring;
using MonoDevelop.AnalysisCore.Fixes;
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.QuickTasks;
using ICSharpCode.NRefactory.CSharp;
using Gtk;
using MonoDevelop.CodeActions;
using ICSharpCode.NRefactory.Refactoring;

namespace MonoDevelop.CodeIssues
{
	partial class CodeIssueOptionsDialog : Gtk.Dialog
	{
		readonly BaseCodeIssueProvider inspector;
		
		public CodeIssueOptionsDialog (BaseCodeIssueProvider inspector)
		{
			this.inspector = inspector;
			Build ();
			HasSeparator = false;

			isEnabledCheckbutton.Active = inspector.GetIsEnabled ();
			labelInspectionTitle.Text = inspector.Title;
			var s = inspector.GetSeverity ();
			radiobuttonHide.Active = s == Severity.None;
			radiobuttonError.Active = s == Severity.Error;
			radiobuttonWarning.Active = s == Severity.Warning;
			radiobuttonHint.Active = s == Severity.Hint;
			radiobuttonSuggestion.Active = s == Severity.Suggestion;
			buttonOk.Clicked += HandleClicked;
			buttonCancel.Clicked += (sender, e) => Destroy ();
			Response += (o, args) => {
				if (args.ResponseId == ResponseType.Close)
					Destroy ();
			};
		}

		void HandleClicked (object sender, EventArgs e)
		{
			if (radiobuttonHide.Active) {
				inspector.SetSeverity (Severity.None);
			} else if (radiobuttonError.Active) {
				inspector.SetSeverity (Severity.Error);
			} else if (radiobuttonWarning.Active) {
				inspector.SetSeverity (Severity.Warning);
			} else if (radiobuttonHint.Active) {
				inspector.SetSeverity (Severity.Hint);
			} else if (radiobuttonSuggestion.Active) {
				inspector.SetSeverity (Severity.Suggestion);
			}
			inspector.SetIsEnabled (isEnabledCheckbutton.Active);
			MonoDevelop.SourceEditor.OptionPanels.ColorShemeEditor.RefreshAllColors ();
			Destroy ();
		}
	}
}

