//
// CommentTasksProvider.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018
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
using System.Threading;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Tasks
{
	static class CommentTasksProvider
	{
		static ITodoListProvider todoListProvider;

		static CommentTasksProvider()
		{
			todoListProvider = Ide.Composition.CompositionManager.GetExportedValue<ITodoListProvider> ();

			todoListProvider.TodoListUpdated += TodoListUpdated;

			Ide.IdeApp.Workspace.SolutionLoaded += OnSolutionLoaded;
			CommentTag.SpecialCommentTagsChanged += OnSpecialTagsChanged;

			TodoListUpdated += (sender, args) => {
				var local = args;
			};
		}

		public static void Initialize ()
		{
		}

		static async void OnSolutionLoaded (object sender, SolutionEventArgs args)
		{
			var sol = args.Solution;

			var ws = await TypeSystemService.GetWorkspaceAsync (sol);
			UpdateWorkspaceOptions (ws);

			foreach (var ea in todoListProvider.GetTodoItemsUpdatedEventArgs (ws, CancellationToken.None)) {
				// Do something.
			}
		}

		static void UpdateWorkspaceOptions (Microsoft.CodeAnalysis.Workspace ws)
		{
			// Roslyn uses | as separator.
			ws.Options = ws.Options.WithChangedOption (TodoCommentOptions.TokenList, CommentTag.ToString (CommentTag.SpecialCommentTags, "|"));
		}

		static void OnSpecialTagsChanged (object sender, EventArgs args)
		{
			foreach (var ws in TypeSystemService.AllWorkspaces)
				UpdateWorkspaceOptions (ws);
		}

		public static event EventHandler<TodoItemsUpdatedArgs> TodoListUpdated;

		public static ImmutableArray<TodoItem> GetTodoItems (Microsoft.CodeAnalysis.Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
		{
			return todoListProvider.GetTodoItems (workspace, documentId, cancellationToken);
		}
	}
}
