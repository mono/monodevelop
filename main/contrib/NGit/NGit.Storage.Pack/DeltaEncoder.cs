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
using NGit;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>
	/// Encodes an instruction stream for
	/// <see cref="BinaryDelta">BinaryDelta</see>
	/// .
	/// </summary>
	public class DeltaEncoder
	{
		/// <summary>Maximum number of bytes to be copied in pack v2 format.</summary>
		/// <remarks>
		/// Maximum number of bytes to be copied in pack v2 format.
		/// <p>
		/// Historical limitations have this at 64k, even though current delta
		/// decoders recognize larger copy instructions.
		/// </remarks>
		private const int MAX_V2_COPY = unchecked((int)(0x10000));

		/// <summary>Maximum number of bytes used by a copy instruction.</summary>
		/// <remarks>Maximum number of bytes used by a copy instruction.</remarks>
		private const int MAX_COPY_CMD_SIZE = 8;

		/// <summary>Maximum length that an an insert command can encode at once.</summary>
		/// <remarks>Maximum length that an an insert command can encode at once.</remarks>
		private const int MAX_INSERT_DATA_SIZE = 127;

		private readonly OutputStream @out;

		private readonly byte[] buf = new byte[MAX_COPY_CMD_SIZE * 4];

		private readonly int limit;

		private int size;

		/// <summary>Create an encoder with no upper bound on the instruction stream size.</summary>
		/// <remarks>Create an encoder with no upper bound on the instruction stream size.</remarks>
		/// <param name="out">buffer to store the instructions written.</param>
		/// <param name="baseSize">size of the base object, in bytes.</param>
		/// <param name="resultSize">
		/// size of the resulting object, after applying this instruction
		/// stream to the base object, in bytes.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// the output buffer cannot store the instruction stream's
		/// header with the size fields.
		/// </exception>
		public DeltaEncoder(OutputStream @out, long baseSize, long resultSize) : this(@out
			, baseSize, resultSize, 0)
		{
		}

		/// <summary>Create an encoder with an upper limit on the instruction size.</summary>
		/// <remarks>Create an encoder with an upper limit on the instruction size.</remarks>
		/// <param name="out">buffer to store the instructions written.</param>
		/// <param name="baseSize">size of the base object, in bytes.</param>
		/// <param name="resultSize">
		/// size of the resulting object, after applying this instruction
		/// stream to the base object, in bytes.
		/// </param>
		/// <param name="limit">
		/// maximum number of bytes to write to the out buffer declaring
		/// the stream is over limit and should be discarded. May be 0 to
		/// specify an infinite limit.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// the output buffer cannot store the instruction stream's
		/// header with the size fields.
		/// </exception>
		public DeltaEncoder(OutputStream @out, long baseSize, long resultSize, int limit)
		{
			// private static final int MAX_V3_COPY = (0xff << 16) | (0xff << 8) | 0xff;
			this.@out = @out;
			this.limit = limit;
			WriteVarint(baseSize);
			WriteVarint(resultSize);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteVarint(long sz)
		{
			int p = 0;
			while (sz >= unchecked((int)(0x80)))
			{
				buf[p++] = unchecked((byte)(unchecked((int)(0x80)) | (((int)sz) & unchecked((int)
					(0x7f)))));
				sz = (long)(((ulong)sz) >> 7);
			}
			buf[p++] = unchecked((byte)(((int)sz) & unchecked((int)(0x7f))));
			size += p;
			if (limit <= 0 || size < limit)
			{
				@out.Write(buf, 0, p);
			}
		}

		/// <returns>current size of the delta stream, in bytes.</returns>
		public virtual int GetSize()
		{
			return size;
		}

		/// <summary>Insert a literal string of text, in UTF-8 encoding.</summary>
		/// <remarks>Insert a literal string of text, in UTF-8 encoding.</remarks>
		/// <param name="text">the string to insert.</param>
		/// <returns>
		/// true if the insert fits within the limit; false if the insert
		/// would cause the instruction stream to exceed the limit.
		/// </returns>
		/// <exception cref="System.IO.IOException">the instruction buffer can't store the instructions.
		/// 	</exception>
		public virtual bool Insert(string text)
		{
			return Insert(Constants.Encode(text));
		}

		/// <summary>Insert a literal binary sequence.</summary>
		/// <remarks>Insert a literal binary sequence.</remarks>
		/// <param name="text">the binary to insert.</param>
		/// <returns>
		/// true if the insert fits within the limit; false if the insert
		/// would cause the instruction stream to exceed the limit.
		/// </returns>
		/// <exception cref="System.IO.IOException">the instruction buffer can't store the instructions.
		/// 	</exception>
		public virtual bool Insert(byte[] text)
		{
			return Insert(text, 0, text.Length);
		}

		/// <summary>Insert a literal binary sequence.</summary>
		/// <remarks>Insert a literal binary sequence.</remarks>
		/// <param name="text">the binary to insert.</param>
		/// <param name="off">
		/// offset within
		/// <code>text</code>
		/// to start copying from.
		/// </param>
		/// <param name="cnt">number of bytes to insert.</param>
		/// <returns>
		/// true if the insert fits within the limit; false if the insert
		/// would cause the instruction stream to exceed the limit.
		/// </returns>
		/// <exception cref="System.IO.IOException">the instruction buffer can't store the instructions.
		/// 	</exception>
		public virtual bool Insert(byte[] text, int off, int cnt)
		{
			if (cnt <= 0)
			{
				return true;
			}
			if (0 < limit)
			{
				int hdrs = cnt / MAX_INSERT_DATA_SIZE;
				if (cnt % MAX_INSERT_DATA_SIZE != 0)
				{
					hdrs++;
				}
				if (limit < size + hdrs + cnt)
				{
					return false;
				}
			}
			do
			{
				int n = Math.Min(MAX_INSERT_DATA_SIZE, cnt);
				@out.Write(unchecked((byte)n));
				@out.Write(text, off, n);
				off += n;
				cnt -= n;
				size += 1 + n;
			}
			while (0 < cnt);
			return true;
		}

		/// <summary>Create a copy instruction to copy from the base object.</summary>
		/// <remarks>Create a copy instruction to copy from the base object.</remarks>
		/// <param name="offset">
		/// position in the base object to copy from. This is absolute,
		/// from the beginning of the base.
		/// </param>
		/// <param name="cnt">number of bytes to copy.</param>
		/// <returns>
		/// true if the copy fits within the limit; false if the copy
		/// would cause the instruction stream to exceed the limit.
		/// </returns>
		/// <exception cref="System.IO.IOException">the instruction buffer cannot store the instructions.
		/// 	</exception>
		public virtual bool Copy(long offset, int cnt)
		{
			if (cnt == 0)
			{
				return true;
			}
			int p = 0;
			// We cannot encode more than MAX_V2_COPY bytes in a single
			// command, so encode that much and start a new command.
			// This limit is imposed by the pack file format rules.
			//
			while (MAX_V2_COPY < cnt)
			{
				p = EncodeCopy(p, offset, MAX_V2_COPY);
				offset += MAX_V2_COPY;
				cnt -= MAX_V2_COPY;
				if (buf.Length < p + MAX_COPY_CMD_SIZE)
				{
					if (0 < limit && limit < size + p)
					{
						return false;
					}
					@out.Write(buf, 0, p);
					size += p;
					p = 0;
				}
			}
			p = EncodeCopy(p, offset, cnt);
			if (0 < limit && limit < size + p)
			{
				return false;
			}
			@out.Write(buf, 0, p);
			size += p;
			return true;
		}

		private int EncodeCopy(int p, long offset, int cnt)
		{
			int cmd = unchecked((int)(0x80));
			int cmdPtr = p++;
			// save room for the command
			if ((offset & unchecked((int)(0xff))) != 0)
			{
				cmd |= unchecked((int)(0x01));
				buf[p++] = unchecked((byte)(offset & unchecked((int)(0xff))));
			}
			if ((offset & (unchecked((int)(0xff)) << 8)) != 0)
			{
				cmd |= unchecked((int)(0x02));
				buf[p++] = unchecked((byte)(((long)(((ulong)offset) >> 8)) & unchecked((int)(0xff
					))));
			}
			if ((offset & (unchecked((int)(0xff)) << 16)) != 0)
			{
				cmd |= unchecked((int)(0x04));
				buf[p++] = unchecked((byte)(((long)(((ulong)offset) >> 16)) & unchecked((int)(0xff
					))));
			}
			if ((offset & (unchecked((int)(0xff)) << 24)) != 0)
			{
				cmd |= unchecked((int)(0x08));
				buf[p++] = unchecked((byte)(((long)(((ulong)offset) >> 24)) & unchecked((int)(0xff
					))));
			}
			if (cnt != MAX_V2_COPY)
			{
				if ((cnt & unchecked((int)(0xff))) != 0)
				{
					cmd |= unchecked((int)(0x10));
					buf[p++] = unchecked((byte)(cnt & unchecked((int)(0xff))));
				}
				if ((cnt & (unchecked((int)(0xff)) << 8)) != 0)
				{
					cmd |= unchecked((int)(0x20));
					buf[p++] = unchecked((byte)(((int)(((uint)cnt) >> 8)) & unchecked((int)(0xff))));
				}
				if ((cnt & (unchecked((int)(0xff)) << 16)) != 0)
				{
					cmd |= unchecked((int)(0x40));
					buf[p++] = unchecked((byte)(((int)(((uint)cnt) >> 16)) & unchecked((int)(0xff))));
				}
			}
			buf[cmdPtr] = unchecked((byte)cmd);
			return p;
		}
	}
}
