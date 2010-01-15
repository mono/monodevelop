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

using System;

using Cecil.Decompiler.Ast;

namespace Cecil.Decompiler.Steps {

	class CanCastStep : BaseCodeTransformer, IDecompilationStep {

		public static readonly IDecompilationStep Instance = new CanCastStep ();

		const string SafeCastKey = "SafeCast";

		static Pattern.ICodePattern CanCastPattern = new Pattern.Binary {
			Left = new Pattern.SafeCast { Bind = sc => new Pattern.MatchData (SafeCastKey, sc) },
			Operator = new Pattern.Constant { Value = BinaryOperator.GreaterThan },
			Right = new Pattern.Literal { Value = null }
		};

		public override ICodeNode VisitBinaryExpression (BinaryExpression node)
		{
			var result = Pattern.CodePattern.Match (CanCastPattern, node);
			if (!result.Success)
				return base.VisitBinaryExpression (node);

			var safe_cast = (SafeCastExpression) result [SafeCastKey];
			return new CanCastExpression (
				(Expression) Visit (safe_cast.Expression),
				safe_cast.TargetType);
		}

		public BlockStatement Process (DecompilationContext context, BlockStatement body)
		{
			return (BlockStatement) VisitBlockStatement (body);
		}
	}
}
