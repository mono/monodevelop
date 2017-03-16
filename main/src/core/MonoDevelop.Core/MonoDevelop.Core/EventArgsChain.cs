// 
// EventArgsChain.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using System.Collections.Generic;
using System.Collections;

namespace MonoDevelop.Core
{
	public interface IEventArgsChain
	{
		void MergeWith (IEventArgsChain args);
	}
	
	public class EventArgsChain<T>: EventArgs, ICollection<T>, IEnumerable<T>, IEventArgsChain
	{
		List<T> events = new List<T> ();
		
		public EventArgsChain ()
		{
		}
		
		public EventArgsChain (IEnumerable<T> args)
		{
			events.AddRange (args);
		}
		
		void IEventArgsChain.MergeWith (IEventArgsChain chain)
		{
			events.AddRange (((EventArgsChain<T>)chain).events);
		}
		
		public void MergeWith (EventArgsChain<T> chain)
		{
			events.AddRange (chain.events);
		}

		public void AddRange (IEnumerable<T> args)
		{
			events.AddRange (args);
		}

		#region ICollection[T] implementation
		public void Add (T eventArgs)
		{
			events.Add (eventArgs);
		}

		public void Clear ()
		{
			events.Clear ();
		}

		public bool Contains (T item)
		{
			return events.Contains (item);
		}

		public void CopyTo (T[] array, int arrayIndex)
		{
			events.CopyTo (array, arrayIndex);
		}

		public bool Remove (T item)
		{
			return events.Remove (item);
		}

		public int Count {
			get {
				return events.Count;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}
		#endregion

		public List<T>.Enumerator GetEnumerator ()
		{
			return events.GetEnumerator ();
		}

		#region IEnumerable implementation
		System.Collections.IEnumerator IEnumerable.GetEnumerator ()
		{
			return events.GetEnumerator ();
		}
		#endregion

		#region IEnumerable[T] implementation
		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			return events.GetEnumerator ();
		}
		#endregion
	}
}

