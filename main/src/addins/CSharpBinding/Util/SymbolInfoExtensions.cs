//
// SymbolInfoExtensions.cs
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
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class SymbolInfoExtensions
	{
		public static IEnumerable<ISymbol> GetAllSymbols(this SymbolInfo info)
		{
			return GetAllSymbolsWorker(info).Distinct();
		}

		private static IEnumerable<ISymbol> GetAllSymbolsWorker(this SymbolInfo info)
		{
			if (info.Symbol != null)
			{
				yield return info.Symbol;
			}

			foreach (var symbol in info.CandidateSymbols)
			{
				yield return symbol;
			}
		}

		public static ISymbol GetAnySymbol(this SymbolInfo info)
		{
			return info.GetAllSymbols().FirstOrDefault();
		}

		public static IEnumerable<ISymbol> GetBestOrAllSymbols(this SymbolInfo info)
		{
			if (info.Symbol != null)
			{
				return SpecializedCollections.SingletonEnumerable(info.Symbol);
			}
			else if (info.CandidateSymbols.Length > 0)
			{
				return info.CandidateSymbols;
			}

			return SpecializedCollections.EmptyEnumerable<ISymbol>();
		}
	}
}

