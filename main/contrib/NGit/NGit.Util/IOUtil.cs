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
using NGit.Internal;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	/// <summary>Input/Output utilities</summary>
	public class IOUtil
	{
		/// <summary>Read an entire local file into memory as a byte array.</summary>
		/// <remarks>Read an entire local file into memory as a byte array.</remarks>
		/// <param name="path">location of the file to read.</param>
		/// <returns>complete contents of the requested local file.</returns>
		/// <exception cref="System.IO.FileNotFoundException">the file does not exist.</exception>
		/// <exception cref="System.IO.IOException">the file exists, but its contents cannot be read.
		/// 	</exception>
		public static byte[] ReadFully(FilePath path)
		{
			return IOUtil.ReadFully(path, int.MaxValue);
		}

		/// <summary>Read at most limit bytes from the local file into memory as a byte array.
		/// 	</summary>
		/// <remarks>Read at most limit bytes from the local file into memory as a byte array.
		/// 	</remarks>
		/// <param name="path">location of the file to read.</param>
		/// <param name="limit">
		/// maximum number of bytes to read, if the file is larger than
		/// only the first limit number of bytes are returned
		/// </param>
		/// <returns>
		/// complete contents of the requested local file. If the contents
		/// exceeds the limit, then only the limit is returned.
		/// </returns>
		/// <exception cref="System.IO.FileNotFoundException">the file does not exist.</exception>
		/// <exception cref="System.IO.IOException">the file exists, but its contents cannot be read.
		/// 	</exception>
		public static byte[] ReadSome(FilePath path, int limit)
		{
			FileInputStream @in = new FileInputStream(path);
			try
			{
				byte[] buf = new byte[limit];
				int cnt = 0;
				for (; ; )
				{
					int n = @in.Read(buf, cnt, buf.Length - cnt);
					if (n <= 0)
					{
						break;
					}
					cnt += n;
				}
				if (cnt == buf.Length)
				{
					return buf;
				}
				byte[] res = new byte[cnt];
				System.Array.Copy(buf, 0, res, 0, cnt);
				return res;
			}
			finally
			{
				try
				{
					@in.Close();
				}
				catch (IOException)
				{
				}
			}
		}

		// do nothing
		/// <summary>Read an entire local file into memory as a byte array.</summary>
		/// <remarks>Read an entire local file into memory as a byte array.</remarks>
		/// <param name="path">location of the file to read.</param>
		/// <param name="max">
		/// maximum number of bytes to read, if the file is larger than
		/// this limit an IOException is thrown.
		/// </param>
		/// <returns>complete contents of the requested local file.</returns>
		/// <exception cref="System.IO.FileNotFoundException">the file does not exist.</exception>
		/// <exception cref="System.IO.IOException">the file exists, but its contents cannot be read.
		/// 	</exception>
		public static byte[] ReadFully(FilePath path, int max)
		{
			FileInputStream @in = new FileInputStream(path);
			try
			{
				long sz = Math.Max(path.Length(), 1);
				if (sz > max)
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().fileIsTooLarge, path));
				}
				byte[] buf = new byte[(int)sz];
				int valid = 0;
				for (; ; )
				{
					if (buf.Length == valid)
					{
						if (buf.Length == max)
						{
							int next = @in.Read();
							if (next < 0)
							{
								break;
							}
							throw new IOException(MessageFormat.Format(JGitText.Get().fileIsTooLarge, path));
						}
						byte[] nb = new byte[Math.Min(buf.Length * 2, max)];
						System.Array.Copy(buf, 0, nb, 0, valid);
						buf = nb;
					}
					int n = @in.Read(buf, valid, buf.Length - valid);
					if (n < 0)
					{
						break;
					}
					valid += n;
				}
				if (valid < buf.Length)
				{
					byte[] nb = new byte[valid];
					System.Array.Copy(buf, 0, nb, 0, valid);
					buf = nb;
				}
				return buf;
			}
			finally
			{
				try
				{
					@in.Close();
				}
				catch (IOException)
				{
				}
			}
		}

		// ignore any close errors, this was a read only stream
		/// <summary>Read an entire input stream into memory as a ByteBuffer.</summary>
		/// <remarks>
		/// Read an entire input stream into memory as a ByteBuffer.
		/// Note: The stream is read to its end and is not usable after calling this
		/// method. The caller is responsible for closing the stream.
		/// </remarks>
		/// <param name="in">input stream to be read.</param>
		/// <param name="sizeHint">
		/// a hint on the approximate number of bytes contained in the
		/// stream, used to allocate temporary buffers more efficiently
		/// </param>
		/// <returns>
		/// complete contents of the input stream. The ByteBuffer always has
		/// a writable backing array, with
		/// <code>position() == 0</code>
		/// and
		/// <code>limit()</code>
		/// equal to the actual length read. Callers may rely
		/// on obtaining the underlying array for efficient data access. If
		/// <code>sizeHint</code>
		/// was too large, the array may be over-allocated,
		/// resulting in
		/// <code>limit() &lt; array().length</code>
		/// .
		/// </returns>
		/// <exception cref="System.IO.IOException">there was an error reading from the stream.
		/// 	</exception>
		public static ByteBuffer ReadWholeStream(InputStream @in, int sizeHint)
		{
			byte[] @out = new byte[sizeHint];
			int pos = 0;
			while (pos < @out.Length)
			{
				int read = @in.Read(@out, pos, @out.Length - pos);
				if (read < 0)
				{
					return ByteBuffer.Wrap(@out, 0, pos);
				}
				pos += read;
			}
			int last = @in.Read();
			if (last < 0)
			{
				return ByteBuffer.Wrap(@out, 0, pos);
			}
			TemporaryBuffer.Heap tmp = new TemporaryBuffer.Heap(int.MaxValue);
			tmp.Write(@out);
			tmp.Write(last);
			tmp.Copy(@in);
			return ByteBuffer.Wrap(tmp.ToByteArray());
		}

		/// <summary>Read the entire byte array into memory, or throw an exception.</summary>
		/// <remarks>Read the entire byte array into memory, or throw an exception.</remarks>
		/// <param name="fd">input stream to read the data from.</param>
		/// <param name="dst">buffer that must be fully populated, [off, off+len).</param>
		/// <param name="off">position within the buffer to start writing to.</param>
		/// <param name="len">number of bytes that must be read.</param>
		/// <exception cref="Sharpen.EOFException">the stream ended before dst was fully populated.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">there was an error reading from the stream.
		/// 	</exception>
		public static void ReadFully(InputStream fd, byte[] dst, int off, int len)
		{
			while (len > 0)
			{
				int r = fd.Read(dst, off, len);
				if (r <= 0)
				{
					throw new EOFException(JGitText.Get().shortReadOfBlock);
				}
				off += r;
				len -= r;
			}
		}

		/// <summary>Read the entire byte array into memory, unless input is shorter</summary>
		/// <param name="fd">input stream to read the data from.</param>
		/// <param name="dst">buffer that must be fully populated, [off, off+len).</param>
		/// <param name="off">position within the buffer to start writing to.</param>
		/// <returns>number of bytes in buffer or stream, whichever is shortest</returns>
		/// <exception cref="System.IO.IOException">there was an error reading from the stream.
		/// 	</exception>
		public static int ReadFully(InputStream fd, byte[] dst, int off)
		{
			int r;
			int len = 0;
			while ((r = fd.Read(dst, off, dst.Length - off)) >= 0 && len < dst.Length)
			{
				off += r;
				len += r;
			}
			return len;
		}

		/// <summary>Skip an entire region of an input stream.</summary>
		/// <remarks>
		/// Skip an entire region of an input stream.
		/// <p>
		/// The input stream's position is moved forward by the number of requested
		/// bytes, discarding them from the input. This method does not return until
		/// the exact number of bytes requested has been skipped.
		/// </remarks>
		/// <param name="fd">the stream to skip bytes from.</param>
		/// <param name="toSkip">total number of bytes to be discarded. Must be &gt;= 0.</param>
		/// <exception cref="Sharpen.EOFException">
		/// the stream ended before the requested number of bytes were
		/// skipped.
		/// </exception>
		/// <exception cref="System.IO.IOException">there was an error reading from the stream.
		/// 	</exception>
		public static void SkipFully(InputStream fd, long toSkip)
		{
			while (toSkip > 0)
			{
				long r = fd.Skip(toSkip);
				if (r <= 0)
				{
					throw new EOFException(JGitText.Get().shortSkipOfBlock);
				}
				toSkip -= r;
			}
		}

		public IOUtil()
		{
		}
		// Don't create instances of a static only utility.
	}
}
