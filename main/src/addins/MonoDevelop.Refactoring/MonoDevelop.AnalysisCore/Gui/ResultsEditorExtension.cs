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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.AnalysisCore;
using MonoDevelop.AnalysisCore.Gui;
using MonoDevelop.CodeActions;
using MonoDevelop.CodeIssues;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Gui;


namespace MonoDevelop.AnalysisCore.Gui
{
	class AnalysisDocument
	{
		public TextEditor Editor { get; private set; }
		public DocumentLocation CaretLocation { get; private set; }
		public DocumentContext DocumentContext { get; private set; }

		public AnalysisDocument (TextEditor editor, DocumentContext documentContext)
		{
			this.Editor = editor;
			this.CaretLocation = editor.CaretLocation;
			this.DocumentContext = documentContext;
		}
	}

	public class ResultsEditorExtension : TextEditorExtension, IQuickTaskProvider
	{
		bool disposed;
		static IDiagnosticService diagService = Ide.Composition.CompositionManager.GetExportedValue<IDiagnosticService> ();
		
		protected override void Initialize ()
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
			if (disposed) 
				return;
			enabled = false;
			diagService.DiagnosticsUpdated -= OnDiagnosticsUpdated;
			CancelUpdateTimout ();
			AnalysisOptions.AnalysisEnabled.Changed -= AnalysisOptionsChanged;
			foreach (var queue in markers) {
				while (queue.Value.Count > 0)
					Editor.RemoveMarker (queue.Value.Dequeue ());
			}
			disposed = true;
			base.Dispose ();
		}
		
		bool enabled;
		
		public bool Enabled {
			get { return enabled; }
			set {
				if (enabled != value) {
					if (value)
						Enable ();
					else
						Disable ();
				}
			}
		}
		
		void Enable ()
		{
			if (enabled)
				return;
			enabled = true;

			diagService.DiagnosticsUpdated += OnDiagnosticsUpdated;
			if (DocumentContext.ParsedDocument != null)
				UpdateInitialDiagnostics ();
		}
		
		void Disable ()
		{
			if (!enabled)
				return;
			enabled = false;
			diagService.DiagnosticsUpdated -= OnDiagnosticsUpdated;
			CancelUpdateTimout ();
			new ResultsUpdater (this, new Result[0], null, CancellationToken.None).Update ();
		}
		
		CancellationTokenSource src = new CancellationTokenSource ();
		object updateLock = new object();

		void UpdateInitialDiagnostics ()
		{
			if (!AnalysisOptions.EnableFancyFeatures)
				return;

			var doc = DocumentContext.ParsedDocument;
			if (doc == null || DocumentContext.IsAdHocProject)
				return;

			var ad = new AnalysisDocument (Editor, DocumentContext);

			Task.Run (() => {
				var ws = DocumentContext.RoslynWorkspace;
				var project = DocumentContext.AnalysisDocument.Project.Id;
				var document = DocumentContext.AnalysisDocument.Id;

				// Force an initial diagnostic update from the engine.
				foreach (var updateArgs in diagService.GetDiagnosticsUpdatedEventArgs (ws, project, document, src.Token)) {
					var diagnostics = AdjustInitialDiagnostics (DocumentContext.AnalysisDocument.Project.Solution, updateArgs, src.Token);
					if (diagnostics.Length == 0) {
						continue;
					}

					var e = DiagnosticsUpdatedArgs.DiagnosticsCreated (
						updateArgs.Id, updateArgs.Workspace, DocumentContext.AnalysisDocument.Project.Solution, updateArgs.ProjectId, updateArgs.DocumentId, diagnostics);

					OnDiagnosticsUpdated (this, e);
				}
			});
		}

		private ImmutableArray<DiagnosticData> AdjustInitialDiagnostics (
				Solution solution, UpdatedEventArgs args, CancellationToken cancellationToken)
		{
			// we only reach here if there is the document
			var document = solution.GetDocument (args.DocumentId);
			// if there is no source text for this document, we don't populate the initial tags. this behavior is equivalent of existing
			// behavior in OnDiagnosticsUpdated.
			if (!document.TryGetText (out var text)) {
				return ImmutableArray<DiagnosticData>.Empty;
			}

			// GetDiagnostics returns whatever cached diagnostics in the service which can be stale ones. for example, build error will be most likely stale
			// diagnostics. so here we make sure we filter out any diagnostics that is not in the text range.
			var builder = ArrayBuilder<DiagnosticData>.GetInstance ();
			var fullSpan = new TextSpan (0, text.Length);
			foreach (var diagnostic in diagService.GetDiagnostics (
				args.Workspace, args.ProjectId, args.DocumentId, args.Id, includeSuppressedDiagnostics: false, cancellationToken: cancellationToken)) {
				if (fullSpan.Contains (diagnostic.GetExistingOrCalculatedTextSpan (text))) {
					builder.Add (diagnostic);
				}
			}

			return builder.ToImmutableAndFree ();
		}

