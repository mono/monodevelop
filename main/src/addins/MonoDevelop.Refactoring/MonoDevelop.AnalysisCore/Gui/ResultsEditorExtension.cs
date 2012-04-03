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
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Content;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.SourceEditor.QuickTasks;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.AnalysisCore.Gui
{
	public class ResultsEditorExtension : TextEditorExtension, IQuickTaskProvider
	{
		bool disposed;
		
		public override void Initialize ()
		{
			base.Initialize ();

			AnalysisOptions.AnalysisEnabled.Changed += AnalysisOptionsChanged;
			AnalysisOptionsChanged (null, null);
		}

		void AnalysisOptionsChanged (object sender, EventArgs e)
		{
			Enabled = AnalysisOptions.AnalysisEnabled;
		}
		
		public override void Dispose ()
		{
			if (!disposed) {
				AnalysisOptions.AnalysisEnabled.Changed -= AnalysisOptionsChanged;
				CancelTask ();
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

		void CancelTask ()
		{
			if (src != null) {
				src.Cancel ();
				oldTask.Wait ();
			}
		}
		
		void Disable ()
		{
			Document.DocumentParsed -= OnDocumentParsed;
			CancelTask ();
			new ResultsUpdater (this, new Result[0], CancellationToken.None).Update ();
		}
		
		Task oldTask;
		CancellationTokenSource src = null;
		//FIXME: rate-limit this, so we don't send multiple new documents while it's processing
		void OnDocumentParsed (object sender, EventArgs args)
		{
			if (!QuickTaskStrip.EnableFancyFeatures)
				return;
			var doc = Document.ParsedDocument;
			if (doc == null)
				return;
			if (src != null) {
				src.Cancel ();
				oldTask.Wait ();
			}
			src = new CancellationTokenSource ();
			var treeType = new RuleTreeType ("Document", Path.GetExtension (doc.FileName));
			var task = AnalysisService.QueueAnalysis (Document, treeType);
			oldTask = task.ContinueWith (t => new ResultsUpdater (this, t.Result, src.Token).Update ());
		}
		
		class ResultsUpdater 
		{
			readonly ResultsEditorExtension ext;
			readonly CancellationToken cancellationToken;
			
			//the number of markers at the head of the queue that need tp be removed
			int oldMarkers;
			IEnumerator<Result> enumerator;
			
			public ResultsUpdater (ResultsEditorExtension ext, IEnumerable<Result> results, CancellationToken cancellationToken)
			{
				if (ext == null)
					throw new ArgumentNullException ("ext");
				if (results == null)
					throw new ArgumentNullException ("results");
				this.ext = ext;
				this.cancellationToken = cancellationToken;
				this.oldMarkers = ext.markers.Count;
				enumerator = ((IEnumerable<Result>)results).GetEnumerator ();
			}
			
			public void Update ()
			{
				if (!QuickTaskStrip.EnableFancyFeatures)
					return;
				ext.tasks.Clear ();
				GLib.Idle.Add (IdleHandler);
			}
			
			//this runs as a glib idle handler so it can add/remove text editor markers
			//in order to to block the GUI thread, we batch them in UPDATE_COUNT
			bool IdleHandler ()
			{
				if (cancellationToken.IsCancellationRequested)
					return false;
				var editor = ext.Editor;
				if (editor == null || editor.Document == null)
					return false;
				//clear the old results out at the same rate we add in the new ones
				for (int i = 0; oldMarkers > 0 && i < UPDATE_COUNT; i++) {
					if (cancellationToken.IsCancellationRequested)
						return false;
					editor.Document.RemoveMarker (ext.markers.Dequeue ());
					oldMarkers--;
				}
				//add in the new markers
				for (int i = 0; i < UPDATE_COUNT; i++) {
					if (!enumerator.MoveNext ()) {
						ext.OnTasksUpdated (EventArgs.Empty);
						return false;
					}
					if (cancellationToken.IsCancellationRequested)
						return false;
					var currentResult = (Result)enumerator.Current;
					
					if (currentResult.InspectionMark != IssueMarker.None) {
						var marker = new ResultMarker (currentResult);
						marker.IsVisible = currentResult.Underline;
						editor.Document.AddMarker (marker.Line, marker);
						ext.markers.Enqueue (marker);
					}
					
					ext.tasks.Add (new QuickTask (currentResult.Message, currentResult.Region.Begin, currentResult.Level));
				}
				
				return true;
			}
		}
		
		//all markers known to be in the editor
		Queue<ResultMarker> markers = new Queue<ResultMarker> ();
		
		const int UPDATE_COUNT = 20;
		
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
		
		#endregion
	}
}
