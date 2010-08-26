/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

using System;
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
	public class MutableObjectId : AnyObjectId
	{
		public MutableObjectId()
			: this(ObjectId.ZeroId)
		{
		}

		public MutableObjectId(AnyObjectId src)
			: base(src)
		{
		}

		/// <summary>
		/// Make this id match <see cref="ObjectId.ZeroId"/>.
		/// </summary>
		public void Clear()
		{
			W1 = 0;
			W2 = 0;
			W3 = 0;
			W4 = 0;
			W5 = 0;
		}

		public void FromRaw(byte[] bs)
		{
			FromRaw(bs, 0);
		}

		public void FromRaw(byte[] bs, int p)
		{
			W1 = NB.DecodeInt32(bs, p);
			W2 = NB.DecodeInt32(bs, p + 4);
			W3 = NB.DecodeInt32(bs, p + 8);
			W4 = NB.DecodeInt32(bs, p + 12);
			W5 = NB.DecodeInt32(bs, p + 16);
		}

		public void FromRaw(int[] ints)
		{
			FromRaw(ints, 0);
		}

		public void FromRaw(int[] ints, int p)
		{
			W1 = ints[p];
			W2 = ints[p + 1];
			W3 = ints[p + 2];
			W4 = ints[p + 3];
			W5 = ints[p + 4];
		}

		public void FromString(byte[] buf, int offset)
		{
			FromHexString(buf, offset);
		}

		public void FromString(string str)
		{
            if (str.Length != Constants.OBJECT_ID_STRING_LENGTH)
			{
				throw new ArgumentException("Invalid id: " + str, "str");
			}

			FromHexString(Constants.encodeASCII(str), 0);
		}

		private void FromHexString(byte[] bs, int p)
		{
			try
			{
                W1 = RawParseUtils.parseHexInt32(bs, p);
                W2 = RawParseUtils.parseHexInt32(bs, p + 8);
                W3 = RawParseUtils.parseHexInt32(bs, p + 16);
                W4 = RawParseUtils.parseHexInt32(bs, p + 24);
                W5 = RawParseUtils.parseHexInt32(bs, p + 32);
			}
            catch (IndexOutOfRangeException e)
            {
                throw new InvalidObjectIdException(bs, p, Constants.OBJECT_ID_STRING_LENGTH, e);
            }
		}

		public override ObjectId ToObjectId()
		{
			return new ObjectId(this);
		}
	}
}