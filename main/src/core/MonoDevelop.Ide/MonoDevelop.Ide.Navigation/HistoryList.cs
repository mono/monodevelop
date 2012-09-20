// 
// NavigationHistoryService.cs
//  
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//   Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Content;

//the list may only contain reference types, because it uses reference equality to ensure
//that otherwise equal items may appear in the list more than once
namespace MonoDevelop.Ide.Navigation
{
	class HistoryList : IEnumerable<NavigationHistoryItem>
	{
		const int DEFAULT_MAX_LENGTH = 200;
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
			DestroyItem (current);
			current = newCurrent;
			AttachItem (current);
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
			
			if (back.Count > maxLength) {
				// The dispose call will indirectly remove from the list
				DestroyItem (back.First.Value);
				back.RemoveFirst ();
			}
			current = point;
			AttachItem (current);
			current.SetVisited ();
			
			if (forward.First != null && (DateTime.Now - forward.First.Value.Visited).TotalSeconds > FORWARD_HISTORY_TIMEOUT_SECONDS) {
				ClearForward ();
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
				DestroyItem (current);
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
			foreach (NavigationHistoryItem it in forward)
				DestroyItem (it);
			foreach (NavigationHistoryItem it in back)
				DestroyItem (it);
			
			forward.Clear ();
			back.Clear ();
			
			if (current != null) {
				DestroyItem (current);
				current = null;
			}
		}
		
		public void ClearForward ()
		{
			foreach (NavigationHistoryItem it in forward)
				DestroyItem (it);
			forward.Clear ();
		}
		
		void AttachItem (NavigationHistoryItem item)
		{
			item.SetParentList (this);
		}
		
		void DetachItem (NavigationHistoryItem item)
		{
			item.SetParentList (null);
		}
		
		void DestroyItem (NavigationHistoryItem item)
		{
			DetachItem (item);
			item.Dispose ();
		}

		internal void NotifyDestroyed (NavigationHistoryItem item)
		{
			DetachItem (item);
			Remove (item);
		}
		
		void Remove (NavigationHistoryItem point)
		{
			if (object.ReferenceEquals (current, point)) {
				current = null;
				return;
			}
			
			LinkedListNode<NavigationHistoryItem> node = back.Last;
			while (node != null) {
				if (object.ReferenceEquals (node.Value, point)) {
					back.Remove (node);
					return;
				}
				node = node.Previous;
			}
			
			node = forward.First;
			while (node != null) {
				if (object.ReferenceEquals (node.Value, point)) {
					forward.Remove (node);
					return;
				}
				node = node.Next;
			}
		}
	}
}
