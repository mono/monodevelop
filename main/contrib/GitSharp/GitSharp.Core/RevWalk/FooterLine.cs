/*
 * Copyright (C) 2009, Google Inc.
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

using System.Text;
using GitSharp.Core.Util;

namespace GitSharp.Core.RevWalk
{
	/// <summary>
	/// Single line at the end of a message, such as a "Signed-off-by: someone".
	/// <para />
	/// These footer lines tend to be used to represent additional information about
	/// a commit, like the path it followed through reviewers before finally being
	/// accepted into the project's main repository as an immutable commit.
	/// </summary>
	/// <seealso cref="RevCommit.GetFooterLines()"/>
	public class FooterLine
	{
		private readonly byte[] _buffer;
		private readonly Encoding _enc;
		private readonly int _keyStart;
		private readonly int _keyEnd;
		private readonly int _valStart;
		private readonly int _valEnd;

		public FooterLine(byte[] b, Encoding e, int ks, int ke, int vs, int ve)
		{
			_buffer = b;
			_enc = e;
			_keyStart = ks;
			_keyEnd = ke;
			_valStart = vs;
			_valEnd = ve;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key">
		/// Key to test this line's key name against.
		/// </param>
		/// <returns>
		/// true if <code>code key.Name.Equals(Key, StringComparison.InvariantCultureIgnoreCase))</code>.
		/// </returns>
		public bool Matches(FooterKey key)
		{
			if (key == null)
				throw new System.ArgumentNullException ("key");
			
			byte[] kRaw = key.Raw;
			int len = kRaw.Length;
			int bPtr = _keyStart;
			if (_keyEnd - bPtr != len) return false;

			for (int kPtr = 0; bPtr < len; )
			{
				byte b = _buffer[bPtr++];
				if ('A' <= b && b <= 'Z')
				{
					b += 'a' - 'A';
				}

				if (b != kRaw[kPtr++]) return false;
			}

			return true;
		}

		/// <summary>
		/// Key name of this footer; that is the text before the ":" on the
		/// line footer's line. The text is decoded according to the commit's
		/// specified (or assumed) character encoding.
		/// </summary>
		public string Key
		{
			get { return RawParseUtils.decode(_enc, _buffer, _keyStart, _keyEnd); }
		}

		/// <summary>
		/// Value of this footer; that is the text after the ":" and any
		/// leading whitespace has been skipped. May be the empty string if
		/// the footer has no value (line ended with ":"). The text is
		/// decoded according to the commit's specified (or assumed)
		/// character encoding.
		/// </summary>
		public string Value
		{
			get { return RawParseUtils.decode(_enc, _buffer, _valStart, _valEnd); }
		}

		/// <summary>
		/// 
		/// Extract the email address (if present) from the footer.
		/// <para />
		/// If there is an email address looking string inside of angle brackets
		/// (e.g. "&lt;a@b&gt;"), the return value is the part extracted from inside the
		/// brackets. If no brackets are found, then <see cref="Value"/> is returned
		/// if the value contains an '@' sign. Otherwise, null.
		/// </summary>
		/// <returns>email address appearing in the value of this footer, or null.</returns>
		public string getEmailAddress()
		{
			int lt = RawParseUtils.nextLF(_buffer, _valStart, (byte)'<');
			if (_valEnd <= lt)
			{
				int at = RawParseUtils.nextLF(_buffer, _valStart, (byte)'@');
				if (_valStart < at && at < _valEnd)
				{
					return Value;
				}

				return null;
			}

			int gt = RawParseUtils.nextLF(_buffer, lt, (byte)'>');
			if (_valEnd < gt) return null;
			
			return RawParseUtils.decode(_enc, _buffer, lt, gt - 1);
		}

		public override string ToString()
		{
			return Key + ": " + Value;
		}
	}
}
