/*
Copyright (c) 2000,2001,2002,2003 ymnk, JCraft,Inc. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

  1. Redistributions of source code must retain the above copyright notice,
     this list of conditions and the following disclaimer.

  2. Redistributions in binary form must reproduce the above copyright 
     notice, this list of conditions and the following disclaimer in 
     the documentation and/or other materials provided with the distribution.

  3. The names of the authors may not be used to endorse or promote products
     derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

This program is based on zlib-1.1.3, so all credit should go authors
Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
and contributors of zlib.
*/

using NSch.ZLib;
using Sharpen;

namespace NSch.ZLib
{
	public class ZInputStream : FilterInputStream
	{
		protected internal ZStream z = new ZStream();

		protected internal int bufsize = 512;

		protected internal int flush = JZlib.Z_NO_FLUSH;

		protected internal byte[] buf = new byte[512];

		protected internal byte[] buf1 = new byte[1];

		protected internal bool compress;

		protected internal InputStream @in = null;

		protected ZInputStream(InputStream @in) : this(@in, false)
		{
		}

		public ZInputStream(InputStream @in, bool nowrap) : base(@in)
		{
			this.@in = @in;
			z.InflateInit(nowrap);
			compress = false;
			z.next_in = buf;
			z.next_in_index = 0;
			z.avail_in = 0;
		}

		public ZInputStream(InputStream @in, int level) : base(@in)
		{
			this.@in = @in;
			z.DeflateInit(level);
			compress = true;
			z.next_in = buf;
			z.next_in_index = 0;
			z.avail_in = 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read()
		{
			if (Read(buf1, 0, 1) == -1)
			{
				return (-1);
			}
			return (buf1[0] & unchecked((int)(0xFF)));
		}

		private bool nomoreinput = false;

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] b, int off, int len)
		{
			if (len == 0)
			{
				return (0);
			}
			int err;
			z.next_out = b;
			z.next_out_index = off;
			z.avail_out = len;
			do
			{
				if ((z.avail_in == 0) && (!nomoreinput))
				{
					// if buffer is empty and more input is avaiable, refill it
					z.next_in_index = 0;
					z.avail_in = @in.Read(buf, 0, bufsize);
					//(bufsize<z.avail_out ? bufsize : z.avail_out));
					if (z.avail_in == -1)
					{
						z.avail_in = 0;
						nomoreinput = true;
					}
				}
				if (compress)
				{
					err = z.Deflate(flush);
				}
				else
				{
					err = z.Inflate(flush);
				}
				if (nomoreinput && (err == JZlib.Z_BUF_ERROR))
				{
					return (-1);
				}
				if (err != JZlib.Z_OK && err != JZlib.Z_STREAM_END)
				{
					throw new ZStreamException((compress ? "de" : "in") + "flating: " + z.msg);
				}
				if ((nomoreinput || err == JZlib.Z_STREAM_END) && (z.avail_out == len))
				{
					return (-1);
				}
			}
			while (z.avail_out == len && err == JZlib.Z_OK);
			//System.err.print("("+(len-z.avail_out)+")");
			return (len - z.avail_out);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long Skip(long n)
		{
			int len = 512;
			if (n < len)
			{
				len = (int)n;
			}
			byte[] tmp = new byte[len];
			return ((long)Read(tmp));
		}

		public virtual int GetFlushMode()
		{
			return (flush);
		}

		public virtual void SetFlushMode(int flush)
		{
			this.flush = flush;
		}

		/// <summary>Returns the total number of bytes input so far.</summary>
		/// <remarks>Returns the total number of bytes input so far.</remarks>
		public virtual long GetTotalIn()
		{
			return z.total_in;
		}

		/// <summary>Returns the total number of bytes output so far.</summary>
		/// <remarks>Returns the total number of bytes output so far.</remarks>
		public virtual long GetTotalOut()
		{
			return z.total_out;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			@in.Close();
		}
	}
}
