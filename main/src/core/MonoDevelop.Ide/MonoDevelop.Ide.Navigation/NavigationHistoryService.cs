// 
// NavigationHistoryService.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//   Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TextEditing;

namespace MonoDevelop.Ide.Navigation
{
	public static class NavigationHistoryService
	{
		static HistoryList history = new HistoryList ();
		
		//used to prevent re-logging the current point during a switch
		static bool switching;
		
		//whethor the current node is transient. Prevents excession automatic logging when switching rapidly between 
		//documents
		static bool currentIsTransient;
		
		//the amount of time until a "transient" current node bevomes "permanent"
		static uint TRANSIENT_TIMEOUT = 10000; //ms
		
		static Document currentDoc;
		
		static NavigationHistoryService ()
		{
			IdeApp.Workspace.LastWorkspaceItemClosed += delegate {
				history.Clear ();
				OnHistoryChanged ();
			};
			
			//keep nav points up to date
			TextEditorService.LineCountChanged += LineCountChanged;
			TextEditorService.LineCountChangesCommitted += CommitCountChanges;
			TextEditorService.LineCountChangesReset += ResetCountChanges;
			IdeApp.Workspace.FileRenamedInProject += FileRenamed;
			
			IdeApp.Workbench.ActiveDocumentChanged += ActiveDocChanged;
		}
		
		public static void LogActiveDocument ()
		{
			LogActiveDocument (false);
		}
		
		public static void LogActiveDocument (bool transient)
		{
			if (switching)
				return;
			
			NavigationPoint point = GetNavPointForActiveDoc ();
			if (point == null)
				return;
			
			NavigationHistoryItem item = new NavigationHistoryItem (point);
			
			//if the current node's transient but has been around for a while, consider making it permanent
			if (Current == null ||
			    (currentIsTransient && DateTime.Now.Subtract (Current.Created).TotalMilliseconds > TRANSIENT_TIMEOUT))
			{
				currentIsTransient = false;
			}
			
			//if the current point's transient, always replace it
			if (currentIsTransient)
			{
				//collapse down possible extra point in history
				NavigationHistoryItem backOne = history[-1];
				if (backOne != null && point.ShouldReplace (backOne.NavigationPoint)) {
					// The new node is the same as the last permanent, so we can discard it
					history.RemoveCurrent ();
					currentIsTransient = false;
					item.Dispose ();
				} else {
					currentIsTransient = transient;
					history.ReplaceCurrent (item);
				}
			}
			//if the new point wants to replace the old one, let it
			else if (Current != null && !transient && point.ShouldReplace (Current.NavigationPoint))
			{
				history.ReplaceCurrent (item);
				
				//but in this case, the point should not be transient -- unless the old point was,
				//but that's handled earlier
				currentIsTransient = false;
			}
			//final choice: append the the node
			//BUT only if the existing current node would not want to replace the new node
			else if (Current == null || !Current.NavigationPoint.ShouldReplace (point))
			{
				history.AddPoint (item);
				currentIsTransient = transient;
			}
			else
				point.Dispose ();
				
			OnHistoryChanged ();
		}
		
		static NavigationPoint GetNavPointForActiveDoc ()
		{
			return GetNavPointForDoc (IdeApp.Workbench.ActiveDocument);
		}
		
		static NavigationPoint GetNavPointForDoc (Document doc)
		{
			if (doc == null)
				return null;
			
			NavigationPoint point = null;
			
			INavigable navigable = doc.GetContent<INavigable> ();
			if (navigable != null) {
				point = navigable.BuildNavigationPoint ();
				if (point != null)
					return point;
			}
			
			IEditableTextBuffer editBuf = doc.GetContent<IEditableTextBuffer> ();
			if (editBuf != null) {
				point = new TextFileNavigationPoint (doc, editBuf);
				if (point != null)
					return point;
			}
			
			return new DocumentNavigationPoint (doc);
		}
		
		#region Navigation
		
		public static bool CanMoveForward {
			get { return history.CanMoveForward; }
		}
		
		public static bool CanMoveBack {
			get { return history.CanMoveBack; }
		}
		
		public static void MoveForward ()
		{
			LogActiveDocument ();
			history.MoveForward ();
			SwitchToCurrent ();
			OnHistoryChanged ();
		}
		
