// BreakpointStore.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mono.Debugging.Client
{
	
	
	public sealed class BreakpointStore: ICollection<Breakpoint>
	{
		List<Breakpoint> breakpoints = new List<Breakpoint> ();
		
		public int Count {
			get {
				return breakpoints.Count;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public Breakpoint Add (string filename, int line)
		{
			return Add (filename, line, true);
		}
		
		public Breakpoint Add (string filename, int line, bool activate)
		{
			Breakpoint bp = new Breakpoint (filename, line);
			Add (bp);
			return bp;
		}

		public void Add (Breakpoint bp)
		{
			breakpoints.Add (bp);
			bp.Store = this;
			OnBreakpointAdded (bp);
		}
		
		public void Remove (string filename, int line)
		{
			filename = System.IO.Path.GetFullPath (filename);
			
			for (int n=0; n<breakpoints.Count; n++) {
				Breakpoint bp = breakpoints [n];
				if (bp.FileName == filename && bp.Line == line) {
					breakpoints.RemoveAt (n);
					OnBreakpointRemoved (bp);
					n--;
				}
			}
		}
		
		public void Toggle (string filename, int line)
		{
			ReadOnlyCollection<Breakpoint> col = GetBreakpointsAtFileLine (filename, line);
			if (col.Count > 0) {
				foreach (Breakpoint bp in col)
					Remove (bp);
			}
			else {
				Add (filename, line);
			}
		}
		
		public ReadOnlyCollection<Breakpoint> GetBreakpointsAtFile (string filename)
		{
			filename = System.IO.Path.GetFullPath (filename);
			
			List<Breakpoint> list = new List<Breakpoint> ();
			foreach (Breakpoint bp in breakpoints)
				if (bp.FileName == filename)
					list.Add (bp);
			return list.AsReadOnly ();
		}
		
		public ReadOnlyCollection<Breakpoint> GetBreakpointsAtFileLine (string filename, int line)
		{
			filename = System.IO.Path.GetFullPath (filename);
			
			List<Breakpoint> list = new List<Breakpoint> ();
			foreach (Breakpoint bp in breakpoints)
				if (bp.FileName == filename && bp.Line == line)
					list.Add (bp);
			return list.AsReadOnly ();
		}
		
		public bool Remove (Breakpoint bp)
		{
			if (breakpoints.Remove (bp)) {
				OnBreakpointRemoved (bp);
				return true;
			}
			return false;
		}

		public IEnumerator GetEnumerator ()
		{
			return breakpoints.GetEnumerator ();
		}

		IEnumerator<Breakpoint> IEnumerable<Breakpoint>.GetEnumerator ()
		{
			return breakpoints.GetEnumerator ();
		}

		public void Clear ()
		{
			List<Breakpoint> oldList = breakpoints;
			breakpoints = new List<Breakpoint> ();
			foreach (Breakpoint bp in oldList)
				OnBreakpointRemoved (bp);
		}

		public bool Contains (Breakpoint item)
		{
			return breakpoints.Contains (item);
		}

		public void CopyTo (Breakpoint[] array, int arrayIndex)
		{
			breakpoints.CopyTo (array, arrayIndex);
		}
		
		internal void EnableBreakpoint (Breakpoint bp, bool enabled)
		{
			OnChanged ();
			if (BreakpointEnableStatusChanged != null)
				BreakpointEnableStatusChanged (this, new BreakpointEventArgs (bp));
			NotifyStatusChanged (bp);
		}
		
		void OnBreakpointAdded (Breakpoint bp)
		{
			OnChanged ();
			if (BreakpointAdded != null)
				BreakpointAdded (this, new BreakpointEventArgs (bp));
		}
		
		void OnBreakpointRemoved (Breakpoint bp)
		{
			OnChanged ();
			if (BreakpointRemoved != null)
				BreakpointRemoved (this, new BreakpointEventArgs (bp));
		}
		
		void OnChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		internal void NotifyStatusChanged (Breakpoint bp)
		{
			try {
				if (BreakpointStatusChanged != null)
					BreakpointStatusChanged (this, new BreakpointEventArgs (bp));
			} catch {
				// Ignone
			}
		}
		
		public event EventHandler<BreakpointEventArgs> BreakpointAdded;
		public event EventHandler<BreakpointEventArgs> BreakpointRemoved;
		public event EventHandler<BreakpointEventArgs> BreakpointStatusChanged;
		internal event EventHandler<BreakpointEventArgs> BreakpointEnableStatusChanged;
		public event EventHandler Changed;
	}
}
