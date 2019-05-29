//
// TagCommentsTextEditorExtension.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Editor.Extension
{
	[Obsolete]
	class TagCommentsTextEditorExtension : TextEditorExtension, IQuickTaskProvider
	{
		ITodoListProvider todoListProvider = CompositionManager.Instance.GetExportedValue<ITodoListProvider> ();
		CancellationTokenSource src = new CancellationTokenSource ();
		bool isDisposed;

		protected override void Initialize ()
		{
			todoListProvider.TodoListUpdated += OnTodoListUpdated;
		}

		public override void Dispose ()
		{
			if (isDisposed)
				return;
			isDisposed = true;

			this.tasks = ImmutableArray<QuickTask>.Empty;
			OnTasksUpdated (EventArgs.Empty);
			todoListProvider.TodoListUpdated -= OnTodoListUpdated;
			base.Dispose ();
		}

		#region IQuickTaskProvider implementation
		ImmutableArray<QuickTask> tasks = ImmutableArray<QuickTask>.Empty;

		public event EventHandler TasksUpdated;

		protected virtual void OnTasksUpdated (EventArgs e) => TasksUpdated?.Invoke (this, e);
		public ImmutableArray<QuickTask> QuickTasks => tasks;

		void OnTodoListUpdated (object sender, TodoItemsUpdatedArgs args)
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
			var token = src.Token;
			if (DocumentContext.AnalysisDocument == null || DocumentContext.AnalysisDocument.Id != args.DocumentId)
				return;

			var ws = args.Workspace as MonoDevelopWorkspace;
			if (ws == null) {
				// could be WebEditorRoslynWorkspace
				return;
			}

			var doc = ws.GetDocument (args.DocumentId);
			if (doc == null)
				return;

			var project = ws.GetMonoProject (args.ProjectId);
			if (project == null)
				return;

			var newTasks = ImmutableArray.CreateBuilder<QuickTask> (args.TodoItems.Length);
			Runtime.RunInMainThread (() => {
				foreach (var todoItem in args.TodoItems) {
					if (token.IsCancellationRequested)
						return;

					var offset = Editor.LocationToOffset (todoItem.MappedLine + 1, todoItem.MappedColumn + 1);
					var newTask = new QuickTask (todoItem.Message, offset, DiagnosticSeverity.Info);
					newTasks.Add (newTask);
				}

				if (token.IsCancellationRequested || isDisposed)
					return;
				tasks = newTasks.MoveToImmutable ();
				OnTasksUpdated (EventArgs.Empty);
			});
		}
		#endregion
	}
}
