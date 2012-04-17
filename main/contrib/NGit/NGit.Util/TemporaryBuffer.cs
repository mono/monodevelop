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
using NGit;
using NGit.Internal;
using NGit.Util;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Util
{
	/// <summary>A fully buffered output stream.</summary>
	/// <remarks>
	/// A fully buffered output stream.
	/// <p>
	/// Subclasses determine the behavior when the in-memory buffer capacity has been
	/// exceeded and additional bytes are still being received for output.
	/// </remarks>
	public abstract class TemporaryBuffer : OutputStream
	{
		/// <summary>Default limit for in-core storage.</summary>
		/// <remarks>Default limit for in-core storage.</remarks>
		protected internal const int DEFAULT_IN_CORE_LIMIT = 1024 * 1024;

		/// <summary>Chain of data, if we are still completely in-core; otherwise null.</summary>
		/// <remarks>Chain of data, if we are still completely in-core; otherwise null.</remarks>
		private AList<TemporaryBuffer.Block> blocks;

		/// <summary>Maximum number of bytes we will permit storing in memory.</summary>
		/// <remarks>
		/// Maximum number of bytes we will permit storing in memory.
		/// <p>
		/// When this limit is reached the data will be shifted to a file on disk,
		/// preventing the JVM heap from growing out of control.
		/// </remarks>
		private int inCoreLimit;

		/// <summary>
		/// If
		/// <see cref="inCoreLimit">inCoreLimit</see>
		/// has been reached, remainder goes here.
		/// </summary>
		private OutputStream overflow;

		/// <summary>Create a new empty temporary buffer.</summary>
		/// <remarks>Create a new empty temporary buffer.</remarks>
		/// <param name="limit">
		/// maximum number of bytes to store in memory before entering the
		/// overflow output path.
		/// </param>
		protected internal TemporaryBuffer(int limit)
		{
			inCoreLimit = limit;
			Reset();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(int b)
		{
			if (overflow != null)
			{
				overflow.Write(b);
				return;
			}
			TemporaryBuffer.Block s = Last();
			if (s.IsFull())
			{
				if (ReachedInCoreLimit())
				{
					overflow.Write(b);
					return;
				}
				s = new TemporaryBuffer.Block();
				blocks.AddItem(s);
			}
			s.buffer[s.count++] = unchecked((byte)b);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(byte[] b, int off, int len)
		{
			if (overflow == null)
			{
				while (len > 0)
				{
					TemporaryBuffer.Block s = Last();
					if (s.IsFull())
					{
						if (ReachedInCoreLimit())
						{
							break;
						}
						s = new TemporaryBuffer.Block();
						blocks.AddItem(s);
					}
					int n = Math.Min(s.buffer.Length - s.count, len);
					System.Array.Copy(b, off, s.buffer, s.count, n);
					s.count += n;
					len -= n;
					off += n;
				}
			}
			if (len > 0)
			{
				overflow.Write(b, off, len);
			}
		}

		/// <summary>Dumps the entire buffer into the overflow stream, and flushes it.</summary>
		/// <remarks>Dumps the entire buffer into the overflow stream, and flushes it.</remarks>
		/// <exception cref="System.IO.IOException">
		/// the overflow stream cannot be started, or the buffer contents
		/// cannot be written to it, or it failed to flush.
		/// </exception>
		protected internal virtual void DoFlush()
		{
			if (overflow == null)
			{
				SwitchToOverflow();
			}
			overflow.Flush();
		}

		/// <summary>Copy all bytes remaining on the input stream into this buffer.</summary>
		/// <remarks>Copy all bytes remaining on the input stream into this buffer.</remarks>
		/// <param name="in">the stream to read from, until EOF is reached.</param>
		/// <exception cref="System.IO.IOException">
		/// an error occurred reading from the input stream, or while
		/// writing to a local temporary file.
		/// </exception>
		public virtual void Copy(InputStream @in)
		{
			if (blocks != null)
			{
				for (; ; )
				{
					TemporaryBuffer.Block s = Last();
					if (s.IsFull())
					{
						if (ReachedInCoreLimit())
						{
							break;
						}
						s = new TemporaryBuffer.Block();
						blocks.AddItem(s);
					}
					int n = @in.Read(s.buffer, s.count, s.buffer.Length - s.count);
					if (n < 1)
					{
						return;
					}
					s.count += n;
				}
			}
			byte[] tmp = new byte[TemporaryBuffer.Block.SZ];
			int n_1;
			while ((n_1 = @in.Read(tmp)) > 0)
			{
				overflow.Write(tmp, 0, n_1);
			}
		}

		/// <summary>Obtain the length (in bytes) of the buffer.</summary>
		/// <remarks>
		/// Obtain the length (in bytes) of the buffer.
		/// <p>
		/// The length is only accurate after
		/// <see cref="Close()">Close()</see>
		/// has been invoked.
		/// </remarks>
		/// <returns>total length of the buffer, in bytes.</returns>
		public virtual long Length()
		{
			return InCoreLength();
		}

		private long InCoreLength()
		{
			TemporaryBuffer.Block last = Last();
			return ((long)blocks.Count - 1) * TemporaryBuffer.Block.SZ + last.count;
		}

		/// <summary>Convert this buffer's contents into a contiguous byte array.</summary>
		/// <remarks>
		/// Convert this buffer's contents into a contiguous byte array.
		/// <p>
		/// The buffer is only complete after
		/// <see cref="Close()">Close()</see>
		/// has been invoked.
		/// </remarks>
		/// <returns>
		/// the complete byte array; length matches
		/// <see cref="Length()">Length()</see>
		/// .
		/// </returns>
		/// <exception cref="System.IO.IOException">an error occurred reading from a local temporary file
		/// 	</exception>
		/// <exception cref="System.OutOfMemoryException">the buffer cannot fit in memory</exception>
		public virtual byte[] ToByteArray()
		{
			long len = Length();
			if (int.MaxValue < len)
			{
				throw new OutOfMemoryException(JGitText.Get().lengthExceedsMaximumArraySize);
			}
			byte[] @out = new byte[(int)len];
			int outPtr = 0;
			foreach (TemporaryBuffer.Block b in blocks)
			{
				System.Array.Copy(b.buffer, 0, @out, outPtr, b.count);
				outPtr += b.count;
			}
			return @out;
		}

		/// <summary>Send this buffer to an output stream.</summary>
		/// <remarks>
		/// Send this buffer to an output stream.
		/// <p>
		/// This method may only be invoked after
		/// <see cref="Close()">Close()</see>
		/// has completed
		/// normally, to ensure all data is completely transferred.
		/// </remarks>
		/// <param name="os">stream to send this buffer's complete content to.</param>
		/// <param name="pm">
		/// if not null progress updates are sent here. Caller should
		/// initialize the task and the number of work units to <code>
		/// <see cref="Length()">Length()</see>
		/// /1024</code>.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// an error occurred reading from a temporary file on the local
		/// system, or writing to the output stream.
		/// </exception>
		public virtual void WriteTo(OutputStream os, ProgressMonitor pm)
		{
			if (pm == null)
			{
				pm = NullProgressMonitor.INSTANCE;
			}
			foreach (TemporaryBuffer.Block b in blocks)
			{
				os.Write(b.buffer, 0, b.count);
				pm.Update(b.count / 1024);
			}
		}

		/// <summary>Open an input stream to read from the buffered data.</summary>
		/// <remarks>
		/// Open an input stream to read from the buffered data.
		/// <p>
		/// This method may only be invoked after
		/// <see cref="Close()">Close()</see>
		/// has completed
		/// normally, to ensure all data is completely transferred.
		/// </remarks>
		/// <returns>
		/// a stream to read from the buffer. The caller must close the
		/// stream when it is no longer useful.
		/// </returns>
		/// <exception cref="System.IO.IOException">an error occurred opening the temporary file.
		/// 	</exception>
		public virtual InputStream OpenInputStream()
		{
			return new TemporaryBuffer.BlockInputStream(this);
		}

		/// <summary>Reset this buffer for reuse, purging all buffered content.</summary>
		/// <remarks>Reset this buffer for reuse, purging all buffered content.</remarks>
		public virtual void Reset()
		{
			if (overflow != null)
			{
				Destroy();
			}
			if (inCoreLimit < TemporaryBuffer.Block.SZ)
			{
				blocks = new AList<TemporaryBuffer.Block>(1);
				blocks.AddItem(new TemporaryBuffer.Block(inCoreLimit));
			}
			else
			{
				blocks = new AList<TemporaryBuffer.Block>(inCoreLimit / TemporaryBuffer.Block.SZ);
				blocks.AddItem(new TemporaryBuffer.Block());
			}
		}

		/// <summary>Open the overflow output stream, so the remaining output can be stored.</summary>
		/// <remarks>Open the overflow output stream, so the remaining output can be stored.</remarks>
		/// <returns>
		/// the output stream to receive the buffered content, followed by
		/// the remaining output.
		/// </returns>
		/// <exception cref="System.IO.IOException">the buffer cannot create the overflow stream.
		/// 	</exception>
		protected internal abstract OutputStream Overflow();

		private TemporaryBuffer.Block Last()
		{
			return blocks[blocks.Count - 1];
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool ReachedInCoreLimit()
		{
			if (InCoreLength() < inCoreLimit)
			{
				return false;
			}
			SwitchToOverflow();
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SwitchToOverflow()
		{
			overflow = Overflow();
			TemporaryBuffer.Block last = blocks.Remove(blocks.Count - 1);
			foreach (TemporaryBuffer.Block b in blocks)
			{
				overflow.Write(b.buffer, 0, b.count);
			}
			blocks = null;
			overflow = new SafeBufferedOutputStream(overflow, TemporaryBuffer.Block.SZ);
			overflow.Write(last.buffer, 0, last.count);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			if (overflow != null)
			{
				try
				{
					overflow.Close();
				}
				finally
				{
					overflow = null;
				}
			}
		}

		/// <summary>Clear this buffer so it has no data, and cannot be used again.</summary>
		/// <remarks>Clear this buffer so it has no data, and cannot be used again.</remarks>
		public virtual void Destroy()
		{
			blocks = null;
			if (overflow != null)
			{
				try
				{
					overflow.Close();
				}
				catch (IOException)
				{
				}
				finally
				{
					// We shouldn't encounter an error closing the file.
					overflow = null;
				}
			}
		}

		/// <summary>A fully buffered output stream using local disk storage for large data.</summary>
		/// <remarks>
		/// A fully buffered output stream using local disk storage for large data.
		/// <p>
		/// Initially this output stream buffers to memory and is therefore similar
		/// to ByteArrayOutputStream, but it shifts to using an on disk temporary
		/// file if the output gets too large.
		/// <p>
		/// The content of this buffered stream may be sent to another OutputStream
		/// only after this stream has been properly closed by
		/// <see cref="TemporaryBuffer.Close()">TemporaryBuffer.Close()</see>
		/// .
		/// </remarks>
		public class LocalFile : TemporaryBuffer
		{
			/// <summary>Directory to store the temporary file under.</summary>
			/// <remarks>Directory to store the temporary file under.</remarks>
			private readonly FilePath directory;

			/// <summary>Location of our temporary file if we are on disk; otherwise null.</summary>
			/// <remarks>
			/// Location of our temporary file if we are on disk; otherwise null.
			/// <p>
			/// If we exceeded the
			/// <see cref="TemporaryBuffer.inCoreLimit">TemporaryBuffer.inCoreLimit</see>
			/// we nulled out
			/// <see cref="TemporaryBuffer.blocks">TemporaryBuffer.blocks</see>
			/// and created this file instead. All output goes here through
			/// <see cref="TemporaryBuffer.overflow">TemporaryBuffer.overflow</see>
			/// .
			/// </remarks>
			private FilePath onDiskFile;

			/// <summary>Create a new temporary buffer.</summary>
			/// <remarks>Create a new temporary buffer.</remarks>
			public LocalFile() : this(null, DEFAULT_IN_CORE_LIMIT)
			{
			}

			/// <summary>Create a new temporary buffer, limiting memory usage.</summary>
			/// <remarks>Create a new temporary buffer, limiting memory usage.</remarks>
			/// <param name="inCoreLimit">
			/// maximum number of bytes to store in memory. Storage beyond
			/// this limit will use the local file.
			/// </param>
			protected internal LocalFile(int inCoreLimit) : this(null, inCoreLimit)
			{
			}

			/// <summary>Create a new temporary buffer, limiting memory usage.</summary>
			/// <remarks>Create a new temporary buffer, limiting memory usage.</remarks>
			/// <param name="directory">
			/// if the buffer has to spill over into a temporary file, the
			/// directory where the file should be saved. If null the
			/// system default temporary directory (for example /tmp) will
			/// be used instead.
			/// </param>
			public LocalFile(FilePath directory) : this(directory, DEFAULT_IN_CORE_LIMIT)
			{
			}

			/// <summary>Create a new temporary buffer, limiting memory usage.</summary>
			/// <remarks>Create a new temporary buffer, limiting memory usage.</remarks>
			/// <param name="directory">
			/// if the buffer has to spill over into a temporary file, the
			/// directory where the file should be saved. If null the
			/// system default temporary directory (for example /tmp) will
			/// be used instead.
			/// </param>
			/// <param name="inCoreLimit">
			/// maximum number of bytes to store in memory. Storage beyond
			/// this limit will use the local file.
			/// </param>
			public LocalFile(FilePath directory, int inCoreLimit) : base(inCoreLimit)
			{
				this.directory = directory;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override OutputStream Overflow()
			{
				onDiskFile = FilePath.CreateTempFile("jgit_", ".buf", directory);
				return new FileOutputStream(onDiskFile);
			}

			public override long Length()
			{
				if (onDiskFile == null)
				{
					return base.Length();
				}
				return onDiskFile.Length();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override byte[] ToByteArray()
			{
				if (onDiskFile == null)
				{
					return base.ToByteArray();
				}
				long len = Length();
				if (int.MaxValue < len)
				{
					throw new OutOfMemoryException(JGitText.Get().lengthExceedsMaximumArraySize);
				}
				byte[] @out = new byte[(int)len];
				FileInputStream @in = new FileInputStream(onDiskFile);
				try
				{
					IOUtil.ReadFully(@in, @out, 0, (int)len);
				}
				finally
				{
					@in.Close();
				}
				return @out;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void WriteTo(OutputStream os, ProgressMonitor pm)
			{
				if (onDiskFile == null)
				{
					base.WriteTo(os, pm);
					return;
				}
				if (pm == null)
				{
					pm = NullProgressMonitor.INSTANCE;
				}
				FileInputStream @in = new FileInputStream(onDiskFile);
				try
				{
					int cnt;
					byte[] buf = new byte[TemporaryBuffer.Block.SZ];
					while ((cnt = @in.Read(buf)) >= 0)
					{
						os.Write(buf, 0, cnt);
						pm.Update(cnt / 1024);
					}
				}
				finally
				{
					@in.Close();
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override InputStream OpenInputStream()
			{
				if (onDiskFile == null)
				{
					return base.OpenInputStream();
				}
				return new FileInputStream(onDiskFile);
			}

			public override void Destroy()
			{
				base.Destroy();
				if (onDiskFile != null)
				{
					try
					{
						if (!onDiskFile.Delete())
						{
							onDiskFile.DeleteOnExit();
						}
					}
					finally
					{
						onDiskFile = null;
					}
				}
			}
		}

		/// <summary>A temporary buffer that will never exceed its in-memory limit.</summary>
		/// <remarks>
		/// A temporary buffer that will never exceed its in-memory limit.
		/// <p>
		/// If the in-memory limit is reached an IOException is thrown, rather than
		/// attempting to spool to local disk.
		/// </remarks>
		public class Heap : TemporaryBuffer
		{
			/// <summary>Create a new heap buffer with a maximum storage limit.</summary>
			/// <remarks>Create a new heap buffer with a maximum storage limit.</remarks>
			/// <param name="limit">
			/// maximum number of bytes that can be stored in this buffer.
			/// Storing beyond this many will cause an IOException to be
			/// thrown during write.
			/// </param>
			protected internal Heap(int limit) : base(limit)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override OutputStream Overflow()
			{
				throw new IOException(JGitText.Get().inMemoryBufferLimitExceeded);
			}
		}

		internal class Block
		{
			internal const int SZ = 8 * 1024;

			internal readonly byte[] buffer;

			internal int count;

			public Block()
			{
				buffer = new byte[SZ];
			}

			internal Block(int sz)
			{
				buffer = new byte[sz];
			}

			internal virtual bool IsFull()
			{
				return count == buffer.Length;
			}
		}

		private class BlockInputStream : InputStream
		{
			private byte[] singleByteBuffer;

			private int blockIndex;

			private TemporaryBuffer.Block block;

			private int blockPos;

			public BlockInputStream(TemporaryBuffer _enclosing)
			{
				this._enclosing = _enclosing;
				this.block = this._enclosing.blocks[this.blockIndex];
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read()
			{
				if (this.singleByteBuffer == null)
				{
					this.singleByteBuffer = new byte[1];
				}
				int n = this.Read(this.singleByteBuffer);
				return n == 1 ? this.singleByteBuffer[0] & unchecked((int)(0xff)) : -1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Skip(long cnt)
			{
				long skipped = 0;
				while (0 < cnt)
				{
					int n = (int)Math.Min(this.block.count - this.blockPos, cnt);
					if (0 < n)
					{
						this.blockPos += n;
						skipped += n;
						cnt -= n;
					}
					else
					{
						if (this.NextBlock())
						{
							continue;
						}
						else
						{
							break;
						}
					}
				}
				return skipped;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read(byte[] b, int off, int len)
			{
				if (len == 0)
				{
					return 0;
				}
				int copied = 0;
				while (0 < len)
				{
					int c = Math.Min(this.block.count - this.blockPos, len);
					if (0 < c)
					{
						System.Array.Copy(this.block.buffer, this.blockPos, b, off, c);
						this.blockPos += c;
						off += c;
						len -= c;
						copied += c;
					}
					else
					{
						if (this.NextBlock())
						{
							continue;
						}
						else
						{
							break;
						}
					}
				}
				return 0 < copied ? copied : -1;
			}

			private bool NextBlock()
			{
				if (++this.blockIndex < this._enclosing.blocks.Count)
				{
					this.block = this._enclosing.blocks[this.blockIndex];
					this.blockPos = 0;
					return true;
				}
				return false;
			}

			private readonly TemporaryBuffer _enclosing;
		}
	}
}
