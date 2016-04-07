//
// IAssemblySymbolExtensions.cs
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
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class IAssemblySymbolExtensions
	{
		private const string AttributeSuffix = "Attribute";

		public static bool ContainsNamespaceName(
			this List<IAssemblySymbol> assemblies,
			string namespaceName)
		{
			// PERF: Expansion of "assemblies.Any(a => a.NamespaceNames.Contains(namespaceName))"
			// to avoid allocating a lambda.
			foreach (var a in assemblies)
			{
				if (a.NamespaceNames.Contains(namespaceName))
				{
					return true;
				}
			}

			return false;
		}

		public static bool ContainsTypeName(this List<IAssemblySymbol> assemblies, string typeName, bool tryWithAttributeSuffix = false)
		{
			if (!tryWithAttributeSuffix)
			{
				// PERF: Expansion of "assemblies.Any(a => a.TypeNames.Contains(typeName))"
				// to avoid allocating a lambda.
				foreach (var a in assemblies)
				{
					if (a.TypeNames.Contains(typeName))
					{
						return true;
					}
				}
			}
			else
			{
				var attributeName = typeName + AttributeSuffix;
				foreach (var a in assemblies)
				{
					var typeNames = a.TypeNames;
					if (typeNames.Contains(typeName) || typeNames.Contains(attributeName))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}

