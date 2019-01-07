//
//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Ide.Editor.Extension
{
	/// <summary>
	/// MEF extension point to provide implementations of content interfaces on the text
	/// editor.
	/// </summary>
	/// <remarks>
	/// Implementations should be exported with [Export(IEditorContentProvider)] and
	/// should specify an Order, at least one ContentType, and optionally one or more TextViewRole values.
	/// </remarks>
	public interface IEditorContentProvider
	{
		/// <summary>
		/// Gets an instance of the content interface <paramref name="type"/> for the <paramref name="view"/>.
		/// </summary>
		object GetContent (ITextView view, Type type);

		/// <summary>
		/// Gets multiple instance of the content interface <paramref name="type"/> for the <paramref name="view"/>.
		/// </summary>
		IEnumerable<object> GetContents (ITextView view, Type type);
	}

	/// <summary>
	/// Metadata interface for <see cref="IEditorContentProvider" /> providers
	/// </summary>
	public interface IEditorContentProviderMetadata : INamedContentTypeMetadata
	{
		[DefaultValue (null)]
		IEnumerable<string> TextViewRoles { get; }
	}
}
