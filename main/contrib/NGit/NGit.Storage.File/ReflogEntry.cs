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
using NGit;
using NGit.Internal;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>Parsed reflog entry</summary>
	[System.Serializable]
	public class ReflogEntry
	{
		private const long serialVersionUID = 1L;

		private ObjectId oldId;

		private ObjectId newId;

		private PersonIdent who;

		private string comment;

		internal ReflogEntry(byte[] raw, int pos)
		{
			oldId = ObjectId.FromString(raw, pos);
			pos += Constants.OBJECT_ID_STRING_LENGTH;
			if (raw[pos++] != ' ')
			{
				throw new ArgumentException(JGitText.Get().rawLogMessageDoesNotParseAsLogEntry);
			}
			newId = ObjectId.FromString(raw, pos);
			pos += Constants.OBJECT_ID_STRING_LENGTH;
			if (raw[pos++] != ' ')
			{
				throw new ArgumentException(JGitText.Get().rawLogMessageDoesNotParseAsLogEntry);
			}
			who = RawParseUtils.ParsePersonIdentOnly(raw, pos);
			int p0 = RawParseUtils.Next(raw, pos, '\t');
			if (p0 >= raw.Length)
			{
				comment = string.Empty;
			}
			else
			{
				// personident has no \t, no comment present
				int p1 = RawParseUtils.NextLF(raw, p0);
				comment = p1 > p0 ? RawParseUtils.Decode(raw, p0, p1 - 1) : string.Empty;
			}
		}

		/// <returns>the commit id before the change</returns>
		public virtual ObjectId GetOldId()
		{
			return oldId;
		}

		/// <returns>the commit id after the change</returns>
		public virtual ObjectId GetNewId()
		{
			return newId;
		}

		/// <returns>user performing the change</returns>
		public virtual PersonIdent GetWho()
		{
			return who;
		}

		/// <returns>textual description of the change</returns>
		public virtual string GetComment()
		{
			return comment;
		}

		public override string ToString()
		{
			return "Entry[" + oldId.Name + ", " + newId.Name + ", " + GetWho() + ", " + GetComment
				() + "]";
		}

		/// <returns>
		/// a
		/// <see cref="CheckoutEntry">CheckoutEntry</see>
		/// with parsed information about a branch
		/// switch, or null if the entry is not a checkout
		/// </returns>
		public virtual CheckoutEntry ParseCheckout()
		{
			if (GetComment().StartsWith(CheckoutEntry.CHECKOUT_MOVING_FROM))
			{
				return new CheckoutEntry(this);
			}
			else
			{
				return null;
			}
		}
	}
}
