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

using NGit.Storage.File;
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>Creates the version 2 pack table of contents files.</summary>
	/// <remarks>Creates the version 2 pack table of contents files.</remarks>
	/// <seealso cref="PackIndexWriter">PackIndexWriter</seealso>
	/// <seealso cref="PackIndexV2">PackIndexV2</seealso>
	internal class PackIndexWriterV2 : PackIndexWriter
	{
		private const int MAX_OFFSET_32 = unchecked((int)(0x7fffffff));

		private const int IS_OFFSET_64 = unchecked((int)(0x80000000));

		protected internal PackIndexWriterV2(OutputStream dst) : base(dst)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void WriteImpl()
		{
			WriteTOC(2);
			WriteFanOutTable();
			WriteObjectNames();
			WriteCRCs();
			WriteOffset32();
			WriteOffset64();
			WriteChecksumFooter();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObjectNames()
		{
			foreach (PackedObjectInfo oe in entries)
			{
				oe.CopyRawTo(@out);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteCRCs()
		{
			foreach (PackedObjectInfo oe in entries)
			{
				NB.EncodeInt32(tmp, 0, oe.GetCRC());
				@out.Write(tmp, 0, 4);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteOffset32()
		{
			int o64 = 0;
			foreach (PackedObjectInfo oe in entries)
			{
				long o = oe.GetOffset();
				if (o <= MAX_OFFSET_32)
				{
					NB.EncodeInt32(tmp, 0, (int)o);
				}
				else
				{
					NB.EncodeInt32(tmp, 0, IS_OFFSET_64 | o64++);
				}
				@out.Write(tmp, 0, 4);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteOffset64()
		{
			foreach (PackedObjectInfo oe in entries)
			{
				long o = oe.GetOffset();
				if (MAX_OFFSET_32 < o)
				{
					NB.EncodeInt64(tmp, 0, o);
					@out.Write(tmp, 0, 8);
				}
			}
		}
	}
}
