//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
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
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Profiling
{
	public class ProfilingSnapshotCollection : IEnumerable<IProfilingSnapshot>
	{
		public event ProfilingSnapshotEventHandler SnapshotAdded;
		public event ProfilingSnapshotEventHandler SnapshotRemoved;
		
		private string filename;

		private List<IProfilingSnapshot> summaries;
		private EventHandler nameChangedHandler;
		
		public ProfilingSnapshotCollection (string filename)
		{
			if (filename == null)
				throw new ArgumentNullException ("filename");
			
			this.filename = filename;

			summaries = new List<IProfilingSnapshot> ();
			nameChangedHandler = new EventHandler (OnNameChanged);
		}

		public IProfilingSnapshot this[int index] {
			get { return summaries[index]; }
		}

		public void Add (IProfilingSnapshot item)
		{
			summaries.Add (item);
			item.NameChanged += nameChangedHandler;
			Save ();
			OnSnapshotAdded (new ProfilingSnapshotEventArgs (item));			
		}
		
		public int Count {
			get { return summaries.Count; }
		}

		public int IndexOf (IProfilingSnapshot item)
		{
			return summaries.IndexOf (item);
		}

		public void Insert (int index, IProfilingSnapshot item)
		{
			summaries.Insert (index, item);
			item.NameChanged += nameChangedHandler;
			Save ();
			OnSnapshotAdded (new ProfilingSnapshotEventArgs (item));			
		}

		public void Remove (IProfilingSnapshot item)
		{
			summaries.Remove (item);
			item.NameChanged -= nameChangedHandler;
			Save ();
			OnSnapshotRemoved (new ProfilingSnapshotEventArgs (item));			
		}

		public bool Contains (IProfilingSnapshot item)
		{
			return summaries.Contains (item);
		}
		
		public IEnumerator<IProfilingSnapshot> GetEnumerator ()
		{
			return summaries.GetEnumerator ();
		}
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return (summaries as IEnumerable).GetEnumerator ();
		}
		
		protected virtual void OnSnapshotAdded (ProfilingSnapshotEventArgs args)
		{
			if (SnapshotAdded != null )
				SnapshotAdded (this, args);
		}
		
		protected virtual void OnSnapshotRemoved (ProfilingSnapshotEventArgs args)
		{
			if (SnapshotRemoved != null )
				SnapshotRemoved (this, args);
		}
		
		public void Load ()
		{
			XmlDocument doc = new XmlDocument ();
			if (!File.Exists (filename))
				return;

			try {
				doc.Load (filename);

				foreach (XmlNode node in doc.DocumentElement.SelectNodes ("ProfilingSnapshot")) {
					if (node.NodeType != XmlNodeType.Element)
						continue;
					
					XmlElement element = node as XmlElement;
					ProfilingService.LoadSnapshot (element.GetAttribute ("profiler"), element.GetAttribute ("filename"));
				}
			} catch (Exception e) {
				LoggingService.LogError ("ProfilingSnapshotCollection", "Load Profiling Snapshots", e);
			}
		}
		
		public void Save ()
		{
			XmlDocument doc = new XmlDocument ();
			
			XmlElement root = doc.CreateElement ("ProfilingSnapshots");
			doc.AppendChild (root);
			
			foreach (IProfilingSnapshot snapshot in this) {
				XmlElement snapshotElement = doc.CreateElement ("ProfilingSnapshot");
				snapshotElement.SetAttribute ("profiler", snapshot.Profiler.Identifier);
				snapshotElement.SetAttribute ("filename", snapshot.FileName);
				root.AppendChild (snapshotElement);
			}
			doc.Save (filename);
		}
			
		protected void OnNameChanged (object sender, EventArgs args)
		{
			Save ();
		}
	}
}
