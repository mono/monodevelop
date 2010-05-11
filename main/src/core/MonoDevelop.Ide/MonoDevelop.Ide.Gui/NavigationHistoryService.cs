// 
// NavigationHistoryService.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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

namespace MonoDevelop.Ide.Gui
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
			MonoDevelop.Projects.Text.TextFileService.LineCountChanged += LineCountChanged;
			MonoDevelop.Projects.Text.TextFileService.CommitCountChanges += CommitCountChanges;
			MonoDevelop.Projects.Text.TextFileService.ResetCountChanges += ResetCountChanges;
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
			NavigationHistoryItem current = history.Current;
			history.Clear ();
			history.AddPoint (current);
		}
		
		internal static void Remove (NavigationHistoryItem point)
		{
			history.Remove (point);
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
			
			if (currentDoc.TextEditor != null) {
				currentDoc.TextEditor.TextChanged += BufferTextChanged;
				currentDoc.TextEditor.CursorPositionChanged += BufferCaretPositionChanged;
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
			if (currentDoc.TextEditor != null) {
				currentDoc.TextEditor.TextChanged -= BufferTextChanged;
				currentDoc.TextEditor.CursorPositionChanged -= BufferCaretPositionChanged;
			}
			currentDoc = null;
		}
		
		static void BufferCaretPositionChanged (object sender, EventArgs args)
		{
			LogActiveDocument (true);
		}
		
		static void BufferTextChanged (object sender, TextChangedEventArgs args)
		{
			LogActiveDocument ();
		}
		
		#endregion
		
		#region Text file line number and snippet updating
		
		static void LineCountChanged (object sender, MonoDevelop.Projects.Text.LineCountEventArgs args)
		{
//			MonoDevelop.Projects.Text.ITextFile textFile = (MonoDevelop.Projects.Text.ITextFile) sender;
		}
		
		static void CommitCountChanges (object sender, MonoDevelop.Projects.Text.TextFileEventArgs args)
		{
//			MonoDevelop.Projects.Text.ITextFile textFile = (MonoDevelop.Projects.Text.ITextFile) sender;
		}
		
		static void ResetCountChanges (object sender, MonoDevelop.Projects.Text.TextFileEventArgs args)
		{
//			MonoDevelop.Projects.Text.ITextFile textFile = (MonoDevelop.Projects.Text.ITextFile) sender;
		}
		
		static void FileRenamed (object sender, MonoDevelop.Projects.ProjectFileRenamedEventArgs args)
		{
			bool changed = false;
			foreach (NavigationHistoryItem point in history) {
				DocumentNavigationPoint dp = point.NavigationPoint as DocumentNavigationPoint;
				changed &= (dp != null && dp.HandleRenameEvent (args.OldName, args.NewName));
			}
			if (changed)
				OnHistoryChanged ();
		}
		
		#endregion
	}
	
	public class NavigationHistoryItem
	{
		DateTime created = DateTime.Now;
		NavigationPoint navPoint;
		
		internal NavigationHistoryItem (NavigationPoint navPoint)
		{
			this.navPoint = navPoint;
			navPoint.Destroyed += HandleNavPointDestroyed;
		}

		void HandleNavPointDestroyed (object sender, EventArgs e)
		{
			NavigationHistoryService.Remove (this);
			navPoint.Destroyed -= HandleNavPointDestroyed;
		}
		
		public void Show ()
		{
			if (!NavigationHistoryService.IsCurrent (this))
				NavigationHistoryService.MoveTo (this);
			NavigationPoint.Show ();
		}
		
		public string DisplayName {
			get { return navPoint.DisplayName; }
		}
		
		public DateTime Created {
			get { return created; }
		}
		
		internal DateTime Visited { get; private set; }
		
		internal void SetVisited ()
		{
			Visited = DateTime.Now;
		}
		
		internal NavigationPoint NavigationPoint {
			get { return navPoint; }
		}
	}
	
	public abstract class NavigationPoint
	{
		public abstract string DisplayName { get; }
		//public abstract string Tooltip { get; }
		
		public abstract void Show ();
		
		// used for fuzzy matching to decide whether to replace an existing nav point
		// e.g if user just moves around a little, we don't want to add too many points
		public virtual bool ShouldReplace (NavigationPoint oldPoint)
		{
			return this.Equals (oldPoint);
		}
		
		// To be called by subclass when the navigation point is not valid anymore
		protected virtual void OnDestroyed ()
		{
			if (Destroyed != null)
				Destroyed (this, EventArgs.Empty);
		}
		
		public event EventHandler Destroyed;
		
		public override string ToString ()
		{
			return string.Format ("[NavigationPoint {0}]", DisplayName);
		}
	}
	
	//the list may only contain reference types, because it uses reference equality to ensure
	//that otherwise equal items may appear in the list more than once
	class HistoryList : IEnumerable<NavigationHistoryItem>
	{
		const int DEFAULT_MAX_LENGTH = 30;
		const int FORWARD_HISTORY_TIMEOUT_SECONDS = 60;
		int maxLength;
		
		LinkedList<NavigationHistoryItem> forward = new LinkedList<NavigationHistoryItem> ();
		NavigationHistoryItem current;
		LinkedList<NavigationHistoryItem> back = new LinkedList<NavigationHistoryItem> ();
		
		public HistoryList () : this (DEFAULT_MAX_LENGTH) {}
		
		public HistoryList (int maxLength)
		{
			this.maxLength = maxLength;
		}
		
		public int MaxLength { get {return maxLength; } }
		
		public int Count {
			get {
				return back.Count + forward.Count + (current == null? 0 : 1);
			}
		}
		
		public NavigationHistoryItem Current { get { return current; } }
		
		public void ReplaceCurrent (NavigationHistoryItem newCurrent)
		{
			current = newCurrent;
			current.SetVisited ();
		}
		
		public IEnumerable<NavigationHistoryItem> ForwardPoints {
			get {
				LinkedListNode<NavigationHistoryItem> node = forward.First;
				while (node != null) {
					yield return node.Value;
					node = node.Next;
				}
			}
		}
		
		IEnumerator<NavigationHistoryItem> IEnumerable<NavigationHistoryItem>.GetEnumerator ()
		{
			if (current == null)
				yield break;
			
			LinkedListNode<NavigationHistoryItem> node = back.First;
			while (node != null) {
				yield return node.Value;
				node = node.Next;
			}
			yield return current;
			node = forward.First;
			while (node != null) {
				yield return node.Value;
				node = node.Next;
			}
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable<NavigationHistoryItem>)this).GetEnumerator ();
		}
		
		public IEnumerable<NavigationHistoryItem> BackPoints {
			get {
				LinkedListNode<NavigationHistoryItem> node = back.Last;
				while (node != null) {
					yield return node.Value;
					node = node.Previous;
				}
			}
		}
		
		public void AddPoint (NavigationHistoryItem point)
		{
			if (current != null)
				back.AddLast (current);
			
			if (back.Count > maxLength)
				back.RemoveFirst ();
			current = point;
			current.SetVisited ();
			
			if (forward.First != null && (DateTime.Now - forward.First.Value.Visited).TotalSeconds > FORWARD_HISTORY_TIMEOUT_SECONDS)
				forward.Clear ();
		}
		
		//used for editing out items that are no longer valid
		public void Remove (NavigationHistoryItem point)
		{
			if (object.ReferenceEquals (current, point)) {
				current = null;
				//remove the next node if the node we removed was between identical nodes
				if (back.Last != null && forward.First != null && object.Equals (back.Last.Value, forward.First.Value))
					forward.Remove (forward.First);
				return;
			}
			
			LinkedListNode<NavigationHistoryItem> node = back.Last;
			while (node != null) {
				if (object.ReferenceEquals (node.Value, point)) {
					LinkedListNode<NavigationHistoryItem> next = node.Previous;
					back.Remove (node);
					
					//remove the next node if the node we removed was between identical nodes
					if (next != null) {
						NavigationHistoryItem compareTo = next.Next != null? next.Next.Value : current;
						if (object.Equals (compareTo, next.Value))
							back.Remove (next);
					}
					return;
				}
				node = node.Previous;
			}
			
			node = forward.First;
			while (node != null) {
				if (object.ReferenceEquals (node.Value, point)) {
					LinkedListNode<NavigationHistoryItem> next = node.Next;
					forward.Remove (node);
					
					//remove the next node if the node we removed was between identical nodes
					if (next != null) {
						NavigationHistoryItem compareTo = next.Previous != null? next.Previous.Value : current;
						if (object.Equals (compareTo, next.Value))
							forward.Remove (next);
					}
					return;
				}
				node = node.Next;
			}
		}
		
		public IList<NavigationHistoryItem> GetList (int desiredLength)
		{
			int currentIndex;
			return GetList (desiredLength, out currentIndex);
		}
		
		public IList<NavigationHistoryItem> GetList (int desiredLength, out int currentIndex)
		{
			if (current == null) {
				currentIndex = -1;
				return new NavigationHistoryItem[0];
			}
			
			//balance the list around the central item
			int half = ((desiredLength - 1) / 2);
			int forwardNeeded = Math.Min (half, forward.Count);
			int backNeeded = Math.Min (half, back.Count);
			if (forwardNeeded + backNeeded < (desiredLength - 1))
				backNeeded = Math.Min (desiredLength - forwardNeeded, back.Count);
			if (forwardNeeded + backNeeded < (desiredLength - 1))
				forwardNeeded = Math.Min (desiredLength - backNeeded, forward.Count);
			
			//create the array
			int length = forwardNeeded + backNeeded + 1;
			NavigationHistoryItem[] list = new NavigationHistoryItem[length];
			
			//add the current point
			list[backNeeded] = current;
			currentIndex = backNeeded;
			
			//add the backwards points
			LinkedListNode<NavigationHistoryItem> pointer = back.Last;
			for (int i = backNeeded - 1; i >= 0; i--) {
				list[i] = pointer.Value;
				pointer = pointer.Previous;
			}
			
			//add the forwards points
			pointer = forward.First;
			for (int i = backNeeded + 1; i < length; i++) {
				list[i] = pointer.Value;
				pointer = pointer.Next;
			}
			
			return list;
		}
		
		public bool CanMoveForward {
			get { return forward.First != null; }
		}
		
		public bool CanMoveBack {
			get { return back.Last != null; }
		}
		
		public void MoveForward ()
		{
			if (!CanMoveForward)
				throw new InvalidOperationException ("Cannot move forward.");
			
			back.AddLast (current);
			current = forward.First.Value;
			current.SetVisited ();
			forward.RemoveFirst ();
		}
		
		public void MoveBack ()
		{
			if (!CanMoveBack)
				throw new InvalidOperationException ("Cannot move back.");
			
			forward.AddFirst (current);
			current = back.Last.Value;
			current.SetVisited ();
			back.RemoveLast ();
		}
		
		public void RemoveCurrent ()
		{
			if (CanMoveBack) {
				current = back.Last.Value;
				current.SetVisited ();
				back.RemoveLast ();
			}
		}
		
		public NavigationHistoryItem this [int index] {
			get {
				if (index == 0)
					return current;
				
				IEnumerator<NavigationHistoryItem> enumerator = (index < 0)?
					BackPoints.GetEnumerator () : ForwardPoints.GetEnumerator ();
				
				int i = Math.Abs (index); 
				while (i > 1 && enumerator.MoveNext ()) {
					i--;
				}
				
				return enumerator.MoveNext ()? enumerator.Current : null;
			}
		}
		
		public void MoveTo (NavigationHistoryItem point)
		{
			if (IsCurrent (point))
				return;
			
			LinkedListNode<NavigationHistoryItem> searchPointer;
			
			//look for the node in the "forward" list, walking forward
			searchPointer = forward.First;
			int moveCount = 0;
			while (searchPointer != null) {
				//when we find it
				if (object.ReferenceEquals (searchPointer.Value, point)) {
					
					if (current != null)
						back.AddLast (current);
					
					//move all the nodes up this point to the "back" list
					for (; moveCount > 0; moveCount--) {
						back.AddLast (forward.First.Value);
						forward.RemoveFirst ();
					}
					
					//set it as the current node
					current = forward.First.Value;
					current.SetVisited ();
					forward.RemoveFirst ();
					
					return;
				}
				searchPointer = searchPointer.Next;
				moveCount++;
			}
					
			
			//look for the node in the "back" list, walking backward
			searchPointer = back.Last;
			moveCount = 0;
			while (searchPointer != null) {
				//when we find it
				if (object.ReferenceEquals (searchPointer.Value, point)) {
					
					if (current != null)
						forward.AddFirst (current);
					
					//move all the nodes up this point to the "forward" list
					for (; moveCount > 0; moveCount--) {
						forward.AddFirst (back.Last.Value);
						back.RemoveLast ();
					}
					
					//set it as the current node
					current = back.Last.Value;
					current.SetVisited ();
					back.RemoveLast ();
					
					return;
				}
				searchPointer = searchPointer.Previous;
				moveCount++;
			}
			
			throw new InvalidOperationException ("The item is not in the history");
		}
		
		public bool IsCurrent (NavigationHistoryItem point)
		{
			return object.ReferenceEquals (point, current);
		}
		
		public void Clear ()
		{
			forward.Clear ();
			back.Clear ();
			current = null;
		}
		
		public void ClearForward ()
		{
			forward.Clear ();
		}
	}
}
