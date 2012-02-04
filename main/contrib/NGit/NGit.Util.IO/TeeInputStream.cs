/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using Sharpen;

namespace NGit.Util.IO
{
	/// <summary>Input stream that copies data read to another output stream.</summary>
	/// <remarks>
	/// Input stream that copies data read to another output stream.
	/// This stream is primarily useful with a
	/// <see cref="NGit.Util.TemporaryBuffer">NGit.Util.TemporaryBuffer</see>
	/// , where any
	/// data read or skipped by the caller is also duplicated into the temporary
	/// buffer. Later the temporary buffer can then be used instead of the original
	/// source stream.
	/// During close this stream copies any remaining data from the source stream
	/// into the destination stream.
	/// </remarks>
	public class TeeInputStream : InputStream
	{
		private byte[] skipBuffer;

		private InputStream src;

		private OutputStream dst;

		/// <summary>Initialize a tee input stream.</summary>
		/// <remarks>Initialize a tee input stream.</remarks>
		/// <param name="src">source stream to consume.</param>
		/// <param name="dst">
		/// destination to copy the source to as it is consumed. Typically
		/// this is a
		/// <see cref="NGit.Util.TemporaryBuffer">NGit.Util.TemporaryBuffer</see>
		/// .
		/// </param>
		public TeeInputStream(InputStream src, OutputStream dst)
		{
			this.src = src;
			this.dst = dst;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read()
		{
			byte[] b = SkipBuffer();
			int n = Read(b, 0, 1);
			return n == 1 ? b[0] & unchecked((int)(0xff)) : -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long Skip(long cnt)
		{
			long skipped = 0;
			byte[] b = SkipBuffer();
			while (0 < cnt)
			{
				int n = src.Read(b, 0, (int)Math.Min(b.Length, cnt));
				if (n <= 0)
				{
					break;
				}
				dst.Write(b, 0, n);
				skipped += n;
				cnt -= n;
			}
			return skipped;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] b, int off, int len)
		{
			if (len == 0)
			{
				return 0;
			}
			int n = src.Read(b, off, len);
			if (0 < n)
			{
				dst.Write(b, off, n);
			}
			return n;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			byte[] b = SkipBuffer();
			for (; ; )
			{
				int n = src.Read(b);
				if (n <= 0)
				{
					break;
				}
				dst.Write(b, 0, n);
			}
			dst.Close();
			src.Close();
		}

		private byte[] SkipBuffer()
		{
			if (skipBuffer == null)
			{
				skipBuffer = new byte[2048];
			}
			return skipBuffer;
		}
	}
}
