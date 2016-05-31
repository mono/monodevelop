//
// ErrorDocumentationProvider.cs
//
// Author:
//       Vincent Dondain <vincent.dondain@xamarin.com>
//
// Copyright (c) 2016 Xamarin
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
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Extensions
{
	[ExtensionNode (Description = "A link to the error's documentation to be used in the error pad.")]
	class ErrorDocumentationProvider : TypeExtensionNode
	{
		[NodeAttribute ("regex", true, "Regex to get the error code.")]
		string regex = null;

		[NodeAttribute ("url", true, "URL to the error's documentation with a placeholder for the error code.")]
		string url = null;

		/// <summary>
		/// Provides a link to the documentation using the extension's regex and url template.
		/// </summary>
		/// <returns>The documentation link or null.</returns>
		/// <param name="errorDescription">The error message with the error code to parse.</param>
		public string GetDocumentationLink (string errorDescription) {
			string address = null;
			if (!string.IsNullOrEmpty (errorDescription) && !string.IsNullOrEmpty (regex)) {
				var mtError = System.Text.RegularExpressions.Regex.Match (errorDescription, regex);
				if (!string.IsNullOrEmpty (mtError.Value) && !string.IsNullOrEmpty (url))
					address = string.Format (url, mtError.Value);
			}
			return address;
		}
	}
}

