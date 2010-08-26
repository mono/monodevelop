/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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

using System;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System.IO;

namespace GitSharp.Core
{
	/// <summary>
	/// A window for accessing git packs using a <see cref="Stream"/> for storage.
	/// </summary>
    internal class ByteBufferWindow : ByteWindow, IDisposable
    {
        private readonly Stream _stream;

        internal ByteBufferWindow(PackFile pack, long o, Stream b)
            : base(pack, o, b.Length)
        {
            _stream = b;
        }

		protected override int copy(int pos, byte[] dstbuf, int dstoff, int cnt)
        {
            _stream.Position = pos;
			cnt = (int)Math.Min(_stream.Length - pos, cnt);
            _stream.Read(dstbuf, dstoff, cnt);
			return cnt;
        }

    	protected override int Inflate(int pos, byte[] dstbuf, int dstoff, Inflater inf)
        {
            var tmp = new byte[512];
            var s = _stream;
            s.Position=pos;
            while ((s.Length-s.Position) > 0 && !inf.IsFinished)
            {
                if (inf.IsNeedingInput)
                {
                    var n = (int)Math.Min((s.Length - s.Position), tmp.Length);
                    s.Read(tmp, 0, n);
                    inf.SetInput(tmp, 0, n);
                }
                dstoff += inf.Inflate(dstbuf, dstoff, dstbuf.Length - dstoff);
            }
            while (!inf.IsFinished && !inf.IsNeedingInput)
                dstoff += inf.Inflate(dstbuf, dstoff, dstbuf.Length - dstoff);
            return dstoff;
        }

        protected override void inflateVerify(int pos, Inflater inf)
        {
            var tmp = new byte[512];
            var s = _stream;
            s.Position = pos;
            while ((s.Length - s.Position) > 0 && !inf.IsFinished)
            {
                if (inf.IsNeedingInput)
                {
                    var n = (int)Math.Min((s.Length - s.Position), tmp.Length);
                    s.Read(tmp, 0, n);
                    inf.SetInput(tmp, 0, n);
                }
                inf.Inflate(VerifyGarbageBuffer, 0, VerifyGarbageBuffer.Length);
            }
            while (!inf.IsFinished && !inf.IsNeedingInput)
                inf.Inflate(VerifyGarbageBuffer, 0, VerifyGarbageBuffer.Length);
        }
		
		public override void Dispose ()
		{
            base.Dispose();
			_stream.Dispose();
		}
		
    }
}
