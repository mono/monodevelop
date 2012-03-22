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
using MonoDevelop.ContextAction;
using MonoDevelop.Refactoring;
using MonoDevelop.AnalysisCore.Fixes;
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.QuickTasks;
using Gtk;

namespace MonoDevelop.Inspection
{
	public partial class InspectionOptions : Gtk.Dialog
	{
		readonly AnalysisContextAction act;
		
		public InspectionOptions (AnalysisContextAction act)
		{
			this.act = act;
			Build ();
			HasSeparator = false;

			var r = act.Result as InspectorResults;
			labelInspectionTitle.Text = r.Inspector.Inspector.Title;
			var s = r.Inspector.GetSeverity ();
			radiobuttonHide.Active = s == QuickTaskSeverity.None;
			radiobuttonError.Active = s == QuickTaskSeverity.Error;
			radiobuttonWarning.Active = s == QuickTaskSeverity.Warning;
			radiobuttonHint.Active = s == QuickTaskSeverity.Hint;
			radiobuttonSuggestion.Active = s == QuickTaskSeverity.Suggestion;
			buttonOk.Clicked += HandleClicked;
			buttonCancel.Clicked += (sender, e) => Destroy ();
			Response += (o, args) => {
				if (args.ResponseId == ResponseType.Close)
					Destroy ();
			};
		}

		void HandleClicked (object sender, EventArgs e)
		{
			var r = act.Result as InspectorResults;
			if (radiobuttonHide.Active) {
				r.Inspector.SetSeverity (QuickTaskSeverity.None);
			} else if (radiobuttonError.Active) {
				r.Inspector.SetSeverity (QuickTaskSeverity.Error);
			} else if (radiobuttonWarning.Active) {
				r.Inspector.SetSeverity (QuickTaskSeverity.Warning);
			} else if (radiobuttonHint.Active) {
				r.Inspector.SetSeverity (QuickTaskSeverity.Hint);
			} else if (radiobuttonSuggestion.Active) {
				r.Inspector.SetSeverity (QuickTaskSeverity.Suggestion);
			}
			MonoDevelop.SourceEditor.OptionPanels.ColorShemeEditor.RefreshAllColors ();
			Destroy ();
		}
	}
}

