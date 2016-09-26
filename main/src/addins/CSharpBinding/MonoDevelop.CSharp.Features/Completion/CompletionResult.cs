//
// CompletionResult.cs
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
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class CompletionResult : IReadOnlyList<CompletionData>
	{
		public static readonly CompletionResult Empty = new CompletionResult ();

		readonly List<CompletionData> data = new List<CompletionData> ();

		public string DefaultCompletionString {
			get;
			internal set;
		}

		bool autoCompleteEmptyMatch = true;

		public bool AutoCompleteEmptyMatch {
			get { return autoCompleteEmptyMatch; }
			set { autoCompleteEmptyMatch = value; }
		}

		public bool AutoCompleteEmptyMatchOnCurlyBracket {
			get;
			set;
		}

		public bool AutoSelect {
			get;
			set;
		}

		public bool CloseOnSquareBrackets {
			get;
			set;
		}

		internal SyntaxContext SyntaxContext;

		public readonly List<IMethodSymbol> PossibleDelegates = new List<IMethodSymbol>();

		public List<CompletionData>.Enumerator GetEnumerator ()
		{
			return data.GetEnumerator ();
		}

		#region IReadOnlyList<ICompletionData> implemenation
		IEnumerator<CompletionData> IEnumerable<CompletionData>.GetEnumerator ()
		{
			return data.GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return data.GetEnumerator();
		}
		
		public CompletionData this[int index] {
			get {
				return data [index];
			}
		}

		public int Count {
			get {
				return data.Count;
			}
		}
		#endregion
		
		internal CompletionResult()
		{
			AutoSelect = true;
			AutoCompleteEmptyMatchOnCurlyBracket = true;
		}
		
		internal void AddData (CompletionData completionData)
		{
			var displayText = completionData.DisplayText;
			foreach (var od in data) {
				if (od.IsOverload (completionData) && completionData.IsOverload (od)) {
					od.AddOverload (completionData);
					return;
				}
			}
			data.Add(completionData); 
		}

		internal void AddRange (IEnumerable<CompletionData> completionData)
		{
			foreach (var cd in completionData) {
				AddData (cd);
			}
		}

		public static CompletionResult Create(IEnumerable<CompletionData> data)
		{
			var result = new CompletionResult();
			result.data.AddRange(data);
			return result;
		}
	}
}

