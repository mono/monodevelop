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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.CodeIssues;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;

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
			DocumentContext.DocumentParsed -= OnDocumentParsed;
			CancelUpdateTimout ();
			CancelTask ();
			AnalysisOptions.AnalysisEnabled.Changed -= AnalysisOptionsChanged;
			while (markers.Count > 0)
				Editor.RemoveMarker (markers.Dequeue ());
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
			DocumentContext.DocumentParsed += OnDocumentParsed;
			if (DocumentContext.ParsedDocument != null)
				OnDocumentParsed (null, null);
		}

		void CancelTask ()
		{
			lock (updateLock) {
				if (src != null) {
					src.Cancel ();
				}
			}
		}
		
		void Disable ()
		{
			if (!enabled)
				return;
			enabled = false;
			DocumentContext.DocumentParsed -= OnDocumentParsed;
			CancelTask ();
			new ResultsUpdater (this, new Result[0], CancellationToken.None).Update ();
		}
		
		CancellationTokenSource src = null;
		object updateLock = new object();
		uint updateTimeout = 0;

		void OnDocumentParsed (object sender, EventArgs args)
		{
			if (!AnalysisOptions.EnableFancyFeatures)
				return;
			CancelUpdateTimout ();
			var doc = DocumentContext.ParsedDocument;
			if (doc == null || DocumentContext.IsAdHocProject)
				return;
			updateTimeout = GLib.Timeout.Add (250, delegate {
				lock (updateLock) {
					CancelTask ();
					src = new CancellationTokenSource ();
					var token = src.Token;
					var ad = new AnalysisDocument (Editor, DocumentContext);
					Task.Run (async () => {
						try {
							var result = await CodeDiagnosticRunner.Check (ad, token);
							if (token.IsCancellationRequested)
								return;
							var updater = new ResultsUpdater (this, result, token);
							updater.Update ();
						} catch (Exception) {
						}
					});
					updateTimeout = 0;
					return false;
				}
			});
		}

		public void CancelUpdateTimout ()
		{
			if (updateTimeout != 0) {
				GLib.Source.Remove (updateTimeout);
				updateTimeout = 0;
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
			
			public ResultsUpdater (ResultsEditorExtension ext, IEnumerable<Result> results, CancellationToken cancellationToken)
			{
				if (ext == null)
					throw new ArgumentNullException ("ext");
				if (results == null)
					throw new ArgumentNullException ("results");
				this.ext = ext;
				this.cancellationToken = cancellationToken;
				this.oldMarkers = ext.markers.Count;
				builder = ImmutableArray<QuickTask>.Empty.ToBuilder ();
				enumerator = ((IEnumerable<Result>)results).GetEnumerator ();
			}
			
			public void Update ()
			{
				if (!AnalysisOptions.EnableFancyFeatures || cancellationToken.IsCancellationRequested)
					return;
				ext.tasks = ext.tasks.Clear ();
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

			//this runs as a glib idle handler so it can add/remove text editor markers
			//in order to to block the GUI thread, we batch them in UPDATE_COUNT
			bool IdleHandler ()
			{
				if (cancellationToken.IsCancellationRequested)
					return false;
				var editor = ext.Editor;
				if (editor == null)
					return false;
				//clear the old results out at the same rate we add in the new ones
				for (int i = 0; oldMarkers > 0 && i < UPDATE_COUNT; i++) {
					if (cancellationToken.IsCancellationRequested)
						return false;
					editor.RemoveMarker (ext.markers.Dequeue ());
					oldMarkers--;
				}
				//add in the new markers
				for (int i = 0; i < UPDATE_COUNT; i++) {
					if (!enumerator.MoveNext ()) {
						ext.tasks = builder.ToImmutable ();
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
						if (currentResult.InspectionMark == IssueMarker.GrayOut) {
							var marker = TextMarkerFactory.CreateGenericTextSegmentMarker (editor, TextSegmentMarkerEffect.GrayOut, TextSegment.FromBounds (start, end));
							marker.IsVisible = currentResult.Underline;
							marker.Tag = currentResult;
							editor.AddMarker (marker);
							ext.markers.Enqueue (marker);
//							editor.Parent.TextViewMargin.RemoveCachedLine (editor.GetLineByOffset (start));
//							editor.Parent.QueueDraw ();
						} else {
							var effect = currentResult.InspectionMark == IssueMarker.DottedLine ? TextSegmentMarkerEffect.DottedLine : TextSegmentMarkerEffect.WavedLine;
							var marker = TextMarkerFactory.CreateGenericTextSegmentMarker (editor, effect, TextSegment.FromBounds (start, end));
							marker.Color = GetColor (editor, currentResult);
							marker.IsVisible = currentResult.Underline && currentResult.Level != DiagnosticSeverity.Hidden;
							marker.Tag = currentResult;
							editor.AddMarker (marker);
							ext.markers.Enqueue (marker);
						}
					}
					builder.Add (new QuickTask (currentResult.Message, currentResult.Region.Start, currentResult.Level));
				}
				
				return true;
			}
		}
		
		//all markers known to be in the editor
		Queue<IGenericTextSegmentMarker> markers = new Queue<IGenericTextSegmentMarker> ();
		
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
			return markers.Select (m => m.Tag).OfType<Result> ();
		}

		#region IQuickTaskProvider implementation
		ImmutableArray<QuickTask> tasks = ImmutableArray<QuickTask>.Empty;

		public event EventHandler TasksUpdated;

		protected virtual void OnTasksUpdated (EventArgs e)
		{
			EventHandler handler = this.TasksUpdated;
			if (handler != null)
				handler (this, e);
		}
		
		public ImmutableArray<QuickTask> QuickTasks {
			get {
				return tasks;
			}
		}
		
		#endregion
	}
}
