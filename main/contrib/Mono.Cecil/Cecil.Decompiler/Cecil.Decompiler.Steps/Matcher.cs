#region license
//
//	(C) 2007 - 2008 Novell, Inc. http://www.novell.com
//	(C) 2007 - 2008 Jb Evain http://evain.net
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using Mono.Cecil.Cil;

using Cecil.Decompiler.Ast;

namespace Cecil.Decompiler.Steps {

	abstract class Matcher : Ast.BaseCodeVisitor {

		bool @continue = true;
		bool match = false;

		public bool Continue
		{
			get { return @continue; }
			set { @continue = value; }
		}

		public bool Match
		{
			get { return match; }
			set { match = value; }
		}

		public override void Visit (ICodeNode node)
		{
			if (@continue)
				base.Visit (node);
		}
	}

	class VariableMatcher : Matcher {

		VariableReference variable;

		VariableMatcher (VariableReference variable)
		{
			this.variable = variable;
		}

		public override void VisitVariableReferenceExpression (VariableReferenceExpression node)
		{
			if (node.Variable != variable)
				return;

			Match = true;
			Continue = false;
		}

		public static bool FindVariableInExpression (VariableReference variable, Expression expression)
		{
			var matcher = new VariableMatcher (variable);
			matcher.Visit (expression);
			return matcher.Match;
		}
	}
}
