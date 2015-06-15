//
// SemanticEquivalence.cs
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
using System.Reflection;
using Microsoft.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.ComponentModel.Design;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class SemanticEquivalence
	{
		readonly static Type typeInfo;

		static SemanticEquivalence ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.Shared.Extensions.SemanticEquivalence" + ReflectionNamespaces.WorkspacesAsmName, true);
			areSemanticallyEquivalentMethod = typeInfo.GetMethod ("AreSemanticallyEquivalent", BindingFlags.Static | BindingFlags.Public);
		}

		static readonly MethodInfo areSemanticallyEquivalentMethod;

		public static bool AreSemanticallyEquivalent(
			SemanticModel semanticModel1,
			SemanticModel semanticModel2,
			SyntaxNode node1,
			SyntaxNode node2,
			Func<SyntaxNode, bool> predicate = null)
        {
			try {
				return (bool)areSemanticallyEquivalentMethod.Invoke (null, new object[] { 
					semanticModel1,
					semanticModel2,
					node1,
					node2,
					predicate
				});
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return false;
			}
        }

	}
}