		async void OnDiagnosticsUpdated (object sender, DiagnosticsUpdatedArgs e)
		{
			if (!enabled)
				return;

			var doc = DocumentContext.ParsedDocument;
			if (doc == null || DocumentContext.IsAdHocProject)
				return;

			if (DocumentContext.AnalysisDocument == null)
				return;
			
			if (e.DocumentId != DocumentContext.AnalysisDocument.Id || e.ProjectId != DocumentContext.AnalysisDocument.Project.Id)
				return;

			var token = CancelUpdateTimeout (e.Id);
			var ad = new AnalysisDocument (Editor, DocumentContext);
			try {
				var result = await CodeDiagnosticRunner.Check (ad, token, e.Diagnostics).ConfigureAwait (false);
				var updater = new ResultsUpdater (this, result, e.Id, token);
				updater.Update ();
			} catch (Exception) {
			}
		}

		public void CancelUpdateTimout ()
		{
			lock (cancellations) {
				foreach (var cts in cancellations)
					cts.Value.Cancel ();
				cancellations.Clear ();
			}
			
			src?.Cancel ();
			src = new CancellationTokenSource ();
		}

		CancellationToken CancelUpdateTimeout (object id)
		{
			lock (cancellations) {
				if (cancellations.TryGetValue (id, out var cts))
					cts.Cancel ();

				cancellations[id] = cts = new CancellationTokenSource ();
				return cts.Token;
			}
		}


		class ResultsUpdater 
		{
			readonly ResultsEditorExtension ext;
			readonly CancellationToken cancellationToken;
			
			//the number of markers at the head of the queue that need tp be removed
			int oldMarkers;
			IEnumerator<Result> enumerator;
			ImmutableArray<QuickTask>.Builder builder;
			object id;
			
			public ResultsUpdater (ResultsEditorExtension ext, IEnumerable<Result> results, object resultsId, CancellationToken cancellationToken)
			{
				if (ext == null)
					throw new ArgumentNullException ("ext");
				if (results == null)
					throw new ArgumentNullException ("results");
				this.ext = ext;
				id = resultsId;
				this.cancellationToken = cancellationToken;

				Queue<IGenericTextSegmentMarker> oldMarkers;
				if (resultsId != null) {
					if (!ext.markers.TryGetValue (id, out oldMarkers))
						ext.markers [id] = oldMarkers = new Queue<IGenericTextSegmentMarker> ();
					this.oldMarkers = oldMarkers.Count;
				}
				
				builder = ImmutableArray<QuickTask>.Empty.ToBuilder ();
				enumerator = results.GetEnumerator ();
			}
			
			public void Update ()
			{
				if (cancellationToken.IsCancellationRequested)
					return;
				if (id != null)
					ext.tasks.Remove (id);
				GLib.Idle.Add (IdleHandler);
			}

			static Cairo.Color GetColor (TextEditor editor, Result result)
			{
				switch (result.Level) {
				case DiagnosticSeverity.Hidden:
					return SyntaxHighlightingService.GetColor (DefaultSourceEditorOptions.Instance.GetEditorTheme (), EditorThemeColors.Background);
				case DiagnosticSeverity.Error:
					return SyntaxHighlightingService.GetColor (DefaultSourceEditorOptions.Instance.GetEditorTheme (), EditorThemeColors.UnderlineError);
				case DiagnosticSeverity.Warning:
					return SyntaxHighlightingService.GetColor (DefaultSourceEditorOptions.Instance.GetEditorTheme (), EditorThemeColors.UnderlineWarning);
				case DiagnosticSeverity.Info:
					return SyntaxHighlightingService.GetColor (DefaultSourceEditorOptions.Instance.GetEditorTheme (), EditorThemeColors.UnderlineSuggestion);
				default:
					throw new System.ArgumentOutOfRangeException ();
				}
			}

