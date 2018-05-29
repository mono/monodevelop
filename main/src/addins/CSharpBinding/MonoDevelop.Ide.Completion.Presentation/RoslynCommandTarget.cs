//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using MonoDevelop.Ide.Composition;
using EC = Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace MonoDevelop.Ide.Completion.Presentation
{
	public class RoslynCommandTarget
	{
		internal ICommandHandlerService CurrentRoslynHandlers { get; set; }
		internal IEditorCommandHandlerService CurrentEditorHandlers { get; set; }

		internal ITextBuffer _languageBuffer;
		internal ITextView _textView;

		private RoslynCommandTarget (ITextView textView, ITextBuffer languageBuffer)
		{
			var commandHandlerServiceFactory = CompositionManager.GetExportedValue<ICommandHandlerServiceFactory> ();
			if (commandHandlerServiceFactory != null) {
				commandHandlerServiceFactory.Initialize (languageBuffer.ContentType.TypeName);
				CurrentRoslynHandlers = commandHandlerServiceFactory.GetService (languageBuffer);
			}

			var editorCommandHandlerServiceFactory = CompositionManager.GetExportedValue<IEditorCommandHandlerServiceFactory> ();
			if (editorCommandHandlerServiceFactory != null) {
				CurrentEditorHandlers = editorCommandHandlerServiceFactory.GetService (textView, languageBuffer);
			}

			_languageBuffer = languageBuffer;
			_textView = textView;
		}

		public static RoslynCommandTarget FromViewAndBuffer (ITextView textView, ITextBuffer languageBuffer)
		{
			return languageBuffer.Properties.GetOrCreateSingletonProperty<RoslynCommandTarget> (() => new RoslynCommandTarget (textView, languageBuffer));
		}

		public void ExecuteTypeCharacter (char typedChar, Action lastHandler)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.TypeCharCommandArgs (view, buffer, typedChar),
				lastHandler);
			};

			CurrentRoslynHandlers?.Execute (_languageBuffer.ContentType,
				args: new TypeCharCommandArgs (_textView, _languageBuffer, typedChar),
				lastHandler: action);
		}

		public void ExecuteTab (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.TabKeyCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new TabKeyCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}

		public void ExecuteBackspace (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.BackspaceKeyCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new BackspaceKeyCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}

		public void ExecuteDelete (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.DeleteKeyCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new DeleteKeyCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}

		public void ExecuteReturn (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.ReturnKeyCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new ReturnKeyCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}

		public void ExecuteUp (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.UpKeyCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new UpKeyCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}

		public void ExecuteDown (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.DownKeyCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new DownKeyCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}

		public void ExecuteUncommentBlock (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.UncommentSelectionCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new UncommentSelectionCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}

		public void ExecuteCommentBlock (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.CommentSelectionCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new CommentSelectionCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}

		public void ExecuteInvokeCompletionList (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.InvokeCompletionListCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new InvokeCompletionListCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}

		public void ExecuteCommitUniqueCompletionListItemCommand (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.CommitUniqueCompletionListItemCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new CommitUniqueCompletionListItemCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}

		public void ExecuteEscapeKeyCommandArgs (Action executeNextCommandTarget)
		{
			Action action = () => {
				CurrentEditorHandlers?.Execute ((view, buffer) => new EC.EscapeKeyCommandArgs (view, buffer),
				executeNextCommandTarget);
			};

			CurrentRoslynHandlers.Execute (_languageBuffer.ContentType,
				args: new EscapeKeyCommandArgs (_textView, _languageBuffer),
				lastHandler: action);
		}
	}
}