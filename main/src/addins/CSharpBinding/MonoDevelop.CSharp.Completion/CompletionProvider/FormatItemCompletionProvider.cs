//
// FormatItemContextHandler.cs
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
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	[ExportCompletionProvider ("FormatItemCompletionProvider", LanguageNames.CSharp)]
	class FormatItemCompletionProvider : CommonCompletionProvider
	{
		public override bool ShouldTriggerCompletion (SourceText text, int position, CompletionTrigger trigger, Microsoft.CodeAnalysis.Options.OptionSet options)
		{
			if (trigger.Character == ':')
				return true;
			return false;
		}

		public override async Task ProvideCompletionsAsync (Microsoft.CodeAnalysis.Completion.CompletionContext completionContext)
		{
			var document = completionContext.Document;
			var position = completionContext.Position;
			var cancellationToken = completionContext.CancellationToken;

			var semanticModel = await document.GetSemanticModelForSpanAsync (new TextSpan (position, 0), cancellationToken).ConfigureAwait (false);

			var workspace = document.Project.Solution.Workspace;
			var ctx = CSharpSyntaxContext.CreateContext (workspace, semanticModel, position, cancellationToken);
			if (ctx.TargetToken.Parent != null && ctx.TargetToken.Parent.Parent != null &&
				ctx.TargetToken.Parent.Parent.IsKind (SyntaxKind.Argument)) {
				SourceText text;
				if (!completionContext.Document.TryGetText (out text)) {
					text = await completionContext.Document.GetTextAsync ();
				}
				var currentChar = text [completionContext.Position - 1];
				if (ctx.TargetToken.Parent == null || !ctx.TargetToken.Parent.IsKind (SyntaxKind.StringLiteralExpression) ||
					ctx.TargetToken.Parent.Parent == null || !ctx.TargetToken.Parent.Parent.IsKind (SyntaxKind.Argument) ||
					ctx.TargetToken.Parent.Parent.Parent == null || !ctx.TargetToken.Parent.Parent.Parent.IsKind (SyntaxKind.ArgumentList) ||
					ctx.TargetToken.Parent.Parent.Parent.Parent == null || !ctx.TargetToken.Parent.Parent.Parent.Parent.IsKind (SyntaxKind.InvocationExpression)) {
					return;
				}
				var formatArgument = GetFormatItemNumber (document, position);
				var invocationExpression = ctx.TargetToken.Parent.Parent.Parent.Parent as InvocationExpressionSyntax;
				GetFormatCompletionData (completionContext, semanticModel, invocationExpression, formatArgument, currentChar);
			}
		}

		static int GetFormatItemNumber (Document document, int offset)
		{
			int number = 0;
			var o = offset - 2;
			var text = document.GetTextAsync ().Result;
			while (o > 0) {
				char ch = text [o];
				if (ch == '{')
					return number;
				if (!char.IsDigit (ch))
					break;
				number = number * 10 + ch - '0';
				o--;
			}
			return -1;
		}

		void GetFormatCompletionData (Microsoft.CodeAnalysis.Completion.CompletionContext engine, SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, int formatArgument, char currentChar)
		{
			var symbolInfo = semanticModel.GetSymbolInfo (invocationExpression);
			var method = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol> ().FirstOrDefault ();
			var ma = invocationExpression.Expression as MemberAccessExpressionSyntax;

			if (ma != null && ma.Name.ToString () == "ToString") {
				if (method == null || currentChar != '"') {
					return;
				}
				if (method != null) {
					GetFormatCompletionForType (engine, method.ContainingType);
				}
				return;
			} else {
				if (method == null || currentChar != ':') {
					return;
				}
				ExpressionSyntax fmtArgumets;
				IList<ExpressionSyntax> args;
				if (FormatStringHelper.TryGetFormattingParameters (semanticModel, invocationExpression, out fmtArgumets, out args, null)) {
					ITypeSymbol type = null;
					if (formatArgument + 1 < args.Count) {
						var invokeArgument = semanticModel.GetSymbolInfo (args [formatArgument + 1]);
						if (invokeArgument.Symbol != null)
							type = invokeArgument.Symbol.GetReturnType ();
					}
					GetFormatCompletionForType (engine, type);
				}
			}
		}

		void GetFormatCompletionForType(Microsoft.CodeAnalysis.Completion.CompletionContext completionContext, ITypeSymbol type)
		{
			if (type == null) {
				GenerateNumberFormatitems (completionContext);
				GenerateDateTimeFormatitems (completionContext);
				GenerateTimeSpanFormatitems (completionContext);
				GenerateEnumFormatitems (completionContext);
				GenerateGuidFormatitems (completionContext);
				return;
			}

			switch (type.ToString ()) {
			case "long":
			case "System.Int64":
			case "ulong":
			case "System.UInt64":
			case "int":
			case "System.Int32":
			case "uint":
			case "System.UInt32":
			case "short":
			case "System.Int16":
			case "ushort":
			case "System.UInt16":
			case "byte":
			case "System.Byte":
			case "sbyte":
			case "System.SByte":
				GenerateNumberFormatitems (completionContext);
				break;
			case "float":
			case "System.Single":
			case "double":
			case "System.Double":
			case "decimal":
			case "System.Decimal":
				GenerateNumberFormatitems (completionContext);
				break;
			case "System.Enum":
				GenerateEnumFormatitems (completionContext);
				break;
			case "System.DateTime":
				GenerateDateTimeFormatitems (completionContext);
				break;
			case "System.TimeSpan":
				GenerateTimeSpanFormatitems (completionContext);
				break;
			case "System.Guid":
				GenerateGuidFormatitems (completionContext);
				break;
			}
		}

		static CompletionItem CreateCompletionItem (string completionText, string description, object example)
		{
			var pDict = ImmutableDictionary<string, string>.Empty;
			pDict = pDict.Add ("DescriptionMarkup", "- <span foreground=\"darkgray\" size='small'>" + description + "</span>");
			try {
				pDict = pDict.Add ("RightSideMarkup", "<span size='small'>" + string.Format ("{0:" + completionText + "}", example) + "</span>");
			} catch (Exception e) {
				LoggingService.LogError ("Format error.", e);
			}
			return CompletionItem.Create (completionText, properties: pDict);
		}

		static readonly DateTime curDate = DateTime.Now;
		static readonly CompletionItem [] formatItems =  {
			CreateCompletionItem ("D", "decimal", 123),
			CreateCompletionItem ("D5", "decimal", 123),
			CreateCompletionItem ("C", "currency", 123),
			CreateCompletionItem ("C0", "currency", 123),
			CreateCompletionItem ("E", "exponential", 1.23E4),
			CreateCompletionItem ("E2", "exponential", 1.234),
			CreateCompletionItem ("e2", "exponential", 1.234),
			CreateCompletionItem ("F", "fixed-point", 123.45),
			CreateCompletionItem ("F1", "fixed-point", 123.45),
			CreateCompletionItem ("G", "general", 1.23E+56),
			CreateCompletionItem ("g2", "general", 1.23E+56),
			CreateCompletionItem ("N", "number", 12345.68),
			CreateCompletionItem ("N1", "number", 12345.68),
			CreateCompletionItem ("P", "percent", 12.34),
			CreateCompletionItem ("P1", "percent", 12.34),
			CreateCompletionItem ("R", "round-trip", 0.1230000001),
			CreateCompletionItem ("X", "hexadecimal", 1234),
			CreateCompletionItem ("x8", "hexadecimal", 1234),
			CreateCompletionItem ("0000", "custom", 123),
			CreateCompletionItem ("####", "custom", 123),
			CreateCompletionItem ("##.###", "custom", 1.23),
			CreateCompletionItem ("##.000", "custom", 1.23),
			CreateCompletionItem ("## 'items'", "custom", 12)
		};

		void GenerateNumberFormatitems (Microsoft.CodeAnalysis.Completion.CompletionContext completionContext)
		{
			completionContext.AddItems (formatItems);
		}

		static readonly CompletionItem [] timeFormatItems =  {
			CreateCompletionItem ("D", "long date", curDate),
			CreateCompletionItem ("d", "short date", curDate),
			CreateCompletionItem ("F", "full date long", curDate),
			CreateCompletionItem ("f", "full date short", curDate),
			CreateCompletionItem ("G", "general long", curDate),
			CreateCompletionItem ("g", "general short", curDate),
			CreateCompletionItem ("M", "month", curDate),
			CreateCompletionItem ("O", "ISO 8601", curDate),
			CreateCompletionItem ("R", "RFC 1123", curDate),
			CreateCompletionItem ("s", "sortable", curDate),
			CreateCompletionItem ("T", "long time", curDate),
			CreateCompletionItem ("t", "short time", curDate),
			CreateCompletionItem ("U", "universal full", curDate),
			CreateCompletionItem ("u", "universal sortable", curDate),
			CreateCompletionItem ("Y", "year month", curDate),
			CreateCompletionItem ("yy-MM-dd", "custom", curDate),
			CreateCompletionItem ("yyyy MMMMM dd", "custom", curDate),
			CreateCompletionItem ("yy-MMM-dd ddd", "custom", curDate),
			CreateCompletionItem ("yyyy-M-d dddd", "custom", curDate),
			CreateCompletionItem ("hh:mm:ss t z", "custom", curDate),
			CreateCompletionItem ("hh:mm:ss tt zz", "custom", curDate),
			CreateCompletionItem ("HH:mm:ss tt zz", "custom", curDate),
			CreateCompletionItem ("HH:m:s tt zz", "custom", curDate)
		};

		void GenerateDateTimeFormatitems (Microsoft.CodeAnalysis.Completion.CompletionContext completionContext)
		{
			completionContext.AddItems (timeFormatItems);
		}

		[Flags]
		enum TestEnum
		{
			EnumCaseName = 0,
			Flag1 = 1,
			Flag2 = 2,
			Flags
		}

		static readonly CompletionItem [] enumFormatItems =  {
			CreateCompletionItem ("G", "string value", TestEnum.EnumCaseName),
			CreateCompletionItem ("F", "flags value", TestEnum.Flags),
			CreateCompletionItem ("D", "integer value", TestEnum.Flags),
			CreateCompletionItem ("X", "hexadecimal", TestEnum.Flags)
		};

		void GenerateEnumFormatitems (Microsoft.CodeAnalysis.Completion.CompletionContext completionContext)
		{
			completionContext.AddItems (enumFormatItems);
		}

		static readonly CompletionItem [] timeSpanFormatItems =  {
			CreateCompletionItem ("c", "invariant", new TimeSpan (0, 1, 23, 456)),
			CreateCompletionItem ("G", "general long", new TimeSpan (0, 1, 23, 456)),
			CreateCompletionItem ("g", "general short", new TimeSpan (0, 1, 23, 456))
		};

		void GenerateTimeSpanFormatitems (Microsoft.CodeAnalysis.Completion.CompletionContext completionContext)
		{
			completionContext.AddItems (timeSpanFormatItems);
		}

		static Guid defaultGuid = Guid.NewGuid();

		static readonly CompletionItem [] guidFormatItems =  {
			CreateCompletionItem ("N", "digits", defaultGuid),
			CreateCompletionItem ("D", "hypens", defaultGuid),
			CreateCompletionItem ("B", "braces", defaultGuid),
			CreateCompletionItem ("P", "parentheses", defaultGuid)
		};

		void GenerateGuidFormatitems (Microsoft.CodeAnalysis.Completion.CompletionContext completionContext)
		{
			completionContext.AddItems (guidFormatItems);
		}
	}
}