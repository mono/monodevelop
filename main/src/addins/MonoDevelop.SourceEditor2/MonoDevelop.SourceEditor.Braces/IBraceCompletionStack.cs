//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace MonoDevelop.SourceEditor.Braces
{
	using Microsoft.VisualStudio.Text;
	using Microsoft.VisualStudio.Text.BraceCompletion;
	using System.Collections.ObjectModel;

	internal interface IBraceCompletionStack
	{
		/// <summary>
		/// Gets the top most session in the stack.
		/// </summary>
		IBraceCompletionSession TopSession { get; }

		/// <summary>
		/// Adds a session to the top of the stack.
		/// </summary>
		void PushSession (IBraceCompletionSession session);

		/// <summary>
		/// Gets the list of sessions in the stack, ordered from bottom to top.
		/// </summary>
		ReadOnlyObservableCollection<IBraceCompletionSession> Sessions { get; }

		/// <summary>
		/// Remove all sessions which do not contain the given point.
		/// </summary>
		/// <param name="point">current caret point</param>
		void RemoveOutOfRangeSessions (SnapshotPoint point);

		/// <summary>
		/// Remove all sessions from the stack.
		/// </summary>
		void Clear ();
	}
}
