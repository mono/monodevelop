// 
// ProfileDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using Gtk;
using System.Collections.Generic;

namespace MonoDevelop.Profiler
{
	public partial class ProfileDialog : Gtk.Window
	{
		
		TimeLineWidget timeLineWidget;
		TreeStore store;
		ListStore threadStore;
		LogBuffer buffer;
		uint timeout;

		protected override void OnDestroyed ()
		{
			if (timeout != 0) {
				GLib.Source.Remove (timeout);
				timeout = 0;
			}
			base.OnDestroyed ();
		}
		
		const int TimeSelfColumn = 4;
		const int TotalTimeColumn = 5;
		const int MethodInfoColumn = 6;

		public ProfileDialog (string fileName) : base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
			timeLineWidget = new TimeLineWidget (this);
			scrolledwindow1.AddWithViewport (timeLineWidget);
			scrolledwindow1.ShowAll ();
			
			threadStore = new ListStore (typeof(string), typeof(AnalyseVisitor.ThreadContext));
			comboboxThreads.Model = threadStore;
			comboboxThreads.Changed += HandleComboboxThreadsChanged;
			store = new TreeStore (typeof(string), // name
				typeof(string), // icon
				typeof(ulong), // call count
				typeof(double), // time with children %
				typeof(double), // time self (ms)
				typeof(double), // total time (ms)
				typeof(AnalyseVisitor.MethodInfo) // method info
			);
			profileResultsView.Model = store;
			
			var methodColumn = new TreeViewColumn ();
			methodColumn.SortColumnId = 0;
			methodColumn.SortIndicator = true;
			methodColumn.Title = "Method";
			methodColumn.Resizable = true;
			
			var textRender = new CellRendererText ();
			methodColumn.PackStart (textRender, true);
			methodColumn.AddAttribute (textRender, "text", 0);
			profileResultsView.AppendColumn (methodColumn);
			
			var timeSelfColumn = new TreeViewColumn ();
			timeSelfColumn.SortColumnId = TimeSelfColumn;
			timeSelfColumn.SortIndicator = true;
			timeSelfColumn.Title = "Time self (ms)";
			timeSelfColumn.Resizable = true;
			timeSelfColumn.PackStart (textRender, true);
			timeSelfColumn.SetCellDataFunc (textRender, delegate (Gtk.TreeViewColumn column, Gtk.CellRenderer cell,Gtk.TreeModel model, Gtk.TreeIter iter) {
				double time = (double)model.GetValue (iter, TimeSelfColumn);
				((CellRendererText)cell).Text = string.Format ("{0:0.0}", time);
			});
			profileResultsView.AppendColumn (timeSelfColumn);
			
			var totalTimeColumn = new TreeViewColumn ();
			totalTimeColumn.SortColumnId = TimeSelfColumn;
			totalTimeColumn.SortIndicator = true;
			totalTimeColumn.Title = "Total time (ms)";
			totalTimeColumn.Resizable = true;
			totalTimeColumn.PackStart (textRender, true);
			totalTimeColumn.SetCellDataFunc (textRender, delegate (Gtk.TreeViewColumn column, Gtk.CellRenderer cell,Gtk.TreeModel model, Gtk.TreeIter iter) {
				double time = (double)model.GetValue (iter, TotalTimeColumn);
				((CellRendererText)cell).Text = string.Format ("{0:0.0}", time);
			});
			profileResultsView.AppendColumn (totalTimeColumn);
			
			var timeWithChidrenColumn = new TreeViewColumn ();
			timeWithChidrenColumn.SortColumnId = 3;
			timeWithChidrenColumn.SortIndicator = true;
			timeWithChidrenColumn.Title = "Time from parent (%)";
			timeWithChidrenColumn.Resizable = true;
			var timeCellRenderer = new TimeCellRenderer ();
			timeWithChidrenColumn.PackStart (timeCellRenderer, true);
			timeWithChidrenColumn.AddAttribute (timeCellRenderer, "time", 3);
			profileResultsView.AppendColumn (timeWithChidrenColumn);
			
			var callCountColumn = new TreeViewColumn ();
			callCountColumn.SortIndicator = true;
			callCountColumn.SortColumnId = 2;
			callCountColumn.Resizable = true;
			callCountColumn.Title = "Call count";
			callCountColumn.PackStart (textRender, true);
			callCountColumn.SetCellDataFunc (textRender, delegate (Gtk.TreeViewColumn column, Gtk.CellRenderer cell,Gtk.TreeModel model, Gtk.TreeIter iter) {
				ulong callCount = (ulong)model.GetValue (iter, 2);
				((CellRendererText)cell).Text = callCount.ToString ();
			});
			
