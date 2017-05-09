//
// SpeculativeNameContextHandler.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class SpeculativeNameContextHandler : CompletionContextHandler
	{
		protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var tree = await completionContext.Document.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
			if (tree.IsInNonUserCode (completionContext.Position, cancellationToken) ||
				tree.IsPreProcessorDirectiveContext (completionContext.Position, cancellationToken) ||
				info.CompletionTriggerReason != CompletionTriggerReason.CompletionCommand &&
			    info.CompletionTriggerReason != CompletionTriggerReason.BackspaceOrDeleteCommand)
				return Enumerable.Empty<CompletionData>();

			var token = tree.FindTokenOnLeftOfPosition(completionContext.Position, cancellationToken);
			if (token.Span.End == completionContext.Position)
				return Enumerable.Empty<CompletionData>();
			var parent = token.Parent.AncestorsAndSelf ().OfType<GenericNameSyntax> ().FirstOrDefault () ?? token.Parent;

			if (!parent.Parent.IsKind (SyntaxKind.IncompleteMember) &&
			    !IsLocal(parent) &&
			    !parent.Parent.IsKind (SyntaxKind.Parameter) &&
			    !parent.Parent.IsKind (SyntaxKind.ForEachStatement)) {
				return Enumerable.Empty<CompletionData>();
			}

			if (info.TriggerCharacter != ' ' &&
				parent.Parent.IsKind (SyntaxKind.ExpressionStatement)) {
				return Enumerable.Empty<CompletionData>();
			}
			var list = new List<CompletionData> ();

			if (parent.IsKind(SyntaxKind.PredefinedType)) {			
				switch (token.Kind()) {
				case SyntaxKind.ObjectKeyword:
					list.Add (engine.Factory.CreateGenericData(this, "o", GenericDataType.NameProposal));
					list.Add (engine.Factory.CreateGenericData(this, "obj", GenericDataType.NameProposal));
					return list;
				case SyntaxKind.BoolKeyword:
					list.Add (engine.Factory.CreateGenericData(this, "b", GenericDataType.NameProposal));
					list.Add (engine.Factory.CreateGenericData(this, "pred", GenericDataType.NameProposal));
					return list;
				case SyntaxKind.CharKeyword:
		       		list.Add (engine.Factory.CreateGenericData(this, "c", GenericDataType.NameProposal));
					list.Add (engine.Factory.CreateGenericData(this, "ch", GenericDataType.NameProposal));
					return list;
				case SyntaxKind.StringKeyword:
					list.Add (engine.Factory.CreateGenericData(this, "str", GenericDataType.NameProposal));
					return list;
				case SyntaxKind.DoubleKeyword:
				case SyntaxKind.FloatKeyword:
				case SyntaxKind.DecimalKeyword:
					list.Add (engine.Factory.CreateGenericData(this, "d", GenericDataType.NameProposal));
					list.Add (engine.Factory.CreateGenericData(this, "f", GenericDataType.NameProposal));
					list.Add (engine.Factory.CreateGenericData(this, "m", GenericDataType.NameProposal));
					return list;
				default:
					list.Add (engine.Factory.CreateGenericData(this, "i", GenericDataType.NameProposal));
					list.Add (engine.Factory.CreateGenericData(this, "j", GenericDataType.NameProposal));
					list.Add (engine.Factory.CreateGenericData(this, "k", GenericDataType.NameProposal));
					return list;
				}
			} else {
				var incompleteMember = parent.Parent as IncompleteMemberSyntax;
				if (incompleteMember != null)
					return list;
				var gns = parent as GenericNameSyntax;
				var names = WordParser.BreakWords (gns != null ? gns.Identifier.ToString () : token.ToString ().Trim ());
				var possibleName = new StringBuilder ();
				for (int i = 0; i < names.Count; i++) {
					possibleName.Length = 0;
					for (int j = i; j < names.Count; j++) {
						if (string.IsNullOrEmpty (names [j])) {
							continue;
						}
						if (j == i) { 
							names [j] = Char.ToLower (names [j] [0]) + names [j].Substring (1);
						}
						possibleName.Append (names [j]);
					}
					list.Add (engine.Factory.CreateGenericData (this, possibleName.ToString (), GenericDataType.NameProposal));
				}
			}
			return list;
		}

		bool IsLocal (SyntaxNode tokenParent)
		{
			if ((tokenParent.IsKind (SyntaxKind.GenericName) || tokenParent.IsKind (SyntaxKind.IdentifierName) || tokenParent.IsKind (SyntaxKind.PredefinedType)) &&
			    (tokenParent.Parent.IsKind (SyntaxKind.ExpressionStatement) || tokenParent.Parent.IsKind (SyntaxKind.VariableDeclaration)))
				return true;
			return false;
		}
	}
}