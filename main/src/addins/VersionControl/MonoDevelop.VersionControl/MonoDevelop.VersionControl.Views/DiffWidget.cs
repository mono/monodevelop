// 
// DiffWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Views
{
	[System.ComponentModel.ToolboxItem(false)]
	public partial class DiffWidget : Gtk.Bin
	{
		VersionControlDocumentInfo info;
		Mono.TextEditor.TextEditor diffTextEditor;
		MonoDevelop.VersionControl.Views.ComparisonWidget comparisonWidget;

		internal ComparisonWidget ComparisonWidget {
			get {
				return comparisonWidget;
			}
		}
		
		string LabelText {
			get {
				if (comparisonWidget.Diff.Count == 0)
					return GettextCatalog.GetString ("Both files are equal");
				return string.Format (GettextCatalog.GetPluralString ("{0} difference", "{0} differences", comparisonWidget.Diff.Count), comparisonWidget.Diff.Count);
			}
		}
		
		public Mono.TextEditor.TextEditor FocusedEditor {
			get {
				return comparisonWidget.FocusedEditor;
			}
		}
		
		public DiffWidget (VersionControlDocumentInfo info) : this (info, false)
		{
		}
		
		public DiffWidget (VersionControlDocumentInfo info, bool viewOnly)
		{
			this.info = info;
			this.Build ();
			comparisonWidget = new MonoDevelop.VersionControl.Views.ComparisonWidget (viewOnly);
			
			fixed1.SetSizeRequest (16, 16);
			this.buttonNext.Clicked += (sender, args) => ComparisonWidget.GotoNext ();
			this.buttonPrev.Clicked += (sender, args) => ComparisonWidget.GotoPrev ();
			notebook1.Page = 0;
			vboxComparisonView.PackStart (comparisonWidget, true, true, 0);
			comparisonWidget.Show ();
			
			comparisonWidget.DiffChanged += delegate {
				labelOverview.Markup = "<big>" + LabelText + "</big>";
				SetButtonSensitivity ();
			};
			comparisonWidget.SetVersionControlInfo (info);
			this.buttonDiff.Clicked += HandleButtonDiffhandleClicked;
			diffTextEditor = new global::Mono.TextEditor.TextEditor ();
			diffTextEditor.Document.MimeType = "text/x-diff";
			if (info.Document != null) {
				diffTextEditor.Options.FontName = info.Document.Editor.Options.FontName;
				diffTextEditor.Options.ColorScheme = info.Document.Editor.Options.ColorScheme;
				diffTextEditor.Options.ShowTabs = info.Document.Editor.Options.ShowTabs;
				diffTextEditor.Options.ShowSpaces = info.Document.Editor.Options.ShowSpaces;
				diffTextEditor.Options.ShowInvalidLines = info.Document.Editor.Options.ShowInvalidLines;
				diffTextEditor.Options.ShowInvalidLines = info.Document.Editor.Options.ShowInvalidLines;
			} else {
				var options = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance;
				diffTextEditor.Options.FontName = options.FontName;
				diffTextEditor.Options.ColorScheme = options.ColorScheme;
				diffTextEditor.Options.ShowTabs = options.ShowTabs;
				diffTextEditor.Options.ShowSpaces = options.ShowSpaces;
				diffTextEditor.Options.ShowInvalidLines = options.ShowInvalidLines;
				diffTextEditor.Options.ShowInvalidLines = options.ShowInvalidLines;
			}
			
			diffTextEditor.Options.ShowFoldMargin = false;
			diffTextEditor.Options.ShowIconMargin = false;
			diffTextEditor.Document.ReadOnly = true;
			scrolledwindow1.Child = diffTextEditor;
			diffTextEditor.Show ();
			SetButtonSensitivity ();
		}
		
		void SetButtonSensitivity ()
		{
			this.buttonNext.Sensitive = this.buttonPrev.Sensitive = notebook1.Page == 0 &&  comparisonWidget.Diff != null && comparisonWidget.Diff.Count > 0;
		}
		
		void HandleButtonDiffhandleClicked (object sender, EventArgs e)
		{
			if (notebook1.Page == 0) {
				buttonDiff.Label = GettextCatalog.GetString ("_Compare");
				diffTextEditor.Document.Text = Mono.TextEditor.Utils.Diff.GetDiffString (comparisonWidget.Diff,
					comparisonWidget.DiffEditor.Document,
					comparisonWidget.OriginalEditor.Document,
					(info.Item.Path) + "\t\t"+ GetRevisionText (comparisonWidget.DiffEditor, comparisonWidget.diffRevision),
					(info.Item.Path) + "\t\t"+ GetRevisionText (comparisonWidget.OriginalEditor, comparisonWidget.originalRevision)
				);
				
				notebook1.Page = 1;
			} else {
				buttonDiff.Label = GettextCatalog.GetString ("_Patch");
				notebook1.Page = 0;
			}
			
			SetButtonSensitivity ();
		}
		
		string GetRevisionText (Mono.TextEditor.TextEditor editor, Revision rev)
		{
			if (!editor.Document.ReadOnly)
				return GettextCatalog.GetString ("(working copy)");
			if (rev == null)
				return GettextCatalog.GetString ("(base)");
			return string.Format (GettextCatalog.GetString ("(revision {0})"), rev.ToString ());
		}
			
	}
}