			profileResultsView.AppendColumn (callCountColumn);
			profileResultsView.TestExpandRow += HandleProfileResultsViewTestExpandRow;
			profileResultsView.ShowExpanders = true;
			
//		timeout = GLib.Timeout.Add (500, delegate {
			try {
				buffer = LogBuffer.Read (fileName);
			} catch (Exception) {
			}
			AnalyzeBuffer ();
/*				return true;
			});*/
		}
		
		
		long SelectedThreadID {
			get {
				if (comboboxThreads.Active > 0) {
					TreeIter iter;
					if (threadStore.GetIterFromString (out iter, comboboxThreads.Active.ToString ())) {
						var thread = (AnalyseVisitor.ThreadContext)threadStore.GetValue (iter, 1);
						return thread.ThreadId;
					}
				}
				return 0L;
			}
		}
		
		void HandleComboboxThreadsChanged (object sender, EventArgs e)
		{
			visitor.LookupThread = SelectedThreadID;
			AnalyzeBuffer ();
		}
		
		internal AnalyseVisitor visitor = new AnalyseVisitor ();
		HashSet<string> expandedRows = new HashSet<string> ();
		void HandleProfileResultsViewTestExpandRow (object o, TestExpandRowArgs args)
		{
			string path = store.GetPath (args.Iter).ToString ();
			if (expandedRows.Contains (path)) {
				args.RetVal = false;
				return;
			}
			expandedRows.Add (path);
			var info = (AnalyseVisitor.MethodInfo)store.GetValue (args.Iter, MethodInfoColumn);
			TreeIter child;
			while (store.IterChildren (out child, args.Iter)) {
				store.Remove (ref child);
			}
			
			foreach (var methodCall in info.MethodCalls.Values) {
				AppendMethodInfo (args.Iter, methodCall);
			}
		}
		
		public void SetTime (ulong start, ulong end)
		{
			if (buffer == null)
				return;
			if (start == 0 && end == 0) {
				visitor.StartTime = visitor.EndTime = 0;
			} else {
				ulong t0 = buffer.buffers [0].Header.TimeBase;
				visitor.StartTime = t0 + start;
				visitor.EndTime = t0 + end;
			}
			AnalyzeBuffer ();
		}
		
		internal ulong totalTime = 0;
		void AnalyzeBuffer ()
		{
			if (buffer == null)
				return;
			expandedRows.Clear ();
			long selectedID = SelectedThreadID;
			visitor.LookupThread = selectedID;
			visitor.Reset ();
			totalTime = 0;
			foreach (var b in buffer.buffers) {
				visitor.CurrentBuffer = b;
				b.RunVisitor (visitor);
				totalTime += visitor.TimeBase - b.Header.TimeBase;
			}
			
			comboboxThreads.Changed -= HandleComboboxThreadsChanged;
			threadStore.Clear ();
			threadStore.AppendValues ("[All Threads]", null);
			bool first = true;
			int active = 0;
			int i = 1;
			foreach (var thread in visitor.threadDictionary.Values) {
				string name = thread.Name;
				if (string.IsNullOrEmpty (name) && first) {
					name = "GUI Thread";
				}
				first = false;
				if (thread.ThreadId == selectedID)
					active = i;
				threadStore.AppendValues (name + " (" + thread.ThreadId + ")", thread);
				i++;
			}
			comboboxThreads.Active = active;
			comboboxThreads.Changed += HandleComboboxThreadsChanged;
			store.Clear ();
			
			foreach (var threadContext in visitor.threadDictionary.Values) {
				if (selectedID != 0 && threadContext.ThreadId != selectedID)
					continue;
				foreach (var info in threadContext.MethodCalls.Values) {
					AppendMethodInfo (TreeIter.Zero, info);
				}
			}
			timeLineWidget.Update ();
		}
		
