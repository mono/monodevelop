///	Copyright (c) 2006 James Kolpack (james dot kolpack at google mail)
///	Copyright (C) 2010 Henon <meinrad.recheis@gmail.com>
///	
///	Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
///	associated documentation files (the "Software"), to deal in the Software without restriction, 
///	including without limitation the rights to use, copy, modify, merge, publish, distribute, 
///	sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
///	furnished to do so, subject to the following conditions:
///	
///	The above copyright notice and this permission notice shall be included in all copies or 
///	substantial portions of the Software.
///	
///	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
///	INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
///	PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
///	LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT 
///	OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
///	OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GitSharp.Core.Util
{
	/// <summary>
	/// PipeStream is a thread-safe read/write data stream for use between two threads in a 
	/// single-producer/single-consumer type problem.
	/// </summary>
	/// <version>2006/10/13 1.0</version>
	/// <remarks>Update on 2008/10/9 1.1 - uses Monitor instead of Manual Reset events for more elegant synchronicity.</remarks>
	public class PipeStream : Stream
	{
		#region Private members

		/// <summary>
		/// Queue of bytes provides the datastructure for transmitting from an
		/// input stream to an output stream.
		/// </summary>
		/// <remarks>Possible more effecient ways to accomplish this.</remarks>
		private readonly Queue<byte> mBuffer = new Queue<byte>();

		/// <summary>
		/// Indicates that the input stream has been flushed and that
		/// all remaining data should be written to the output stream.
		/// </summary>
		private bool mFlushed;

		/// <summary>
		/// Maximum number of bytes to store in the buffer.
		/// </summary>
		private long mMaxBufferLength = 200 * MB;

		/// <summary>
		/// Setting this to true will cause Read() to block if it appears
		/// that it will run out of data.
		/// </summary>
		private bool mBlockLastRead;

		#endregion

		#region Public Const members

		/// <summary>
		/// Number of bytes in a kilobyte
		/// </summary>
		public const long KB = 1024;

		/// <summary>
		/// Number of bytes in a megabyte
		/// </summary>
		public const long MB = KB * 1024;

		#endregion

		#region Public properties

		/// <summary>
		/// Gets or sets the maximum number of bytes to store in the buffer.
		/// </summary>
		/// <value>The length of the max buffer.</value>
		public long MaxBufferLength
		{
			get { return mMaxBufferLength; }
			set { mMaxBufferLength = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether to block last read method before the buffer is empty.
		/// When true, Read() will block until it can fill the passed in buffer and count.
		/// When false, Read() will not block, returning all the available buffer data.
		/// </summary>
		/// <remarks>
		/// Setting to true will remove the possibility of ending a stream reader prematurely.
		/// </remarks>
		/// <value>
		/// 	<c>true</c> if block last read method before the buffer is empty; otherwise, <c>false</c>.
		/// </value>
		public bool BlockLastReadBuffer
		{
			get { return mBlockLastRead; }
			set
			{
				mBlockLastRead = value;

				// when turning off the block last read, signal Read() that it may now read the rest of the buffer.
				if (!mBlockLastRead)
					lock (mBuffer)
						Monitor.Pulse(mBuffer);
			}
		}

		#endregion

		#region Stream overide methods

		public override void Close()
		{
			CheckDisposed();
			base.Close();
			Dispose();
			lock (mBuffer)
				Monitor.Pulse(mBuffer);
		}

		///<summary>
		///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		///</summary>
		///<filterpriority>2</filterpriority>
		public new void Dispose()
		{
			if (mDisposed)
				return;
			// clear the internal buffer
			mBuffer.Clear();
			mDisposed = true;
		}

		private bool mDisposed;

		///<summary>
		///When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		///</summary>
		///
		///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
		public override void Flush()
		{
			CheckDisposed();
			mFlushed = true;
			lock (mBuffer)
				Monitor.Pulse(mBuffer);
		}

		private void CheckDisposed()
		{
			if (mDisposed)
				throw new ObjectDisposedException("stream has been closed");
		}

		///<summary>
		///When overridden in a derived class, sets the position within the current stream.
		///</summary>
		///<returns>
		///The new position within the current stream.
		///</returns>
		///<param name="offset">A byte offset relative to the origin parameter. </param>
		///<param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"></see> indicating the reference point used to obtain the new position. </param>
		///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		///<exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
		///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		///<summary>
		///When overridden in a derived class, sets the length of the current stream.
		///</summary>
		///<param name="value">The desired length of the current stream in bytes. </param>
		///<exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
		///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>2</filterpriority>
		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}


		///<summary>
		///When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		///</summary>
		///<returns>
		///The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
		///</returns>
		///<param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream. </param>
		///<param name="count">The maximum number of bytes to be read from the current stream. </param>
		///<param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source. </param>
		///<exception cref="T:System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
		///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		///<exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
		///<exception cref="T:System.ArgumentNullException">buffer is null. </exception>
		///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		///<exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception><filterpriority>1</filterpriority>
		public override int Read(byte[] buffer, int offset, int count)
		{
			CheckDisposed();
			if (offset != 0)
				throw new NotImplementedException("Offsets with value of non-zero are not supported");
			if (buffer == null)
				throw new ArgumentException("Buffer is null");
			if (offset + count > buffer.Length)
				throw new ArgumentException("The sum of offset and count is greater than the buffer length. ");
			if (offset < 0 || count < 0)
				throw new ArgumentOutOfRangeException("offset", "offset or count is negative.");
			if (BlockLastReadBuffer && count >= mMaxBufferLength)
				throw new ArgumentException(String.Format("count({0}) > mMaxBufferLength({1})", count, mMaxBufferLength));

			if (count == 0)
				return 0;

			int readLength = 0;

			lock (mBuffer)
			{
				while (!ReadAvailable(count))
				{
					Monitor.Wait(mBuffer);
					CheckDisposed();
				}
				// fill the read buffer
				for (; readLength < count && Length > 0; readLength++)
				{
					buffer[readLength] = mBuffer.Dequeue();
				}

				Monitor.Pulse(mBuffer);
			}
			return readLength;
		}

		/// <summary>
		/// Returns true if there are 
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		private bool ReadAvailable(int count)
		{
			return (Length >= count || mFlushed) &&
				(Length >= (count + 1) || !BlockLastReadBuffer);
		}

		///<summary>
		///When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		///</summary>
		///<param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream. </param>
		///<param name="count">The number of bytes to be written to the current stream. </param>
		///<param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream. </param>
		///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		///<exception cref="T:System.NotSupportedException">The stream does not support writing. </exception>
		///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		///<exception cref="T:System.ArgumentNullException">buffer is null. </exception>
		///<exception cref="T:System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
		///<exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception><filterpriority>1</filterpriority>
		public override void Write(byte[] buffer, int offset, int count)
		{
			CheckDisposed();
			if (buffer == null)
				throw new ArgumentException("Buffer is null");
			if (offset + count > buffer.Length)
				throw new ArgumentException("The sum of offset and count is greater than the buffer length. ");
			if (offset < 0 || count < 0)
				throw new ArgumentOutOfRangeException("offset", "offset or count is negative.");
			if (count == 0)
				return;

			lock (mBuffer)
			{
				// wait until the buffer isn't full
				while (Length >= mMaxBufferLength)
					Monitor.Wait(mBuffer);
				CheckDisposed();
				mFlushed = false; // if it were flushed before, it soon will not be.

				// queue up the buffer data
				for (int i = offset; i < offset + count; i++)
				{
					mBuffer.Enqueue(buffer[i]);
				}

				Monitor.Pulse(mBuffer); // signal that write has occured
			}
		}

		///<summary>
		///When overridden in a derived class, gets a value indicating whether the current stream supports reading.
		///</summary>
		///<returns>
		///true if the stream supports reading; otherwise, false.
		///</returns>
		///<filterpriority>1</filterpriority>
		public override bool CanRead
		{
			get { return true; }
		}

		///<summary>
		///When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
		///</summary>
		///<returns>
		///true if the stream supports seeking; otherwise, false.
		///</returns>
		///<filterpriority>1</filterpriority>
		public override bool CanSeek
		{
			get { return false; }
		}

		///<summary>
		///When overridden in a derived class, gets a value indicating whether the current stream supports writing.
		///</summary>
		///<returns>
		///true if the stream supports writing; otherwise, false.
		///</returns>
		///<filterpriority>1</filterpriority>
		public override bool CanWrite
		{
			get { return true; }
		}

		///<summary>
		///When overridden in a derived class, gets the length in bytes of the stream.
		///</summary>
		///<returns>
		///A long value representing the length of the stream in bytes.
		///</returns>
		///
		///<exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
		///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
		public override long Length
		{
			get { return mBuffer.Count; }
		}

		///<summary>
		///When overridden in a derived class, gets or sets the position within the current stream.
		///</summary>
		///<returns>
		///The current position within the stream.
		///</returns>
		///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		///<exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
		///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
		public override long Position
		{
			get { return 0; }
			set { throw new NotImplementedException(); }
		}

		#endregion
	}
}