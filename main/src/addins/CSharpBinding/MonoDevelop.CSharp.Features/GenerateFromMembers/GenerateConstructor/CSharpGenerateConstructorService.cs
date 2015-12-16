//
// CSharpGenerateConstructorService.cs
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory6.CSharp.GenerateFromMembers.GenerateConstructor
{
	public class CSharpGenerateConstructorService :
	AbstractGenerateConstructorService<CSharpGenerateConstructorService, MemberDeclarationSyntax>
	{
		protected override Task<IList<MemberDeclarationSyntax>> GetSelectedMembersAsync(
			Document document, TextSpan textSpan, CancellationToken cancellationToken)
		{
			return GenerateFromMembersHelpers.GetSelectedMembersAsync(document, textSpan, cancellationToken);
		}

		protected override IEnumerable<ISymbol> GetDeclaredSymbols(
			SemanticModel semanticModel, MemberDeclarationSyntax memberDeclaration, CancellationToken cancellationToken)
		{
			return GenerateFromMembersHelpers.GetDeclaredSymbols(semanticModel, memberDeclaration, cancellationToken);
		}
	}

}