		void AppendMethodInfo (TreeIter iter, AnalyseVisitor.MethodInfo info)
		{
			TreeIter subiter;
			double time = (double)totalTime;
			if (info.Parent != null) {
				time = info.Parent.TimeWithChildren;
			} else {
				if (visitor.StartTime != 0) {
					time = Math.Min (totalTime, visitor.EndTime - visitor.StartTime);
				}
			}
			if (iter.Equals (TreeIter.Zero)) {
				subiter = store.AppendValues (visitor.LookupName (info.MethodBase), 
						"", 
						info.Calls, 
						100.0 * (double)info.TimeWithChildren / time, 
						(info.TimeWithChildren - info.CalleeTime) / 1000.0, 
						info.TimeWithChildren / 1000.0, 
						info);
			} else {
				subiter = store.AppendValues (iter, visitor.LookupName (info.MethodBase), 
						"", 
						info.Calls, 
						100.0 * (double)info.TimeWithChildren / time, 
						(info.TimeWithChildren - info.CalleeTime) / 1000.0, 
						info.TimeWithChildren / 1000.0, 
						info);
			}
			if (info.MethodCalls.Count > 0) {
				store.AppendValues (subiter, "", 
						"", 
						0UL, 
						0d, 
						0d, 
						0d, 
						null);
			}
			
		}

		public class AnalyseVisitor : EventVisitor
		{
			Buffer currentBuffer;
			ThreadContext currentThread;
			long methodBase;
			ulong timeBase;
			ulong eventCount = 0;
			ulong lastTimeBase = 0;
			const
				int interval = 100000 ; 
			public
				List<ulong> Events = new List<ulong> ();
			
			public
				ulong StartTime {
				get;
				set;
			}

			public
			ulong EndTime {
				get;
				set;
			}

			public ulong TimeBase {
				get { 
					return timeBase;
				}
				set { 
					eventCount++;
					timeBase = value;
					if (timeBase - lastTimeBase > interval) {
						Events.Add (eventCount);
						eventCount = 0;
						lastTimeBase = timeBase;
					}
				}
			}
			
			long lookupThread;
			public long LookupThread {
				get { return lookupThread; }
				set { lookupThread = value; }
			}
			
			public Buffer CurrentBuffer {
				get { return currentBuffer; }
				set {
					currentBuffer = value;
					methodBase = currentBuffer.Header.MethodBase;
					lastTimeBase = TimeBase = currentBuffer.Header.TimeBase;
					if (!threadDictionary.TryGetValue (currentBuffer.Header.ThreadId, out currentThread)) {
						currentThread = new ThreadContext () { ThreadId = currentBuffer.Header.ThreadId };
						threadDictionary [currentBuffer.Header.ThreadId] = currentThread;
					}
				}
			}
			
			public class ThreadContext
			{
				public long ThreadId { get; set; }
				public string Name { get; set; }
				
				public Dictionary<long, MethodInfo> MethodCalls = new Dictionary<long, MethodInfo> ();
				
				public Stack<MethodInfo> Stack = new Stack<MethodInfo> ();
				public Stack<ulong> timeStack = new Stack<ulong> ();
				public Stack<ulong> calleeTimeStack = new Stack<ulong> ();

				public long LastTime { get; set; }
				
				MethodInfo GetInfo (long methodBase)
				{
					Dictionary<long, MethodInfo> dict;
					if (Stack.Count > 0) {
						dict = Stack.Peek ().MethodCalls;
					} else {
						dict = MethodCalls;
					}
					MethodInfo info;
					if (!dict.TryGetValue (methodBase, out info)) {
						dict [methodBase] = info = new MethodInfo (methodBase);
						if (Stack.Count > 0)
							info.Parent = Stack.Peek ();
					}
					return info;
				}
				
				public void PushMethod (long methodBase, ulong timeStamp)
				{
					MethodInfo info = GetInfo (methodBase);
					
					info.RecurseCount++;
					info.Calls++;
					
					timeStack.Push (timeStamp);
					calleeTimeStack.Push (0);
					Stack.Push (info);
				}

				public void PopMethod (long methodBase, ulong timeStamp)
				{
					if (Stack.Count > 0 && Stack.Peek ().MethodBase == methodBase) {
						MethodInfo methodInfo = Stack.Pop ();
						methodInfo.RecurseCount--;
						ulong timeDiff = timeStamp - timeStack.Pop ();
						methodInfo.TimeWithChildren += timeDiff;
						methodInfo.CalleeTime = calleeTimeStack.Pop ();
						if (calleeTimeStack.Count > 0)
							calleeTimeStack.Push (calleeTimeStack.Pop () + timeDiff);
					}
				}
			}
			
