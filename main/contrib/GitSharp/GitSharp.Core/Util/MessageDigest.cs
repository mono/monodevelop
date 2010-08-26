/*
 * Copyright (C) 2009, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Security.Cryptography;
using System.IO;

namespace GitSharp.Core.Util
{
    public abstract class MessageDigest : IDisposable
    {
        public static MessageDigest getInstance(string algorithm)
        {
            switch (algorithm.ToLower())
            {
                case "sha-1":
                    return new MessageDigest<SHA1Managed>();
                case "md5":
                    return new MessageDigest<MD5CryptoServiceProvider>();
                default:
                    throw new NotSupportedException(string.Format("The requested algorithm \"{0}\" is not supported.", algorithm));
            }
        }

        public abstract byte[] Digest();
        public abstract byte[] Digest(byte[] input);
        public abstract void Reset();
        public abstract void Update(byte input);
        public abstract void Update(byte[] input);
        public abstract void Update(byte[] input, int index, int count);
        public abstract void Dispose();
    }

    public class MessageDigest<TAlgorithm> : MessageDigest where TAlgorithm : HashAlgorithm, new()
    {
        private CryptoStream _stream;
        private TAlgorithm _hash;

        public MessageDigest()
        {
            Init();
        }

        private void Init()
        {
            _hash = new TAlgorithm();
            _stream = new CryptoStream(Stream.Null, _hash, CryptoStreamMode.Write);
        }

        public override byte[] Digest()
        {
            _stream.FlushFinalBlock();
            var ret = _hash.Hash;
            Reset();
            return ret;
        }

        public override byte[] Digest(byte[] input)
        {
            using (var me = new MessageDigest<TAlgorithm>())
            {
                me.Update(input);
                return me.Digest();
            }
        }

        public override void Reset()
        {
            Dispose();
            Init();
        }

        public override void Update(byte input)
        {
            _stream.WriteByte(input);
        }

        public override void Update(byte[] input)
        {
            _stream.Write(input, 0, input.Length);
        }

        public override void Update(byte[] input, int index, int count)
        {
            _stream.Write(input, index, count);
        }

        public override void Dispose()
        {
            if (_stream != null)
                _stream.Dispose();
            _stream = null;
        }
    }
}
