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
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl.Views
{
	[System.ComponentModel.ToolboxItem(false)]
	partial class DiffWidget : Gtk.Bin
	{
		VersionControlDocumentInfo info;
		Mono.TextEditor.MonoTextEditor diffTextEditor;
		ComparisonWidget comparisonWidget;
		DocumentToolButton buttonNext;
		DocumentToolButton buttonPrev;
		Gtk.Button buttonDiff;
		Gtk.Label labelOverview;

		internal ComparisonWidget ComparisonWidget {
			get {
				return comparisonWidget;
			}
		}
		
		string LabelText {
			get {
				if (!comparisonWidget.Diff.Any ())
					return GettextCatalog.GetString ("Both files are equal");
				int added=0, removed=0;
				foreach (var h in comparisonWidget.Diff) {
					added += h.Inserted;
					removed += h.Removed;
				}
				string changes = string.Format (GettextCatalog.GetPluralString ("{0} change", "{0} changes", comparisonWidget.Diff.Count), comparisonWidget.Diff.Count);
				string additions = string.Format (GettextCatalog.GetPluralString ("{0} line added", "{0} lines added", added), added);
				string removals = string.Format (GettextCatalog.GetPluralString ("{0} line removed", "{0} lines removed", removed), removed);
				
				return changes + " (" + additions + ", " + removals + ")";
			}
		}
		
		public Mono.TextEditor.MonoTextEditor FocusedEditor {
			get {
				return comparisonWidget.FocusedEditor;
			}
		}
		
		public DiffWidget (VersionControlDocumentInfo info, bool viewOnly = false)
		{
			this.info = info;
			this.Build ();
			comparisonWidget = new ComparisonWidget (viewOnly);
			buttonNext = new DocumentToolButton (Gtk.Stock.GoUp, GettextCatalog.GetString ("Previous Change"));
			buttonPrev = new DocumentToolButton (Gtk.Stock.GoDown, GettextCatalog.GetString ("Next Change"));
			labelOverview = new Gtk.Label () { Xalign = 0 };
			buttonDiff = new Gtk.Button (GettextCatalog.GetString ("Unified Diff"));
			
			this.buttonNext.Clicked += (sender, args) => ComparisonWidget.GotoNext ();
			this.buttonPrev.Clicked += (sender, args) => ComparisonWidget.GotoPrev ();
			notebook1.Page = 0;
			vboxComparisonView.PackStart (comparisonWidget, true, true, 0);
			comparisonWidget.Show ();
			
			comparisonWidget.DiffChanged += delegate {
				labelOverview.Markup = LabelText;
				SetButtonSensitivity ();
			};
			comparisonWidget.SetVersionControlInfo (info);
			this.buttonDiff.Clicked += HandleButtonDiffhandleClicked;
			diffTextEditor = new global::Mono.TextEditor.MonoTextEditor (new Mono.TextEditor.TextDocument (), CommonTextEditorOptions.Instance);
			diffTextEditor.Document.MimeType = "text/x-diff";
			
			diffTextEditor.Options.ShowFoldMargin = false;
			diffTextEditor.Options.ShowIconMargin = false;
			diffTextEditor.Options.DrawIndentationMarkers = PropertyService.Get ("DrawIndentationMarkers", false);
			diffTextEditor.Document.IsReadOnly = true;
			scrolledwindow1.Child = diffTextEditor;
			diffTextEditor.Show ();
			SetButtonSensitivity ();
		}

		internal void SetToolbar (DocumentToolbar toolbar)
		{
			toolbar.Add (labelOverview, true);
			toolbar.Add (buttonDiff);
			toolbar.Add (buttonPrev);
			toolbar.Add (buttonNext);
			toolbar.ShowAll ();
		}
		
		void SetButtonSensitivity ()
		{
			this.buttonNext.GetNativeWidget<Gtk.Widget> ().Sensitive = this.buttonPrev.GetNativeWidget<Gtk.Widget> ().Sensitive =
				notebook1.Page == 0 &&  comparisonWidget.Diff != null && comparisonWidget.Diff.Count > 0;
		}
		
		void HandleButtonDiffhandleClicked (object sender, EventArgs e)
		{
			if (notebook1.Page == 0) {
				buttonDiff.Label = GettextCatalog.GetString ("_Compare");
				notebook1.Page = 1;
				UpdatePatchView ();
			} else {
				buttonDiff.Label = GettextCatalog.GetString ("Unified Diff");
				notebook1.Page = 0;
			}
			
			SetButtonSensitivity ();
		}
		
		public void UpdatePatchView ()
		{
			if (notebook1.Page == 1) {
				diffTextEditor.Document.Text = Mono.TextEditor.Utils.Diff.GetDiffString (comparisonWidget.Diff,
					comparisonWidget.DiffEditor.Document,
					comparisonWidget.OriginalEditor.Document,
					(info.Item.Path) + "\t\t"+ GetRevisionText (comparisonWidget.DiffEditor, comparisonWidget.diffRevision),
					(info.Item.Path) + "\t\t"+ GetRevisionText (comparisonWidget.OriginalEditor, comparisonWidget.originalRevision)
				);
			}
		}
		
		static string GetRevisionText (Mono.TextEditor.MonoTextEditor editor, Revision rev)
		{
			if (!editor.Document.IsReadOnly)
				return GettextCatalog.GetString ("(working copy)");
			if (rev == null)
				return GettextCatalog.GetString ("(base)");
			return string.Format (GettextCatalog.GetString ("(revision {0})"), rev.ToString ());
		}

		[Components.Commands.CommandUpdateHandler (Ide.Commands.ViewCommands.ShowNext)]
		public void ShowNextUpdateHandler (Components.Commands.CommandInfo info)
		{
			if (!Visible)
				info.Bypass = true;
			else {
				info.Text = GettextCatalog.GetString ("Show Next (Difference)");
			}
		}

		[Components.Commands.CommandHandler (Ide.Commands.ViewCommands.ShowNext)]
		public void ShowNextHandler ()
		{
			ComparisonWidget.GotoNext ();
		}

		[Components.Commands.CommandUpdateHandler (Ide.Commands.ViewCommands.ShowPrevious)]
		public void ShowPreviousUpdateHandler (Components.Commands.CommandInfo info)
		{
			if (!Visible)
				info.Bypass = true;
			else {
				info.Text = GettextCatalog.GetString ("Show Previous (Difference)");
			}
		}

		[Components.Commands.CommandHandler (Ide.Commands.ViewCommands.ShowPrevious)]
		public void ShowPreviousHandler ()
		{
			ComparisonWidget.GotoPrev ();
		}
	}
}

