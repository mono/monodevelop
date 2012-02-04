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
using NGit.Diff;
using Sharpen;

namespace NGit.Util.IO
{
	/// <summary>An OutputStream that expands LF to CRLF.</summary>
	/// <remarks>
	/// An OutputStream that expands LF to CRLF.
	/// <p>
	/// Existing CRLF are not expanded to CRCRLF, but retained as is.
	/// </remarks>
	public class AutoCRLFOutputStream : OutputStream
	{
		private readonly OutputStream @out;

		private int buf = -1;

		private byte[] binbuf = new byte[8000];

		private int binbufcnt = 0;

		private bool isBinary;

		/// <param name="out"></param>
		public AutoCRLFOutputStream(OutputStream @out)
		{
			this.@out = @out;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(int b)
		{
			int overflow = Buffer(unchecked((byte)b));
			if (overflow >= 0)
			{
				return;
			}
			if (isBinary)
			{
				@out.Write(b);
				return;
			}
			if (b == '\n')
			{
				if (buf == '\r')
				{
					@out.Write('\n');
					buf = -1;
				}
				else
				{
					if (buf == -1)
					{
						@out.Write('\r');
						@out.Write('\n');
						buf = -1;
					}
				}
			}
			else
			{
				if (b == '\r')
				{
					@out.Write(b);
					buf = '\r';
				}
				else
				{
					@out.Write(b);
					buf = -1;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(byte[] b)
		{
			int overflow = Buffer(b, 0, b.Length);
			if (overflow > 0)
			{
				Write(b, b.Length - overflow, overflow);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(byte[] b, int off, int len)
		{
			int overflow = Buffer(b, off, len);
			if (overflow < 0)
			{
				return;
			}
			off = off + len - overflow;
			len = overflow;
			if (len == 0)
			{
				return;
			}
			int lastw = off;
			if (isBinary)
			{
				@out.Write(b, off, len);
				return;
			}
			for (int i = off; i < off + len; ++i)
			{
				byte c = b[i];
				if (c == '\r')
				{
					buf = '\r';
				}
				else
				{
					if (c == '\n')
					{
						if (buf != '\r')
						{
							if (lastw < i)
							{
								@out.Write(b, lastw, i - lastw);
							}
							@out.Write('\r');
							lastw = i;
						}
						buf = -1;
					}
					else
					{
						buf = -1;
					}
				}
			}
			if (lastw < off + len)
			{
				@out.Write(b, lastw, off + len - lastw);
			}
			if (b[off + len - 1] == '\r')
			{
				buf = '\r';
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int Buffer(byte b)
		{
			if (binbufcnt > binbuf.Length)
			{
				return 1;
			}
			binbuf[binbufcnt++] = b;
			if (binbufcnt == binbuf.Length)
			{
				DecideMode();
			}
			return 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int Buffer(byte[] b, int off, int len)
		{
			if (binbufcnt > binbuf.Length)
			{
				return len;
			}
			int copy = Math.Min(binbuf.Length - binbufcnt, len);
			System.Array.Copy(b, off, binbuf, binbufcnt, copy);
			binbufcnt += copy;
			int remaining = len - copy;
			if (remaining > 0)
			{
				DecideMode();
			}
			return remaining;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DecideMode()
		{
			isBinary = RawText.IsBinary(binbuf, binbufcnt);
			int cachedLen = binbufcnt;
			binbufcnt = binbuf.Length + 1;
			// full!
			Write(binbuf, 0, cachedLen);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Flush()
		{
			if (binbufcnt < binbuf.Length)
			{
				DecideMode();
			}
			buf = -1;
			@out.Flush();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			Flush();
			@out.Close();
		}
	}
}
