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
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Multiplexes data and progress messages.</summary>
	/// <remarks>
	/// Multiplexes data and progress messages.
	/// <p>
	/// This stream is buffered at packet sizes, so the caller doesn't need to wrap
	/// it in yet another buffered stream.
	/// </remarks>
	public class SideBandOutputStream : OutputStream
	{
		/// <summary>Channel used for pack data.</summary>
		/// <remarks>Channel used for pack data.</remarks>
		public const int CH_DATA = SideBandInputStream.CH_DATA;

		/// <summary>Channel used for progress messages.</summary>
		/// <remarks>Channel used for progress messages.</remarks>
		public const int CH_PROGRESS = SideBandInputStream.CH_PROGRESS;

		/// <summary>Channel used for error messages.</summary>
		/// <remarks>Channel used for error messages.</remarks>
		public const int CH_ERROR = SideBandInputStream.CH_ERROR;

		/// <summary>Default buffer size for a small amount of data.</summary>
		/// <remarks>Default buffer size for a small amount of data.</remarks>
		public const int SMALL_BUF = 1000;

		/// <summary>Maximum buffer size for a single packet of sideband data.</summary>
		/// <remarks>Maximum buffer size for a single packet of sideband data.</remarks>
		public const int MAX_BUF = 65520;

		internal const int HDR_SIZE = 5;

		private readonly OutputStream @out;

		private readonly byte[] buffer;

		/// <summary>
		/// Number of bytes in
		/// <see cref="buffer">buffer</see>
		/// that are valid data.
		/// <p>
		/// Initialized to
		/// <see cref="HDR_SIZE">HDR_SIZE</see>
		/// if there is no application data in the
		/// buffer, as the packet header always appears at the start of the buffer.
		/// </summary>
		private int cnt;

		/// <summary>Create a new stream to write side band packets.</summary>
		/// <remarks>Create a new stream to write side band packets.</remarks>
		/// <param name="chan">
		/// channel number to prefix all packets with, so the remote side
		/// can demultiplex the stream and get back the original data.
		/// Must be in the range [0, 255].
		/// </param>
		/// <param name="sz">
		/// maximum size of a data packet within the stream. The remote
		/// side needs to agree to the packet size to prevent buffer
		/// overflows. Must be in the range [HDR_SIZE + 1, MAX_BUF).
		/// </param>
		/// <param name="os">
		/// stream that the packets are written onto. This stream should
		/// be attached to a SideBandInputStream on the remote side.
		/// </param>
		public SideBandOutputStream(int chan, int sz, OutputStream os)
		{
			if (chan <= 0 || chan > 255)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().channelMustBeInRange0_255
					, chan));
			}
			if (sz <= HDR_SIZE)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().packetSizeMustBeAtLeast
					, sz, HDR_SIZE));
			}
			else
			{
				if (MAX_BUF < sz)
				{
					throw new ArgumentException(MessageFormat.Format(JGitText.Get().packetSizeMustBeAtMost
						, sz, MAX_BUF));
				}
			}
			@out = os;
			buffer = new byte[sz];
			buffer[4] = unchecked((byte)chan);
			cnt = HDR_SIZE;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void FlushBuffer()
		{
			if (HDR_SIZE < cnt)
			{
				WriteBuffer();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Flush()
		{
			FlushBuffer();
			@out.Flush();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(byte[] b, int off, int len)
		{
			while (0 < len)
			{
				int capacity = buffer.Length - cnt;
				if (cnt == HDR_SIZE && capacity < len)
				{
					// Our block to write is bigger than the packet size,
					// stream it out as-is to avoid unnecessary copies.
					PacketLineOut.FormatLength(buffer, buffer.Length);
					@out.Write(buffer, 0, HDR_SIZE);
					@out.Write(b, off, capacity);
					off += capacity;
					len -= capacity;
				}
				else
				{
					if (capacity == 0)
					{
						WriteBuffer();
					}
					int n = Math.Min(len, capacity);
					System.Array.Copy(b, off, buffer, cnt, n);
					cnt += n;
					off += n;
					len -= n;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(int b)
		{
			if (cnt == buffer.Length)
			{
				WriteBuffer();
			}
			buffer[cnt++] = unchecked((byte)b);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteBuffer()
		{
			PacketLineOut.FormatLength(buffer, cnt);
			@out.Write(buffer, 0, cnt);
			cnt = HDR_SIZE;
		}
	}
}
