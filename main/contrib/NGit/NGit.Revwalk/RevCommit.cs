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

using System.Collections.Generic;
using System.Text;
using NGit;
using NGit.Revwalk;
using NGit.Util;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>A commit reference to a commit in the DAG.</summary>
	/// <remarks>A commit reference to a commit in the DAG.</remarks>
	[System.Serializable]
	public class RevCommit : RevObject
	{
		/// <summary>Parse a commit from its canonical format.</summary>
		/// <remarks>
		/// Parse a commit from its canonical format.
		/// This method constructs a temporary revision pool, parses the commit as
		/// supplied, and returns it to the caller. Since the commit was built inside
		/// of a private revision pool its parent pointers will be initialized, but
		/// will not have their headers loaded.
		/// Applications are discouraged from using this API. Callers usually need
		/// more than one commit. Use
		/// <see cref="RevWalk.ParseCommit(NGit.AnyObjectId)">RevWalk.ParseCommit(NGit.AnyObjectId)
		/// 	</see>
		/// to
		/// obtain a RevCommit from an existing repository.
		/// </remarks>
		/// <param name="raw">the canonical formatted commit to be parsed.</param>
		/// <returns>
		/// the parsed commit, in an isolated revision pool that is not
		/// available to the caller.
		/// </returns>
		public static NGit.Revwalk.RevCommit Parse(byte[] raw)
		{
			return Parse(new RevWalk((ObjectReader)null), raw);
		}

		/// <summary>Parse a commit from its canonical format.</summary>
		/// <remarks>
		/// Parse a commit from its canonical format.
		/// This method inserts the commit directly into the caller supplied revision
		/// pool, making it appear as though the commit exists in the repository,
		/// even if it doesn't.  The repository under the pool is not affected.
		/// </remarks>
		/// <param name="rw">
		/// the revision pool to allocate the commit within. The commit's
		/// tree and parent pointers will be obtained from this pool.
		/// </param>
		/// <param name="raw">the canonical formatted commit to be parsed.</param>
		/// <returns>
		/// the parsed commit, in an isolated revision pool that is not
		/// available to the caller.
		/// </returns>
		public static NGit.Revwalk.RevCommit Parse(RevWalk rw, byte[] raw)
		{
			ObjectInserter.Formatter fmt = new ObjectInserter.Formatter();
			bool retain = rw.IsRetainBody();
			rw.SetRetainBody(true);
			NGit.Revwalk.RevCommit r = rw.LookupCommit(fmt.IdFor(Constants.OBJ_COMMIT, raw));
			r.ParseCanonical(rw, raw);
			rw.SetRetainBody(retain);
			return r;
		}

		internal static readonly NGit.Revwalk.RevCommit[] NO_PARENTS = new NGit.Revwalk.RevCommit
			[] {  };

		private RevTree tree;

		internal NGit.Revwalk.RevCommit[] parents;

		internal int commitTime;

		internal int inDegree;

		private byte[] buffer;

		/// <summary>Create a new commit reference.</summary>
		/// <remarks>Create a new commit reference.</remarks>
		/// <param name="id">object name for the commit.</param>
		protected internal RevCommit(AnyObjectId id) : base(id)
		{
		}

		// An int here for performance, overflows in 2038
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

		internal virtual void ParseCanonical(RevWalk walk, byte[] raw)
		{
			MutableObjectId idBuffer = walk.idBuffer;
			idBuffer.FromString(raw, 5);
			tree = walk.LookupTree(idBuffer);
			int ptr = 46;
			if (parents == null)
			{
				NGit.Revwalk.RevCommit[] pList = new NGit.Revwalk.RevCommit[1];
				int nParents = 0;
				for (; ; )
				{
					if (raw[ptr] != 'p')
					{
						break;
					}
					idBuffer.FromString(raw, ptr + 7);
					NGit.Revwalk.RevCommit p = walk.LookupCommit(idBuffer);
					if (nParents == 0)
					{
						pList[nParents++] = p;
					}
					else
					{
						if (nParents == 1)
						{
							pList = new NGit.Revwalk.RevCommit[] { pList[0], p };
							nParents = 2;
						}
						else
						{
							if (pList.Length <= nParents)
							{
								NGit.Revwalk.RevCommit[] old = pList;
								pList = new NGit.Revwalk.RevCommit[pList.Length + 32];
								System.Array.Copy(old, 0, pList, 0, nParents);
							}
							pList[nParents++] = p;
						}
					}
					ptr += 48;
				}
				if (nParents != pList.Length)
				{
					NGit.Revwalk.RevCommit[] old = pList;
					pList = new NGit.Revwalk.RevCommit[nParents];
					System.Array.Copy(old, 0, pList, 0, nParents);
				}
				parents = pList;
			}
			// extract time from "committer "
			ptr = RawParseUtils.Committer(raw, ptr);
			if (ptr > 0)
			{
				ptr = RawParseUtils.NextLF(raw, ptr, '>');
				// In 2038 commitTime will overflow unless it is changed to long.
				commitTime = RawParseUtils.ParseBase10(raw, ptr, null);
			}
			if (walk.IsRetainBody())
			{
				buffer = raw;
			}
			flags |= PARSED;
		}

		public sealed override int Type
		{
			get
			{
				return Constants.OBJ_COMMIT;
			}
		}

		internal static void CarryFlags(NGit.Revwalk.RevCommit c, int carry)
		{
			for (; ; )
			{
				NGit.Revwalk.RevCommit[] pList = c.parents;
				if (pList == null)
				{
					return;
				}
				int n = pList.Length;
				if (n == 0)
				{
					return;
				}
				for (int i = 1; i < n; i++)
				{
					NGit.Revwalk.RevCommit p = pList[i];
					if ((p.flags & carry) == carry)
					{
						continue;
					}
					p.flags |= carry;
					CarryFlags(p, carry);
				}
				c = pList[0];
				if ((c.flags & carry) == carry)
				{
					return;
				}
				c.flags |= carry;
			}
		}

		/// <summary>Carry a RevFlag set on this commit to its parents.</summary>
		/// <remarks>
		/// Carry a RevFlag set on this commit to its parents.
		/// <p>
		/// If this commit is parsed, has parents, and has the supplied flag set on
		/// it we automatically add it to the parents, grand-parents, and so on until
		/// an unparsed commit or a commit with no parents is discovered. This
		/// permits applications to force a flag through the history chain when
		/// necessary.
		/// </remarks>
		/// <param name="flag">the single flag value to carry back onto parents.</param>
		public virtual void Carry(RevFlag flag)
		{
			int carry = flags & flag.mask;
			if (carry != 0)
			{
				CarryFlags(this, carry);
			}
		}

		/// <summary>Time from the "committer " line of the buffer.</summary>
		/// <remarks>Time from the "committer " line of the buffer.</remarks>
		/// <returns>time, expressed as seconds since the epoch.</returns>
		public int CommitTime
		{
			get
			{
				return commitTime;
			}
		}

		/// <summary>Get a reference to this commit's tree.</summary>
		/// <remarks>Get a reference to this commit's tree.</remarks>
		/// <returns>tree of this commit.</returns>
		public RevTree Tree
		{
			get
			{
				return tree;
			}
		}

		/// <summary>Get the number of parent commits listed in this commit.</summary>
		/// <remarks>Get the number of parent commits listed in this commit.</remarks>
		/// <returns>number of parents; always a positive value but can be 0.</returns>
		public int ParentCount
		{
			get
			{
				return parents.Length;
			}
		}

		/// <summary>Get the nth parent from this commit's parent list.</summary>
		/// <remarks>Get the nth parent from this commit's parent list.</remarks>
		/// <param name="nth">
		/// parent index to obtain. Must be in the range 0 through
		/// <see cref="ParentCount()">ParentCount()</see>
		/// -1.
		/// </param>
		/// <returns>the specified parent.</returns>
		/// <exception cref="System.IndexOutOfRangeException">an invalid parent index was specified.
		/// 	</exception>
		public NGit.Revwalk.RevCommit GetParent(int nth)
		{
			return parents[nth];
		}

		/// <summary>Obtain an array of all parents (<b>NOTE - THIS IS NOT A COPY</b>).</summary>
		/// <remarks>
		/// Obtain an array of all parents (<b>NOTE - THIS IS NOT A COPY</b>).
		/// <p>
		/// This method is exposed only to provide very fast, efficient access to
		/// this commit's parent list. Applications relying on this list should be
		/// very careful to ensure they do not modify its contents during their use
		/// of it.
		/// </remarks>
		/// <returns>the array of parents.</returns>
		public NGit.Revwalk.RevCommit[] Parents
		{
			get
			{
				return parents;
			}
		}

		/// <summary>Obtain the raw unparsed commit body (<b>NOTE - THIS IS NOT A COPY</b>).</summary>
		/// <remarks>
		/// Obtain the raw unparsed commit body (<b>NOTE - THIS IS NOT A COPY</b>).
		/// <p>
		/// This method is exposed only to provide very fast, efficient access to
		/// this commit's message buffer within a RevFilter. Applications relying on
		/// this buffer should be very careful to ensure they do not modify its
		/// contents during their use of it.
		/// </remarks>
		/// <returns>
		/// the raw unparsed commit body. This is <b>NOT A COPY</b>.
		/// Altering the contents of this buffer may alter the walker's
		/// knowledge of this commit, and the results it produces.
		/// </returns>
		public byte[] RawBuffer
		{
			get
			{
				return buffer;
			}
		}

		/// <summary>Parse the author identity from the raw buffer.</summary>
		/// <remarks>
		/// Parse the author identity from the raw buffer.
		/// <p>
		/// This method parses and returns the content of the author line, after
		/// taking the commit's character set into account and decoding the author
		/// name and email address. This method is fairly expensive and produces a
		/// new PersonIdent instance on each invocation. Callers should invoke this
		/// method only if they are certain they will be outputting the result, and
		/// should cache the return value for as long as necessary to use all
		/// information from it.
		/// <p>
		/// RevFilter implementations should try to use
		/// <see cref="NGit.Util.RawParseUtils">NGit.Util.RawParseUtils</see>
		/// to scan
		/// the
		/// <see cref="RawBuffer()">RawBuffer()</see>
		/// instead, as this will allow faster evaluation
		/// of commits.
		/// </remarks>
		/// <returns>
		/// identity of the author (name, email) and the time the commit was
		/// made by the author; null if no author line was found.
		/// </returns>
		public PersonIdent GetAuthorIdent()
		{
			byte[] raw = buffer;
			int nameB = RawParseUtils.Author(raw, 0);
			if (nameB < 0)
			{
				return null;
			}
			return RawParseUtils.ParsePersonIdent(raw, nameB);
		}

		/// <summary>Parse the committer identity from the raw buffer.</summary>
		/// <remarks>
		/// Parse the committer identity from the raw buffer.
		/// <p>
		/// This method parses and returns the content of the committer line, after
		/// taking the commit's character set into account and decoding the committer
		/// name and email address. This method is fairly expensive and produces a
		/// new PersonIdent instance on each invocation. Callers should invoke this
		/// method only if they are certain they will be outputting the result, and
		/// should cache the return value for as long as necessary to use all
		/// information from it.
		/// <p>
		/// RevFilter implementations should try to use
		/// <see cref="NGit.Util.RawParseUtils">NGit.Util.RawParseUtils</see>
		/// to scan
		/// the
		/// <see cref="RawBuffer()">RawBuffer()</see>
		/// instead, as this will allow faster evaluation
		/// of commits.
		/// </remarks>
		/// <returns>
		/// identity of the committer (name, email) and the time the commit
		/// was made by the committer; null if no committer line was found.
		/// </returns>
		public PersonIdent GetCommitterIdent()
		{
			byte[] raw = buffer;
			int nameB = RawParseUtils.Committer(raw, 0);
			if (nameB < 0)
			{
				return null;
			}
			return RawParseUtils.ParsePersonIdent(raw, nameB);
		}

		/// <summary>Parse the complete commit message and decode it to a string.</summary>
		/// <remarks>
		/// Parse the complete commit message and decode it to a string.
		/// <p>
		/// This method parses and returns the message portion of the commit buffer,
		/// after taking the commit's character set into account and decoding the
		/// buffer using that character set. This method is a fairly expensive
		/// operation and produces a new string on each invocation.
		/// </remarks>
		/// <returns>decoded commit message as a string. Never null.</returns>
		public string GetFullMessage()
		{
			byte[] raw = buffer;
			int msgB = RawParseUtils.CommitMessage(raw, 0);
			if (msgB < 0)
			{
				return string.Empty;
			}
			System.Text.Encoding enc = RawParseUtils.ParseEncoding(raw);
			return RawParseUtils.Decode(enc, raw, msgB, raw.Length);
		}

		/// <summary>Parse the commit message and return the first "line" of it.</summary>
		/// <remarks>
		/// Parse the commit message and return the first "line" of it.
		/// <p>
		/// The first line is everything up to the first pair of LFs. This is the
		/// "oneline" format, suitable for output in a single line display.
		/// <p>
		/// This method parses and returns the message portion of the commit buffer,
		/// after taking the commit's character set into account and decoding the
		/// buffer using that character set. This method is a fairly expensive
		/// operation and produces a new string on each invocation.
		/// </remarks>
		/// <returns>
		/// decoded commit message as a string. Never null. The returned
		/// string does not contain any LFs, even if the first paragraph
		/// spanned multiple lines. Embedded LFs are converted to spaces.
		/// </returns>
		public string GetShortMessage()
		{
			byte[] raw = buffer;
			int msgB = RawParseUtils.CommitMessage(raw, 0);
			if (msgB < 0)
			{
				return string.Empty;
			}
			System.Text.Encoding enc = RawParseUtils.ParseEncoding(raw);
			int msgE = RawParseUtils.EndOfParagraph(raw, msgB);
			string str = RawParseUtils.Decode(enc, raw, msgB, msgE);
			if (HasLF(raw, msgB, msgE))
			{
				str = str.Replace('\n', ' ');
			}
			return str;
		}

		internal static bool HasLF(byte[] r, int b, int e)
		{
			while (b < e)
			{
				if (r[b++] == '\n')
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Determine the encoding of the commit message buffer.</summary>
		/// <remarks>
		/// Determine the encoding of the commit message buffer.
		/// <p>
		/// Locates the "encoding" header (if present) and then returns the proper
		/// character set to apply to this buffer to evaluate its contents as
		/// character data.
		/// <p>
		/// If no encoding header is present,
		/// <see cref="NGit.Constants.CHARSET">NGit.Constants.CHARSET</see>
		/// is assumed.
		/// </remarks>
		/// <returns>
		/// the preferred encoding of
		/// <see cref="RawBuffer()">RawBuffer()</see>
		/// .
		/// </returns>
		public System.Text.Encoding Encoding
		{
			get
			{
				return RawParseUtils.ParseEncoding(buffer);
			}
		}

		/// <summary>Parse the footer lines (e.g.</summary>
		/// <remarks>
		/// Parse the footer lines (e.g. "Signed-off-by") for machine processing.
		/// <p>
		/// This method splits all of the footer lines out of the last paragraph of
		/// the commit message, providing each line as a key-value pair, ordered by
		/// the order of the line's appearance in the commit message itself.
		/// <p>
		/// A footer line's key must match the pattern
		/// <code>^[A-Za-z0-9-]+:</code>
		/// , while
		/// the value is free-form, but must not contain an LF. Very common keys seen
		/// in the wild are:
		/// <ul>
		/// <li>
		/// <code>Signed-off-by</code>
		/// (agrees to Developer Certificate of Origin)
		/// <li>
		/// <code>Acked-by</code>
		/// (thinks change looks sane in context)
		/// <li>
		/// <code>Reported-by</code>
		/// (originally found the issue this change fixes)
		/// <li>
		/// <code>Tested-by</code>
		/// (validated change fixes the issue for them)
		/// <li>
		/// <code>CC</code>
		/// ,
		/// <code>Cc</code>
		/// (copy on all email related to this change)
		/// <li>
		/// <code>Bug</code>
		/// (link to project's bug tracking system)
		/// </ul>
		/// </remarks>
		/// <returns>ordered list of footer lines; empty list if no footers found.</returns>
		public IList<FooterLine> GetFooterLines()
		{
			byte[] raw = buffer;
			int ptr = raw.Length - 1;
			while (raw[ptr] == '\n')
			{
				// trim any trailing LFs, not interesting
				ptr--;
			}
			int msgB = RawParseUtils.CommitMessage(raw, 0);
			AList<FooterLine> r = new AList<FooterLine>(4);
			System.Text.Encoding enc = Encoding;
			for (; ; )
			{
				ptr = RawParseUtils.PrevLF(raw, ptr);
				if (ptr <= msgB)
				{
					break;
				}
				// Don't parse commit headers as footer lines.
				int keyStart = ptr + 2;
				if (raw[keyStart] == '\n')
				{
					break;
				}
				// Stop at first paragraph break, no footers above it.
				int keyEnd = RawParseUtils.EndOfFooterLineKey(raw, keyStart);
				if (keyEnd < 0)
				{
					continue;
				}
				// Not a well formed footer line, skip it.
				// Skip over the ': *' at the end of the key before the value.
				//
				int valStart = keyEnd + 1;
				while (valStart < raw.Length && raw[valStart] == ' ')
				{
					valStart++;
				}
				// Value ends at the LF, and does not include it.
				//
				int valEnd = RawParseUtils.NextLF(raw, valStart);
				if (raw[valEnd - 1] == '\n')
				{
					valEnd--;
				}
				r.AddItem(new FooterLine(raw, enc, keyStart, keyEnd, valStart, valEnd));
			}
			Sharpen.Collections.Reverse(r);
			return r;
		}

		/// <summary>Get the values of all footer lines with the given key.</summary>
		/// <remarks>Get the values of all footer lines with the given key.</remarks>
		/// <param name="keyName">footer key to find values of, case insensitive.</param>
		/// <returns>
		/// values of footers with key of
		/// <code>keyName</code>
		/// , ordered by their
		/// order of appearance. Duplicates may be returned if the same
		/// footer appeared more than once. Empty list if no footers appear
		/// with the specified key, or there are no footers at all.
		/// </returns>
		/// <seealso cref="GetFooterLines()">GetFooterLines()</seealso>
		public IList<string> GetFooterLines(string keyName)
		{
			return GetFooterLines(new FooterKey(keyName));
		}

		/// <summary>Get the values of all footer lines with the given key.</summary>
		/// <remarks>Get the values of all footer lines with the given key.</remarks>
		/// <param name="keyName">footer key to find values of, case insensitive.</param>
		/// <returns>
		/// values of footers with key of
		/// <code>keyName</code>
		/// , ordered by their
		/// order of appearance. Duplicates may be returned if the same
		/// footer appeared more than once. Empty list if no footers appear
		/// with the specified key, or there are no footers at all.
		/// </returns>
		/// <seealso cref="GetFooterLines()">GetFooterLines()</seealso>
		public IList<string> GetFooterLines(FooterKey keyName)
		{
			IList<FooterLine> src = GetFooterLines();
			if (src.IsEmpty())
			{
				return Sharpen.Collections.EmptyList<string>();
			}
			AList<string> r = new AList<string>(src.Count);
			foreach (FooterLine f in src)
			{
				if (f.Matches(keyName))
				{
					r.AddItem(f.GetValue());
				}
			}
			return r;
		}

		/// <summary>Reset this commit to allow another RevWalk with the same instances.</summary>
		/// <remarks>
		/// Reset this commit to allow another RevWalk with the same instances.
		/// <p>
		/// Subclasses <b>must</b> call <code>super.reset()</code> to ensure the
		/// basic information can be correctly cleared out.
		/// </remarks>
		public virtual void Reset()
		{
			inDegree = 0;
		}

		internal void DisposeBody()
		{
			buffer = null;
		}

		public override string ToString()
		{
			StringBuilder s = new StringBuilder();
			s.Append(Constants.TypeString(Type));
			s.Append(' ');
			s.Append(Name);
			s.Append(' ');
			s.Append(commitTime);
			s.Append(' ');
			AppendCoreFlags(s);
			return s.ToString();
		}
	}
}
