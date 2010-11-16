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
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Creates loose objects in a
	/// <see cref="ObjectDirectory">ObjectDirectory</see>
	/// .
	/// </summary>
	internal class ObjectDirectoryInserter : ObjectInserter
	{
		private readonly FileObjectDatabase db;

		private readonly WriteConfig config;

		private Deflater deflate;

		internal ObjectDirectoryInserter(FileObjectDatabase dest, Config cfg)
		{
			db = dest;
			config = cfg.Get(WriteConfig.KEY);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override ObjectId Insert(int type, long len, InputStream @is)
		{
			MessageDigest md = Digest();
			FilePath tmp = ToTemp(md, type, len, @is);
			ObjectId id = ObjectId.FromRaw(md.Digest());
			switch (db.InsertUnpackedObject(tmp, id, false))
			{
				case FileObjectDatabase.InsertLooseObjectResult.INSERTED:
				case FileObjectDatabase.InsertLooseObjectResult.EXISTS_PACKED:
				case FileObjectDatabase.InsertLooseObjectResult.EXISTS_LOOSE:
				{
					return id;
				}

				case FileObjectDatabase.InsertLooseObjectResult.FAILURE:
				default:
				{
					break;
					break;
				}
			}
			FilePath dst = db.FileFor(id);
			throw new ObjectWritingException("Unable to create new object: " + dst);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Flush()
		{
		}

		// Do nothing. Objects are immediately visible.
		public override void Release()
		{
			if (deflate != null)
			{
				try
				{
					deflate.Finish();
				}
				finally
				{
					deflate = null;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.IO.FileNotFoundException"></exception>
		/// <exception cref="Sharpen.Error"></exception>
		private FilePath ToTemp(MessageDigest md, int type, long len, InputStream @is)
		{
			bool delete = true;
			FilePath tmp = NewTempFile();
			try
			{
				FileOutputStream fOut = new FileOutputStream(tmp);
				try
				{
					OutputStream @out = fOut;
					if (config.GetFSyncObjectFiles())
					{
						@out = Channels.NewOutputStream(fOut.GetChannel());
					}
					DeflaterOutputStream cOut = Compress(@out);
					DigestOutputStream dOut = new DigestOutputStream(cOut, md);
					WriteHeader(dOut, type, len);
					byte[] buf = Buffer();
					while (len > 0)
					{
						int n = @is.Read(buf, 0, (int)Math.Min(len, buf.Length));
						if (n <= 0)
						{
							throw ShortInput(len);
						}
						dOut.Write(buf, 0, n);
						len -= n;
					}
					dOut.Flush();
					cOut.Finish();
				}
				finally
				{
					if (config.GetFSyncObjectFiles())
					{
						fOut.GetChannel().Force(true);
					}
					fOut.Close();
				}
				delete = false;
				return tmp;
			}
			finally
			{
				if (delete)
				{
					tmp.Delete();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void WriteHeader(OutputStream @out, int type, long len)
		{
			@out.Write(Constants.EncodedTypeString(type));
			@out.Write(unchecked((byte)' '));
			@out.Write(Constants.EncodeASCII(len));
			@out.Write(unchecked((byte)0));
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual FilePath NewTempFile()
		{
			return FilePath.CreateTempFile("noz", null, db.GetDirectory());
		}

		internal virtual DeflaterOutputStream Compress(OutputStream @out)
		{
			if (deflate == null)
			{
				deflate = new Deflater(config.GetCompression());
			}
			else
			{
				deflate.Reset();
			}
			return new DeflaterOutputStream(@out, deflate);
		}

		private static EOFException ShortInput(long missing)
		{
			return new EOFException("Input did not match supplied length. " + missing + " bytes are missing."
				);
		}
	}
}
