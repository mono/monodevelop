//
// RoslynCompletionCategory.cs
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

using System.Linq;
using ICSharpCode.NRefactory6.CSharp.Completion;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynCompletionCategory : CompletionCategory
	{
		readonly ISymbol symbol;

		public RoslynCompletionCategory (ISymbol symbol)
		{
			this.symbol = symbol;
			this.DisplayText = Ambience.EscapeText (symbol.ToDisplayString (MonoDevelop.Ide.TypeSystem.Ambience.NameFormat));
			this.Icon = MonoDevelop.Ide.TypeSystem.Stock.GetStockIcon (symbol);
		}

		public override int CompareTo (CompletionCategory other)
		{
			if (other == null)
				return 1;
			var t1 = symbol as INamedTypeSymbol;
			if (other is DelegateCreationContextHandler.DelegateCreationCategory)
				return 1;
			var t2 = ((RoslynCompletionCategory)other).symbol as INamedTypeSymbol;
			if (t1 != null && t2 != null) {
				if (t1.AllInterfaces.Contains (t2) || t1.GetBaseTypes().Contains (t2))
					return -1;
				if (t2.AllInterfaces.Contains (t1) || t2.GetBaseTypes().Contains (t1))
					return 1;
			}

			return this.DisplayText.CompareTo (other.DisplayText);
		}
	}
}

