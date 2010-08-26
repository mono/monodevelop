/*
 * Copyright (C) 2008-2010, Google Inc.
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;

namespace GitSharp.Core.Transport
{

	/// <summary>
	/// Multiplexes data and progress messages.
	/// <para/>
	/// This stream is buffered at packet sizes, so the caller doesn't need to wrap
	/// it in yet another buffered stream.
	/// </summary>
	public class SideBandOutputStream : Stream
	{
		public const int CH_DATA = SideBandInputStream.CH_DATA;
		public const int CH_PROGRESS = SideBandInputStream.CH_PROGRESS;
		public const int CH_ERROR = SideBandInputStream.CH_ERROR;
		public const int SMALL_BUF = 1000;
		public const int MAX_BUF = 65520;
		public const int HDR_SIZE = 5;

		private readonly Stream _out;
		private readonly byte[] _buffer;

		/// <summary>
		/// Number of bytes in <see cref="_buffer"/> that are valid data.
		/// <para/>
		/// Initialized to <see cref="HDR_SIZE"/> if there is no application data in the
		/// buffer, as the packet header always appears at the start of the buffer.
		/// </summary>
		private int cnt;

		/// <summary>
		/// Create a new stream to write side band packets.
		/// </summary>
		/// <param name="chan">channel number to prefix all packets with, so the remote side
		/// can demultiplex the stream and get back the original data.
		/// Must be in the range [0, 255].</param>
		/// <param name="sz">maximum size of a data packet within the stream. The remote
		/// side needs to agree to the packet size to prevent buffer
		/// overflows. Must be in the range [HDR_SIZE + 1, MAX_BUF).</param>
		/// <param name="os">stream that the packets are written onto. This stream should
		/// be attached to a SideBandInputStream on the remote side.</param>
		public SideBandOutputStream(int chan, int sz, Stream os)
		{
			if (chan <= 0 || chan > 255)
				throw new ArgumentException("channel " + chan
						+ " must be in range [0, 255]");
			if (sz <= HDR_SIZE)
				throw new ArgumentException("packet size " + sz
						+ " must be >= " + HDR_SIZE);
			else if (MAX_BUF < sz)
				throw new ArgumentException("packet size " + sz
						+ " must be <= " + MAX_BUF);

			_out = os;
			_buffer = new byte[sz];
			_buffer[4] = (byte)chan;
			cnt = HDR_SIZE;
		}

		public override void Flush()
		{
			if (HDR_SIZE < cnt)
				WriteBuffer();
			_out.Flush();
		}

		public override void Write(byte[] b, int off, int len)
		{
			while (0 < len)
			{
				int capacity = _buffer.Length - cnt;
				if (cnt == HDR_SIZE && capacity < len)
				{
					// Our block to write is bigger than the packet size,
					// stream it out as-is to avoid unnecessary copies.
					PacketLineOut.FormatLength(_buffer, _buffer.Length);
					_out.Write(_buffer, 0, HDR_SIZE);
					_out.Write(b, off, capacity);
					off += capacity;
					len -= capacity;

				}
				else
				{
					if (capacity == 0)
						WriteBuffer();

					int n = Math.Min(len, capacity);
					Array.Copy(b, off, _buffer, cnt, n);
					cnt += n;
					off += n;
					len -= n;
				}
			}
		}

		public void Write(int b)
		{
			if (cnt == _buffer.Length)
				WriteBuffer();
			_buffer[cnt++] = (byte)b;
		}

		private void WriteBuffer()
		{
			PacketLineOut.FormatLength(_buffer, cnt);
			_out.Write(_buffer, 0, cnt);
			cnt = HDR_SIZE;
		}

		/// <summary>
		/// We are forced to implement this interface member even though we don't need it
		/// </summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// We are forced to implement this interface member even though we don't need it
		/// </summary>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// We are forced to implement this interface member even though we don't need it
		/// </summary>
		public override bool CanRead
		{
			get { return false; }
		}

		/// <summary>
		/// We are forced to implement this interface member even though we don't need it
		/// </summary>
		public override bool CanWrite
		{
			get { return true; }
		}

		/// <summary>
		/// We are forced to implement this interface member even though we don't need it
		/// </summary>
		public override bool CanSeek
		{
			get { return false; }
		}

		/// <summary>
		/// We are forced to implement this interface member even though we don't need it
		/// </summary>
		public override long Length
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// We are forced to implement this interface member even though we don't need it
		/// </summary>
		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return 	_out.Read(buffer, offset, count);
		}
	}

}