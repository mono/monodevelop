//
// Projection.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Editor;
using System.Collections.Immutable;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor.Projection
{
	public sealed class Projection
	{
		public ITextDocument Document { get; private set; }

		public IReadOnlyList<ProjectedSegment> ProjectedSegments {
			get;
			private set;
		}

		TextEditor projectedEditor;

		internal TextEditor ProjectedEditor {
			get {
				return projectedEditor;
			}
		}

		ProjectedDocumentContext projectedDocumentContext;

		internal DocumentContext ProjectedContext {
			get {
				return projectedDocumentContext;
			}
		}

		public TextEditor CreateProjectedEditor (DocumentContext originalContext)
		{ 
			if (projectedEditor == null) {
				projectedEditor = TextEditorFactory.CreateNewEditor (Document);
				projectedDocumentContext = new ProjectedDocumentContext (projectedEditor, originalContext);
				projectedEditor.InitializeExtensionChain (projectedDocumentContext);
			}
			return projectedEditor;
		}

		public Projection (ITextDocument document, IReadOnlyList<ProjectedSegment> projectedSegments)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			this.Document = document;
			this.ProjectedSegments = projectedSegments;
		}
	}
}