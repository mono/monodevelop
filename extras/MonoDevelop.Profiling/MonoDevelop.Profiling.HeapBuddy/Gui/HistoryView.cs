//
// Authors:
//   Ben Maurer  <bmaurer@ximian.com>
//   Jon Trowbridge  <trow@novell.com>
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Novell, Inc.
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

using Gtk;
using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.Profiling.HeapBuddy
{
	public class HistoryView : AbstractViewContent
	{
		private HeapBuddyProfilingSnapshot snapshot;

		private ScrolledWindow window;
		private TreeView list;
		private ListStore store;
		
		public HistoryView ()
		{
			window = new ScrolledWindow ();
			list = new TreeView ();
			list.RulesHint = true;
			
			store = new ListStore (typeof (string), typeof (string), typeof (string));
			list.Model = store;
			
			CellRendererText timeRenderer = new CellRendererText ();
			CellRendererText eventRenderer = new CellRendererText ();
			CellRendererText descRenderer = new CellRendererText ();
			
			TreeViewColumn columnTime = new TreeViewColumn ();
			TreeViewColumn columnEvent = new TreeViewColumn ();
			TreeViewColumn columnDesc = new TreeViewColumn ();
			
			columnTime.Title = GettextCatalog.GetString ("Time");
			columnEvent.Title = GettextCatalog.GetString ("Event");
			columnDesc.Title = GettextCatalog.GetString ("Description");
			
			columnTime.PackStart (timeRenderer, true);
			columnEvent.PackStart (eventRenderer, true);
			columnDesc.PackStart (descRenderer, true);
			
			columnTime.AddAttribute (timeRenderer, "text", 0);
			columnEvent.AddAttribute (eventRenderer, "text", 1);
			columnDesc.AddAttribute (descRenderer, "text", 2);
			
			list.AppendColumn (columnTime);
			list.AppendColumn (columnEvent);
			list.AppendColumn (columnDesc);
			
			window.Add (list);
			window.ShowAll ();
		}

		public override bool IsDirty {
			get { return false; }
			set {  }
		}

		public override string StockIconId {
			get { return "md-prof-history"; }
		}
		
		public override string UntitledName {
			get { return snapshot.Name + " - " + GettextCatalog.GetString ("History"); }
		}

		public override Widget Control {
			get { return window; }
		}
		
		public override void Load (string fileName) {}
		
		public void Load (HeapBuddyProfilingSnapshot snapshot)
		{
			this.snapshot = snapshot;
			OutfileReader reader = snapshot.Outfile;

			Resize[] resizes = reader.Resizes;
			Gc[] gcs = reader.Gcs;

			int i_resize = 0;
			int i_gc = 0;
			long heap_size = 0;

			while (i_resize < resizes.Length || i_gc < gcs.Length) {
				
				Resize r = null;
				if (i_resize < resizes.Length)
					r = resizes [i_resize];
				
				Gc gc = null;
				if (i_gc < gcs.Length)
					gc = gcs [i_gc];

				string timestamp, tag, message;

				if (r != null && (gc == null || r.Generation <= gc.Generation)) {
					timestamp = string.Format ("{0:HH:mm:ss}", r.Timestamp);

					if (r.PreviousSize == 0) {
						tag =  GettextCatalog.GetString ("Init");
						message = String.Format (GettextCatalog.GetString ("Initialized heap to {0}"),
							ProfilingService.PrettySize (r.NewSize));
					} else {
						tag =  GettextCatalog.GetString ("Resize");
						message = String.Format (GettextCatalog.GetString ("Grew heap from {0} to {1}") +
							Environment.NewLine +
							GettextCatalog.GetString ("{2} in {3} live objects") +
							Environment.NewLine +
							GettextCatalog.GetString ("Heap went from {4:0.0}% to {5:0.0}% capacity"),
							ProfilingService.PrettySize (r.PreviousSize),
							ProfilingService.PrettySize (r.NewSize),
							ProfilingService.PrettySize (r.TotalLiveBytes),
							r.TotalLiveObjects,
							r.PreResizeCapacity, r.PostResizeCapacity);
					}

					heap_size = r.NewSize;
					++i_resize;

				} else {
					timestamp = String.Format ("{0:HH:mm:ss}", gc.Timestamp);
					if (gc.Generation >= 0) {
						tag =  GettextCatalog.GetString ("GC ") + gc.Generation;
						message = String.Format (GettextCatalog.GetString ("Collected {0} of {1} objects ({2:0.0}%)") +
							Environment.NewLine +
							GettextCatalog.GetString ("Collected {3} of {4} ({5:0.0}%)") +
						        Environment.NewLine +
							GettextCatalog.GetString ("Heap went from {6:0.0}% to {7:0.0}% capacity"),
							gc.FreedObjects,
							gc.PreGcLiveObjects,
							gc.FreedObjectsPercentage,
							ProfilingService.PrettySize (gc.FreedBytes),
							ProfilingService.PrettySize (gc.PreGcLiveBytes),
							gc.FreedBytesPercentage,
							100.0 * gc.PreGcLiveBytes / heap_size,
							100.0 * gc.PostGcLiveBytes / heap_size);
					} else {
						tag = GettextCatalog.GetString ("Exit");
						message = String.Format (GettextCatalog.GetString ("{0} live objects using {1}"),
									 gc.PreGcLiveObjects,
									 ProfilingService.PrettySize (gc.PreGcLiveBytes));
					}
					++i_gc;
				}

				store.AppendValues (timestamp, tag, message);
			}
		}
	}
}
