//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace MonoDevelop.SourceEditor.Braces
{
	using System;
	using Microsoft.VisualStudio.Text;

	internal interface IBraceCompletionAdornmentService
	{
		/// <summary>
		/// Gets or sets the tracking point used by the brace completion adornment
		/// to indicate the closing brace. The adornment span is length one
		/// with the given point as the end.
		/// </summary>
		/// <remarks>Setting the tracking point to null clears the adornment.</remarks>
		ITrackingPoint Point { get; set; }
	}
}
