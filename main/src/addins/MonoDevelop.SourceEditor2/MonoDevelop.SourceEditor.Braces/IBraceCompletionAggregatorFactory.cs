//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
namespace MonoDevelop.SourceEditor.Braces
{
	internal interface IBraceCompletionAggregatorFactory
	{
		/// <summary>
		/// Creates a brace completion aggregator to simplify
		/// creating a session that best matches the buffer 
		/// content type.
		/// </summary>
		IBraceCompletionAggregator CreateAggregator ();

		/// <summary>
		/// Gives an IEnumerable of all content types with providers.
		/// </summary>
		IEnumerable<string> ContentTypes { get; }
	}
}
