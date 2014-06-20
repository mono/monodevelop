//
// RazorDom.cs
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

using MonoDevelop.Xml.StateEngine;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory;
using System.Diagnostics;
using Mono.TextEditor;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.AspNet.Mvc.StateEngine
{
	public abstract class RazorExpression : XNode
	{
		public RazorExpression (DomRegion region) : base (region) { }
		public RazorExpression (TextLocation start) : base (start) { }
		protected RazorExpression () { }
	}

	public class RazorExplicitExpression : RazorExpression
	{
		public RazorExplicitExpression (DomRegion region) : base (region) { }
		public RazorExplicitExpression (TextLocation start) : base (start) { }
		protected RazorExplicitExpression () { }

		protected override XObject NewInstance () { return new RazorExplicitExpression (); }

		public override string ToString ()
		{
			return string.Format ("[RazorExplicitExpression Location='{0}'", this.Region);
		}

		public override string FriendlyPathRepresentation
		{
			get { return "@( )"; }
		}
	}

	public class RazorImplicitExpression : RazorExpression
	{
		public RazorImplicitExpression (DomRegion region) : base (region) { }
		public RazorImplicitExpression (TextLocation start) : base (start) { }
		protected RazorImplicitExpression () { }

		protected override XObject NewInstance () { return new RazorImplicitExpression (); }

		public override string ToString ()
		{
			return string.Format ("[RazorImplicitExpression Location='{0}'", this.Region);
		}

		public override string FriendlyPathRepresentation
		{
			get { return "@"; }
		}
	}

	public class RazorComment : XNode
	{
		public RazorComment (DomRegion region) : base (region) { }
		public RazorComment (TextLocation start) : base (start) { }
		protected RazorComment () { }

		protected override XObject NewInstance () { return new RazorComment (); }

		public override string ToString ()
		{
			return string.Format ("[RazorComment Location='{0}'", this.Region);
		}

		public override string FriendlyPathRepresentation
		{
			get { return "@* *@"; }
		}
	}

	public abstract class RazorCodeFragment : XContainer
	{
		public RazorCodeFragment (TextLocation start) : base (start) {}

		protected RazorCodeFragment () { }

		public string Name { get; set; }
		public TextLocation? FirstBracket { get; set; }
		public MonoDevelop.Ide.Editor.TextEditor Document {
			get	{
				if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.Editor != null)
					return IdeApp.Workbench.ActiveDocument.Editor;
				return null;
			}
		}

		public bool IsEndingBracket (TextLocation bracketLocation)
		{
			// If document isn't entirely loaded
			if (Document == null || Document.LineCount < Region.BeginLine)
				return false;

			if (!FirstBracket.HasValue && !FindFirstBracket (bracketLocation))
				return false;

			int firstBracketOffset = Document.LocationToOffset (FirstBracket.Value.Line, FirstBracket.Value.Column);
			int currentBracketOffset = Document.LocationToOffset (bracketLocation.Line, bracketLocation.Column);


			return SimpleBracketMatcher.GetMatchingBracketOffset (Document, firstBracketOffset) == currentBracketOffset;
		}

		public bool FindFirstBracket (TextLocation currentLocation)
		{
			if (Document.LineCount  < Region.BeginLine)
				return false;

			int firstBracketPosition = Document.GetTextBetween (Region.Begin, currentLocation).IndexOf ('{');
			if (firstBracketPosition == -1)
				return false;

			int beginOffset = Document.LocationToOffset (Region.Begin);
			FirstBracket = Document.OffsetToLocation (beginOffset + firstBracketPosition);
			return true;
		}
	}

	public class RazorCodeBlock : RazorCodeFragment
	{
		public RazorCodeBlock (TextLocation start) : base (start) { }
		protected RazorCodeBlock () { }

		protected override XObject NewInstance () { return new RazorCodeBlock (); }

		public override string ToString ()
		{
			return string.Format ("[RazorCodeBlock Location='{0}'", this.Region);
		}

		public override string FriendlyPathRepresentation
		{
			get { return "@{ }"; }
		}
	}

	public class RazorDirective : RazorCodeFragment
	{
		public RazorDirective (TextLocation start) : base (start) { }
		protected RazorDirective () { }

		protected override XObject NewInstance () { return new RazorDirective (); }

		public bool IsSimpleDirective { get; set; }

		public override string ToString ()
		{
			return string.Format ("[RazorDirective Location='{0}'", this.Region);
		}

		public override string FriendlyPathRepresentation
		{
			get { return "@" + Name; }
		}
	}

	public class RazorStatement : RazorCodeFragment
	{
		public RazorStatement (TextLocation start) : base (start) { }
		protected RazorStatement () { }

		protected override XObject NewInstance () { return new RazorStatement (); }

		public override string ToString ()
		{
			return string.Format ("[RazorStatement Location='{0}'", this.Region);
		}

		public override string FriendlyPathRepresentation
		{
			get { return "@" + Name; }
		}
	}
}
