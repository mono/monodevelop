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
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	internal abstract class WalkEncryption
	{
		internal static readonly WalkEncryption NONE = new WalkEncryption.NoEncryption();

		internal static readonly string JETS3T_CRYPTO_VER = "jets3t-crypto-ver";

		internal static readonly string JETS3T_CRYPTO_ALG = "jets3t-crypto-alg";

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract OutputStream Encrypt(OutputStream os);

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract InputStream Decrypt(InputStream @in);

		internal abstract void Request(HttpURLConnection u, string prefix);

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract void Validate(HttpURLConnection u, string p);

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void ValidateImpl(HttpURLConnection u, string p, string
			 version, string name)
		{
			string v;
			v = u.GetHeaderField(p + JETS3T_CRYPTO_VER);
			if (v == null)
			{
				v = string.Empty;
			}
			if (!version.Equals(v))
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().unsupportedEncryptionVersion
					, v));
			}
			v = u.GetHeaderField(p + JETS3T_CRYPTO_ALG);
			if (v == null)
			{
				v = string.Empty;
			}
			if (!name.Equals(v))
			{
				throw new IOException(JGitText.Get().unsupportedEncryptionAlgorithm + v);
			}
		}

		internal virtual IOException Error(Exception why)
		{
			IOException e;
			e = new IOException(MessageFormat.Format(JGitText.Get().encryptionError, why.Message
				));
			Sharpen.Extensions.InitCause(e, why);
			return e;
		}

		private class NoEncryption : WalkEncryption
		{
			internal override void Request(HttpURLConnection u, string prefix)
			{
			}

			// Don't store any request properties.
			/// <exception cref="System.IO.IOException"></exception>
			internal override void Validate(HttpURLConnection u, string p)
			{
				ValidateImpl(u, p, string.Empty, string.Empty);
			}

			internal override InputStream Decrypt(InputStream @in)
			{
				return @in;
			}

			internal override OutputStream Encrypt(OutputStream os)
			{
				return os;
			}
		}
//
//		internal class ObjectEncryptionV2 : WalkEncryption
//		{
//			private static int ITERATION_COUNT = 5000;
//
//			private static byte[] salt = new byte[] { unchecked((byte)unchecked((int)(0xA4)))
//				, unchecked((byte)unchecked((int)(0x0B))), unchecked((byte)unchecked((int)(0xC8)
//				)), unchecked((byte)unchecked((int)(0x34))), unchecked((byte)unchecked((int)(0xD6
//				))), unchecked((byte)unchecked((int)(0x95))), unchecked((byte)unchecked((int)(0xF3
//				))), unchecked((byte)unchecked((int)(0x13))) };
//
//			private readonly string algorithmName;
//
//			private readonly SecretKey skey;
//
//			// FIXME: How should this be converted?
//			//private readonly PBEParameterSpec aspec;
//
//			/// <exception cref="Sharpen.InvalidKeySpecException"></exception>
//			/// <exception cref="Sharpen.NoSuchAlgorithmException"></exception>
//			internal ObjectEncryptionV2(string algo, string key)
//			{
//				algorithmName = algo;
//				PBEKeySpec s;
//				s = new PBEKeySpec(key.ToCharArray(), salt, ITERATION_COUNT, 32);
//				skey = SecretKeyFactory.GetInstance(algo).GenerateSecret(s);
//				aspec = new PBEParameterSpec(salt, ITERATION_COUNT);
//			}
//
//			internal override void Request(HttpURLConnection u, string prefix)
//			{
//				u.SetRequestProperty(prefix + JETS3T_CRYPTO_VER, "2");
//				u.SetRequestProperty(prefix + JETS3T_CRYPTO_ALG, algorithmName);
//			}
//
//			/// <exception cref="System.IO.IOException"></exception>
//			internal override void Validate(HttpURLConnection u, string p)
//			{
//				ValidateImpl(u, p, "2", algorithmName);
//			}
//
//			/// <exception cref="System.IO.IOException"></exception>
//			internal override OutputStream Encrypt(OutputStream os)
//			{
//				try
//				{
//					Sharpen.Cipher c = Sharpen.Cipher.GetInstance(algorithmName);
//					c.Init(Sharpen.Cipher.ENCRYPT_MODE, skey, aspec);
//					return new CipherOutputStream(os, c);
//				}
//				catch (NoSuchAlgorithmException e)
//				{
//					throw Error(e);
//				}
//				catch (NoSuchPaddingException e)
//				{
//					throw Error(e);
//				}
//				catch (InvalidKeyException e)
//				{
//					throw Error(e);
//				}
//				catch (InvalidAlgorithmParameterException e)
//				{
//					throw Error(e);
//				}
//			}
//
//			/// <exception cref="System.IO.IOException"></exception>
//			internal override InputStream Decrypt(InputStream @in)
//			{
//				try
//				{
//					Sharpen.Cipher c = Sharpen.Cipher.GetInstance(algorithmName);
//					c.Init(Sharpen.Cipher.DECRYPT_MODE, skey, aspec);
//					return new CipherInputStream(@in, c);
//				}
//				catch (NoSuchAlgorithmException e)
//				{
//					throw Error(e);
//				}
//				catch (NoSuchPaddingException e)
//				{
//					throw Error(e);
//				}
//				catch (InvalidKeyException e)
//				{
//					throw Error(e);
//				}
//				catch (InvalidAlgorithmParameterException e)
//				{
//					throw Error(e);
//				}
//			}
//		}
	}
}
