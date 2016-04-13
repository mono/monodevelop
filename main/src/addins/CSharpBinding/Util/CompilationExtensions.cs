//
// CompilationExtensions.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Linq;
using MonoDevelop.Ide.TypeSystem;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class CompilationExtensions
	{
		static INamespaceSymbol FindNamespace(INamespaceSymbol globalNamespace, string fullName, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (string.IsNullOrEmpty(fullName))
				return globalNamespace;
			var stack = new Stack<INamespaceSymbol>();
			stack.Push(globalNamespace);

			while (stack.Count > 0) {
				if (cancellationToken.IsCancellationRequested)
					return null;
				var currentNs = stack.Pop();
				if (currentNs.GetFullName() == fullName)
					return currentNs;
				foreach (var childNamespace in currentNs.GetNamespaceMembers())
					stack.Push(childNamespace);
			}
			return null;
		}
		
		public static ITypeSymbol GetTypeSymbol(this Compilation compilation, string ns, string name, int arity, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			var nsSymbol = FindNamespace (compilation.GlobalNamespace, ns, cancellationToken);
			if (nsSymbol == null)
				return null;
			return nsSymbol.GetTypeMembers(name, arity).FirstOrDefault ();
		}
	}
}

