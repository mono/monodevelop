// 
// VersionControlView.cs
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
using System.IO;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Diff;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.VersionControl.Views
{
	
	public class VersionControlDocumentInfo
	{
		public Document Document {
			get;
			set;
		}

		public VersionControlItem Item {
			get;
			set;
		}

		public Revision[] History {
			get;
			set;
		}
		
		public VersionInfo VersionInfo {
			get;
			set;
		}

		public VersionControlDocumentInfo (Document document, VersionControlItem item)
		{
			this.Document = document;
			this.Item = item;
		}

		public void Start ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					History      = Item.Repository.GetHistory (Item.Path, null);
					VersionInfo  = Item.Repository.GetVersionInfo (Item.Path, false);
				} catch (Exception ex) {
					LoggingService.LogError ("Error retrieving history", ex);
				}
				
				DispatchService.GuiDispatch (delegate {
					OnUpdated (EventArgs.Empty);
				});
			});
		}

		protected virtual void OnUpdated (EventArgs e)
		{
			EventHandler handler = this.Updated;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler Updated;

	}
	
	internal class ComparisonView : BaseView, IAttachableViewContent, IUndoHandler
	{
		ComparisonWidget widget;

		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}

		public Diff Diff {
			get {
				return widget.Diff;
			}
		}
		
		public static void AttachViewContents (Document document, VersionControlItem item)
		{
			IWorkbenchWindow window = document.Window;
			if (window.SubViewContents.Any (sub => sub is ComparisonView))
				return;
			
			VersionControlDocumentInfo info = new VersionControlDocumentInfo (document, item);
			
			ComparisonView comparisonView = new ComparisonView (info);
			window.AttachViewContent (comparisonView);
			window.AttachViewContent (new PatchView (comparisonView, info));
			window.AttachViewContent (new BlameView (info));
			window.AttachViewContent (new LogView (info));
			
			info.Start ();
		}

		public static void Show (VersionControlItemList items)
		{
			foreach (VersionControlItem item in items) {
				var document = IdeApp.Workbench.OpenDocument (item.Path);
				ComparisonView.AttachViewContents (document, item);
				document.Window.SwitchView (1);
			}
		}
		
		public static bool CanShow (VersionControlItemList items)
		{
			foreach (VersionControlItem item in items) {
				if (item.Repository.IsModified (item.Path))
					return true;
			}
			return false;
		}
		
		VersionControlDocumentInfo info;
		public ComparisonView (VersionControlDocumentInfo info) : base ("Comparison")
		{
			this.info = info;
			widget = new ComparisonWidget (info);
			
			widget.OriginalEditor.Document.MimeType = widget.DiffEditor.Document.MimeType = info.Document.Editor.Document.MimeType;
			widget.OriginalEditor.Options.FontName = widget.DiffEditor.Options.FontName = info.Document.Editor.Options.FontName;
			widget.OriginalEditor.Options.ColorScheme = widget.DiffEditor.Options.ColorScheme = info.Document.Editor.Options.ColorScheme;
			widget.OriginalEditor.Options.ShowFoldMargin = widget.DiffEditor.Options.ShowFoldMargin = false;
			widget.OriginalEditor.Options.ShowIconMargin = widget.DiffEditor.Options.ShowIconMargin = false;
			widget.DiffEditor.Document.Text = info.Item.Repository.GetBaseText (info.Item.Path);
			widget.SetLocal (widget.OriginalEditor.GetTextEditorData ());
			widget.ShowAll ();
		}
		
		
		public override void Dispose ()
		{
			base.Dispose ();
		}

		#region IAttachableViewContent implementation
		public void Selected ()
		{
			widget.OriginalEditor.Document.IgnoreFoldings = true;
			widget.OriginalEditor.Caret.Location = info.Document.Editor.Caret.Location;
			widget.OriginalEditor.CenterToCaret ();
			widget.OriginalEditor.GrabFocus ();
		}
		
		public void Deselected ()
		{
			info.Document.Editor.Caret.Location = widget.OriginalEditor.Caret.Location;
			info.Document.Editor.CenterToCaret ();
			widget.OriginalEditor.Document.IgnoreFoldings = false;
		}

		public void BeforeSave ()
		{
		}

		public void BaseContentChanged ()
		{
		}
		
		#endregion
		
		#region IUndoHandler implementation
		void IUndoHandler.Undo ()
		{
			this.widget.OriginalEditor.Document.Undo ();
		}

		void IUndoHandler.Redo ()
		{
			this.widget.OriginalEditor.Document.Redo ();
		}

		void IUndoHandler.BeginAtomicUndo ()
		{
			this.widget.OriginalEditor.Document.BeginAtomicUndo ();
		}

		void IUndoHandler.EndAtomicUndo ()
		{
			this.widget.OriginalEditor.Document.EndAtomicUndo ();
		}

		bool IUndoHandler.EnableUndo {
			get {
				return this.widget.OriginalEditor.Document.CanUndo;
			}
		}

		bool IUndoHandler.EnableRedo {
			get {
				return this.widget.OriginalEditor.Document.CanRedo;
			}
		}
		#endregion

	}
}

