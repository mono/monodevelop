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
using NGit.Revwalk;
using NGit.Util;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>An annotated tag.</summary>
	/// <remarks>An annotated tag.</remarks>
	[System.Serializable]
	public class RevTag : RevObject
	{
		/// <summary>Parse an annotated tag from its canonical format.</summary>
		/// <remarks>
		/// Parse an annotated tag from its canonical format.
		/// This method constructs a temporary revision pool, parses the tag as
		/// supplied, and returns it to the caller. Since the tag was built inside of
		/// a private revision pool its object pointer will be initialized, but will
		/// not have its headers loaded.
		/// Applications are discouraged from using this API. Callers usually need
		/// more than one object. Use
		/// <see cref="RevWalk.ParseTag(NGit.AnyObjectId)">RevWalk.ParseTag(NGit.AnyObjectId)
		/// 	</see>
		/// to obtain
		/// a RevTag from an existing repository.
		/// </remarks>
		/// <param name="raw">the canonical formatted tag to be parsed.</param>
		/// <returns>
		/// the parsed tag, in an isolated revision pool that is not
		/// available to the caller.
		/// </returns>
		/// <exception cref="NGit.Errors.CorruptObjectException">the tag contains a malformed header that cannot be handled.
		/// 	</exception>
		public static NGit.Revwalk.RevTag Parse(byte[] raw)
		{
			return Parse(new RevWalk((ObjectReader)null), raw);
		}

		/// <summary>Parse an annotated tag from its canonical format.</summary>
		/// <remarks>
		/// Parse an annotated tag from its canonical format.
		/// This method inserts the tag directly into the caller supplied revision
		/// pool, making it appear as though the tag exists in the repository, even
		/// if it doesn't. The repository under the pool is not affected.
		/// </remarks>
		/// <param name="rw">
		/// the revision pool to allocate the tag within. The tag's object
		/// pointer will be obtained from this pool.
		/// </param>
		/// <param name="raw">the canonical formatted tag to be parsed.</param>
		/// <returns>
		/// the parsed tag, in an isolated revision pool that is not
		/// available to the caller.
		/// </returns>
		/// <exception cref="NGit.Errors.CorruptObjectException">the tag contains a malformed header that cannot be handled.
		/// 	</exception>
		public static NGit.Revwalk.RevTag Parse(RevWalk rw, byte[] raw)
		{
			ObjectInserter.Formatter fmt = new ObjectInserter.Formatter();
			bool retain = rw.IsRetainBody();
			rw.SetRetainBody(true);
			NGit.Revwalk.RevTag r = rw.LookupTag(fmt.IdFor(Constants.OBJ_TAG, raw));
			r.ParseCanonical(rw, raw);
			rw.SetRetainBody(retain);
			return r;
		}

		private RevObject @object;

		private byte[] buffer;

		private string tagName;

		/// <summary>Create a new tag reference.</summary>
		/// <remarks>Create a new tag reference.</remarks>
		/// <param name="id">object name for the tag.</param>
		protected internal RevTag(AnyObjectId id) : base(id)
		{
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal override void ParseHeaders(RevWalk walk)
		{
			ParseCanonical(walk, walk.GetCachedBytes(this));
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal override void ParseBody(RevWalk walk)
		{
			if (buffer == null)
			{
				buffer = walk.GetCachedBytes(this);
				if ((flags & PARSED) == 0)
				{
					ParseCanonical(walk, buffer);
				}
			}
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		internal virtual void ParseCanonical(RevWalk walk, byte[] rawTag)
		{
			MutableInteger pos = new MutableInteger();
			int oType;
			pos.value = 53;
			// "object $sha1\ntype "
			oType = Constants.DecodeTypeString(this, rawTag, unchecked((byte)'\n'), pos);
			walk.idBuffer.FromString(rawTag, 7);
			@object = walk.LookupAny(walk.idBuffer, oType);
			int p = pos.value += 4;
			// "tag "
			int nameEnd = RawParseUtils.NextLF(rawTag, p) - 1;
			tagName = RawParseUtils.Decode(Constants.CHARSET, rawTag, p, nameEnd);
			if (walk.IsRetainBody())
			{
				buffer = rawTag;
			}
			flags |= PARSED;
		}

		public sealed override int Type
		{
			get
			{
				return Constants.OBJ_TAG;
			}
		}

		/// <summary>Parse the tagger identity from the raw buffer.</summary>
		/// <remarks>
		/// Parse the tagger identity from the raw buffer.
		/// <p>
		/// This method parses and returns the content of the tagger line, after
		/// taking the tag's character set into account and decoding the tagger
		/// name and email address. This method is fairly expensive and produces a
		/// new PersonIdent instance on each invocation. Callers should invoke this
		/// method only if they are certain they will be outputting the result, and
		/// should cache the return value for as long as necessary to use all
		/// information from it.
		/// </remarks>
		/// <returns>
		/// identity of the tagger (name, email) and the time the tag
		/// was made by the tagger; null if no tagger line was found.
		/// </returns>
		public PersonIdent GetTaggerIdent()
		{
			byte[] raw = buffer;
			int nameB = RawParseUtils.Tagger(raw, 0);
			if (nameB < 0)
			{
				return null;
			}
			return RawParseUtils.ParsePersonIdent(raw, nameB);
		}

		/// <summary>Parse the complete tag message and decode it to a string.</summary>
		/// <remarks>
		/// Parse the complete tag message and decode it to a string.
		/// <p>
		/// This method parses and returns the message portion of the tag buffer,
		/// after taking the tag's character set into account and decoding the buffer
		/// using that character set. This method is a fairly expensive operation and
		/// produces a new string on each invocation.
		/// </remarks>
		/// <returns>decoded tag message as a string. Never null.</returns>
		public string GetFullMessage()
		{
			byte[] raw = buffer;
			int msgB = RawParseUtils.TagMessage(raw, 0);
			if (msgB < 0)
			{
				return string.Empty;
			}
			Encoding enc = RawParseUtils.ParseEncoding(raw);
			return RawParseUtils.Decode(enc, raw, msgB, raw.Length);
		}

		/// <summary>Parse the tag message and return the first "line" of it.</summary>
		/// <remarks>
		/// Parse the tag message and return the first "line" of it.
		/// <p>
		/// The first line is everything up to the first pair of LFs. This is the
		/// "oneline" format, suitable for output in a single line display.
		/// <p>
		/// This method parses and returns the message portion of the tag buffer,
		/// after taking the tag's character set into account and decoding the buffer
		/// using that character set. This method is a fairly expensive operation and
		/// produces a new string on each invocation.
		/// </remarks>
		/// <returns>
		/// decoded tag message as a string. Never null. The returned string
		/// does not contain any LFs, even if the first paragraph spanned
		/// multiple lines. Embedded LFs are converted to spaces.
		/// </returns>
		public string GetShortMessage()
		{
			byte[] raw = buffer;
			int msgB = RawParseUtils.TagMessage(raw, 0);
			if (msgB < 0)
			{
				return string.Empty;
			}
			Encoding enc = RawParseUtils.ParseEncoding(raw);
			int msgE = RawParseUtils.EndOfParagraph(raw, msgB);
			string str = RawParseUtils.Decode(enc, raw, msgB, msgE);
			if (RevCommit.HasLF(raw, msgB, msgE))
			{
				str = str.Replace('\n', ' ');
			}
			return str;
		}

		/// <summary>Get a reference to the object this tag was placed on.</summary>
		/// <remarks>Get a reference to the object this tag was placed on.</remarks>
		/// <returns>object this tag refers to.</returns>
		public RevObject GetObject()
		{
			return @object;
		}

		/// <summary>Get the name of this tag, from the tag header.</summary>
		/// <remarks>Get the name of this tag, from the tag header.</remarks>
		/// <returns>name of the tag, according to the tag header.</returns>
		public string GetTagName()
		{
			return tagName;
		}

		internal void DisposeBody()
		{
			buffer = null;
		}
	}
}
