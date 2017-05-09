//
// ImmutableTextTextReader.cs
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
using System.IO;

namespace Mono.TextEditor.Utils
{
	sealed class ImmutableTextTextReader : TextReader
	{
		readonly ImmutableText immutableText;

		int index;

		public ImmutableTextTextReader(ImmutableText immutableText)
		{
			if (immutableText == null)
				throw new ArgumentNullException(nameof (immutableText));
			this.immutableText = immutableText;
		}

		public override int Peek()
		{
			if (index >= immutableText.Length)
				return -1;
			return immutableText[index];
		}

		public override int Read()
		{
			if (index >= immutableText.Length)
				return -1;
			return immutableText[index++];
		}

		public override int Read(char[] buffer, int index, int count)
		{
			if (immutableText == null)
				return 0;
			count = System.Math.Min (this.index + count, immutableText.Length) - this.index;
			if (count <= 0)
				return  0;
			immutableText.CopyTo (this.index, buffer, index, count);
			this.index += count;
			return count;
		}
	}
}