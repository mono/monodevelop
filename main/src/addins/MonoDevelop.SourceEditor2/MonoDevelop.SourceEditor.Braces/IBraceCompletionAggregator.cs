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
	using Microsoft.VisualStudio.Text.Editor;
	using Microsoft.VisualStudio.Utilities;

	internal interface IBraceCompletionAggregator
	{
		/// <summary>
		/// A unique list of all opening braces that have providers.
		/// </summary>
		string OpeningBraces { get; }

		/// <summary>
		/// A unique list of all closing braces that have providers
		/// </summary>
		string ClosingBraces { get; }

		/// <summary>
		/// Checks if a provider exists for the content type and opening brace.
		/// </summary>
		/// <param name="contentType">buffer content type</param>
		/// <param name="openingBrace">opening brace character</param>
		/// <returns>True if there is a matching provider</returns>
		bool IsSupportedContentType (IContentType contentType, char openingBrace);

		/// <summary>
		/// Creates a session using the best provider for the buffer content type and opening brace.
		/// </summary>
		/// <param name="textView">current text view</param>
		/// <param name="openingPoint">current caret point</param>
		/// <param name="openingBrace">opening brace chraracter</param>
		/// <param name="session">Session created by the provider.</param>
		/// <returns>True if the provider created a session.</returns>
		bool TryCreateSession (ITextView textView, SnapshotPoint openingPoint, char openingBrace, out IBraceCompletionSession session);
	}
}
