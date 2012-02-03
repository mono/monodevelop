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

using System.IO;
using NSch.ZLib;
using Sharpen;

namespace NSch.ZLib
{
	public class ZOutputStream : OutputStream
	{
		protected internal ZStream z = new ZStream();

		protected internal int bufsize = 512;

		protected internal int flush = JZlib.Z_NO_FLUSH;

		protected internal byte[] buf = new byte[512];

		protected internal byte[] buf1 = new byte[1];

		protected internal bool compress;

		protected internal OutputStream @out;

		public ZOutputStream(OutputStream @out) : base()
		{
			this.@out = @out;
			z.InflateInit();
			compress = false;
		}

		public ZOutputStream(OutputStream @out, int level) : this(@out, level, false)
		{
		}

		public ZOutputStream(OutputStream @out, int level, bool nowrap) : base()
		{
			this.@out = @out;
			z.DeflateInit(level, nowrap);
			compress = true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(int b)
		{
			buf1[0] = unchecked((byte)b);
			Write(buf1, 0, 1);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(byte[] b, int off, int len)
		{
			if (len == 0)
			{
				return;
			}
			int err;
			z.next_in = b;
			z.next_in_index = off;
			z.avail_in = len;
			do
			{
				z.next_out = buf;
				z.next_out_index = 0;
				z.avail_out = bufsize;
				if (compress)
				{
					err = z.Deflate(flush);
				}
				else
				{
					err = z.Inflate(flush);
				}
				if (err != JZlib.Z_OK)
				{
					throw new ZStreamException((compress ? "de" : "in") + "flating: " + z.msg);
				}
				@out.Write(buf, 0, bufsize - z.avail_out);
			}
			while (z.avail_in > 0 || z.avail_out == 0);
		}

		public virtual int GetFlushMode()
		{
			return (flush);
		}

		public virtual void SetFlushMode(int flush)
		{
			this.flush = flush;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Finish()
		{
			int err;
			do
			{
				z.next_out = buf;
				z.next_out_index = 0;
				z.avail_out = bufsize;
				if (compress)
				{
					err = z.Deflate(JZlib.Z_FINISH);
				}
				else
				{
					err = z.Inflate(JZlib.Z_FINISH);
				}
				if (err != JZlib.Z_STREAM_END && err != JZlib.Z_OK)
				{
					throw new ZStreamException((compress ? "de" : "in") + "flating: " + z.msg);
				}
				if (bufsize - z.avail_out > 0)
				{
					@out.Write(buf, 0, bufsize - z.avail_out);
				}
			}
			while (z.avail_in > 0 || z.avail_out == 0);
			Flush();
		}

		public virtual void End()
		{
			if (z == null)
			{
				return;
			}
			if (compress)
			{
				z.DeflateEnd();
			}
			else
			{
				z.InflateEnd();
			}
			z.Free();
			z = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				try
				{
					Finish();
				}
				catch (IOException)
				{
				}
			}
			finally
			{
				End();
				@out.Close();
				@out = null;
			}
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
		public override void Flush()
		{
			@out.Flush();
		}
	}
}
