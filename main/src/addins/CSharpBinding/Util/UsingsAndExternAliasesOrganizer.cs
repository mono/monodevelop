//
// UsingsAndExternAliasesOrganizer.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.ExceptionServices;
using System.Reflection;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class UsingsAndExternAliasesOrganizer
	{
		static UsingsAndExternAliasesOrganizer ()
		{
			var typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CSharp.Utilities.UsingsAndExternAliasesOrganizer" + ReflectionNamespaces.CSWorkspacesAsmName, true);

			organizeMethod = typeInfo.GetMethod ("Organize");
		}

		readonly static System.Reflection.MethodInfo organizeMethod;

		public static void Organize(
			SyntaxList<ExternAliasDirectiveSyntax> externAliasList,
			SyntaxList<UsingDirectiveSyntax> usingList,
			bool placeSystemNamespaceFirst,
			out SyntaxList<ExternAliasDirectiveSyntax> organizedExternAliasList,
			out SyntaxList<UsingDirectiveSyntax> organizedUsingList)
		{
			try {
				var args = new object[] { externAliasList, usingList, placeSystemNamespaceFirst, default(SyntaxList<ExternAliasDirectiveSyntax>), default(SyntaxList<ExternAliasDirectiveSyntax>), default(SyntaxList<UsingDirectiveSyntax>)};
				organizeMethod.Invoke (null, args);
				organizedExternAliasList = (SyntaxList<ExternAliasDirectiveSyntax>)args [3];
				organizedUsingList = (SyntaxList<UsingDirectiveSyntax>)args [4];
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			}
		}
	}
}

