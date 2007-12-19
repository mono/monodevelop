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
	public class BacktracesView : AbstractViewContent
	{
		private HeapBuddyProfilingSnapshot snapshot;

		private ScrolledWindow window;
		private TreeView tree;
		private TreeStore store;

		public BacktracesView ()
		{
			window = new ScrolledWindow ();
			tree = new TreeView ();
			tree.RulesHint = true;
			
			//                               icon            type            count            #bytes         avg size         avg age
			store = new TreeStore (typeof (string), typeof (string), typeof (string), typeof (string), typeof (string), typeof (string), typeof (object));
			tree.Model = store;

			CellRendererPixbuf pixbufRenderer = new CellRendererPixbuf ();
			CellRendererText typeRenderer = new CellRendererText ();
			CellRendererText countRenderer = new CellRendererText ();
			CellRendererText totalSizeRenderer = new CellRendererText ();
			CellRendererText avgSizeRenderer = new CellRendererText ();
			CellRendererText avgAgeRenderer = new CellRendererText ();
			
			TreeViewColumn columnType = new TreeViewColumn ();
			TreeViewColumn columnCount = new TreeViewColumn ();
			TreeViewColumn columnTotalSize = new TreeViewColumn ();
			TreeViewColumn columnAvgSize = new TreeViewColumn ();
			TreeViewColumn columnAvgAge = new TreeViewColumn ();
			
			columnType.Title = GettextCatalog.GetString ("Type");
			columnCount.Title = GettextCatalog.GetString ("Count");
			columnTotalSize.Title = GettextCatalog.GetString ("Total Size");
			columnAvgSize.Title = GettextCatalog.GetString ("Avg Size");
			columnAvgAge.Title = GettextCatalog.GetString ("Avg Age");
			
			columnType.PackStart (pixbufRenderer, false);
			columnType.PackStart (typeRenderer, true);
			columnCount.PackStart (countRenderer, true);
			columnTotalSize.PackStart (totalSizeRenderer, true);
			columnAvgSize.PackStart (avgSizeRenderer, true);
			columnAvgAge.PackStart (avgAgeRenderer, true);
			
			columnType.AddAttribute (pixbufRenderer, "stock-id", 0);
			columnType.AddAttribute (typeRenderer, "text", 1);
			columnCount.AddAttribute (countRenderer, "text", 2);
			columnTotalSize.AddAttribute (totalSizeRenderer, "text", 3);
			columnAvgSize.AddAttribute (avgSizeRenderer, "text", 4);
			columnAvgAge.AddAttribute (avgAgeRenderer, "text", 5);
			
			tree.AppendColumn (columnType);
			tree.AppendColumn (columnCount);
			tree.AppendColumn (columnTotalSize);
			tree.AppendColumn (columnAvgSize);
			tree.AppendColumn (columnAvgAge);
			
			int nc = 0;
			foreach (TreeViewColumn c in tree.Columns) {
				store.SetSortFunc (nc, CompareNodes);
				c.SortColumnId = nc++;
			}
			store.SetSortColumnId (1, SortType.Descending);
			
			window.Add (tree);
			window.ShowAll ();
		}

		public override bool IsDirty {
			get { return false; }
			set {  }
		}

		public override string StockIconId {
			get { return "md-method"; }
		}
		
		public override string UntitledName {
			get { return snapshot.Name + " - " + GettextCatalog.GetString ("Backtraces"); }
		}

		public override Widget Control {
			get { return window; }
		}
		
		public override void Load (string fileName) {}
		
		public void Load (HeapBuddyProfilingSnapshot snapshot)
		{
			this.snapshot = snapshot;

			foreach (Backtrace bt in snapshot.Outfile.Backtraces) {
				TreeIter iter = store.AppendValues ("md-class", bt.Type.Name, bt.LastObjectStats.AllocatedCount.ToString (),
					ProfilingService.PrettySize (bt.LastObjectStats.AllocatedTotalBytes),
					String.Format ("{0:0.0}", bt.LastObjectStats.AllocatedAverageBytes),
					String.Format ("{0:0.0}", bt.LastObjectStats.AllocatedAverageAge), bt);
				
				foreach (Frame frame in bt.Frames) {
					if (!frame.MethodName.StartsWith ("(wrapper"))
						store.AppendValues (iter, "md-method", frame.MethodName, String.Empty, String.Empty,
							String.Empty, String.Empty, frame);
				}
			}
		}
		
		int CompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			int col;
			SortType type;
			store.GetSortColumnId (out col, out type);
			
			object o1 = model.GetValue (a, 6);
			object o2 = model.GetValue (b, 6);
			
			if (o1 is Backtrace && o2 is Backtrace) {
				Backtrace b1 = (Backtrace) o1;
				Backtrace b2 = (Backtrace) o2;
				switch (col) {
					case 0:
						return string.Compare (b1.Type.Name, b2.Type.Name);
					case 1:
						return b1.LastObjectStats.AllocatedCount.CompareTo (b2.LastObjectStats.AllocatedCount);
					case 2:
						return b1.LastObjectStats.AllocatedTotalBytes.CompareTo (b2.LastObjectStats.AllocatedTotalBytes);
					case 3:
						return b1.LastObjectStats.AllocatedAverageBytes.CompareTo (b2.LastObjectStats.AllocatedAverageBytes);
					case 4:
						return b1.LastObjectStats.AllocatedAverageAge.CompareTo (b2.LastObjectStats.AllocatedAverageAge);
					default:
						return 1;
				}
			} else if (o1 is Frame && o2 is Frame) {
				return ((Frame)o1).MethodName.CompareTo (((Frame)o2).MethodName);
			} else if (o1 is Frame) {
				return 1;
			} else {
				return -1;
			}
		}
	}
}
