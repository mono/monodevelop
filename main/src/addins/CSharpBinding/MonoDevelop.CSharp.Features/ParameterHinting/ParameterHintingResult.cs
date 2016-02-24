//
// ParameterHintingResult.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class ParameterHintingResult : IReadOnlyList<IParameterHintingData>
	{
		public static readonly ParameterHintingResult Empty = new ParameterHintingResult (-1);
		
		readonly List<IParameterHintingData> data = new List<IParameterHintingData> ();
		
		/// <summary>
		/// Gets the start offset of the parameter expression node.
		/// </summary>
		public int StartOffset { 
			get;
			private set;
		}

		#region IReadOnlyList<IParameterHintingData> implementation
		
		public IEnumerator<IParameterHintingData> GetEnumerator()
		{
			return data.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)data).GetEnumerator();
		}
		
		public IParameterHintingData this[int index] {
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
		
		internal protected ParameterHintingResult(int startOffset)
		{
			this.StartOffset = startOffset;
		}
		
		internal protected void AddData (IParameterHintingData parameterHintingData)
		{
			data.Add(parameterHintingData); 
		}
		
		internal protected void AddRange (IEnumerable<IParameterHintingData> parameterHintingDataCollection)
		{
			data.AddRange(parameterHintingDataCollection); 
		}
	}
}

