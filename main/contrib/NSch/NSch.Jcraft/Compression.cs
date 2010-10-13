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

using NSch.ZLib;
using Sharpen;

namespace NSch.Jcraft
{
	public class Compression : NSch.Compression
	{
		private const int BUF_SIZE = 4096;

		private int type;

		private ZStream stream;

		private byte[] tmpbuf = new byte[BUF_SIZE];

		public Compression()
		{
			stream = new ZStream();
		}

		public override void Init(int type, int level)
		{
			if (type == DEFLATER)
			{
				stream.DeflateInit(level);
				this.type = DEFLATER;
			}
			else
			{
				if (type == INFLATER)
				{
					stream.InflateInit();
					inflated_buf = new byte[BUF_SIZE];
					this.type = INFLATER;
				}
			}
		}

		private byte[] inflated_buf;

		public override int Compress(byte[] buf, int start, int len)
		{
			stream.next_in = buf;
			stream.next_in_index = start;
			stream.avail_in = len - start;
			int status;
			int outputlen = start;
			do
			{
				stream.next_out = tmpbuf;
				stream.next_out_index = 0;
				stream.avail_out = BUF_SIZE;
				status = stream.Deflate(JZlib.Z_PARTIAL_FLUSH);
				switch (status)
				{
					case JZlib.Z_OK:
					{
						System.Array.Copy(tmpbuf, 0, buf, outputlen, BUF_SIZE - stream.avail_out);
						outputlen += (BUF_SIZE - stream.avail_out);
						break;
					}

					default:
					{
						System.Console.Error.WriteLine("compress: deflate returnd " + status);
						break;
					}
				}
			}
			while (stream.avail_out == 0);
			return outputlen;
		}

		public override byte[] Uncompress(byte[] buffer, int start, int[] length)
		{
			int inflated_end = 0;
			stream.next_in = buffer;
			stream.next_in_index = start;
			stream.avail_in = length[0];
			while (true)
			{
				stream.next_out = tmpbuf;
				stream.next_out_index = 0;
				stream.avail_out = BUF_SIZE;
				int status = stream.Inflate(JZlib.Z_PARTIAL_FLUSH);
				switch (status)
				{
					case JZlib.Z_OK:
					{
						if (inflated_buf.Length < inflated_end + BUF_SIZE - stream.avail_out)
						{
							byte[] foo = new byte[inflated_end + BUF_SIZE - stream.avail_out];
							System.Array.Copy(inflated_buf, 0, foo, 0, inflated_end);
							inflated_buf = foo;
						}
						System.Array.Copy(tmpbuf, 0, inflated_buf, inflated_end, BUF_SIZE - stream.avail_out
							);
						inflated_end += (BUF_SIZE - stream.avail_out);
						length[0] = inflated_end;
						break;
					}

					case JZlib.Z_BUF_ERROR:
					{
						if (inflated_end > buffer.Length - start)
						{
							byte[] foo = new byte[inflated_end + start];
							System.Array.Copy(buffer, 0, foo, 0, start);
							System.Array.Copy(inflated_buf, 0, foo, start, inflated_end);
							buffer = foo;
						}
						else
						{
							System.Array.Copy(inflated_buf, 0, buffer, start, inflated_end);
						}
						length[0] = inflated_end;
						return buffer;
					}

					default:
					{
						System.Console.Error.WriteLine("uncompress: inflate returnd " + status);
						return null;
						break;
					}
				}
			}
		}
	}
}
