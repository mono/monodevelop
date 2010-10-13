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
using System.IO;
using NGit;
using NGit.Storage.Pack;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>
	/// Custom output stream to support
	/// <see cref="PackWriter">PackWriter</see>
	/// .
	/// </summary>
	public sealed class PackOutputStream : OutputStream
	{
		private readonly int BYTES_TO_WRITE_BEFORE_CANCEL_CHECK = 128 * 1024;

		private readonly ProgressMonitor writeMonitor;

		private readonly OutputStream @out;

		private readonly PackWriter packWriter;

		private readonly CRC32 crc = new CRC32();

		private readonly MessageDigest md = Constants.NewMessageDigest();

		private long count;

		private byte[] headerBuffer = new byte[32];

		private byte[] copyBuffer;

		private long checkCancelAt;

		/// <summary>Initialize a pack output stream.</summary>
		/// <remarks>
		/// Initialize a pack output stream.
		/// <p>
		/// This constructor is exposed to support debugging the JGit library only.
		/// Application or storage level code should not create a PackOutputStream,
		/// instead use
		/// <see cref="PackWriter">PackWriter</see>
		/// , and let the writer create the stream.
		/// </remarks>
		/// <param name="writeMonitor">monitor to update on object output progress.</param>
		/// <param name="out">target stream to receive all object contents.</param>
		/// <param name="pw">packer that is going to perform the output.</param>
		public PackOutputStream(ProgressMonitor writeMonitor, OutputStream @out, PackWriter
			 pw)
		{
			this.writeMonitor = writeMonitor;
			this.@out = @out;
			this.packWriter = pw;
			this.checkCancelAt = BYTES_TO_WRITE_BEFORE_CANCEL_CHECK;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(int b)
		{
			count++;
			@out.Write(b);
			crc.Update(b);
			md.Update(unchecked((byte)b));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(byte[] b, int off, int len)
		{
			while (0 < len)
			{
				int n = Math.Min(len, BYTES_TO_WRITE_BEFORE_CANCEL_CHECK);
				count += n;
				if (checkCancelAt <= count)
				{
					if (writeMonitor.IsCancelled())
					{
						throw new IOException(JGitText.Get().packingCancelledDuringObjectsWriting);
					}
					checkCancelAt = count + BYTES_TO_WRITE_BEFORE_CANCEL_CHECK;
				}
				@out.Write(b, off, n);
				crc.Update(b, off, n);
				md.Update(b, off, n);
				off += n;
				len -= n;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Flush()
		{
			@out.Flush();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal void WriteFileHeader(int version, int objectCount)
		{
			System.Array.Copy(Constants.PACK_SIGNATURE, 0, headerBuffer, 0, 4);
			NB.EncodeInt32(headerBuffer, 4, version);
			NB.EncodeInt32(headerBuffer, 8, objectCount);
			Write(headerBuffer, 0, 12);
		}

		/// <summary>Write one object.</summary>
		/// <remarks>
		/// Write one object.
		/// If the object was already written, this method does nothing and returns
		/// quickly. This case occurs whenever an object was written out of order in
		/// order to ensure the delta base occurred before the object that needs it.
		/// </remarks>
		/// <param name="otp">the object to write.</param>
		/// <exception cref="System.IO.IOException">
		/// the object cannot be read from the object reader, or the
		/// output stream is no longer accepting output. Caller must
		/// examine the type of exception and possibly its message to
		/// distinguish between these cases.
		/// </exception>
		public void WriteObject(ObjectToPack otp)
		{
			packWriter.WriteObject(this, otp);
		}

		/// <summary>Commits the object header onto the stream.</summary>
		/// <remarks>
		/// Commits the object header onto the stream.
		/// <p>
		/// Once the header has been written, the object representation must be fully
		/// output, or packing must abort abnormally.
		/// </remarks>
		/// <param name="otp">the object to pack. Header information is obtained.</param>
		/// <param name="rawLength">
		/// number of bytes of the inflated content. For an object that is
		/// in whole object format, this is the same as the object size.
		/// For an object that is in a delta format, this is the size of
		/// the inflated delta instruction stream.
		/// </param>
		/// <exception cref="System.IO.IOException">the underlying stream refused to accept the header.
		/// 	</exception>
		public void WriteHeader(ObjectToPack otp, long rawLength)
		{
			if (otp.IsDeltaRepresentation())
			{
				if (packWriter.IsDeltaBaseAsOffset())
				{
					ObjectToPack baseInPack = otp.GetDeltaBase();
					if (baseInPack != null && baseInPack.IsWritten())
					{
						long start = count;
						int n = EncodeTypeSize(Constants.OBJ_OFS_DELTA, rawLength);
						Write(headerBuffer, 0, n);
						long offsetDiff = start - baseInPack.GetOffset();
						n = headerBuffer.Length - 1;
						headerBuffer[n] = unchecked((byte)(offsetDiff & unchecked((int)(0x7F))));
						while ((offsetDiff >>= 7) > 0)
						{
							headerBuffer[--n] = unchecked((byte)(unchecked((int)(0x80)) | (--offsetDiff & unchecked(
								(int)(0x7F)))));
						}
						Write(headerBuffer, n, headerBuffer.Length - n);
						return;
					}
				}
				int n_1 = EncodeTypeSize(Constants.OBJ_REF_DELTA, rawLength);
				otp.GetDeltaBaseId().CopyRawTo(headerBuffer, n_1);
				Write(headerBuffer, 0, n_1 + Constants.OBJECT_ID_LENGTH);
			}
			else
			{
				int n = EncodeTypeSize(otp.GetType(), rawLength);
				Write(headerBuffer, 0, n);
			}
		}

		private int EncodeTypeSize(int type, long rawLength)
		{
			long nextLength = (long)(((ulong)rawLength) >> 4);
			headerBuffer[0] = unchecked((byte)((nextLength > 0 ? unchecked((int)(0x80)) : unchecked(
				(int)(0x00))) | (type << 4) | (rawLength & unchecked((int)(0x0F)))));
			rawLength = nextLength;
			int n = 1;
			while (rawLength > 0)
			{
				nextLength = (long)(((ulong)nextLength) >> 7);
				headerBuffer[n++] = unchecked((byte)((nextLength > 0 ? unchecked((int)(0x80)) : unchecked(
					(int)(0x00))) | (rawLength & unchecked((int)(0x7F)))));
				rawLength = nextLength;
			}
			return n;
		}

		/// <returns>a temporary buffer writers can use to copy data with.</returns>
		public byte[] GetCopyBuffer()
		{
			if (copyBuffer == null)
			{
				copyBuffer = new byte[16 * 1024];
			}
			return copyBuffer;
		}

		internal void EndObject()
		{
			writeMonitor.Update(1);
		}

		/// <returns>total number of bytes written since stream start.</returns>
		internal long Length()
		{
			return count;
		}

		/// <returns>obtain the current CRC32 register.</returns>
		internal int GetCRC32()
		{
			return (int)crc.GetValue();
		}

		/// <summary>Reinitialize the CRC32 register for a new region.</summary>
		/// <remarks>Reinitialize the CRC32 register for a new region.</remarks>
		internal void ResetCRC32()
		{
			crc.Reset();
		}

		/// <returns>obtain the current SHA-1 digest.</returns>
		internal byte[] GetDigest()
		{
			return md.Digest();
		}
	}
}
