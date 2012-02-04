/*
Copyright (c) 2006-2010 ymnk, JCraft,Inc. All rights reserved.

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

This code is based on jsch (http://www.jcraft.com/jsch).
All credit should go to the authors of jsch.
*/

using System;
using System.IO;
using NSch;
using Sharpen;

namespace NSch
{
	public class IO
	{
		internal InputStream @in;

		internal OutputStream @out;

		internal OutputStream out_ext;

		private bool in_dontclose = false;

		private bool out_dontclose = false;

		private bool out_ext_dontclose = false;

		internal virtual void SetOutputStream(OutputStream @out)
		{
			this.@out = @out;
		}

		internal virtual void SetOutputStream(OutputStream @out, bool dontclose)
		{
			this.out_dontclose = dontclose;
			SetOutputStream(@out);
		}

		internal virtual void SetExtOutputStream(OutputStream @out)
		{
			this.out_ext = @out;
		}

		internal virtual void SetExtOutputStream(OutputStream @out, bool dontclose)
		{
			this.out_ext_dontclose = dontclose;
			SetExtOutputStream(@out);
		}

		internal virtual void SetInputStream(InputStream @in)
		{
			this.@in = @in;
		}

		internal virtual void SetInputStream(InputStream @in, bool dontclose)
		{
			this.in_dontclose = dontclose;
			SetInputStream(@in);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Net.Sockets.SocketException"></exception>
		public virtual void Put(Packet p)
		{
			@out.Write(p.buffer.buffer, 0, p.buffer.index);
			@out.Flush();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Put(byte[] array, int begin, int length)
		{
			@out.Write(array, begin, length);
			@out.Flush();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Put_ext(byte[] array, int begin, int length)
		{
			out_ext.Write(array, begin, length);
			out_ext.Flush();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual int GetByte()
		{
			return @in.Read();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void GetByte(byte[] array)
		{
			GetByte(array, 0, array.Length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void GetByte(byte[] array, int begin, int length)
		{
			do
			{
				int completed = @in.Read(array, begin, length);
				if (completed < 0)
				{
					throw new IOException("End of IO Stream Read");
				}
				begin += completed;
				length -= completed;
			}
			while (length > 0);
		}

		internal virtual void Out_close()
		{
			try
			{
				if (@out != null && !out_dontclose)
				{
					@out.Close();
				}
				@out = null;
			}
			catch (Exception)
			{
			}
		}

		public virtual void Close()
		{
			try
			{
				if (@in != null && !in_dontclose)
				{
					@in.Close();
				}
				@in = null;
			}
			catch (Exception)
			{
			}
			Out_close();
			try
			{
				if (out_ext != null && !out_ext_dontclose)
				{
					out_ext.Close();
				}
				out_ext = null;
			}
			catch (Exception)
			{
			}
		}
	}
}
