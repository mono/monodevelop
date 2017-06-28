//
// ParameterHintingResult.cs
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

namespace MonoDevelop.Ide.CodeCompletion
{
	public class ParameterHintingResult : IReadOnlyList<ParameterHintingData>
	{
		public static readonly ParameterHintingResult Empty = new ParameterHintingResult (new List<ParameterHintingData> (), -1);

		protected readonly List<ParameterHintingData> data;
		/// <summary>
		/// Gets the start offset of the parameter expression node.
		/// </summary>
		public int StartOffset { 
			get;
			private set;
		}

		protected ParameterHintingResult (int startOffset)
		{
			this.data = new List<ParameterHintingData> ();
			this.StartOffset = startOffset;
		}

		public ParameterHintingResult (List<ParameterHintingData> data, int startOffset)
		{
			this.data = data;
			this.StartOffset = startOffset;
		}

		public override int GetHashCode ()
		{
			return StartOffset ^ data.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			return ReferenceEquals (this, obj);
		}

		#region IEnumerable implementation
		public IEnumerator<ParameterHintingData> GetEnumerator ()
		{
			return data.GetEnumerator ();
		}
		#endregion

		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return data.GetEnumerator ();
		}
		#endregion

		#region IReadOnlyList implementation
		public ParameterHintingData this [int index] {
			get {
				return data [index];
			}
		}
		#endregion

		#region IReadOnlyCollection implementation
		public int Count {
			get {
				return data.Count;
			}
		}
		#endregion
	}

}

