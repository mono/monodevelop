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
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.VersionControl.Views
{
	public class VersionControlDocumentInfo
	{
		bool alreadyStarted = false;
		
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
		
		public Repository Repository {
			get;
			set;
		}

		public VersionControlDocumentInfo (Document document, VersionControlItem item, Repository repository)
		{
			this.Document = document;
			this.Item = item;
			this.Repository = repository;
		}

		public void Start ()
		{
			if (alreadyStarted)
				return;
			alreadyStarted = true;
			ThreadPool.QueueUserWorkItem (delegate {
				lock (updateLock) {
					try {
						History      = Item.Repository.GetHistory (Item.Path, null);
						VersionInfo  = Item.Repository.GetVersionInfo (Item.Path, false);
					} catch (Exception ex) {
						LoggingService.LogError ("Error retrieving history", ex);
					}
					
					DispatchService.GuiDispatch (delegate {
						OnUpdated (EventArgs.Empty);
					});
					isUpdated = true;
				}
			});
		}
		
		object updateLock = new object ();
		bool isUpdated = false;
		
		public void RunAfterUpdate (Action act) 
		{
			if (isUpdated) {
				act ();
				return;
			}
			while (!isUpdated)
				Thread.Sleep (10);
			act ();
		}
		
		protected virtual void OnUpdated (EventArgs e)
		{
			EventHandler handler = this.Updated;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler Updated;

	}
	
	class DiffView : BaseView, IAttachableViewContent, IUndoHandler
	{
		DiffWidget widget;

		public override Gtk.Widget Control { 
			get {
				if (widget == null) {
					widget = new DiffWidget (info);
					
					ComparisonWidget.DiffEditor.Document.Text = info.Item.Repository.GetBaseText (info.Item.Path);
					ComparisonWidget.SetLocal (ComparisonWidget.OriginalEditor.GetTextEditorData ());
					widget.ShowAll ();
				}
				return widget;
			}
		}
		
		public ComparisonWidget ComparisonWidget {
			get {
				return this.widget.ComparisonWidget;
			}
		}
		
		public List<Mono.TextEditor.Utils.Hunk> Diff {
			get {
				return ComparisonWidget.Diff;
			}
		}
		
		public static void AttachViewContents (Document document, VersionControlItem item)
		{
			IWorkbenchWindow window = document.Window;
			if (window.SubViewContents.Any (sub => sub is DiffView))
				return;
			
			VersionControlDocumentInfo info = new VersionControlDocumentInfo (document, item, item.Repository);
			
			DiffView comparisonView = new DiffView (info);
			window.AttachViewContent (comparisonView);
//			window.AttachViewContent (new PatchView (comparisonView, info));
			window.AttachViewContent (new BlameView (info));
			window.AttachViewContent (new LogView (info));
			
			if (info.VersionInfo != null && info.VersionInfo.Status == VersionStatus.Conflicted)
				window.AttachViewContent (new MergeView (info));
		}

		public static void Show (VersionControlItemList items)
		{
			foreach (VersionControlItem item in items) {
				var document = IdeApp.Workbench.OpenDocument (item.Path);
				DiffView.AttachViewContents (document, item);
				int viewNum = FindDiffView (document.Window.SubViewContents) + 1;
				document.Window.SwitchView (viewNum);
			}
		}
		
		private static int FindDiffView (IEnumerable<IAttachableViewContent> subContents)
		{
			int idx = -1;
			int i = 0;
			
			foreach (IAttachableViewContent item in subContents) {
				if (item is DiffView)
				{
					idx = i;
					break;
				}
				
				i++;
			}
			
			return idx;
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
		public DiffView (VersionControlDocumentInfo info) : base ("Diff")
		{
			this.info = info;
		}
		
		public DiffView (VersionControlDocumentInfo info, Revision baseRev, Revision toRev) : base ("Diff")
		{
			this.info = info;
			widget = new DiffWidget (info);
			
			ComparisonWidget.OriginalEditor.Document.MimeType = ComparisonWidget.DiffEditor.Document.MimeType = info.Document.Editor.Document.MimeType;
			ComparisonWidget.OriginalEditor.Options.FontName = ComparisonWidget.DiffEditor.Options.FontName = info.Document.Editor.Options.FontName;
			ComparisonWidget.OriginalEditor.Options.ColorScheme = ComparisonWidget.DiffEditor.Options.ColorScheme = info.Document.Editor.Options.ColorScheme;
			ComparisonWidget.OriginalEditor.Options.ShowFoldMargin = ComparisonWidget.DiffEditor.Options.ShowFoldMargin = false;
			ComparisonWidget.OriginalEditor.Options.ShowIconMargin = ComparisonWidget.DiffEditor.Options.ShowIconMargin = false;
			
			ComparisonWidget.SetRevision (ComparisonWidget.DiffEditor, baseRev);
			ComparisonWidget.SetRevision (ComparisonWidget.OriginalEditor, toRev);
			
			widget.ShowAll ();
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
		}

		#region IAttachableViewContent implementation
		public void Selected ()
		{
			info.Start ();
			ComparisonWidget.UpdateLocalText ();
			ComparisonWidget.OriginalEditor.Document.IgnoreFoldings = true;
			ComparisonWidget.OriginalEditor.Caret.Location = info.Document.Editor.Caret.Location;
			ComparisonWidget.OriginalEditor.VAdjustment.Value = info.Document.Editor.VAdjustment.Value;
			ComparisonWidget.OriginalEditor.GrabFocus ();
		}
		
		public void Deselected ()
		{
			info.Document.Editor.Caret.Location = ComparisonWidget.OriginalEditor.Caret.Location;
			info.Document.Editor.VAdjustment.Value = ComparisonWidget.OriginalEditor.VAdjustment.Value;
			ComparisonWidget.OriginalEditor.Document.IgnoreFoldings = false;
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
			this.ComparisonWidget.OriginalEditor.Document.Undo ();
		}

		void IUndoHandler.Redo ()
		{
			this.ComparisonWidget.OriginalEditor.Document.Redo ();
		}

		void IUndoHandler.BeginAtomicUndo ()
		{
			this.ComparisonWidget.OriginalEditor.Document.BeginAtomicUndo ();
		}

		void IUndoHandler.EndAtomicUndo ()
		{
			this.ComparisonWidget.OriginalEditor.Document.EndAtomicUndo ();
		}

		bool IUndoHandler.EnableUndo {
			get {
				return this.ComparisonWidget.OriginalEditor.Document.CanUndo;
			}
		}

		bool IUndoHandler.EnableRedo {
			get {
				return this.ComparisonWidget.OriginalEditor.Document.CanRedo;
			}
		}
		#endregion
	}
}