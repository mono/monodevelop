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
using NGit.Revwalk;
using NGit.Util;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>Single line at the end of a message, such as a "Signed-off-by: someone".
	/// 	</summary>
	/// <remarks>
	/// Single line at the end of a message, such as a "Signed-off-by: someone".
	/// <p>
	/// These footer lines tend to be used to represent additional information about
	/// a commit, like the path it followed through reviewers before finally being
	/// accepted into the project's main repository as an immutable commit.
	/// </remarks>
	/// <seealso cref="RevCommit.GetFooterLines()">RevCommit.GetFooterLines()</seealso>
	public sealed class FooterLine
	{
		private readonly byte[] buffer;

		private readonly Encoding enc;

		private readonly int keyStart;

		private readonly int keyEnd;

		private readonly int valStart;

		private readonly int valEnd;

		internal FooterLine(byte[] b, Encoding e, int ks, int ke, int vs, int ve)
		{
			buffer = b;
			enc = e;
			keyStart = ks;
			keyEnd = ke;
			valStart = vs;
			valEnd = ve;
		}

		/// <param name="key">key to test this line's key name against.</param>
		/// <returns>
		/// true if
		/// <code>key.getName().equalsIgnorecase(getKey())</code>
		/// .
		/// </returns>
		public bool Matches(FooterKey key)
		{
			byte[] kRaw = key.raw;
			int len = kRaw.Length;
			int bPtr = keyStart;
			if (keyEnd - bPtr != len)
			{
				return false;
			}
			for (int kPtr = 0; kPtr < len; )
			{
				byte b = buffer[bPtr++];
				if ('A' <= b && ((sbyte)b) <= 'Z')
				{
					b += (byte)('a') - (byte)('A');
				}
				if (b != kRaw[kPtr++])
				{
					return false;
				}
			}
			return true;
		}

		/// <returns>
		/// key name of this footer; that is the text before the ":" on the
		/// line footer's line. The text is decoded according to the commit's
		/// specified (or assumed) character encoding.
		/// </returns>
		public string GetKey()
		{
			return RawParseUtils.Decode(enc, buffer, keyStart, keyEnd);
		}

		/// <returns>
		/// value of this footer; that is the text after the ":" and any
		/// leading whitespace has been skipped. May be the empty string if
		/// the footer has no value (line ended with ":"). The text is
		/// decoded according to the commit's specified (or assumed)
		/// character encoding.
		/// </returns>
		public string GetValue()
		{
			return RawParseUtils.Decode(enc, buffer, valStart, valEnd);
		}

		/// <summary>Extract the email address (if present) from the footer.</summary>
		/// <remarks>
		/// Extract the email address (if present) from the footer.
		/// <p>
		/// If there is an email address looking string inside of angle brackets
		/// (e.g. "<a@b>"), the return value is the part extracted from inside the
		/// brackets. If no brackets are found, then
		/// <see cref="GetValue()">GetValue()</see>
		/// is returned
		/// if the value contains an '@' sign. Otherwise, null.
		/// </remarks>
		/// <returns>email address appearing in the value of this footer, or null.</returns>
		public string GetEmailAddress()
		{
			int lt = RawParseUtils.NextLF(buffer, valStart, '<');
			if (valEnd <= lt)
			{
				int at = RawParseUtils.NextLF(buffer, valStart, '@');
				if (valStart < at && at < valEnd)
				{
					return GetValue();
				}
				return null;
			}
			int gt = RawParseUtils.NextLF(buffer, lt, '>');
			if (valEnd < gt)
			{
				return null;
			}
			return RawParseUtils.Decode(enc, buffer, lt, gt - 1);
		}

		public override string ToString()
		{
			return GetKey() + ": " + GetValue();
		}
	}
}
