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
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// A
	/// <see cref="ByteWindow">ByteWindow</see>
	/// with an underlying byte array for storage.
	/// </summary>
	internal sealed class ByteArrayWindow : ByteWindow
	{
		private readonly byte[] array;

		internal ByteArrayWindow(PackFile pack, long o, byte[] b) : base(pack, o, b.Length
			)
		{
			array = b;
		}

		protected internal override int Copy(int p, byte[] b, int o, int n)
		{
			n = Math.Min(array.Length - p, n);
			System.Array.Copy(array, p, b, o, n);
			return n;
		}

		/// <exception cref="Sharpen.DataFormatException"></exception>
		protected internal override int SetInput(int pos, Inflater inf)
		{
			int n = array.Length - pos;
			inf.SetInput(array, pos, n);
			return n;
		}

		internal void Crc32(CRC32 @out, long pos, int cnt)
		{
			@out.Update(array, (int)(pos - start), cnt);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal void Write(OutputStream @out, long pos, int cnt)
		{
			@out.Write(array, (int)(pos - start), cnt);
		}

		/// <exception cref="Sharpen.DataFormatException"></exception>
		internal void Check(Inflater inf, byte[] tmp, long pos, int cnt)
		{
			inf.SetInput(array, (int)(pos - start), cnt);
			while (inf.Inflate(tmp, 0, tmp.Length) > 0)
			{
				continue;
			}
		}
	}
}