			static TextSegmentMarkerEffect GetSegmentMarkerEffect (IssueMarker marker)
			{
				if (marker == IssueMarker.GrayOut)
					return TextSegmentMarkerEffect.GrayOut;
				if (marker == IssueMarker.DottedLine)
					return TextSegmentMarkerEffect.DottedLine;
				return TextSegmentMarkerEffect.WavedLine;
			}

			//this runs as a glib idle handler so it can add/remove text editor markers
			//in order to to block the GUI thread, we batch them in UPDATE_COUNT
			bool IdleHandler ()
			{
				if (cancellationToken.IsCancellationRequested)
					return false;
				var editor = ext.Editor;
				if (editor == null)
					return false;

				if (id == null) {
					foreach (var markerQueue in ext.markers) {
						while (markerQueue.Value.Count != 0)
							editor.RemoveMarker (markerQueue.Value.Dequeue ());
					}
					ext.markers.Clear ();
					ext.tasks.Clear ();
					ext.OnTasksUpdated (EventArgs.Empty);
					return false;
				}

				//clear the old results out at the same rate we add in the new ones
				for (int i = 0; oldMarkers > 0 && i < UPDATE_COUNT; i++) {
					if (cancellationToken.IsCancellationRequested)
						return false;
					editor.RemoveMarker (ext.markers [id].Dequeue ());
					oldMarkers--;
				}

				//add in the new markers
				for (int i = 0; i < UPDATE_COUNT; i++) {
					if (!enumerator.MoveNext ()) {
						ext.tasks [id] = builder.ToImmutable ();
						ext.OnTasksUpdated (EventArgs.Empty);
						return false;
					}
					if (cancellationToken.IsCancellationRequested)
						return false;
					var currentResult = (Result)enumerator.Current;
					if (currentResult.InspectionMark != IssueMarker.None) {
						int start = currentResult.Region.Start;
						int end = currentResult.Region.End;
						if (start >= end)
							continue;
						var marker = TextMarkerFactory.CreateGenericTextSegmentMarker (editor, GetSegmentMarkerEffect (currentResult.InspectionMark), TextSegment.FromBounds (start, end));
						marker.Tag = currentResult;
						marker.IsVisible = currentResult.Underline;

						if (currentResult.InspectionMark != IssueMarker.GrayOut) {
							marker.Color = GetColor (editor, currentResult);
							marker.IsVisible &= currentResult.Level != DiagnosticSeverity.Hidden;
						}
						editor.AddMarker (marker);
						ext.markers [id].Enqueue (marker);
					}
					builder.Add (new QuickTask (currentResult.Message, currentResult.Region.Start, currentResult.Level));
				}
				
				return true;
			}
		}
		
		//all markers known to be in the editor
		// Roslyn groups diagnostics by their provider. In this case, we rely on the id passed in to group markers by their id.
		Dictionary<object, Queue<IGenericTextSegmentMarker>> markers = new Dictionary<object, Queue<IGenericTextSegmentMarker>> ();
		Dictionary<object, CancellationTokenSource> cancellations = new Dictionary<object, CancellationTokenSource> ();
		
		const int UPDATE_COUNT = 20;
		
		public IList<Result> GetResultsAtOffset (int offset, CancellationToken token = default (CancellationToken))
		{
//			var location = Editor.Document.OffsetToLocation (offset);
//			var line = Editor.GetLineByOffset (offset);
			
			var list = new List<Result> ();
			foreach (var marker in Editor.GetTextSegmentMarkersAt (offset)) {
				if (token.IsCancellationRequested)
					break;
				var resultMarker = marker as IGenericTextSegmentMarker;
				if (resultMarker != null && resultMarker.Tag is Result)
					list.Add (resultMarker.Tag as Result);
			}
			return list;
		}
		
		public IEnumerable<Result> GetResults ()
		{
			return markers.SelectMany (x => x.Value).Select (m => m.Tag).OfType<Result> ();
		}

		#region IQuickTaskProvider implementation
		Dictionary<object, ImmutableArray<QuickTask>> tasks = new Dictionary<object, ImmutableArray<QuickTask>> ();

		public event EventHandler TasksUpdated;

		protected virtual void OnTasksUpdated (EventArgs e)
		{
			EventHandler handler = this.TasksUpdated;
			if (handler != null)
				handler (this, e);
		}
		
		public ImmutableArray<QuickTask> QuickTasks {
			get {
				return tasks.SelectMany(x => x.Value).AsImmutable ();
			}
		}
		
		#endregion
	}
}
