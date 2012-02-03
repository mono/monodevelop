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

using System.Text;
using NGit;
using NGit.Patch;
using NGit.Util;
using Sharpen;

namespace NGit.Patch
{
	/// <summary>An error in a patch script</summary>
	public class FormatError
	{
		/// <summary>Classification of an error.</summary>
		/// <remarks>Classification of an error.</remarks>
		public enum Severity
		{
			WARNING,
			ERROR
		}

		private readonly byte[] buf;

		private readonly int offset;

		private readonly FormatError.Severity severity;

		private readonly string message;

		internal FormatError(byte[] buffer, int ptr, FormatError.Severity sev, string msg
			)
		{
			buf = buffer;
			offset = ptr;
			severity = sev;
			message = msg;
		}

		/// <returns>the severity of the error.</returns>
		public virtual FormatError.Severity GetSeverity()
		{
			return severity;
		}

		/// <returns>a message describing the error.</returns>
		public virtual string GetMessage()
		{
			return message;
		}

		/// <returns>the byte buffer holding the patch script.</returns>
		public virtual byte[] GetBuffer()
		{
			return buf;
		}

		/// <returns>
		/// byte offset within
		/// <see cref="GetBuffer()">GetBuffer()</see>
		/// where the error is
		/// </returns>
		public virtual int GetOffset()
		{
			return offset;
		}

		/// <returns>line of the patch script the error appears on.</returns>
		public virtual string GetLineText()
		{
			int eol = RawParseUtils.NextLF(buf, offset);
			return RawParseUtils.Decode(Constants.CHARSET, buf, offset, eol);
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			r.Append(GetSeverity().ToString().ToLower());
			r.Append(": at offset ");
			r.Append(GetOffset());
			r.Append(": ");
			r.Append(GetMessage());
			r.Append("\n");
			r.Append("  in ");
			r.Append(GetLineText());
			return r.ToString();
		}
	}
}
