//
// StreamStringReadWriter.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using System.Text;

namespace GitAskPass
{
	sealed class StreamStringReadWriter
	{
		readonly Stream baseStream;

		public StreamStringReadWriter (Stream baseStream)
		{
			this.baseStream = baseStream ?? throw new ArgumentNullException (nameof (baseStream));
		}

		public string ReadLine ()
		{
			int len = 0;
			for (int i = 0; i < 4; i++)
				len |= (int)(baseStream.ReadByte () << (8 * i));

			var stringBuffer = new byte [len];
			baseStream.Read (stringBuffer, 0, len);

			return Encoding.UTF8.GetString (stringBuffer);
		}

		public void WriteLine (string value)
		{
			if (value is null)
				throw new ArgumentNullException (nameof (value));
			var buffer = Encoding.UTF8.GetBytes (value);
			if (buffer.LongLength > int.MaxValue)
				throw new InvalidOperationException ("string too long : " + buffer.LongLength);
			int len = buffer.Length;
			for (int i = 0; i < 4; i++) {
				baseStream.WriteByte ((byte)(len & 255));
				len >>= 8;
			}
			baseStream.Write (buffer, 0, buffer.Length);
			baseStream.Flush ();
		}
	}
}
