/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.RevWalk
{
	/// <summary>
	/// An annotated tag.
	/// </summary>
	public class RevTag : RevObject
	{
		private RevObject _object;
		private byte[] _buffer;
		private string _tagName;

		/// <summary>
		/// Create a new tag reference.
		/// </summary>
		/// <param name="id">
		/// Object name for the tag.
		/// </param>
		internal RevTag(AnyObjectId id)
			: base(id)
		{
		}

        internal override void parseHeaders(RevWalk walk)
        {
            parseCanonical(walk, loadCanonical(walk));
        }

        internal override void parseBody(RevWalk walk)
        {
            if (_buffer == null)
            {
                _buffer = loadCanonical(walk);
                if ((Flags & PARSED) == 0)
                    parseCanonical(walk, _buffer);
            }
        }

		public void parseCanonical(RevWalk walk, byte[] rawTag)
		{
			var pos = new MutableInteger { value = 53 };

			int oType = Constants.decodeTypeString(this, rawTag, (byte)'\n', pos);
			walk.IdBuffer.FromString(rawTag, 7);
			_object = walk.lookupAny(walk.IdBuffer, oType);

			int p = pos.value += 4; // "tag "
			int nameEnd = RawParseUtils.nextLF(rawTag, p) - 1;
			_tagName = RawParseUtils.decode(Constants.CHARSET, rawTag, p, nameEnd);

            if (walk.isRetainBody())
			    _buffer = rawTag;
			Flags |= PARSED;
		}

		public override int Type
		{
			get { return Constants.OBJ_TAG; }
		}

		/// <summary>
		/// Parse the tagger identity from the raw buffer.
		/// <para />
		/// This method parses and returns the content of the tagger line, After
		/// taking the tag's character set into account and decoding the tagger
		/// name and email address. This method is fairly expensive and produces a
		/// new PersonIdent instance on each invocation. Callers should invoke this
		/// method only if they are certain they will be outputting the result, and
		/// should cache the return value for as long as necessary to use all
		/// information from it.
		/// </summary>
		/// <returns>
		/// Identity of the tagger (name, email) and the time the tag
		/// was made by the tagger; null if no tagger line was found.
		/// </returns>
		public PersonIdent getTaggerIdent()
		{
			byte[] raw = _buffer;
			int nameB = RawParseUtils.tagger(raw, 0);
			if (nameB < 0) return null;
			return RawParseUtils.parsePersonIdent(raw, nameB);
		}

		/// <summary>
		/// Parse the complete tag message and decode it to a string.
		/// <para />
		/// This method parses and returns the message portion of the tag buffer,
		/// After taking the tag's character set into account and decoding the buffer
		/// using that character set. This method is a fairly expensive operation and
		/// produces a new string on each invocation.
		/// </summary>
		/// <returns>
		/// Decoded tag message as a string. Never null.
		/// </returns>
		public string getFullMessage()
		{
			byte[] raw = _buffer;
			int msgB = RawParseUtils.tagMessage(raw, 0);
			if (msgB < 0) return string.Empty;
			Encoding enc = RawParseUtils.parseEncoding(raw);
			return RawParseUtils.decode(enc, raw, msgB, raw.Length);
		}

		/// <summary>
		/// Parse the tag message and return the first "line" of it.
		/// <para />
		/// The first line is everything up to the first pair of LFs. This is the
		/// "oneline" format, suitable for output in a single line display.
		/// <para />
		/// This method parses and returns the message portion of the tag buffer,
		/// After taking the tag's character set into account and decoding the buffer
		/// using that character set. This method is a fairly expensive operation and
		/// produces a new string on each invocation.
		/// </summary>
		/// <returns>
		/// Decoded tag message as a string. Never null. The returned string
		/// does not contain any LFs, even if the first paragraph spanned
		/// multiple lines. Embedded LFs are converted to spaces.
		/// </returns>
		public string getShortMessage()
		{
			byte[] raw = _buffer;
			int msgB = RawParseUtils.tagMessage(raw, 0);
			if (msgB < 0) return string.Empty;

			Encoding enc = RawParseUtils.parseEncoding(raw);
			int msgE = RawParseUtils.endOfParagraph(raw, msgB);
			string str = RawParseUtils.decode(enc, raw, msgB, msgE);
			if (RevCommit.hasLF(raw, msgB, msgE))
			{
				str = str.Replace('\n', ' ');
			}

			return str;
		}

		/// <summary>
		/// Parse this tag buffer for display.
		/// </summary>
		/// <param name="walk">revision walker owning this reference.</param>
		/// <returns>parsed tag.</returns>
		public Tag asTag(RevWalk walk)
		{
			return new Tag(walk.Repository, this, _tagName, _buffer);
		}

		/// <summary>
		/// Get a reference to the @object this tag was placed on.
		/// </summary>
		/// <returns>
		/// Object this tag refers to.
		/// </returns>
		public RevObject getObject()
		{
			return _object;
		}

		/// <summary>
		/// Get the name of this tag, from the tag header.
		/// </summary>
		/// <returns>
		/// Name of the tag, according to the tag header.
		/// </returns>
		public string getTagName()
		{
			return _tagName;
		}

        public new void DisposeBody()
        {
            _buffer = null;
        }
	}
}