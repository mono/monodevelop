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
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.TextEditor
{
	[Export (typeof (EditorContentProviderService))]
	sealed class EditorContentProviderService
	{
		[ImportMany (typeof (IEditorContentProvider))]
		internal List<Lazy<IEditorContentProvider, IEditorContentProviderMetadata>> EditorContentProviders { get; set; }

		public IEnumerable<IEditorContentProvider> GetContentProvidersForView (ITextView textView)
		{
			Func<string, bool> contentTypeMatch = textView.TextBuffer.ContentType.IsOfType;

			foreach (var provider in EditorContentProviders) {
				var meta = provider.Metadata;

				if (meta.TextViewRoles != null && !textView.Roles.ContainsAny (meta.TextViewRoles)) {
					continue;
				}

				if (!meta.ContentTypes.Any (contentTypeMatch)) {
					continue;
				}

				yield return provider.Value;
			}
		}
	}
}