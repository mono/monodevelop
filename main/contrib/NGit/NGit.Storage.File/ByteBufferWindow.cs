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
using ICSharpCode.SharpZipLib.Zip.Compression;
using NGit.Storage.File;
using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// A window for accessing git packs using a
	/// <see cref="Sharpen.ByteBuffer">Sharpen.ByteBuffer</see>
	/// for storage.
	/// </summary>
	/// <seealso cref="ByteWindow">ByteWindow</seealso>
	internal sealed class ByteBufferWindow : ByteWindow
	{
		private readonly ByteBuffer buffer;

		internal ByteBufferWindow(PackFile pack, long o, ByteBuffer b) : base(pack, o, b.
			Capacity())
		{
			buffer = b;
		}

		protected internal override int Copy(int p, byte[] b, int o, int n)
		{
			ByteBuffer s = buffer.Slice();
			s.Position(p);
			n = Math.Min(s.Remaining(), n);
			s.Get(b, o, n);
			return n;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override void Write(PackOutputStream @out, long pos, int cnt, MessageDigest
			 digest)
		{
			ByteBuffer s = buffer.Slice();
			s.Position((int)(pos - start));
			while (0 < cnt)
			{
				byte[] buf = @out.GetCopyBuffer();
				int n = Math.Min(cnt, buf.Length);
				s.Get(buf, 0, n);
				@out.Write(buf, 0, n);
				if (digest != null)
				{
					digest.Update(buf, 0, n);
				}
				cnt -= n;
			}
		}

		/// <exception cref="Sharpen.DataFormatException"></exception>
		protected internal override int SetInput(int pos, Inflater inf)
		{
			ByteBuffer s = buffer.Slice();
			s.Position(pos);
			byte[] tmp = new byte[Math.Min(s.Remaining(), 512)];
			s.Get(tmp, 0, tmp.Length);
			inf.SetInput(tmp, 0, tmp.Length);
			return tmp.Length;
		}
	}
}
