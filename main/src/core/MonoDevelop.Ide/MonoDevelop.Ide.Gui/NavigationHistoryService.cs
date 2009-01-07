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
		
		static HistoryList<NavigationPoint> history = new HistoryList<NavigationPoint> ();
		
		//used to prevent re-logging the current point during a switch
		static bool switching;
		
		//whethor the current node is transient. Prevents excession automatic logging when switching rapidly between 
		//documents
		static bool currentIsTransient;
		
		//the amount of time until a "transient" current node bevomes "permanent"
		static uint TRANSIENT_TIMEOUT = 15000; //ms
		
		static Document currentDoc;
		static bool bufferTimeoutRunning = false;
		
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
				NavigationPoint	backOne = history[-1];
				if (backOne != null && point.ShouldReplace (backOne)) {
					history.MoveBack ();
					history.ClearForward ();
					currentIsTransient = false;
				} else {
					currentIsTransient = transient;
				}
				
				history.ReplaceCurrent (point);
			}
			//if the new point wants to replace the old one, let it
			else if (point.ShouldReplace (Current))
			{
				history.ReplaceCurrent (point);
				
				//but in this case, the point should not be transient -- unless the old point was,
				//but that's handled earlier
				currentIsTransient = false;
			}
			//final choice: append the the node
			//BUT only if the existing current node would not want to replace the new node
			else if (Current == null || !Current.ShouldReplace (point))
			{
				history.AddPoint (point);
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
			history.MoveForward ();
			SwitchToCurrent ();
			OnHistoryChanged ();
		}
		
		public static void MoveBack ()
		{
			history.MoveBack ();
			SwitchToCurrent ();
			OnHistoryChanged ();
		}
		
		public static void MoveTo (NavigationPoint point)
		{
			history.MoveTo (point);
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
		
		public static IList<NavigationPoint> GetNavigationList (int desiredLength)
		{
			return history.GetList (desiredLength);
		}
		
		public static IList<NavigationPoint> GetNavigationList (int desiredLength, out int currentIndex)
		{
			return history.GetList (desiredLength, out currentIndex);
		}
		
		public static NavigationPoint Current { get { return history.Current; } }
		
		public static bool IsCurrent (NavigationPoint point)
		{
			return history.IsCurrent (point);
		}
		
		public static void Clear ()
		{
			NavigationPoint current = history.Current;
			history.Clear ();
			history.AddPoint (current);
		}
		
		internal static void Remove (NavigationPoint point)
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
			
			currentDoc.Closed += delegate { DetachFromCurrentDoc (); };
			
			IEditableTextBuffer buf	= currentDoc.GetContent<IEditableTextBuffer> ();
			if (buf != null) {
				buf.CaretPositionSet += BufferCaretPositionChanged;
				buf.TextChanged += BufferTextChanged;
			}
		}
		
		static void DetachFromCurrentDoc ()
		{
			if (currentDoc == null)
				return;
						
			IEditableTextBuffer buf	= currentDoc.GetContent<IEditableTextBuffer> ();
			if (buf != null) {
				buf.CaretPositionSet -= BufferCaretPositionChanged;
				buf.TextChanged -= BufferTextChanged;
			}
			currentDoc = null;
		}
		
		static void BufferCaretPositionChanged (object sender, EventArgs args)
		{
			DoBufferIdleHandler ();
		}
		
		static void BufferTextChanged (object sender, TextChangedEventArgs args)
		{
			DoBufferIdleHandler ();
		}
		
		//logs transient points when the text changes, then schdules a follow-up to make the point permanent if necessary
		static void DoBufferIdleHandler ()
		{
			if (bufferTimeoutRunning)
				return;
			
			LogActiveDocument (true);
			bufferTimeoutRunning = true;
			
			GLib.Timeout.Add (TRANSIENT_TIMEOUT + 100, delegate
			{
				LogActiveDocument (true);
				return false;
			});
		}
		
		#endregion
		
		#region Text file line number and snippet updating
		
		static void LineCountChanged (object sender, MonoDevelop.Projects.Text.LineCountEventArgs args)
		{
			MonoDevelop.Projects.Text.ITextFile textFile = (MonoDevelop.Projects.Text.ITextFile) sender;
		}
		
		static void CommitCountChanges (object sender, MonoDevelop.Projects.Text.TextFileEventArgs args)
		{
			MonoDevelop.Projects.Text.ITextFile textFile = (MonoDevelop.Projects.Text.ITextFile) sender;
		}
		
		static void ResetCountChanges (object sender, MonoDevelop.Projects.Text.TextFileEventArgs args)
		{
			MonoDevelop.Projects.Text.ITextFile textFile = (MonoDevelop.Projects.Text.ITextFile) sender;
		}
		
		static void FileRenamed (object sender, MonoDevelop.Projects.ProjectFileRenamedEventArgs args)
		{
			bool changed = false;
			foreach (NavigationPoint point in history) {
				DocumentNavigationPoint dp = point as DocumentNavigationPoint;
				changed &= (dp != null && dp.HandleRenameEvent (args.OldName, args.NewName));
			}
			if (changed)
				OnHistoryChanged ();
		}
		
		#endregion
	}
	
	public abstract class NavigationPoint
	{
		DateTime created = DateTime.Now;
		
		public abstract string DisplayName { get; }
		public abstract string Tooltip { get; }
		
		public void Show ()
		{
			if (!NavigationHistoryService.IsCurrent (this))
				NavigationHistoryService.MoveTo (this);
			DoShow ();
		}
		
		protected abstract Document DoShow ();
		
		// used for fuzzy matching to decide whether to replace an existing nav point
		// e.g if user just moves around a little, we don't want to add too many points
		public virtual bool ShouldReplace (NavigationPoint oldPoint)
		{
			return this.Equals (oldPoint);
		}
		
		public DateTime Created {
			get { return created; }
		}
		
		public override string ToString ()
		{
			return string.Format ("[NavigationPoint {0}]", DisplayName);
		}
		
		protected void RemoveSelfFromHistory ()
		{
			NavigationHistoryService.Remove (this);
		}
	}
	
	//the list may only contain reference types, because it uses reference equality to ensure
	//that otherwise equal items may appear in the list more than once
	class HistoryList<T> : IEnumerable<T> where T : class
	{
		const int DEFAULT_MAX_LENGTH = 30;
		int maxLength;
		
		LinkedList<T> forward = new LinkedList<T> ();
		T current;
		LinkedList<T> back = new LinkedList<T> ();
		
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
		
		public T Current { get { return current; } }
		
		public void ReplaceCurrent (T newCurrent)
		{
			current = newCurrent;
		}
		
		public IEnumerable<T> ForwardPoints {
			get {
				LinkedListNode<T> node = forward.First;
				while (node != null) {
					yield return node.Value;
					node = node.Next;
				}
			}
		}
		
		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			if (current == null)
				yield break;
			
			LinkedListNode<T> node = back.First;
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
			return ((IEnumerable<T>)this).GetEnumerator ();
		}
		
		public IEnumerable<T> BackPoints {
			get {
				LinkedListNode<T> node = back.Last;
				while (node != null) {
					yield return node.Value;
					node = node.Previous;
				}
			}
		}
		
		public void AddPoint (T point)
		{
			if (current != null)
				back.AddLast (current);
			
			if (back.Count > maxLength)
				back.RemoveFirst ();
			current = point;
			
			//as soon as another point is added, the forward history becomes invalid
			forward.Clear ();
		}
		
		//used for editing out items that are no longer valid
		public void Remove (T point)
		{
			if (object.ReferenceEquals (current, point)) {
				current = null;
				//remove the next node if the node we removed was between identical nodes
				if (back.Last != null && forward.First != null && object.Equals (back.Last.Value, forward.First.Value))
					forward.Remove (forward.First);
				return;
			}
			
			LinkedListNode<T> node = back.Last;
			while (node != null) {
				if (object.ReferenceEquals (node.Value, point)) {
					LinkedListNode<T> next = node.Previous;
					back.Remove (node);
					
					//remove the next node if the node we removed was between identical nodes
					if (next != null) {
						T compareTo = next.Next != null? next.Next.Value : current;
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
					LinkedListNode<T> next = node.Next;
					forward.Remove (node);
					
					//remove the next node if the node we removed was between identical nodes
					if (next != null) {
						T compareTo = next.Previous != null? next.Previous.Value : current;
						if (object.Equals (compareTo, next.Value))
							forward.Remove (next);
					}
					return;
				}
				node = node.Next;
			}
		}
		
		public IList<T> GetList (int desiredLength)
		{
			int currentIndex;
			return GetList (desiredLength, out currentIndex);
		}
		
		public IList<T> GetList (int desiredLength, out int currentIndex)
		{
			if (current == null) {
				currentIndex = -1;
				return new T[0];
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
			T[] list = new T[length];
			
			//add the current point
			list[backNeeded] = current;
			currentIndex = backNeeded;
			
			//add the backwards points
			LinkedListNode<T> pointer = back.Last;
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
			forward.RemoveFirst ();
		}
		
		public void MoveBack ()
		{
			if (!CanMoveBack)
				throw new InvalidOperationException ("Cannot move back.");
			
			forward.AddFirst (current);
			current = back.Last.Value;
			back.RemoveLast ();
		}
		
		public T this [int index] {
			get {
				if (index == 0)
					return current;
				
				IEnumerator<T> enumerator = (index < 0)?
					BackPoints.GetEnumerator () : ForwardPoints.GetEnumerator ();
				
				int i = Math.Abs (index); 
				while (i > 1 && enumerator.MoveNext ()) {
					i--;
				}
				
				return enumerator.MoveNext ()? enumerator.Current : null;
			}
		}
		
		public void MoveTo (T point)
		{
			if (IsCurrent (point))
				return;
			
			LinkedListNode<T> searchPointer;
			
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
					back.RemoveLast ();
					
					return;
				}
				searchPointer = searchPointer.Previous;
				moveCount++;
			}
			
			throw new InvalidOperationException ("The item is not in the history");
		}
		
		public bool IsCurrent (T point)
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
