// 
// ResultsEditorExtension.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.SourceEditor;

namespace MonoDevelop.AnalysisCore.Gui
{
	public class ResultsEditorExtension : TextEditorExtension, IQuickTaskProvider
	{
		bool disposed;
		
		public override void Initialize ()
		{
			base.Initialize ();
			AnalysisOptions.Changed += AnalysisOptionsChanged;
			AnalysisOptionsChanged (null, null);
		}

		void AnalysisOptionsChanged (object sender, EventArgs e)
		{
			Enabled = AnalysisOptions.AnalysisEnabled;
		}
		
		public override void Dispose ()
		{
			if (!disposed) {
				AnalysisOptions.Changed -= AnalysisOptionsChanged;
				Enabled = false;
				disposed = true;
			}
			base.Dispose ();
		}
		
		bool enabled;
		
		public bool Enabled {
			get { return enabled; }
			set {
				if (enabled != value) {
					enabled = value;
					if (value)
						Enable ();
					else
						Disable ();
				}
			}
		}
		
		void Enable ()
		{
			Document.DocumentParsed += OnDocumentParsed;
			if (Document.ParsedDocument != null)
				OnDocumentParsed (null, null);
		}
		
		void Disable ()
		{
			Document.DocumentParsed -= OnDocumentParsed;
			UpdateResults (new Result[0]);
		}
		
		//FIXME: rate-limit this, so we don't send multiple new documents while it's processing
		void OnDocumentParsed (object sender, EventArgs args)
		{
			var doc = Document.ParsedDocument;
			if (doc == null)
				return;
			var treeType = new RuleTreeType ("Document", Path.GetExtension (doc.FileName));
			AnalysisService.QueueAnalysis (Document, treeType, UpdateResults);
		}
		
		void UpdateResults (IList<Result> results)
		{
			lock (updaterLock) {
				if (!Enabled && results.Count > 0)
					return;
				nextResults = results;
				if (!updaterRunning) {
					GLib.Idle.Add (ResultsUpdater);
					updaterRunning = true;
				}
			}
		}
		
		object updaterLock = new object ();
		
		//protected by lock. This is how we hand new results over to ResultsUpdater from the callback.
		bool updaterRunning;
		IList<Result> nextResults;
		
		//only accessed by ResultsUpdater. This is the list it's using to update the text editor.
		int updateIndex = 0;
		IList<Result> currentResults;
		
		//the number of markers at the head of the queue that need tp be removed
		int oldMarkers = 0;
		
		//all markers known to be in the editor
		Queue<ResultMarker> markers = new Queue<ResultMarker> ();
		
		const int UPDATE_COUNT = 20;
		
		//this runs as a glib idle handler so it can add/remove text editor markers
		//in order to to block the GUI thread, we batch them in UPDATE_COUNT
		bool ResultsUpdater ()
		{
			lock (updaterLock) {
				if (nextResults != null) {
					currentResults = nextResults;
					nextResults = null;
					updateIndex = 0;
					oldMarkers += markers.Count;
				}
				//stop the updater when we're done updating results
				if (currentResults.Count == updateIndex && oldMarkers == 0) {
					currentResults = null;
					updaterRunning = false;
					UpdateQuickTasks ();
					return false;
				}
			}
			
			if (Editor == null || Editor.Document == null) 
				return false; // prevents endless idle loop
			
			//clear the old results out at the same rate we add in the new ones
			for (int i = 0; oldMarkers > 0 && i < UPDATE_COUNT; i++) {
				Editor.Document.RemoveMarker (markers.Dequeue ());
				oldMarkers--;
			}
			
			//add in the new markers
			int targetIndex = updateIndex + UPDATE_COUNT;
			for (; updateIndex < targetIndex && updateIndex < currentResults.Count; updateIndex++) {
				var marker = new ResultMarker (currentResults [updateIndex]);
				marker.IsVisible = currentResults [updateIndex].Underline;
				Editor.Document.AddMarker (marker.Line, marker);
				markers.Enqueue (marker);
			}
			return true;
		}
		
		public IList<Result> GetResultsAtOffset (int offset)
		{
			var location = Editor.Document.OffsetToLocation (offset);
			var line = Editor.GetLineByOffset (offset);
			
			var list = new List<Result> ();
			foreach (var marker in line.Markers) {
				var resultMarker = marker as ResultMarker;
				if (resultMarker == null || resultMarker.Line != location.Line)
					continue;
				int cs = resultMarker.ColStart, ce = resultMarker.ColEnd;
				if ((cs >= 0 && cs > location.Column) || (ce >= 0 && ce < location.Column))
					continue;
				list.Add (resultMarker.Result);
			}
			return list;
		}
		
		public IEnumerable<Result> GetResults ()
		{
			return markers.Select (m => m.Result);
		}

		#region IQuickTaskProvider implementation
		List<QuickTask> tasks = new List<QuickTask> ();

		public event EventHandler TasksUpdated;

		protected virtual void OnTasksUpdated (EventArgs e)
		{
			EventHandler handler = this.TasksUpdated;
			if (handler != null)
				handler (this, e);
		}
		
		public IEnumerable<QuickTask> QuickTasks {
			get {
				return tasks;
			}
		}
		
		void UpdateQuickTasks ()
		{
			tasks.Clear ();
			foreach (var result in GetResults ()) {
				var newTask = new QuickTask (result.Message, result.Region.Start, result.Level);
				tasks.Add (newTask);
			}
			
			OnTasksUpdated (EventArgs.Empty);
		}
		
		#endregion
	}
}