		public static void MoveBack ()
		{
			// Log current point before moving back, to make sure a MoveForward will return to the same position
			LogActiveDocument ();
			history.MoveBack ();
			SwitchToCurrent ();
			OnHistoryChanged ();
		}
		
		public static void MoveTo (NavigationHistoryItem item)
		{
			history.MoveTo (item);
			SwitchToCurrent ();
			OnHistoryChanged ();
		}
		
		static void SwitchToCurrent ()
		{
			currentIsTransient = false;
			switching = true;
			if (history.Current != null)
				history.Current.Show ();
			switching = false;
		}
		
		#endregion
		
		public static IList<NavigationHistoryItem> GetNavigationList (int desiredLength)
		{
			return history.GetList (desiredLength);
		}
		
		public static IList<NavigationHistoryItem> GetNavigationList (int desiredLength, out int currentIndex)
		{
			return history.GetList (desiredLength, out currentIndex);
		}
		
		public static NavigationHistoryItem Current { get { return history.Current; } }
		
		public static bool IsCurrent (NavigationHistoryItem point)
		{
			return history.IsCurrent (point);
		}
		
		public static void Clear ()
		{
			history.Clear ();
			LogActiveDocument ();
		}
		
		public static event EventHandler HistoryChanged;
		
		static void OnHistoryChanged ()
		{
			if (HistoryChanged != null)
				HistoryChanged (null, EventArgs.Empty);
		}
		
		#region Handling active doc change events
		
		static void ActiveDocChanged (object sender, EventArgs args)
		{
			LogActiveDocument (true);
			AttachToDoc (IdeApp.Workbench.ActiveDocument);
		}
		
		static void AttachToDoc (Document document)
		{
			DetachFromCurrentDoc ();
			if (document == null)
				return;
			
			currentDoc = document;
			
			currentDoc.Closed += HandleCurrentDocClosed;
			
			if (currentDoc.Editor != null) {
				currentDoc.Editor.Document.TextReplaced += BufferTextChanged;
				currentDoc.Editor.Caret.PositionChanged += BufferCaretPositionChanged;
			}
		}

		static void HandleCurrentDocClosed (object sender, EventArgs e)
		{
			DetachFromCurrentDoc ();
		}
		
		static void DetachFromCurrentDoc ()
		{
			if (currentDoc == null)
				return;
			
			currentDoc.Closed -= HandleCurrentDocClosed;
			if (currentDoc.Editor != null) {
				currentDoc.Editor.Document.TextReplaced -= BufferTextChanged;
				currentDoc.Editor.Caret.PositionChanged -= BufferCaretPositionChanged;
			}
			currentDoc = null;
		}
		
		static void BufferCaretPositionChanged (object sender, EventArgs args)
		{
			LogActiveDocument (true);
		}
		
		static void BufferTextChanged (object sender, EventArgs args)
		{
			LogActiveDocument ();
		}
		
		#endregion
		
		#region Text file line number and snippet updating
		
		static void LineCountChanged (object sender, LineCountEventArgs args)
		{
//			MonoDevelop.Projects.Text.ITextFile textFile = (MonoDevelop.Projects.Text.ITextFile) sender;
		}
		
		static void CommitCountChanges (object sender, TextFileEventArgs args)
		{
//			MonoDevelop.Projects.Text.ITextFile textFile = (MonoDevelop.Projects.Text.ITextFile) sender;
		}
		
		static void ResetCountChanges (object sender, TextFileEventArgs args)
		{
//			MonoDevelop.Projects.Text.ITextFile textFile = (MonoDevelop.Projects.Text.ITextFile) sender;
		}
		
		static void FileRenamed (object sender, ProjectFileRenamedEventArgs e)
		{
			bool changed = false;
			foreach (ProjectFileRenamedEventInfo args in e) {
				foreach (NavigationHistoryItem point in history) {
					DocumentNavigationPoint dp = point.NavigationPoint as DocumentNavigationPoint;
					changed &= (dp != null && dp.HandleRenameEvent (args.OldName, args.NewName));
				}
			}
			if (changed)
				OnHistoryChanged ();
		}
		
		#endregion
	}
}
