//
// GlobalUndoServiceFactory.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Host.Mef;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.TypeSystem
{
	class GlobalUndoHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			var description = GlobalUndoService.UndoDescription;
			if (description == null) {
				info.Text = GettextCatalog.GetString ("Undo Last Global Action");
				info.Enabled = false;
			} else {
				info.Text = GettextCatalog.GetString ("Undo Global '{0}'", description);
				info.Enabled = true;
			}
		}

		protected override void Run ()
		{
			if (NagScreen ())
				GlobalUndoService.Undo ();
		}

		internal static bool NagScreen ()
		{
			return MessageService.Confirm (GettextCatalog.GetString (@"This operation resets the project state. 
All changes done after the operation get lost.
It is recommended to use version control to prevent data loss.

Are you sure you want to continue?"), AlertButton.Yes);
		}
	}

	class GlobalRedoHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			var description = GlobalUndoService.RedoDescription;
			if (description == null) {
				info.Text = GettextCatalog.GetString ("Redo Last Global Action");
				info.Enabled = false;
			} else {
				info.Text = GettextCatalog.GetString ("Redo Global '{0}'", description);
				info.Enabled = true;
			}
		}

		protected override void Run ()
		{
			if (GlobalUndoHandler.NagScreen())
				GlobalUndoService.Redo ();
		}
	}

	interface IMonoDevelopUndoTransaction : IWorkspaceGlobalUndoTransaction
	{
		void TextReplace (string fileName, int offset, int removedChars, string insertedText);
		void RenameFile (string oldName, string newName);
	}

	/// <summary>
	/// A service that provide a way to undo operations applied to the workspace
	/// </summary>
	[ExportWorkspaceService (typeof (IGlobalUndoService), ServiceLayer.Host), Shared]
	partial class GlobalUndoService : IGlobalUndoService
	{
		static Stack<WorkspaceUndoTransaction> undoStack = new Stack<WorkspaceUndoTransaction> ();
		static Stack<WorkspaceUndoTransaction> redoStack = new Stack<WorkspaceUndoTransaction> ();

		public static string UndoDescription => undoStack.Count == 0 ? null : undoStack.Peek ().Description;
		public static string RedoDescription => redoStack.Count == 0 ? null : redoStack.Peek ().Description;

		public static bool CheckUndo()
		{
			if (undoStack.Count == 0)
				return false;
			return undoStack.Peek ().CheckUndoOperation ();
		}

		public static void Undo ()
		{
			if (undoStack.Count == 0)
				return;
			var undo = undoStack.Pop ();
			undo.UndoOperation ();
			redoStack.Push (undo);
		}

		public static bool CheckRedo()
		{
			if (redoStack.Count == 0)
				return false;
			return redoStack.Peek ().CheckRedoOperation ();
		}

		public static void Redo ()
		{
			if (redoStack.Count == 0)
				return;
			var redo = redoStack.Pop ();
			redo.RedoOperation ();
			undoStack.Push (redo);
		}

		internal int ActiveTransactions;

		bool IGlobalUndoService.CanUndo (Workspace workspace)
		{
			return TypeSystemService.Workspace == workspace;
		}

		bool IGlobalUndoService.IsGlobalTransactionOpen (Workspace workspace)
		{
			return ActiveTransactions > 0;
		}

		IWorkspaceGlobalUndoTransaction IGlobalUndoService.OpenGlobalUndoTransaction (Workspace workspace, string description)
		{
			var transaction = new WorkspaceUndoTransaction (workspace, this, description);
			ActiveTransactions++;
			return transaction;
		}
	}
}