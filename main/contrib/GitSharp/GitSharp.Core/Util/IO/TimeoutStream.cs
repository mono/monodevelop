/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
 *
 * This program and the accompanying materials are made available
 * under the terms of the Eclipse Distribution License v1.0 which
 * accompanies this distribution, is reproduced below, and is
 * available at http://www.eclipse.org/org/documents/edl-v10.php
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core.Util
{

	//Note: [henon] this is a unified port of jgit's TimoutInputStream and TimeoutOutputStream

	/// <summary>
	///  Stream with a configurable timeout.
	/// </summary>
	public class TimeoutStream : Stream
	{

		private readonly Stream _stream;
		// private InterruptTimer myTimer;
		private readonly Timer _read_timer = new Timer();
		private readonly Timer _write_timer = new Timer();

		//private int timeout;

		///<summary>
		/// Wrap an input stream with a timeout on all read operations.
		///</summary>
		///<param name="src">base input stream (to read from). The stream must be
		///            interruptible (most socket streams are).</param>
		public TimeoutStream(Stream src)
			: base()
		{
			//myTimer = timer;
			_stream = src;
			_read_timer.Elapsed += OnTimout;
			_write_timer.Elapsed += OnTimout;
		}

		private void OnTimout(object sender, ElapsedEventArgs e)
		{
			_stream.Close();
		}

		///<summary> return number of milliseconds before aborting a read. </summary>
		public int getTimeout()
		{
			return (int)_read_timer.Interval;
		}


		/// <param name="millis"> number of milliseconds before aborting a read. Must be > 0.</param>
		public void setTimeout(int millis)
		{
			if (millis < 0)
				throw new ArgumentException("Invalid timeout: " + millis);
			_read_timer.Interval = millis;
			_write_timer.Interval = millis;
		}

		public override int ReadByte()
		{
			try
			{
				beginRead();
				return _stream.ReadByte();
			}
			catch (ObjectDisposedException)
			{
				throw readTimedOut();
			}
			finally
			{
				endRead();
			}
		}


		public override int Read(byte[] buffer, int offset, int count)
		{
			try
			{
				beginRead();
				return _stream.Read(buffer, offset, count);
			}
			catch (ObjectDisposedException)
			{
				throw readTimedOut();
			}
			finally
			{
				endRead();
			}
		}

		public long Skip(long cnt)
		{
			try
			{
				beginRead();
				return _stream.Seek(cnt, SeekOrigin.Current);
			}
			catch (ObjectDisposedException)
			{
				throw readTimedOut();
			}
			finally
			{
				endRead();
			}
		}

		private void beginRead()
		{
			_read_timer.Start();
		}

		private void endRead()
		{
			_read_timer.Stop();
		}

		private static TimeoutException readTimedOut()
		{
			return new TimeoutException("Read timed out");
		}

		public override void WriteByte(byte value)
		{
			try
			{
				beginWrite();
				_stream.WriteByte(value);
			}
			catch (ObjectDisposedException)
			{
				throw writeTimedOut();
			}
			finally
			{
				endWrite();
			}
		}

		public void Write(byte[] buf)
		{
			Write(buf, 0, buf.Length);
		}


		public override void Write(byte[] buf, int off, int len)
		{
			try
			{
				beginWrite();
				_stream.Write(buf, off, len);
			}
			catch (ObjectDisposedException)
			{
				throw writeTimedOut();
			}
			finally
			{
				endWrite();
			}
		}

		public override void Flush()
		{
			try
			{
				beginWrite();
				_stream.Flush();
			}
			catch (ObjectDisposedException)
			{
				throw writeTimedOut();
			}
			finally
			{
				endWrite();
			}
		}

		public override void Close()
		{
			try
			{
				beginWrite();
				_stream.Close();
			}
			catch (ObjectDisposedException)
			{
				throw writeTimedOut();
			}
			finally
			{
				endWrite();
			}
		}

		private void beginWrite()
		{
			_write_timer.Start();
		}

		private void endWrite()
		{
			_write_timer.Stop();
		}

		private static TimeoutException writeTimedOut()
		{
			return new TimeoutException("Write timed out");
		}

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
		/// </summary>
		/// <returns>
		/// true if the stream supports reading; otherwise, false.
		/// </returns>
		/// <filterpriority>1</filterpriority>
		public override bool CanRead
		{
			get { return _stream.CanRead; }
		}

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <returns>
		/// true if the stream supports seeking; otherwise, false.
		/// </returns>
		/// <filterpriority>1</filterpriority>
		public override bool CanSeek
		{
			get { return _stream.CanSeek; }
		}

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
		/// </summary>
		/// <returns>
		/// true if the stream supports writing; otherwise, false.
		/// </returns>
		/// <filterpriority>1</filterpriority>
		public override bool CanWrite
		{
			get { return _stream.CanWrite; }
		}

		/// <summary>
		/// When overridden in a derived class, gets the length in bytes of the stream.
		/// </summary>
		/// <returns>
		/// A long value representing the length of the stream in bytes.
		/// </returns>
		/// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. 
		///                 </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. 
		///                 </exception><filterpriority>1</filterpriority>
		public override long Length
		{
			get { return _stream.Length; }
		}

		/// <summary>
		/// When overridden in a derived class, gets or sets the position within the current stream.
		/// </summary>
		/// <returns>
		/// The current position within the stream.
		/// </returns>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. 
		///                 </exception><exception cref="T:System.NotSupportedException">The stream does not support seeking. 
		///                 </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. 
		///                 </exception><filterpriority>1</filterpriority>
		public override long Position
		{
			get { return _stream.Position; }
			set { _stream.Position = value; }
		}

		public int Read(byte[] buf)
		{
			return Read(buf, 0, buf.Length);
		}


		/// <summary>
		/// When overridden in a derived class, sets the position within the current stream.
		/// </summary>
		/// <returns>
		/// The new position within the current stream.
		/// </returns>
		/// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter. 
		///                 </param><param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position. 
		///                 </param><exception cref="T:System.IO.IOException">An I/O error occurs. 
		///                 </exception><exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. 
		///                 </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. 
		///                 </exception><filterpriority>1</filterpriority>
		public override long Seek(long offset, SeekOrigin origin)
		{
			return _stream.Seek(offset, origin);
		}

		/// <summary>
		/// When overridden in a derived class, sets the length of the current stream.
		/// </summary>
		/// <param name="value">The desired length of the current stream in bytes. 
		///                 </param><exception cref="T:System.IO.IOException">An I/O error occurs. 
		///                 </exception><exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. 
		///                 </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. 
		///                 </exception><filterpriority>2</filterpriority>
		public override void SetLength(long value)
		{
			_stream.SetLength(value);
		}

	}
}
