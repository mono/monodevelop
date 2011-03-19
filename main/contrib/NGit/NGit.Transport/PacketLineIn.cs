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
using System.IO;
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Transport
{
	public class PacketLineIn
	{
		internal static readonly string END = new StringBuilder(0).ToString();

		internal enum AckNackResult
		{
			NAK,
			ACK,
			ACK_CONTINUE,
			ACK_COMMON,
			ACK_READY
		}

		private readonly InputStream @in;

		private readonly byte[] lineBuffer;

		internal PacketLineIn(InputStream i)
		{
			@in = i;
			lineBuffer = new byte[SideBandOutputStream.SMALL_BUF];
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual PacketLineIn.AckNackResult ReadACK(MutableObjectId returnedId)
		{
			string line = ReadString();
			if (line.Length == 0)
			{
				throw new PackProtocolException(JGitText.Get().expectedACKNAKFoundEOF);
			}
			if ("NAK".Equals(line))
			{
				return PacketLineIn.AckNackResult.NAK;
			}
			if (line.StartsWith("ACK "))
			{
				returnedId.FromString(Sharpen.Runtime.Substring(line, 4, 44));
				if (line.Length == 44)
				{
					return PacketLineIn.AckNackResult.ACK;
				}
				string arg = Sharpen.Runtime.Substring(line, 44);
				if (arg.Equals(" continue"))
				{
					return PacketLineIn.AckNackResult.ACK_CONTINUE;
				}
				else
				{
					if (arg.Equals(" common"))
					{
						return PacketLineIn.AckNackResult.ACK_COMMON;
					}
					else
					{
						if (arg.Equals(" ready"))
						{
							return PacketLineIn.AckNackResult.ACK_READY;
						}
					}
				}
			}
			if (line.StartsWith("ERR "))
			{
				throw new PackProtocolException(Sharpen.Runtime.Substring(line, 4));
			}
			throw new PackProtocolException(MessageFormat.Format(JGitText.Get().expectedACKNAKGot
				, line));
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual string ReadString()
		{
			int len = ReadLength();
			if (len == 0)
			{
				return END;
			}
			len -= 4;
			// length header (4 bytes)
			if (len == 0)
			{
				return string.Empty;
			}
			byte[] raw;
			if (len <= lineBuffer.Length)
			{
				raw = lineBuffer;
			}
			else
			{
				raw = new byte[len];
			}
			IOUtil.ReadFully(@in, raw, 0, len);
			if (raw[len - 1] == '\n')
			{
				len--;
			}
			return RawParseUtils.Decode(Constants.CHARSET, raw, 0, len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual string ReadStringRaw()
		{
			int len = ReadLength();
			if (len == 0)
			{
				return END;
			}
			len -= 4;
			// length header (4 bytes)
			byte[] raw;
			if (len <= lineBuffer.Length)
			{
				raw = lineBuffer;
			}
			else
			{
				raw = new byte[len];
			}
			IOUtil.ReadFully(@in, raw, 0, len);
			return RawParseUtils.Decode(Constants.CHARSET, raw, 0, len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual int ReadLength()
		{
			IOUtil.ReadFully(@in, lineBuffer, 0, 4);
			try
			{
				int len = RawParseUtils.ParseHexInt16(lineBuffer, 0);
				if (len != 0 && len < 4)
				{
					throw new IndexOutOfRangeException();
				}
				return len;
			}
			catch (IndexOutOfRangeException)
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().invalidPacketLineHeader
					, string.Empty + (char)lineBuffer[0] + (char)lineBuffer[1] + (char)lineBuffer[2]
					 + (char)lineBuffer[3]));
			}
		}
	}
}
