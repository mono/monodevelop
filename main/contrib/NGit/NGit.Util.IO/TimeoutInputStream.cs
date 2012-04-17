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
using System.Threading;
using NGit.Internal;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Util.IO
{
	/// <summary>InputStream with a configurable timeout.</summary>
	/// <remarks>InputStream with a configurable timeout.</remarks>
	public class TimeoutInputStream : FilterInputStream
	{
		private readonly InterruptTimer myTimer;

		private int timeout;

		/// <summary>Wrap an input stream with a timeout on all read operations.</summary>
		/// <remarks>Wrap an input stream with a timeout on all read operations.</remarks>
		/// <param name="src">
		/// base input stream (to read from). The stream must be
		/// interruptible (most socket streams are).
		/// </param>
		/// <param name="timer">timer to manage the timeouts during reads.</param>
		public TimeoutInputStream(InputStream src, InterruptTimer timer) : base(src)
		{
			myTimer = timer;
		}

		/// <returns>number of milliseconds before aborting a read.</returns>
		public virtual int GetTimeout()
		{
			return timeout;
		}

		/// <param name="millis">number of milliseconds before aborting a read. Must be &gt; 0.
		/// 	</param>
		public virtual void SetTimeout(int millis)
		{
			if (millis < 0)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidTimeout, millis
					));
			}
			timeout = millis;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read()
		{
			try
			{
				BeginRead();
				return base.Read();
			}
			catch (ThreadInterruptedException)
			{
				throw ReadTimedOut();
			}
			finally
			{
				EndRead();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] buf)
		{
			return Read(buf, 0, buf.Length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] buf, int off, int cnt)
		{
			try
			{
				BeginRead();
				return base.Read(buf, off, cnt);
			}
			catch (ThreadInterruptedException)
			{
				throw ReadTimedOut();
			}
			finally
			{
				EndRead();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long Skip(long cnt)
		{
			try
			{
				BeginRead();
				return base.Skip(cnt);
			}
			catch (ThreadInterruptedException)
			{
				throw ReadTimedOut();
			}
			finally
			{
				EndRead();
			}
		}

		private void BeginRead()
		{
			myTimer.Begin(timeout);
		}

		private void EndRead()
		{
			myTimer.End();
		}

		private static ThreadInterruptedException ReadTimedOut()
		{
			return new ThreadInterruptedException(JGitText.Get().readTimedOut);
		}
	}
}
