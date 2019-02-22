//
// SemanticHighlighting.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Text;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor.Highlighting
{	
	/// <summary>
	/// Semantic highlighting adds the ability to add highlighting for things that require
	/// a background parser to be colored. For example type names.
	/// </summary>
	[Obsolete ("Use the Microsoft.VisualStudio.Text APIs")]
	public abstract class SemanticHighlighting : IDisposable
	{
		protected readonly internal TextEditor editor;
		protected readonly internal DocumentContext documentContext;

		protected SemanticHighlighting (TextEditor editor, DocumentContext documentContext)
		{
			this.editor = editor;
			this.documentContext = documentContext;
			this.documentContext.DocumentParsed += HandleDocumentParsed;
		}

		protected abstract void DocumentParsed ();

		public void NotifySemanticHighlightingUpdate ()
		{
			var handler = SemanticHighlightingUpdated;
			if (handler != null)
				handler (this, EventArgs.Empty);
		}

		/// <summary>
		/// Colorize the specified offset, count and colorizeCallback.
		/// </summary>
		/// <param name="segment">The area to run the colorizer in.</param>
		public abstract IEnumerable<ColoredSegment> GetColoredSegments (ISegment segment);

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			if (DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting)
				DocumentParsed (); 
		}

		public virtual void Dispose ()
		{
			documentContext.DocumentParsed -= HandleDocumentParsed;
		}

		internal event EventHandler SemanticHighlightingUpdated;
	}
}