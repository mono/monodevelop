//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace MonoDevelop.SourceEditor.Braces
{
	using System.Collections.Generic;

	/// <summary>
	/// Metadata for IBraceCompletionSessionProvider exports
	/// </summary>
	public interface IBraceCompletionMetadata
	{
		/// <summary>
		/// List of opening tokens.
		/// </summary>
		IEnumerable<char> OpeningBraces { get; }

		/// <summary>
		/// List of closing tokens.
		/// </summary>
		IEnumerable<char> ClosingBraces { get; }

		/// <summary>
		/// Supported content types.
		/// </summary>
		IEnumerable<string> ContentTypes { get; }
	}
}
