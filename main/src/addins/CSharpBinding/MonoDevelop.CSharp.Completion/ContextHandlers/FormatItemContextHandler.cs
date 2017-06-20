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
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using System.Composition;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	[MetadataAttribute]
	[AttributeUsage (AttributeTargets.Class)]
	internal class ExportCompletionProviderMef1Attribute : ExportAttribute
	{
		public string Name { get; }
		public string Language { get; }

		public ExportCompletionProviderMef1Attribute (string name, string language)
			: base (typeof (CompletionProvider))
		{
			this.Name = name ?? throw new ArgumentNullException (nameof (name));
			this.Language = language ?? throw new ArgumentNullException (nameof (language));
		}
	}

	[ExportCompletionProvider ("FormatItemCompletionProvider", LanguageNames.CSharp)]
	[ExportCompletionProviderMef1Attribute ("FormatItemCompletionProvider", LanguageNames.CSharp)]
	class FormatItemCompletionProvider : CommonCompletionProvider
	{
		public FormatItemCompletionProvider ()
		{
			Console.WriteLine ("created provider");
		}

		public override async Task ProvideCompletionsAsync (Microsoft.CodeAnalysis.Completion.CompletionContext context)
		{
			Console.WriteLine ("Request completions!");
			var document = context.Document;
			var position = context.Position;
			var semanticModel = await context.Document.GetSemanticModelAsync (context.CancellationToken);



			//if (ctx.TargetToken.Parent != null && ctx.TargetToken.Parent.Parent != null &&
			//	ctx.TargetToken.Parent.Parent.IsKind (SyntaxKind.Argument)) {
			//	SourceText text;
			//	if (!completionContext.Document.TryGetText (out text)) {
			//		text = await completionContext.Document.GetTextAsync ();
			//	}
			//	var currentChar = text [completionContext.Position - 1];
			//	if (ctx.TargetToken.Parent == null || !ctx.TargetToken.Parent.IsKind (SyntaxKind.StringLiteralExpression) ||
			//		ctx.TargetToken.Parent.Parent == null || !ctx.TargetToken.Parent.Parent.IsKind (SyntaxKind.Argument) ||
			//		ctx.TargetToken.Parent.Parent.Parent == null || !ctx.TargetToken.Parent.Parent.Parent.IsKind (SyntaxKind.ArgumentList) ||
			//		ctx.TargetToken.Parent.Parent.Parent.Parent == null || !ctx.TargetToken.Parent.Parent.Parent.Parent.IsKind (SyntaxKind.InvocationExpression)) {
			//		return Enumerable.Empty<CompletionData> ();
			//	}
			//	var formatArgument = GetFormatItemNumber (document, position);
			//	var invocationExpression = ctx.TargetToken.Parent.Parent.Parent.Parent as InvocationExpressionSyntax;
			//	return GetFormatCompletionData (engine, semanticModel, invocationExpression, formatArgument, currentChar);
			//}

			//return Enumerable.Empty<CompletionData> ();
		}
	}

	//class FormatItemContextHandler : CompletionContextHandler
	//{
	//public override bool IsTriggerCharacter (Microsoft.CodeAnalysis.Text.SourceText text, int position)
	//{
	//	var ch = text [position];
	//	return ch == ':' || ch == '"';
	//}

	//protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
	//{
	//}


	//static readonly DateTime curDate = DateTime.Now;

	//IEnumerable<CompletionData> GenerateNumberFormatitems(CompletionEngine engine, bool isFloatingPoint)
	//{
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "D", "decimal", 123);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "D5", "decimal", 123);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "C", "currency", 123);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "C0", "currency", 123);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "E", "exponential", 1.23E4);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "E2", "exponential", 1.234);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "e2", "exponential", 1.234);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "F", "fixed-point", 123.45);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "F1", "fixed-point", 123.45);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "G", "general", 1.23E+56);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "g2", "general", 1.23E+56);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "N", "number", 12345.68);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "N1", "number", 12345.68);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "P", "percent", 12.34);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "P1", "percent", 12.34);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "R", "round-trip", 0.1230000001);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "X", "hexadecimal", 1234);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "x8", "hexadecimal", 1234);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "0000", "custom", 123);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "####", "custom", 123);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "##.###", "custom", 1.23);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "##.000", "custom", 1.23);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "## 'items'", "custom", 12);
	//}

	//IEnumerable<CompletionData> GenerateDateTimeFormatitems(CompletionEngine engine)
	//{
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "D", "long date", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "d", "short date", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "F", "full date long", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "f", "full date short", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "G", "general long", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "g", "general short", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "M", "month", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "O", "ISO 8601", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "R", "RFC 1123", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "s", "sortable", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "T", "long time", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "t", "short time", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "U", "universal full", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "u", "universal sortable", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "Y", "year month", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "yy-MM-dd", "custom", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "yyyy MMMMM dd", "custom", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "yy-MMM-dd ddd", "custom", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "yyyy-M-d dddd", "custom", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "hh:mm:ss t z", "custom", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "hh:mm:ss tt zz", "custom", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "HH:mm:ss tt zz", "custom", curDate);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "HH:m:s tt zz", "custom", curDate);

	//}

	//[Flags]
	//enum TestEnum
	//{
	//	EnumCaseName = 0,
	//	Flag1 = 1,
	//	Flag2 = 2,
	//	Flags
	//}

	//IEnumerable<CompletionData> GenerateEnumFormatitems(CompletionEngine engine)
	//{
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "G", "string value", TestEnum.EnumCaseName);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "F", "flags value", TestEnum.Flags);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "D", "integer value", TestEnum.Flags);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "X", "hexadecimal", TestEnum.Flags);
	//}

	//IEnumerable<CompletionData> GenerateTimeSpanFormatitems(CompletionEngine engine)
	//{
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "c", "invariant", new TimeSpan(0, 1, 23, 456));
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "G", "general long", new TimeSpan(0, 1, 23, 456));
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "g", "general short", new TimeSpan(0, 1, 23, 456));
	//}

	//static Guid defaultGuid = Guid.NewGuid();

	//IEnumerable<CompletionData> GenerateGuidFormatitems(CompletionEngine engine)
	//{
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "N", "digits", defaultGuid);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "D", "hypens", defaultGuid);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "B", "braces", defaultGuid);
	//	yield return engine.Factory.CreateFormatItemCompletionData(this, "P", "parentheses", defaultGuid);
	//}


	//static int GetFormatItemNumber(Document document, int offset)
	//{
	//	int number = 0;
	//	var o = offset - 2;
	//	var text = document.GetTextAsync().Result;
	//	while (o > 0) {
	//		char ch = text[o];
	//		if (ch == '{')
	//			return number;
	//		if (!char.IsDigit(ch))
	//			break;
	//		number = number * 10 + ch - '0';
	//		o--;
	//	}
	//	return -1;
	//}

	//IEnumerable<CompletionData> GetFormatCompletionForType(CompletionEngine engine, ITypeSymbol type)
	//{
	//	if (type == null) {
	//		return GenerateNumberFormatitems (engine, false)
	//			.Concat (GenerateDateTimeFormatitems (engine))
	//			.Concat (GenerateTimeSpanFormatitems (engine))
	//			.Concat (GenerateEnumFormatitems (engine))
	//			.Concat (GenerateGuidFormatitems (engine));
	//	}

	//	switch (type.ToString()) {
	//		case "long":
	//		case "System.Int64":
	//		case "ulong":
	//		case "System.UInt64":
	//		case "int":
	//		case "System.Int32":
	//		case "uint":
	//		case "System.UInt32":
	//		case "short":
	//		case "System.Int16":
	//		case "ushort":
	//		case "System.UInt16":
	//		case "byte":
	//		case "System.Byte":
	//		case "sbyte":
	//		case "System.SByte":
	//		return GenerateNumberFormatitems(engine, false);
	//		case "float":
	//		case "System.Single":
	//		case "double":
	//		case "System.Double":
	//		case "decimal":
	//		case "System.Decimal":
	//		return GenerateNumberFormatitems(engine, true);
	//		case "System.Enum":
	//		return GenerateEnumFormatitems(engine);
	//		case "System.DateTime":
	//		return GenerateDateTimeFormatitems(engine);
	//		case "System.TimeSpan":
	//		return GenerateTimeSpanFormatitems(engine);
	//		case "System.Guid":
	//		return GenerateGuidFormatitems(engine);
	//	}
	//	return CompletionResult.Empty.Data;
	//}

	//IEnumerable<CompletionData> GetFormatCompletionData(CompletionEngine engine, SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, int formatArgument, char currentChar)
	//{
	//	var symbolInfo = semanticModel.GetSymbolInfo (invocationExpression);
	//	var method = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol> ().FirstOrDefault ();
	//	var ma = invocationExpression.Expression as MemberAccessExpressionSyntax;

	//	if (ma != null && ma.Name.ToString () == "ToString") {
	//		if (method == null || currentChar != '"') {
	//			return Enumerable.Empty<CompletionData> ();
	//		}
	//		if (method != null) {
	//			return GetFormatCompletionForType (engine, method.ContainingType);
	//		} else {
	//			return Enumerable.Empty<CompletionData> ();
	//		}
	//	} else {
	//		if(method == null || currentChar != ':') {
	//			return Enumerable.Empty<CompletionData> ();
	//		}
	//		ExpressionSyntax fmtArgumets;
	//		IList<ExpressionSyntax> args;
	//		if (FormatStringHelper.TryGetFormattingParameters(semanticModel, invocationExpression, out fmtArgumets, out args, null)) {
	//			ITypeSymbol type = null;
	//			if (formatArgument  + 1< args.Count) {
	//				var invokeArgument = semanticModel.GetSymbolInfo(args[formatArgument + 1]);
	//				if (invokeArgument.Symbol != null)
	//					type = invokeArgument.Symbol.GetReturnType();
	//			}

	//			return GetFormatCompletionForType(engine, type);
	//		}
	//	}
	//	return Enumerable.Empty<CompletionData> ();
	//}
	//}
}