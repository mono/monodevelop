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

using System.IO;
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit.Util.IO
{
	/// <summary>Combines messages from an OutputStream (hopefully in UTF-8) and a Writer.
	/// 	</summary>
	/// <remarks>
	/// Combines messages from an OutputStream (hopefully in UTF-8) and a Writer.
	/// <p>
	/// This class is primarily meant for
	/// <see cref="NGit.Transport.BaseConnection">NGit.Transport.BaseConnection</see>
	/// in contexts where a
	/// standard error stream from a command execution, as well as messages from a
	/// side-band channel, need to be combined together into a buffer to represent
	/// the complete set of messages from a remote repository.
	/// <p>
	/// Writes made to the writer are re-encoded as UTF-8 and interleaved into the
	/// buffer that
	/// <see cref="GetRawStream()">GetRawStream()</see>
	/// also writes to.
	/// <p>
	/// <see cref="ToString()">ToString()</see>
	/// returns all written data, after converting it to a String
	/// under the assumption of UTF-8 encoding.
	/// <p>
	/// Internally
	/// <see cref="NGit.Util.RawParseUtils.Decode(byte[])">NGit.Util.RawParseUtils.Decode(byte[])
	/// 	</see>
	/// is used by
	/// <code>toString()</code>
	/// tries to work out a reasonably correct character set for the raw data.
	/// </remarks>
	public class MessageWriter : TextWriter
	{
		private readonly ByteArrayOutputStream buf;

		private readonly OutputStreamWriter enc;

		/// <summary>Create an empty writer.</summary>
		/// <remarks>Create an empty writer.</remarks>
		public MessageWriter()
		{
			buf = new ByteArrayOutputStream();
			enc = new OutputStreamWriter(GetRawStream(), Constants.CHARSET);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(char[] cbuf, int off, int len)
		{
			lock (buf)
			{
				enc.Write(cbuf, off, len);
				enc.Flush();
			}
		}

		/// <returns>
		/// the underlying byte stream that character writes to this writer
		/// drop into. Writes to this stream should should be in UTF-8.
		/// </returns>
		public virtual OutputStream GetRawStream()
		{
			return buf;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
		}

		// Do nothing, we are buffered with no resources.
		/// <exception cref="System.IO.IOException"></exception>
		public override void Flush()
		{
		}

		// Do nothing, we are buffered with no resources.
		/// <returns>string version of all buffered data.</returns>
		public override string ToString()
		{
			return RawParseUtils.Decode(buf.ToByteArray());
		}
		
		public override System.Text.Encoding Encoding {
			get {
				return Constants.CHARSET;
			}
		}
	}
}
