//
// FormatStringHelper.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class FormatStringHelper
	{
		static readonly string[] parameterNames = { "format", "frmt", "fmt" };
		
		public static bool TryGetFormattingParameters(
			SemanticModel semanticModel,
			InvocationExpressionSyntax invocationExpression,
		    out ExpressionSyntax formatArgument, out IList<ExpressionSyntax> arguments,
			Func<IParameterSymbol, ExpressionSyntax, bool> argumentFilter,
			CancellationToken cancellationToken = default (CancellationToken))
		{
			if (semanticModel == null)
				throw new ArgumentNullException("semanticModel");
			if (invocationExpression == null)
				throw new ArgumentNullException("invocationExpression");
			var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression.Expression, cancellationToken);
			if (argumentFilter == null)
				argumentFilter = (p, e) => true;
			
			var symbol = symbolInfo.Symbol;
			formatArgument = null;
			arguments = new List<ExpressionSyntax>();
			var method = symbol as IMethodSymbol;

			if (symbol == null || symbol.Kind != SymbolKind.Method)
				return false;

			// Serach for method of type: void Name(string format, params object[] args);
			IList<IMethodSymbol> methods = method.ContainingType.GetMembers (method.Name).OfType<IMethodSymbol>().ToList();
			if (!methods.Any(m => m.Parameters.Length == 2 && 
				m.Parameters[0].Type.SpecialType == SpecialType.System_String && parameterNames.Contains(m.Parameters[0].Name) && 
				m.Parameters[1].IsParams))
				return false;

			//var argumentToParameterMap = invocationResolveResult.GetArgumentToParameterMap();
			//var resolvedParameters = invocationResolveResult.Member.Parameters;
			var allArguments = invocationExpression.ArgumentList.Arguments.ToArray();
			for (int i = 0; i < allArguments.Length; i++) {
				var parameterIndex = i; //argumentToParameterMap[i];
				if (parameterIndex < 0 || parameterIndex >= method.Parameters.Length) {
					// No valid mapping for this argument, skip it
					continue;
				}
				var parameter = method.Parameters[parameterIndex];
				var argument = allArguments[i];
				if (i == 0 && parameter.Type.SpecialType == SpecialType.System_String && parameterNames.Contains(parameter.Name)) {
					formatArgument = argument.Expression;
				} /*else if (formatArgument != null && parameter.IsParams && !invocationResolveResult.IsExpandedForm) {
					var ace = argument as ArrayCreateExpression;
					if (ace == null || ace.Initializer.IsNull)
						return false;
					foreach (var element in ace.Initializer.Elements) {
						if (argumentFilter(parameter, element))
							arguments.Add(argument);
					}
				} else*/ if (formatArgument != null && argumentFilter(parameter, argument.Expression)) {
					arguments.Add(argument.Expression);
				}
			}
			return formatArgument != null;
		}
	}
}

