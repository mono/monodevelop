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
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mono.Debugging.Client
{
	public sealed class BreakpointStore: ICollection<BreakEvent>
	{
		List<BreakEvent> breakpoints = new List<BreakEvent> ();
		
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

		public void Add (BreakEvent bp)
		{
			breakpoints.Add (bp);
			bp.Store = this;
			OnBreakEventAdded (bp);
		}
		
		public Catchpoint AddCatchpoint (string exceptioName)
		{
			Catchpoint cp = new Catchpoint (exceptioName);
			Add (cp);
			return cp;
		}
		
		public void Remove (string filename, int line)
		{
			filename = System.IO.Path.GetFullPath (filename);
			
			for (int n=0; n<breakpoints.Count; n++) {
				Breakpoint bp = breakpoints [n] as Breakpoint;
				if (bp != null && bp.FileName == filename && bp.Line == line) {
					breakpoints.RemoveAt (n);
					OnBreakEventRemoved (bp);
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
		
		public ReadOnlyCollection<Breakpoint> GetBreakpoints ()
		{
			List<Breakpoint> list = new List<Breakpoint> ();
			foreach (BreakEvent be in breakpoints) {
				if (be is Breakpoint)
					list.Add ((Breakpoint)be);
			}
			return list.AsReadOnly ();
		}
		
		public ReadOnlyCollection<Catchpoint> GetCatchpoints ()
		{
			List<Catchpoint> list = new List<Catchpoint> ();
			foreach (BreakEvent be in breakpoints) {
				if (be is Catchpoint)
					list.Add ((Catchpoint) be);
			}
			return list.AsReadOnly ();
		}
		
		public ReadOnlyCollection<Breakpoint> GetBreakpointsAtFile (string filename)
		{
			filename = System.IO.Path.GetFullPath (filename);
			
			List<Breakpoint> list = new List<Breakpoint> ();
			foreach (BreakEvent be in breakpoints) {
				Breakpoint bp = be as Breakpoint;
				if (bp != null && bp.FileName == filename)
					list.Add (bp);
			}
			return list.AsReadOnly ();
		}
		
		public ReadOnlyCollection<Breakpoint> GetBreakpointsAtFileLine (string filename, int line)
		{
			filename = System.IO.Path.GetFullPath (filename);
			
			List<Breakpoint> list = new List<Breakpoint> ();
			foreach (BreakEvent be in breakpoints) {
				Breakpoint bp = be as Breakpoint;
				if (bp != null && bp.FileName == filename && bp.Line == line)
					list.Add (bp);
			}
			return list.AsReadOnly ();
		}
		
		public bool Remove (BreakEvent bp)
		{
			if (breakpoints.Remove (bp)) {
				OnBreakEventRemoved (bp);
				return true;
			}
			return false;
		}

		public IEnumerator GetEnumerator ()
		{
			return breakpoints.GetEnumerator ();
		}

		IEnumerator<BreakEvent> IEnumerable<BreakEvent>.GetEnumerator ()
		{
			return breakpoints.GetEnumerator ();
		}

		public void Clear ()
		{
			List<BreakEvent> oldList = breakpoints;
			breakpoints = new List<BreakEvent> ();
			foreach (BreakEvent bp in oldList)
				OnBreakEventRemoved (bp);
		}

		public void ClearBreakpoints ()
		{
			foreach (Breakpoint bp in GetBreakpoints ())
				Remove (bp);
		}

		public void ClearCatchpoints ()
		{
			foreach (Catchpoint bp in GetCatchpoints ())
				Remove (bp);
		}

		public bool Contains (BreakEvent item)
		{
			return breakpoints.Contains (item);
		}

		public void CopyTo (BreakEvent[] array, int arrayIndex)
		{
			breakpoints.CopyTo (array, arrayIndex);
		}
		
		public XmlElement Save ()
		{
			XmlDocument doc = new XmlDocument ();
			XmlElement elem = doc.CreateElement ("BreakpointStore");
			foreach (BreakEvent ev in this) {
				XmlElement be = ev.ToXml (doc);
				elem.AppendChild (be);
			}
			return elem;
		}
		
		public void Load (XmlElement rootElem)
		{
			Clear ();
			foreach (XmlNode n in rootElem.ChildNodes) {
				XmlElement elem = n as XmlElement;
				if (elem == null)
					continue;
				BreakEvent ev = BreakEvent.FromXml (elem);
				if (ev != null)
					Add (ev);
			}
		}
		
		internal void EnableBreakEvent (BreakEvent be, bool enabled)
		{
			OnChanged ();
			if (BreakEventEnableStatusChanged != null)
				BreakEventEnableStatusChanged (this, new BreakEventArgs (be));
			NotifyStatusChanged (be);
		}
		
		void OnBreakEventAdded (BreakEvent be)
		{
			OnChanged ();
			if (BreakEventAdded != null)
				BreakEventAdded (this, new BreakEventArgs ((BreakEvent)be));
			if (be is Breakpoint) {
				if (BreakpointAdded != null)
					BreakpointAdded (this, new BreakpointEventArgs ((Breakpoint)be));
			} else if (be is Catchpoint) {
				if (CatchpointAdded != null)
					CatchpointAdded (this, new CatchpointEventArgs ((Catchpoint)be));
			}
		}
		
		void OnBreakEventRemoved (BreakEvent be)
		{
			OnChanged ();
			if (BreakEventRemoved != null)
				BreakEventRemoved (this, new BreakEventArgs ((BreakEvent)be));
			if (be is Breakpoint) {
				if (BreakpointRemoved != null)
					BreakpointRemoved (this, new BreakpointEventArgs ((Breakpoint)be));
			} else if (be is Catchpoint) {
				if (CatchpointRemoved != null)
					CatchpointRemoved (this, new CatchpointEventArgs ((Catchpoint)be));
			}
		}
		
		void OnChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		internal void NotifyStatusChanged (BreakEvent be)
		{
			try {
				if (BreakEventStatusChanged != null)
					BreakEventStatusChanged (this, new BreakEventArgs ((BreakEvent)be));
				if (be is Breakpoint) {
					if (BreakpointStatusChanged != null)
						BreakpointStatusChanged (this, new BreakpointEventArgs ((Breakpoint)be));
				} else if (be is Catchpoint) {
					if (CatchpointStatusChanged != null)
						CatchpointStatusChanged (this, new CatchpointEventArgs ((Catchpoint)be));
				}
			} catch {
				// Ignone
			}
		}
		
		internal void NotifyBreakEventChanged (BreakEvent be)
		{
			try {
				if (BreakEventModified != null)
					BreakEventModified (this, new BreakEventArgs ((BreakEvent)be));
				if (be is Breakpoint) {
					if (BreakpointModified != null)
						BreakpointModified (this, new BreakpointEventArgs ((Breakpoint)be));
				} else if (be is Catchpoint) {
					if (CatchpointModified != null)
						CatchpointModified (this, new CatchpointEventArgs ((Catchpoint)be));
				}
			} catch {
				// Ignone
			}
		}
		
		public event EventHandler<BreakpointEventArgs> BreakpointAdded;
		public event EventHandler<BreakpointEventArgs> BreakpointRemoved;
		public event EventHandler<BreakpointEventArgs> BreakpointStatusChanged;
		public event EventHandler<BreakpointEventArgs> BreakpointModified;
		public event EventHandler<CatchpointEventArgs> CatchpointAdded;
		public event EventHandler<CatchpointEventArgs> CatchpointRemoved;
		public event EventHandler<CatchpointEventArgs> CatchpointStatusChanged;
		public event EventHandler<CatchpointEventArgs> CatchpointModified;
		public event EventHandler<BreakEventArgs> BreakEventAdded;
		public event EventHandler<BreakEventArgs> BreakEventRemoved;
		public event EventHandler<BreakEventArgs> BreakEventStatusChanged;
		public event EventHandler<BreakEventArgs> BreakEventModified;
		internal event EventHandler<BreakEventArgs> BreakEventEnableStatusChanged;
		public event EventHandler Changed;
	}
}
