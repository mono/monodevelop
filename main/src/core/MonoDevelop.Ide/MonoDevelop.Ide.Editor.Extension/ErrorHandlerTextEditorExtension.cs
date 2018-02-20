//
// ErrorHandlerTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Editor.Extension
{
	class ErrorHandlerTextEditorExtension: TextEditorExtension, IQuickTaskProvider
	{
		CancellationTokenSource src = new CancellationTokenSource ();
		bool isDisposed;

		protected override void Initialize ()
		{
			DocumentContext.DocumentParsed += DocumentContext_DocumentParsed;
		}

		public override void Dispose ()
		{
			if (isDisposed)
				return;
			isDisposed = true;

			this.tasks = ImmutableArray<QuickTask>.Empty;
			OnTasksUpdated (EventArgs.Empty);

			RemoveErrorUnderlines ();
			RemoveErrorUndelinesResetTimerId ();
			CancelDocumentParsedUpdate ();
			DocumentContext.DocumentParsed -= DocumentContext_DocumentParsed;
			base.Dispose ();
		}

		void CancelDocumentParsedUpdate ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}

		void DocumentContext_DocumentParsed (object sender, EventArgs e)
		{
			CancelDocumentParsedUpdate ();
			var token = src.Token;
			var caretLocation = Editor.CaretLocation;
			var parsedDocument = DocumentContext.ParsedDocument;
			Task.Run (async () => {
				try {
					var ctx = DocumentContext;
					if (ctx == null)
						return;

					var docErrors = await parsedDocument.GetErrorsAsync (token).ConfigureAwait (false);
					token.ThrowIfCancellationRequested ();

					UpdateErrorUnderlines (ctx, parsedDocument, docErrors, token);
					token.ThrowIfCancellationRequested ();
					await UpdateQuickTasks (ctx, parsedDocument, docErrors, token).ConfigureAwait (false);
				} catch (OperationCanceledException) {
					// ignore
				}
			}, token);
		}

		#region Error handling
		List<IErrorMarker> errors = new List<IErrorMarker> ();
		uint resetTimerId;

		void RemoveErrorUndelinesResetTimerId ()
		{
			if (resetTimerId > 0) {
				GLib.Source.Remove (resetTimerId);
				resetTimerId = 0;
			}
		}

		void RemoveErrorUnderlines ()
		{
			errors.ForEach (err => Editor.RemoveMarker (err));
			errors.Clear ();
		}

		void UnderLineError (Error info)
		{
			var error = TextMarkerFactory.CreateErrorMarker (Editor, info);
			Editor.AddMarker (error); 
			errors.Add (error);
		}


		void UpdateErrorUnderlines (DocumentContext ctx, ParsedDocument parsedDocument, IReadOnlyList<Error> docErrors, CancellationToken token)
		{
			if (parsedDocument == null || isDisposed)
				return;
			try {
				Application.Invoke ((o, args) => {
					if (token.IsCancellationRequested || isDisposed)
						return;
					RemoveErrorUndelinesResetTimerId ();
					const uint timeout = 500;
					resetTimerId = GLib.Timeout.Add (timeout, delegate {
						if (token.IsCancellationRequested) {
							resetTimerId = 0;
							return false;
						}
						RemoveErrorUnderlines ();
						// Else we underline the error
						if (docErrors != null) {
							foreach (var error in docErrors) {
								UnderLineError (error);
							}
						}
						resetTimerId = 0;
						return false;
					});
				});
			} catch (OperationCanceledException) {
				// ignore
			}
		}
		#endregion

		#region IQuickTaskProvider implementation
		ImmutableArray<QuickTask> tasks = ImmutableArray<QuickTask>.Empty;

		public event EventHandler TasksUpdated;

		protected virtual void OnTasksUpdated (EventArgs e)
		{
			TasksUpdated?.Invoke (this, e);
		}

		public ImmutableArray<QuickTask> QuickTasks {
			get {
				return tasks;
			}
		}

		async Task UpdateQuickTasks (DocumentContext ctx, ParsedDocument parsedDocument, IReadOnlyList<Error> docErrors, CancellationToken token)
		{
			if (isDisposed)
				return;
			var newTasks = ImmutableArray<QuickTask>.Empty.ToBuilder ();
			if (parsedDocument != null) {
				foreach (var cmt in await parsedDocument.GetTagCommentsAsync(token).ConfigureAwait (false)) {
					if (token.IsCancellationRequested)
						return;
					int offset;
					try {
						offset = Editor.LocationToOffset (cmt.Region.Begin.Line, cmt.Region.Begin.Column);
					} catch (Exception) {
						return;
					}
					var newTask = new QuickTask (cmt.Text, offset, DiagnosticSeverity.Info);
					newTasks.Add (newTask);
				}

				foreach (var error in docErrors) {
					if (token.IsCancellationRequested)
						return;
					int offset;
					try {
						offset = Editor.LocationToOffset (error.Region.Begin.Line, error.Region.Begin.Column);
					} catch (Exception) {
						return;
					}
					var newTask = new QuickTask (error.Message, offset, error.ErrorType == ErrorType.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning);
					newTasks.Add (newTask);
				}
			}
			if (token.IsCancellationRequested)
				return;
			Application.Invoke ((o, args) => {
				if (token.IsCancellationRequested || isDisposed)
					return;
				tasks = newTasks.ToImmutable ();
				OnTasksUpdated (EventArgs.Empty);
			});
		}

	#endregion

	}
}

