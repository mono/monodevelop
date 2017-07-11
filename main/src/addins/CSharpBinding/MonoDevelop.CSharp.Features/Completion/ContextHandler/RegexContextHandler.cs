//
// RegexContextHandler.cs
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
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.Text;
using ICSharpCode.NRefactory6.CSharp.Analysis;
using MonoDevelop.Ide;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{	
	class RegexContextHandler : CompletionContextHandler
	{
		public override bool IsTriggerCharacter (Microsoft.CodeAnalysis.Text.SourceText text, int position)
		{
			var ch = text [position];
			return ch == '\\' || base.IsTriggerCharacter (text, position);
		}

		protected override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var document = completionContext.Document;
			var position = completionContext.Position;
			var semanticModel = ctx.SemanticModel;
			if (info.TriggerCharacter == '\\') {
				if (ctx.TargetToken.Parent != null && ctx.TargetToken.Parent.Parent != null &&
				ctx.TargetToken.Parent.Parent.IsKind (SyntaxKind.Argument)) {
					var argument = ctx.TargetToken.Parent.Parent as ArgumentSyntax;

					var symbolInfo = semanticModel.GetSymbolInfo (ctx.TargetToken.Parent.Parent.Parent.Parent);
					if (symbolInfo.Symbol == null)
						return TaskUtil.EmptyEnumerable<CompletionData> ();

					if (SemanticHighlightingVisitor<string>.IsRegexMatchMethod (symbolInfo)) {
						if (((ArgumentListSyntax)argument.Parent).Arguments [1] != argument)
							return TaskUtil.EmptyEnumerable<CompletionData> ();
						completionResult.AutoSelect = false;
						return Task.FromResult (GetFormatCompletionData (engine, argument.Expression.ToString () [0] == '@'));
					}
					if (SemanticHighlightingVisitor<string>.IsRegexConstructor (symbolInfo)) {
						if (((ArgumentListSyntax)argument.Parent).Arguments [0] != argument)
							return TaskUtil.EmptyEnumerable<CompletionData> ();
						completionResult.AutoSelect = false;
						return Task.FromResult (GetFormatCompletionData (engine, argument.Expression.ToString () [0] == '@'));
					}
				}
			} else {
				var ma = ctx.TargetToken.Parent as MemberAccessExpressionSyntax;
				if (ma != null) {
					var symbolInfo = semanticModel.GetSymbolInfo (ma.Expression);
					var typeInfo = semanticModel.GetTypeInfo (ma.Expression);
					var type = typeInfo.Type;
					if (type != null && type.Name == "Match"  && type.ContainingNamespace.GetFullName () == "System.Text.RegularExpressions" ) {
						var items = new List<CompletionData>();
						foreach (var grp in GetGroups (ctx, symbolInfo.Symbol)) {
							items.Add (engine.Factory.CreateGenericData (this, "Groups[\"" + grp + "\"]", GenericDataType.Undefined));
						}

						return Task.FromResult ((IEnumerable<CompletionData>)items);
					}
				}
			}
			return TaskUtil.EmptyEnumerable<CompletionData> ();
		}

		IEnumerable<string> GetGroups (SyntaxContext ctx, ISymbol symbol)
		{
			var root = ctx.SyntaxTree.GetRoot ();
			foreach (var decl in symbol.DeclaringSyntaxReferences) {
				Optional<object> val = null;

				var node = root.FindNode (decl.Span) as VariableDeclaratorSyntax;
				if (node == null)
					continue;
				var invocation = node.Initializer.Value as InvocationExpressionSyntax;
				var invocationSymbol = ctx.SemanticModel.GetSymbolInfo (invocation).Symbol;
				if (invocationSymbol.Name == "Match" && SemanticHighlightingVisitor<string>.IsRegexType (invocationSymbol.ContainingType)) {
					if (invocation.ArgumentList.Arguments.Count == 1) {
						var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
						if (memberAccess == null)
							continue;
						var target = ctx.SemanticModel.GetSymbolInfo (memberAccess.Expression).Symbol;
						if (target.DeclaringSyntaxReferences.Length == 0)
							continue;
						var targetNode = root.FindNode (target.DeclaringSyntaxReferences.First ().Span) as VariableDeclaratorSyntax;
						if (targetNode == null)
							continue;
						var objectCreation = targetNode.Initializer.Value as ObjectCreationExpressionSyntax;
						if (objectCreation == null)
							continue;
						var targetNodeSymbol = ctx.SemanticModel.GetSymbolInfo (objectCreation).Symbol;
						if (SemanticHighlightingVisitor<string>.IsRegexType (targetNodeSymbol.ContainingType)) {
							if (objectCreation.ArgumentList.Arguments.Count < 1)
								continue;
							val = ctx.SemanticModel.GetConstantValue (objectCreation.ArgumentList.Arguments [0].Expression);
						}
					} else {
						if (invocation.ArgumentList.Arguments.Count < 2)
							continue;
						val = ctx.SemanticModel.GetConstantValue (invocation.ArgumentList.Arguments [1].Expression);
					}

					if (!val.HasValue)
						continue;
					var str = val.Value.ToString ();
					int idx = -1;
					while  ((idx = str.IndexOf ("(?<", idx + 1, StringComparison.Ordinal)) >= 0) {
						var closingIndex = str.IndexOf (">", idx, StringComparison.Ordinal);
						if (closingIndex >= idx) {
							yield return str.Substring (idx + 3, closingIndex - idx - 3);
							idx = closingIndex - 1;
						}
					}
				}
			}
		}

		IEnumerable<CompletionData> GetFormatCompletionData (CompletionEngine engine, bool isVerbatimString)
		{
			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"d", "Digit character", null);
			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"D", "Non-digit character", null);

			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"b", "Word boundary", null);
			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"B", "Non-word boundary", null);

			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"w", "Word character", null);
			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"W", "Non-word character", null);

			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"s", "White-space character", null);
			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"S", "Non-white-space character", null);

			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"A", "Start boundary", null);
			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"Z", "End boundary", null);

			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"k<name>", "Named backreference", null);
			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"P{name}", "Negative unicode category or unicode block", null);
			yield return engine.Factory.CreateFormatItemCompletionData(this, (isVerbatimString ? "" : "\\") +"p{name}", "Unicode category or unicode block", null);
		}
	}
}