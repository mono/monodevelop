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
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class ParameterHintingResult : IReadOnlyList<ParameterHintingData>
	{
		public static readonly ParameterHintingResult Empty = new ParameterHintingResult (new List<ParameterHintingData> ());

		protected readonly List<ParameterHintingData> data;

		public int? SelectedItemIndex { get; set; }

		public TextSpan ApplicableSpan { get; set; }

		/// <summary>Used for positioning the parameter list tooltip</summary>
		public int ParameterListStart { get; set; }

		[Obsolete("Use ParameterHintingResult (List<ParameterHintingData> data). startOffset got replaced with ApplicableSpan.")]
		public ParameterHintingResult (List<ParameterHintingData> data, int startOffset)
		{
			this.data = data;
			ParameterListStart = startOffset;
			ApplicableSpan = new TextSpan (startOffset, int.MaxValue);
		}

		public ParameterHintingResult (List<ParameterHintingData> data)
		{
			this.data = data;
		}

		public override int GetHashCode ()
		{
			return ApplicableSpan.GetHashCode () ^ data.GetHashCode ();
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

