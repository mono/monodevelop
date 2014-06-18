//
// RazorCodeFragment.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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

using System.Linq;
using ICSharpCode.NRefactory;
using Mono.TextEditor;
using MonoDevelop.Ide;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.AspNet.Razor.Dom
{
	public abstract class RazorCodeFragment : XContainer
	{
		protected RazorCodeFragment (TextLocation start) : base (start)
		{
		}

		protected RazorCodeFragment ()
		{
		}

		public string Name { get; set; }

		public TextLocation? FirstBracket { get; set; }

		public TextDocument Document {
			get {
				if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.Editor != null)
					return IdeApp.Workbench.ActiveDocument.Editor.Document;
				return null;
			}
		}

		public bool IsEndingBracket (TextLocation bracketLocation)
		{
			// If document isn't entirely loaded
			if (Document == null || Document.Lines.Count () < Region.BeginLine)
				return false;

			if (!FirstBracket.HasValue && !FindFirstBracket (bracketLocation))
				return false;

			int firstBracketOffset = Document.GetOffset (FirstBracket.Value);
			int currentBracketOffset = Document.GetOffset (bracketLocation);

			return Document.GetMatchingBracketOffset (firstBracketOffset) == currentBracketOffset;
		}

		public bool FindFirstBracket (TextLocation currentLocation)
		{
			if (Document.Lines.Count () < Region.BeginLine)
				return false;

			int firstBracketPosition = Document.GetTextBetween (Region.Begin, currentLocation).IndexOf ('{');
			if (firstBracketPosition == -1)
				return false;

			int beginOffset = Document.LocationToOffset (Region.Begin);
			FirstBracket = Document.OffsetToLocation (beginOffset + firstBracketPosition);
			return true;
		}
	}
}
