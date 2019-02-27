//
// MergeView.cs
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
using MonoDevelop.Components;
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.VersionControl.Views
{
	public interface IMergeView
	{
	}
	
	class MergeView : BaseView, IMergeView
	{
		readonly VersionControlDocumentInfo info;
		readonly FileEventInfo fileEventInfo;
		MergeWidget widget;
		readonly MergeWidgetContainer widgetContainer;
		readonly Gtk.Label NoMergeConflictsLabel;

		public override Control Control => widgetContainer;

		public MergeView (VersionControlDocumentInfo info) : base (GettextCatalog.GetString ("Merge"), GettextCatalog.GetString ("Shows the merge view for the current file"))
		{
			this.info = info;
			fileEventInfo = new FileEventInfo (info.Item.Path.FullPath, info.Item.IsDirectory);
			widgetContainer = new MergeWidgetContainer ();
			NoMergeConflictsLabel = new Gtk.Label () { Text = GettextCatalog.GetString ("No merge conflicts detected.") };
			FileService.FileChanged += FileService_FileChanged;
		}

		void RefreshContent ()
		{
			var isConflicted = info?.Item?.VersionInfo?.Status.HasFlag (VersionStatus.Conflicted) ?? false;
			if (isConflicted) {
				if (widget == null) {
					widget = new MergeWidget ();
					widget.Load (info);
				}
				if (widgetContainer.Content != widget) {
					widgetContainer.Content = widget;
				}
			} else {
				if (widgetContainer.Content != NoMergeConflictsLabel) {
					widgetContainer.Content = NoMergeConflictsLabel;
				}
			}
		}

		void FileService_FileChanged (object sender, FileEventArgs e)
		{
			//content is null when is deselected
			if (widgetContainer.Content == null) {
				return;
			}

			//continue only if file server detected some change in this file
			if (e.All (s => s.FileName.CompareTo (fileEventInfo.FileName) < 0)) {
				return;
			}

			//if it is shown we refresh we show the content and refresh the editor (probably this nee
			info.Start ();
			RefreshContent ();
			RefreshMergeEditor ();
		}

		protected override void OnSelected ()
		{
			info.Start ();
			RefreshContent ();
			RefreshMergeEditor ();
		}

		void RefreshMergeEditor ()
		{
			if (widgetContainer.Content is MergeWidget) {
				widget.UpdateLocalText ();
				var buffer = info.Document.GetContent<MonoDevelop.Ide.Editor.TextEditor> ();
				if (buffer != null) {
					var loc = buffer.CaretLocation;
					int line = loc.Line < 1 ? 1 : loc.Line;
					int column = loc.Column < 1 ? 1 : loc.Column;
					widget.MainEditor.SetCaretTo (line, column);
				}
			}
		}

		void ClearContainer () => widgetContainer.Clear ();

		protected override void OnDeselected () => ClearContainer ();

		public override void Dispose ()
		{
			ClearContainer ();
			FileService.FileChanged -= FileService_FileChanged;
			base.Dispose ();
		}

		class MergeWidgetContainer : Gtk.VBox
		{
			Gtk.Widget content;
			public Gtk.Widget Content {
				get => content;
				set {
					if (content == value) {
						return;
					}
					Clear ();
					content = value;
					PackStart (value, true, true, 0);
					ShowAll ();
				}
			}

			public void Clear ()
			{
				if (content != null) {
					Remove (content);
					content = null;
				}
			}
		}
	}
}

