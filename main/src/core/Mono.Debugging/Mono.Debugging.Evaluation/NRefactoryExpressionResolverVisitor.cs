//
// NRefactoryExpressionResolverVisitor.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using System.Text;
using System.Collections.Generic;

using ICSharpCode.NRefactory.CSharp;

using Mono.Debugging.Client;

namespace Mono.Debugging.Evaluation
{
	// FIXME: if we passed the DebuggerSession and SourceLocation into the NRefactoryExpressionEvaluatorVisitor,
	// we wouldn't need to do this resolve step.
	public class NRefactoryExpressionResolverVisitor : DepthFirstAstVisitor
	{
		List<Replacement> replacements = new List<Replacement> ();
		SourceLocation location;
		DebuggerSession session;
		string expression;

		class Replacement
		{
			public string NewText;
			public int Offset;
			public int Length;
		}

		public NRefactoryExpressionResolverVisitor (DebuggerSession session, SourceLocation location, string expression)
		{
			this.expression = expression.Replace ("\n", "").Replace ("\r", "");
			this.session = session;
			this.location = location;
		}

		internal string GetResolvedExpression ()
		{
			if (replacements.Count == 0)
				return expression;

			replacements.Sort (delegate (Replacement r1, Replacement r2) { return r1.Offset.CompareTo (r2.Offset); });
			StringBuilder sb = new StringBuilder ();
			int i = 0;

			foreach (Replacement r in replacements) {
				sb.Append (expression, i, r.Offset - i);
				sb.Append (r.NewText);
				i = r.Offset + r.Length;
			}

			Replacement last = replacements [replacements.Count - 1];
			sb.Append (expression, last.Offset + last.Length, expression.Length - (last.Offset + last.Length));

			return sb.ToString ();
		}

		void ResolveType (string typeName, int genericArgCount, int offset, int length)
		{
			if (genericArgCount > 0)
				typeName += "<" + new string (',', genericArgCount - 1) + ">";

			string type = session.ResolveIdentifierAsType (typeName, location);
			if (!string.IsNullOrEmpty (type)) {
				type = "global::" + type;
				Replacement r = new Replacement () { Offset = offset, Length = length, NewText = type };
				replacements.Add (r);
			}
		}

		public override void VisitIdentifierExpression (IdentifierExpression identifierExpression)
		{
			base.VisitIdentifierExpression (identifierExpression);

			int length = identifierExpression.EndLocation.Column - identifierExpression.StartLocation.Column;
			int offset = identifierExpression.StartLocation.Column - 1;

			ResolveType (identifierExpression.Identifier, 0, offset, length);
		}

		public override void VisitTypeReferenceExpression (TypeReferenceExpression typeReferenceExpression)
		{
			base.VisitTypeReferenceExpression (typeReferenceExpression);

			int length = typeReferenceExpression.EndLocation.Column - typeReferenceExpression.StartLocation.Column;
			int offset = typeReferenceExpression.StartLocation.Column - 1;
			int index = expression.IndexOf ('<', offset);
			int count = 0;

			if (index != -1 && index < offset + length) {
				if (expression[offset + length - 1] != '>')
					return;

				int end = offset + length - 1;
				int start = index + 1;
				string[] args;

				if (!FunctionBreakpoint.TryParseParameters (expression, start, end - start, out args))
					return;

				length = index - offset;
				count = args.Length;
			}

			ResolveType (expression.Substring (offset, length), count, offset, length);
		}

		public override void VisitSimpleType (SimpleType simpleType)
		{
			base.VisitSimpleType (simpleType);

			int length = simpleType.EndLocation.Column - simpleType.StartLocation.Column;
			int offset = simpleType.StartLocation.Column - 1;

			ResolveType (simpleType.Identifier, 0, offset, length);
		}
	}
}
