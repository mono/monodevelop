//
// NRefactoryExpressionEvaluator.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
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

using Mono.Debugging.Client;

using ICSharpCode.NRefactory.CSharp;

namespace Mono.Debugging.Evaluation
{
	public class NRefactoryExpressionEvaluator : ExpressionEvaluator
	{
		Dictionary<string,ValueReference> userVariables = new Dictionary<string, ValueReference> ();

		public override ValueReference Evaluate (EvaluationContext ctx, string expression, object expectedType)
		{
			expression = expression.TrimStart ();

			if (expression.StartsWith ("?"))
				expression = expression.Substring (1).Trim ();

			if (expression.StartsWith ("var") && char.IsWhiteSpace (expression[3])) {
				expression = expression.Substring (4).Trim (' ', '\t');
				string variable = null;

				for (int n = 0; n < expression.Length; n++) {
					if (!char.IsLetterOrDigit (expression[n]) && expression[n] != '_') {
						variable = expression.Substring (0, n);
						if (!expression.Substring (n).Trim (' ', '\t').StartsWith ("="))
							variable = null;
						break;
					}

					if (n == expression.Length - 1) {
						variable = expression;
						expression = null;
						break;
					}
				}

				if (!string.IsNullOrEmpty (variable))
					userVariables[variable] = new UserVariableReference (ctx, variable);

				if (expression == null)
					return null;
			}

			expression = ReplaceExceptionTag (expression, ctx.Options.CurrentExceptionTag);

			var expr = new CSharpParser ().ParseExpression (expression);
			if (expr == null)
				throw new EvaluatorException ("Could not parse expression '{0}'", expression);

			var evaluator = new NRefactoryExpressionEvaluatorVisitor (ctx, expression, expectedType, userVariables);
			return expr.AcceptVisitor<ValueReference> (evaluator);
		}

		public override string Resolve (DebuggerSession session, SourceLocation location, string exp)
		{
			return Resolve (session, location, exp, false);
		}

		string Resolve (DebuggerSession session, SourceLocation location, string expression, bool tryTypeOf)
		{
			expression = expression.TrimStart ();

			if (expression.StartsWith ("?"))
				return "?" + Resolve (session, location, expression.Substring (1).Trim ());

			if (expression.StartsWith ("var") && char.IsWhiteSpace (expression[3]))
				return "var " + Resolve (session, location, expression.Substring (4).Trim (' ', '\t'));

			expression = ReplaceExceptionTag (expression, session.Options.EvaluationOptions.CurrentExceptionTag);

			Expression expr = new CSharpParser ().ParseExpression (expression);
			if (expr == null)
				return expression;

			var resolver = new NRefactoryExpressionResolverVisitor (session, location, expression);
			expr.AcceptVisitor (resolver);

			string resolved = resolver.GetResolvedExpression ();
			if (resolved == expression && !tryTypeOf && (expr is BinaryOperatorExpression) && IsTypeName (expression)) {
				// This is a hack to be able to parse expressions such as "List<string>". The NRefactory parser
				// can parse a single type name, so a solution is to wrap it around a typeof(). We do it if
				// the evaluation fails.
				string res = Resolve (session, location, "typeof(" + expression + ")", true);
				return res.Substring (7, res.Length - 8);
			}

			return resolved;
		}

		public override ValidationResult ValidateExpression (EvaluationContext ctx, string expression)
		{
			expression = expression.TrimStart ();

			if (expression.StartsWith ("?"))
				expression = expression.Substring (1).Trim ();

			if (expression.StartsWith ("var") && char.IsWhiteSpace (expression[3]))
				expression = expression.Substring (4).Trim ();

			expression = ReplaceExceptionTag (expression, ctx.Options.CurrentExceptionTag);

			// Required as a workaround for a bug in the parser (it won't parse simple expressions like numbers)
			if (!expression.EndsWith (";"))
				expression += ";";

			var parser = new CSharpParser ();
			parser.ParseExpression (expression);

			if (parser.HasErrors)
				return new ValidationResult (false, parser.Errors.First ().Message);

			return new ValidationResult (true, null);
		}

		string ReplaceExceptionTag (string exp, string tag)
		{
			// FIXME: Don't replace inside string literals
			return exp.Replace (tag, "__EXCEPTION_OBJECT__");
		}

		bool IsTypeName (string name)
		{
			int pos = 0;
			bool res = ParseTypeName (name + "$", ref pos);
			return res && pos >= name.Length;
		}

		bool ParseTypeName (string name, ref int pos)
		{
			EatSpaces (name, ref pos);
			if (!ParseName (name, ref pos))
				return false;

			EatSpaces (name, ref pos);
			if (!ParseGenericArgs (name, ref pos))
				return false;

			EatSpaces (name, ref pos);
			if (!ParseIndexer (name, ref pos))
				return false;

			EatSpaces (name, ref pos);
			return true;
		}

		void EatSpaces (string name, ref int pos)
		{
			while (char.IsWhiteSpace (name[pos]))
				pos++;
		}

		bool ParseName (string name, ref int pos)
		{
			if (name[0] == 'g' && pos < name.Length - 8 && name.Substring (pos, 8) == "global::")
				pos += 8;

			do {
				int oldp = pos;
				while (char.IsLetterOrDigit (name[pos]))
					pos++;

				if (oldp == pos)
					return false;

				if (name[pos] != '.')
					return true;

				pos++;
			}
			while (true);
		}

		bool ParseGenericArgs (string name, ref int pos)
		{
			if (name [pos] != '<')
				return true;

			pos++;
			EatSpaces (name, ref pos);

			while (true) {
				if (!ParseTypeName (name, ref pos))
					return false;

				EatSpaces (name, ref pos);
				char c = name [pos++];

				if (c == '>')
					return true;

				if (c == ',')
					continue;

				return false;
			}
		}

		bool ParseIndexer (string name, ref int pos)
		{
			if (name [pos] != '[')
				return true;

			do {
				pos++;
				EatSpaces (name, ref pos);
			} while (name [pos] == ',');

			return name [pos++] == ']';
		}
	}
}