			public class MethodInfo
			{
				public MethodInfo Parent {
					get;
					set;
				}
				
				public long RecurseCount {
					get;
					set;
				}
				
				public ulong Calls {
					get;
					set;
				}
				
				public long MethodBase {
					get;
					set;
				}
				
				public ulong TimeWithChildren {
					get;
					set;
				}
				
				public ulong CalleeTime {
					get;
					set;
				}
				
				public Dictionary<long, MethodInfo> MethodCalls = new Dictionary<long, MethodInfo> ();
				
				public MethodInfo (long methodBase)
				{
					this.MethodBase = methodBase;
				}
			}
			
			public Dictionary<long, string> nameDictionary = new Dictionary<long, string> ();
			public Dictionary<long, ThreadContext> threadDictionary = new Dictionary<long, ThreadContext> ();
			
			public string LookupName (long methodBase)
			{
				string name;
				if (nameDictionary.TryGetValue (methodBase, out name))
					return name;
				return "Unknown method";
			}

			public void Reset ()
			{
				Events.Clear ();
				threadDictionary.Clear ();
			}
			
			public override object Visit (MethodEvent methodEvent)
			{
				methodBase += methodEvent.Method;
				TimeBase += methodEvent.TimeDiff;
				switch (methodEvent.Type) {
				case MethodEvent.MethodType.Jit:
					nameDictionary [methodBase] = methodEvent.Name;
					break;
				
				case MethodEvent.MethodType.Enter:
					if (ShouldLog) {
						if (!nameDictionary.ContainsKey (methodBase))
							nameDictionary [methodBase] = "[Unknown method]";
						
						currentThread.PushMethod (methodBase, TimeBase);
					}
					break;
				
				case MethodEvent.MethodType.Leave:
					if (ShouldLog) {
						if (!nameDictionary.ContainsKey (methodBase))
							nameDictionary [methodBase] = "[Unknown method]";
						currentThread.PopMethod (methodBase, TimeBase);
					}
					break;
				}
				return null;
			}
			
			bool ShouldLog {
				get {
					return (LookupThread == 0 || LookupThread == currentThread.ThreadId) && 
						(StartTime == 0 && EndTime == 0 || 
							StartTime < timeBase && timeBase < EndTime);
				}
			}
			
			public override object Visit (ExceptionEvent exceptionEvent)
			{
				methodBase += exceptionEvent.Method;
				TimeBase += exceptionEvent.TimeDiff;
				return null;
			}
			
			public override object Visit (AllocEvent allocEvent)
			{
				TimeBase += allocEvent.TimeDiff;
				return null;
			}
			
			public override object Visit (GcEvent gcEvent)
			{
				TimeBase += gcEvent.TimeDiff;
				return null;
			}
			
			public override object Visit (HandleCreatedGcEvent handleCreatedGcEvent)
			{
				TimeBase += handleCreatedGcEvent.TimeDiff;
				return null;
			}
			
			public override object Visit (HandleDestroyedGcEvent handleDestroyedGcEvent)
			{
				TimeBase += handleDestroyedGcEvent.TimeDiff;
				return null;
			}
			
			public override object Visit (HeapEvent heapEvent)
			{
				TimeBase += heapEvent.TimeDiff;
				return null;
			}
			
			public override object Visit (MetadataEvent metadataEvent)
			{
				TimeBase += metadataEvent.TimeDiff;
				switch (metadataEvent.MType) {
				case MetadataEvent.MetaDataType.Thread:
					ThreadContext ctx;
					long threadId = currentBuffer.Header.PtrBase * metadataEvent.Pointer;
					if (!threadDictionary.TryGetValue (threadId, out ctx)) {
						threadDictionary [threadId] = ctx = new ThreadContext () { ThreadId = threadId };
					}
					ctx.Name = metadataEvent.Name;
					break;
				}
				return null;
			}
			
			public override object Visit (MonitiorEvent monitiorEvent)
			{
				TimeBase += monitiorEvent.TimeDiff;
				return null;
			}
			
			public override object Visit (MoveGcEvent moveGcEvent)
			{
				TimeBase += moveGcEvent.TimeDiff;
				return null;
			}
			
			public override object Visit (ResizeGcEvent resizeGcEvent)
			{
				TimeBase += resizeGcEvent.TimeDiff;
				return null;
			}
		}
	}
}
