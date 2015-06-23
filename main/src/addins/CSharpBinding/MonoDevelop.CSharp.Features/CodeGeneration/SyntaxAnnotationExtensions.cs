//
// SyntaxAnnotationExtensions.cs
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
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace ICSharpCode.NRefactory6.CSharp.CodeGeneration
{
	#if NR6
	public
	#endif
	static class SyntaxAnnotationExtensions
	{
		readonly static Type typeInfo;

		static SyntaxAnnotationExtensions ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CodeGeneration.SyntaxAnnotationExtensions" + ReflectionNamespaces.WorkspacesAsmName, true);
			addAnnotationToSymbolMethod = typeInfo.GetMethod ("AddAnnotationToSymbol", BindingFlags.Public | BindingFlags.Static);
		}

		readonly static MethodInfo addAnnotationToSymbolMethod;

		public static TSymbol AddAnnotationToSymbol<TSymbol>(
			this SyntaxAnnotation annotation,
			TSymbol symbol)
			where TSymbol : ISymbol
		{
			try {
				return (TSymbol)addAnnotationToSymbolMethod.MakeGenericMethod (typeof(TSymbol)).Invoke (null, new object[] { annotation, symbol });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return default (TSymbol);
			}
		}
	}
}
