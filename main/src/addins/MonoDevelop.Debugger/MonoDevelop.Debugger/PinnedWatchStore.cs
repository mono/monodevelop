// 
// PinnedWatchStore.cs
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using Mono.Debugging.Client;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger
{
	public class PinnedWatchStore
	{
		[ExpandedCollection]
		[ItemProperty ("Watch")]
		List<PinnedWatch> watches = new List<PinnedWatch> ();
		Dictionary<Breakpoint,PinnedWatch> liveWatches = new Dictionary<Breakpoint, PinnedWatch> ();
		
		public void Add (PinnedWatch watch)
		{
			lock (watches) {
				if (watch.Store != null)
					throw new InvalidOperationException ("Watch already belongs to another store");
				watch.Store = this;
				watches.Add (watch);
			}
			OnWatchAdded (watch);
			OnChanged ();
		}
		
		public void Remove (PinnedWatch watch)
		{
			lock (watches) {
				if (watch.Store != this)
					return;
				watch.Store = null;
				watches.Remove (watch);
			}
			OnWatchRemoved (watch);
			OnChanged ();
		}
		
		public bool IsWatcherBreakpoint (Breakpoint bp)
		{
			lock (watches) {
				return liveWatches.ContainsKey (bp);
			}
		}
		
		internal void Bind (PinnedWatch watch, Breakpoint be)
		{
			lock (watches) {
				if (be == null) {
					if (watch.BoundTracer != null)
						liveWatches.Remove (watch.BoundTracer);
					watch.LiveUpdate = false;
				} else {
					watch.BoundTracer = be;
					liveWatches [be] = watch;
					watch.LiveUpdate = true;
				}
			}
		}
		
		internal void BindAll (BreakpointStore bps)
		{
			lock (watches) {
				foreach (PinnedWatch w in watches) {
					foreach (Breakpoint bp in bps.GetBreakpoints ()) {
						if ((bp.HitAction & HitAction.PrintExpression) != HitAction.None &&
							bp.TraceExpression == "{" + w.Expression + "}" && bp.FileName == w.File && bp.Line == w.Line)
							Bind (w, bp);
					}
				}
			}
		}
		
		internal void SetAllLiveUpdateBreakpoints (BreakpointStore bps)
		{
			lock (watches) {
				foreach (PinnedWatch w in watches) {
					if (w.LiveUpdate) {
						var bp = CreateLiveUpdateBreakpoint (w);
						Bind(w, bp);
						bps.Add(bp);
					}
				}
			}
		}

		internal Breakpoint CreateLiveUpdateBreakpoint (PinnedWatch watch)
		{
			var bp = new Breakpoint (watch.File, watch.Line);
			bp.TraceExpression = "{" + watch.Expression + "}";
			bp.HitAction = HitAction.PrintExpression;
			bp.NonUserBreakpoint = true;
			return bp;
		}

		internal bool UpdateLiveWatch (Breakpoint bp, string trace)
		{
			lock (watches) {
				PinnedWatch w;
				if (!liveWatches.TryGetValue (bp, out w))
					return false;
				w.UpdateFromTrace (trace);
				return true;
			}
		}
		
		internal void LoadFrom (PinnedWatchStore store)
		{
			try {
				BeginBatchUpdate ();
				lock (watches) {
					List<PinnedWatch> ws = new List<PinnedWatch> (watches);
					watches.Clear ();
					foreach (PinnedWatch watch in ws) {
						watch.Store = null;
						OnWatchRemoved (watch);
					}
					foreach (PinnedWatch watch in store.watches) {
						watch.Store = this;
						watches.Add (watch);
						OnWatchAdded (watch);
					}
				}
				OnChanged ();
			} finally {
				EndBatchUpdate ();
			}
		}
		
		internal void InvalidateAll ()
		{
			try {
				lock (watches) {
					BeginBatchUpdate ();
					foreach (PinnedWatch w in watches)
						w.Invalidate ();
				}
			} finally {
				EndBatchUpdate ();
			}
		}
		
		public IEnumerable<PinnedWatch> GetWatchesForFile (FilePath file)
		{
			List<PinnedWatch> ws = new List<PinnedWatch> ();
			lock (watches) {
				ws.AddRange (watches.Where (w => w.File == file));
			}
			BatchEnsureEvaluated (ws);
			return ws;
		}
		
		void BatchEnsureEvaluated (List<PinnedWatch> ws)
		{
			if (DebuggingService.CurrentFrame == null)
				return;
			try {
				BeginBatchUpdate ();
				List<string> exps = new List<string> ();
				foreach (PinnedWatch w in ws)
					exps.Add (w.Expression);
				if (exps.Count > 0) {
					ObjectValue[] values = DebuggingService.CurrentFrame.GetExpressionValues (exps.ToArray (), true);
					for (int n=0; n<values.Length; n++)
						ws [n].LoadValue (values [n]);
				}
			} finally {
				EndBatchUpdate ();
			}
		}
		
		internal void BatchUpdate (IEnumerable<FilePath> fileNames)
		{
			BeginBatchUpdate ();
			List<PinnedWatch> ws = new List<PinnedWatch> ();
			lock (watches) {
				ws.AddRange (watches.Where (w => fileNames.Contains (w.File)));
			}
			BatchEnsureEvaluated (ws);
		}
		
		public void BeginBatchUpdate ()
		{
			lock (watches) {
				batchUpdate++;
			}
		}
		
		public void EndBatchUpdate ()
		{
			int res;
			
			List<PinnedWatch> oldBatchAdded;
			List<PinnedWatch> oldBatchRemoved;
			List<PinnedWatch> oldBatchChanged;
			bool oldChangedFlag;
			
			lock (watches) {
				res = --batchUpdate;
				oldBatchAdded = batchAdded;
				oldBatchRemoved = batchRemoved;
				oldBatchChanged = batchChanged;
				oldChangedFlag = changedFlag;
				batchAdded = null;
				batchRemoved = null;
				batchChanged = null;
				changedFlag = false;
			}
			if (res == 0) {
				if (oldBatchAdded != null) {
					foreach (PinnedWatch w in oldBatchAdded)
						OnWatchAdded (w);
				}
				if (oldBatchChanged != null) {
					foreach (PinnedWatch w in oldBatchChanged)
						OnWatchChanged (w);
				}
				if (oldBatchRemoved != null) {
					foreach (PinnedWatch w in oldBatchRemoved)
						OnWatchRemoved (w);
				}
				if (oldChangedFlag)
					OnChanged ();
			}
		}
		
		List<PinnedWatch> batchAdded;
		List<PinnedWatch> batchRemoved;
		List<PinnedWatch> batchChanged;
		bool changedFlag;
		int batchUpdate;
		
		void OnWatchAdded (PinnedWatch watch)
		{
			if (batchUpdate > 0) {
				if (batchAdded == null)
					batchAdded = new List<PinnedWatch> ();
				if (!batchAdded.Contains (watch))
					batchAdded.Add (watch);
				return;
			}
			if (WatchAdded != null)
				WatchAdded (this, new PinnedWatchEventArgs (watch));
		}
		
		void OnWatchRemoved (PinnedWatch watch)
		{
			if (batchUpdate > 0) {
				if (batchRemoved == null)
					batchRemoved = new List<PinnedWatch> ();
				if (!batchRemoved.Contains (watch))
					batchRemoved.Add (watch);
				return;
			}
			if (WatchRemoved != null)
				WatchRemoved (this, new PinnedWatchEventArgs (watch));
		}
		
		void OnWatchChanged (PinnedWatch watch)
		{
			if (batchUpdate > 0) {
				if (batchChanged == null)
					batchChanged = new List<PinnedWatch> ();
				if (!batchChanged.Contains (watch))
					batchChanged.Add (watch);
				return;
			}
			Runtime.RunInMainThread (() => {
				if (WatchChanged != null)
					WatchChanged (this, new PinnedWatchEventArgs (watch));
			});
		}
		
		void OnChanged ()
		{
			if (batchUpdate > 0) {
				changedFlag = true;
				return;
			}
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		internal void NotifyWatchChanged (PinnedWatch watch)
		{
			OnWatchChanged (watch);
			OnChanged ();
		}
		
		public event EventHandler Changed;
		public event EventHandler<PinnedWatchEventArgs> WatchAdded;
		public event EventHandler<PinnedWatchEventArgs> WatchRemoved;
		public event EventHandler<PinnedWatchEventArgs> WatchChanged;
	}
	
	public class PinnedWatchEventArgs: EventArgs
	{
		PinnedWatch watch;
		
		public PinnedWatchEventArgs (PinnedWatch watch)
		{
			this.watch = watch;
		}

		public PinnedWatch Watch {
			get { return watch; }
		}
	}
}

