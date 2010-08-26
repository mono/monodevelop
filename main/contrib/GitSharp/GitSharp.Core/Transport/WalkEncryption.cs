/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.IO;
using System.Net;

namespace GitSharp.Core.Transport
{

    public abstract class WalkEncryption
    {
        public static WalkEncryption NONE = new NoEncryption();

        public const string JETS3T_CRYPTO_VER = "jets3t-crypto-ver";
        public const string JETS3T_CRYPTO_ALG = "jets3t-crypto-alg";

        public abstract Stream encrypt(Stream stream);
        public abstract Stream decrypt(Stream stream);

        public abstract void request(HttpWebRequest u, string prefix);
        public abstract void validate(HttpWebRequest u, string p);

        protected void validateImpl(HttpWebRequest u, string p, string version, string name)
        {
			if (u == null)
				throw new ArgumentNullException ("u");
        	if (version == null)
				throw new ArgumentNullException ("version");
        	if (name == null)
				throw new ArgumentNullException ("name");
			
            string v = u.Headers.Get(p + JETS3T_CRYPTO_VER) ?? string.Empty;
            if (!version.Equals(v))
                throw new IOException("Unsupported encryption version: " + v);

            v = u.Headers.Get(p + JETS3T_CRYPTO_ALG) ?? string.Empty;
            if (!name.Equals(v))
                throw new IOException("Unsupported encryption algorithm: " + v);
        }

        public IOException error(Exception why)
        {
			if (why == null)
				throw new ArgumentNullException ("why");
			
            IOException e;
            e = new IOException("Encryption error: " + why.Message, why);
            return e;
        }

        private class NoEncryption : WalkEncryption
        {
            public override void request(HttpWebRequest u, string prefix)
            {
            }

            public override void validate(HttpWebRequest u, string p)
            {
                validateImpl(u, p, string.Empty, string.Empty);
            }

            public override Stream decrypt(Stream stream)
            {
                return stream;
            }

            public override Stream encrypt(Stream stream)
            {
                return stream;
            }
        }

        // TODO: [caytchen] add ObjectEncryptionV2
    }

}