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
using NGit.Errors;
using NGit.Internal;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>Inflates a delta in an incremental way.</summary>
	/// <remarks>
	/// Inflates a delta in an incremental way.
	/// <p>
	/// Implementations must provide a means to access a stream for the base object.
	/// This stream may be accessed multiple times, in order to randomly position it
	/// to match the copy instructions. A
	/// <code>DeltaStream</code>
	/// performs an efficient
	/// skip by only moving through the delta stream, making restarts of stacked
	/// deltas reasonably efficient.
	/// </remarks>
	public abstract class DeltaStream : InputStream
	{
		private const int CMD_COPY = 0;

		private const int CMD_INSERT = 1;

		private const int CMD_EOF = 2;

		private readonly InputStream deltaStream;

		private long baseSize;

		private long resultSize;

		private readonly byte[] cmdbuf = new byte[512];

		private int cmdptr;

		private int cmdcnt;

		/// <summary>Stream to read from the base object.</summary>
		/// <remarks>Stream to read from the base object.</remarks>
		private InputStream baseStream;

		/// <summary>
		/// Current position within
		/// <see cref="baseStream">baseStream</see>
		/// .
		/// </summary>
		private long baseOffset;

		private int curcmd;

		/// <summary>
		/// If
		/// <code>curcmd == CMD_COPY</code>
		/// , position the base has to be at.
		/// </summary>
		private long copyOffset;

		/// <summary>Total number of bytes in this current command.</summary>
		/// <remarks>Total number of bytes in this current command.</remarks>
		private int copySize;

		/// <summary>Construct a delta application stream, reading instructions.</summary>
		/// <remarks>Construct a delta application stream, reading instructions.</remarks>
		/// <param name="deltaStream">the stream to read delta instructions from.</param>
		/// <exception cref="System.IO.IOException">
		/// the delta instruction stream cannot be read, or is
		/// inconsistent with the the base object information.
		/// </exception>
		public DeltaStream(InputStream deltaStream)
		{
			this.deltaStream = deltaStream;
			if (!Fill(cmdbuf.Length))
			{
				throw new EOFException();
			}
			// Length of the base object.
			//
			int c;
			int shift = 0;
			do
			{
				c = cmdbuf[cmdptr++] & unchecked((int)(0xff));
				baseSize |= ((long)(c & unchecked((int)(0x7f)))) << shift;
				shift += 7;
			}
			while ((c & unchecked((int)(0x80))) != 0);
			// Length of the resulting object.
			//
			shift = 0;
			do
			{
				c = cmdbuf[cmdptr++] & unchecked((int)(0xff));
				resultSize |= ((long)(c & unchecked((int)(0x7f)))) << shift;
				shift += 7;
			}
			while ((c & unchecked((int)(0x80))) != 0);
			curcmd = Next();
		}

		/// <summary>Open the base stream.</summary>
		/// <remarks>
		/// Open the base stream.
		/// <p>
		/// The
		/// <code>DeltaStream</code>
		/// may close and reopen the base stream multiple
		/// times if copy instructions use offsets out of order. This can occur if a
		/// large block in the file was moved from near the top, to near the bottom.
		/// In such cases the reopened stream is skipped to the target offset, so
		/// <code>skip(long)</code>
		/// should be as efficient as possible.
		/// </remarks>
		/// <returns>
		/// stream to read from the base object. This stream should not be
		/// buffered (or should be only minimally buffered), and does not
		/// need to support mark/reset.
		/// </returns>
		/// <exception cref="System.IO.IOException">the base object cannot be opened for reading.
		/// 	</exception>
		protected internal abstract InputStream OpenBase();

		/// <returns>length of the base object, in bytes.</returns>
		/// <exception cref="System.IO.IOException">the length of the base cannot be determined.
		/// 	</exception>
		protected internal abstract long GetBaseSize();

		/// <returns>total size of this stream, in bytes.</returns>
		public virtual long GetSize()
		{
			return resultSize;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read()
		{
			byte[] buf = new byte[1];
			int n = Read(buf, 0, 1);
			return n == 1 ? buf[0] & unchecked((int)(0xff)) : -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			deltaStream.Close();
			if (baseStream != null)
			{
				baseStream.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long Skip(long len)
		{
			long act = 0;
			while (0 < len)
			{
				long n = Math.Min(len, copySize);
				switch (curcmd)
				{
					case CMD_COPY:
					{
						copyOffset += n;
						break;
					}

					case CMD_INSERT:
					{
						cmdptr += (int)n;
						break;
					}

					case CMD_EOF:
					{
						return act;
					}

					default:
					{
						throw new CorruptObjectException(JGitText.Get().unsupportedCommand0);
					}
				}
				act += n;
				len -= n;
				copySize -= (int)n;
				if (copySize == 0)
				{
					curcmd = Next();
				}
			}
			return act;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] buf, int off, int len)
		{
			int act = 0;
			while (0 < len)
			{
				int n = Math.Min(len, copySize);
				switch (curcmd)
				{
					case CMD_COPY:
					{
						SeekBase();
						n = baseStream.Read(buf, off, n);
						if (n < 0)
						{
							throw new CorruptObjectException(JGitText.Get().baseLengthIncorrect);
						}
						copyOffset += n;
						baseOffset = copyOffset;
						break;
					}

					case CMD_INSERT:
					{
						System.Array.Copy(cmdbuf, cmdptr, buf, off, n);
						cmdptr += n;
						break;
					}

					case CMD_EOF:
					{
						return 0 < act ? act : -1;
					}

					default:
					{
						throw new CorruptObjectException(JGitText.Get().unsupportedCommand0);
					}
				}
				act += n;
				off += n;
				len -= n;
				copySize -= n;
				if (copySize == 0)
				{
					curcmd = Next();
				}
			}
			return act;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool Fill(int need)
		{
			int n = Have();
			if (need < n)
			{
				return true;
			}
			if (n == 0)
			{
				cmdptr = 0;
				cmdcnt = 0;
			}
			else
			{
				if (cmdbuf.Length - cmdptr < need)
				{
					// There isn't room for the entire worst-case copy command,
					// so shift the array down to make sure we can use the entire
					// command without having it span across the end of the array.
					//
					System.Array.Copy(cmdbuf, cmdptr, cmdbuf, 0, n);
					cmdptr = 0;
					cmdcnt = n;
				}
			}
			do
			{
				n = deltaStream.Read(cmdbuf, cmdcnt, cmdbuf.Length - cmdcnt);
				if (n < 0)
				{
					return 0 < Have();
				}
				cmdcnt += n;
			}
			while (cmdcnt < cmdbuf.Length);
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int Next()
		{
			if (!Fill(8))
			{
				return CMD_EOF;
			}
			int cmd = cmdbuf[cmdptr++] & unchecked((int)(0xff));
			if ((cmd & unchecked((int)(0x80))) != 0)
			{
				// Determine the segment of the base which should
				// be copied into the output. The segment is given
				// as an offset and a length.
				//
				copyOffset = 0;
				if ((cmd & unchecked((int)(0x01))) != 0)
				{
					copyOffset = cmdbuf[cmdptr++] & unchecked((int)(0xff));
				}
				if ((cmd & unchecked((int)(0x02))) != 0)
				{
					copyOffset |= (cmdbuf[cmdptr++] & unchecked((int)(0xff))) << 8;
				}
				if ((cmd & unchecked((int)(0x04))) != 0)
				{
					copyOffset |= (cmdbuf[cmdptr++] & unchecked((int)(0xff))) << 16;
				}
				if ((cmd & unchecked((int)(0x08))) != 0)
				{
					copyOffset |= ((long)(cmdbuf[cmdptr++] & unchecked((int)(0xff)))) << 24;
				}
				copySize = 0;
				if ((cmd & unchecked((int)(0x10))) != 0)
				{
					copySize = cmdbuf[cmdptr++] & unchecked((int)(0xff));
				}
				if ((cmd & unchecked((int)(0x20))) != 0)
				{
					copySize |= (cmdbuf[cmdptr++] & unchecked((int)(0xff))) << 8;
				}
				if ((cmd & unchecked((int)(0x40))) != 0)
				{
					copySize |= (cmdbuf[cmdptr++] & unchecked((int)(0xff))) << 16;
				}
				if (copySize == 0)
				{
					copySize = unchecked((int)(0x10000));
				}
				return CMD_COPY;
			}
			else
			{
				if (cmd != 0)
				{
					// Anything else the data is literal within the delta
					// itself. Page the entire thing into the cmdbuf, if
					// its not already there.
					//
					Fill(cmd);
					copySize = cmd;
					return CMD_INSERT;
				}
				else
				{
					// cmd == 0 has been reserved for future encoding but
					// for now its not acceptable.
					//
					throw new CorruptObjectException(JGitText.Get().unsupportedCommand0);
				}
			}
		}

		private int Have()
		{
			return cmdcnt - cmdptr;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SeekBase()
		{
			if (baseStream == null)
			{
				baseStream = OpenBase();
				if (GetBaseSize() != baseSize)
				{
					throw new CorruptObjectException(JGitText.Get().baseLengthIncorrect);
				}
				IOUtil.SkipFully(baseStream, copyOffset);
				baseOffset = copyOffset;
			}
			else
			{
				if (baseOffset < copyOffset)
				{
					IOUtil.SkipFully(baseStream, copyOffset - baseOffset);
					baseOffset = copyOffset;
				}
				else
				{
					if (baseOffset > copyOffset)
					{
						baseStream.Close();
						baseStream = OpenBase();
						IOUtil.SkipFully(baseStream, copyOffset);
						baseOffset = copyOffset;
					}
				}
			}
		}
	}
}
