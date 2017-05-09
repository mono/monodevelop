// 
// ICompletionDataFactory.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.CodeCompletion;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	public enum GenericDataType
	{
		AttributeTarget,
		Undefined,
		PreprocessorKeyword,
		PreprocessorSymbol,
		NameProposal,
		NamedParameter
	}

	public interface ICompletionDataFactory
	{
		CompletionData CreateKeywordCompletion (ICompletionDataKeyHandler keyHandler, string keyword);

		CompletionData CreateGenericData (ICompletionDataKeyHandler keyHandler, string data, GenericDataType genericDataType = GenericDataType.Undefined);

		CompletionData CreateFormatItemCompletionData (ICompletionDataKeyHandler keyHandler, string format, string description, object example);

		CompletionData CreateXmlDocCompletionData (ICompletionDataKeyHandler keyHandler, string tag, string description = null, string tagInsertionText = null);

		ISymbolCompletionData CreateSymbolCompletionData (ICompletionDataKeyHandler keyHandler, ISymbol symbol);
		ISymbolCompletionData CreateSymbolCompletionData (ICompletionDataKeyHandler keyHandler, ISymbol symbol, string text);

		/// <summary>
		/// Creates enum member completion data. 
		/// Form: Type.Member
		/// Used for generating enum members Foo.A, Foo.B where the enum 'Foo' is valid.
		/// </summary>
		ISymbolCompletionData CreateEnumMemberCompletionData (ICompletionDataKeyHandler keyHandler, ISymbol typeAlias, IFieldSymbol field);

		CompletionData CreateNewOverrideCompletionData (ICompletionDataKeyHandler keyHandler, int declarationBegin, ITypeSymbol currentType, ISymbol m, bool afterKeyword);

		CompletionData CreatePartialCompletionData (ICompletionDataKeyHandler keyHandler, int declarationBegin, ITypeSymbol currentType, IMethodSymbol method, bool afterKeyword);

		/// <summary>
		/// Creates the event creation completion data.
		/// </summary>
		CompletionData CreateNewMethodDelegate (ICompletionDataKeyHandler keyHandler, ITypeSymbol delegateType, string varName, INamedTypeSymbol curType);
		ISymbolCompletionData CreateExistingMethodDelegate (ICompletionDataKeyHandler keyHandler, IMethodSymbol method);

		CompletionData CreateAnonymousMethod (ICompletionDataKeyHandler keyHandler, string displayText, string description, string textBeforeCaret, string textAfterCaret);

		CompletionData CreateObjectCreation (ICompletionDataKeyHandler keyHandler, ITypeSymbol typeToCreate, ISymbol symbol, int declarationBegin, bool afterKeyword);

		CompletionData CreateCastCompletionData (ICompletionDataKeyHandler keyHandler, ISymbol member, SyntaxNode nodeToCast, ITypeSymbol targetType);

		CompletionCategory CreateCompletionDataCategory (ISymbol forSymbol);
	}
}