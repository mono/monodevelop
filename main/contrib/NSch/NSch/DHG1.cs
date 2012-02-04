/*
Copyright (c) 2006-2010 ymnk, JCraft,Inc. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

  1. Redistributions of source code must retain the above copyright notice,
     this list of conditions and the following disclaimer.

  2. Redistributions in binary form must reproduce the above copyright 
     notice, this list of conditions and the following disclaimer in 
     the documentation and/or other materials provided with the distribution.

  3. The names of the authors may not be used to endorse or promote products
     derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

This code is based on jsch (http://www.jcraft.com/jsch).
All credit should go to the authors of jsch.
*/

using System;
using NSch;
using Sharpen;

namespace NSch
{
	public class DHG1 : KeyExchange
	{
		internal static readonly byte[] g = new byte[] { 2 };

		internal static readonly byte[] p = new byte[] { unchecked((byte)unchecked((int)(
			0x00))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int
			)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int
			)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int
			)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int
			)(0xFF))), unchecked((byte)unchecked((int)(0xC9))), unchecked((byte)unchecked((int
			)(0x0F))), unchecked((byte)unchecked((int)(0xDA))), unchecked((byte)unchecked((int
			)(0xA2))), unchecked((byte)unchecked((int)(0x21))), unchecked((byte)unchecked((int
			)(0x68))), unchecked((byte)unchecked((int)(0xC2))), unchecked((byte)unchecked((int
			)(0x34))), unchecked((byte)unchecked((int)(0xC4))), unchecked((byte)unchecked((int
			)(0xC6))), unchecked((byte)unchecked((int)(0x62))), unchecked((byte)unchecked((int
			)(0x8B))), unchecked((byte)unchecked((int)(0x80))), unchecked((byte)unchecked((int
			)(0xDC))), unchecked((byte)unchecked((int)(0x1C))), unchecked((byte)unchecked((int
			)(0xD1))), unchecked((byte)unchecked((int)(0x29))), unchecked((byte)unchecked((int
			)(0x02))), unchecked((byte)unchecked((int)(0x4E))), unchecked((byte)unchecked((int
			)(0x08))), unchecked((byte)unchecked((int)(0x8A))), unchecked((byte)unchecked((int
			)(0x67))), unchecked((byte)unchecked((int)(0xCC))), unchecked((byte)unchecked((int
			)(0x74))), unchecked((byte)unchecked((int)(0x02))), unchecked((byte)unchecked((int
			)(0x0B))), unchecked((byte)unchecked((int)(0xBE))), unchecked((byte)unchecked((int
			)(0xA6))), unchecked((byte)unchecked((int)(0x3B))), unchecked((byte)unchecked((int
			)(0x13))), unchecked((byte)unchecked((int)(0x9B))), unchecked((byte)unchecked((int
			)(0x22))), unchecked((byte)unchecked((int)(0x51))), unchecked((byte)unchecked((int
			)(0x4A))), unchecked((byte)unchecked((int)(0x08))), unchecked((byte)unchecked((int
			)(0x79))), unchecked((byte)unchecked((int)(0x8E))), unchecked((byte)unchecked((int
			)(0x34))), unchecked((byte)unchecked((int)(0x04))), unchecked((byte)unchecked((int
			)(0xDD))), unchecked((byte)unchecked((int)(0xEF))), unchecked((byte)unchecked((int
			)(0x95))), unchecked((byte)unchecked((int)(0x19))), unchecked((byte)unchecked((int
			)(0xB3))), unchecked((byte)unchecked((int)(0xCD))), unchecked((byte)unchecked((int
			)(0x3A))), unchecked((byte)unchecked((int)(0x43))), unchecked((byte)unchecked((int
			)(0x1B))), unchecked((byte)unchecked((int)(0x30))), unchecked((byte)unchecked((int
			)(0x2B))), unchecked((byte)unchecked((int)(0x0A))), unchecked((byte)unchecked((int
			)(0x6D))), unchecked((byte)unchecked((int)(0xF2))), unchecked((byte)unchecked((int
			)(0x5F))), unchecked((byte)unchecked((int)(0x14))), unchecked((byte)unchecked((int
			)(0x37))), unchecked((byte)unchecked((int)(0x4F))), unchecked((byte)unchecked((int
			)(0xE1))), unchecked((byte)unchecked((int)(0x35))), unchecked((byte)unchecked((int
			)(0x6D))), unchecked((byte)unchecked((int)(0x6D))), unchecked((byte)unchecked((int
			)(0x51))), unchecked((byte)unchecked((int)(0xC2))), unchecked((byte)unchecked((int
			)(0x45))), unchecked((byte)unchecked((int)(0xE4))), unchecked((byte)unchecked((int
			)(0x85))), unchecked((byte)unchecked((int)(0xB5))), unchecked((byte)unchecked((int
			)(0x76))), unchecked((byte)unchecked((int)(0x62))), unchecked((byte)unchecked((int
			)(0x5E))), unchecked((byte)unchecked((int)(0x7E))), unchecked((byte)unchecked((int
			)(0xC6))), unchecked((byte)unchecked((int)(0xF4))), unchecked((byte)unchecked((int
			)(0x4C))), unchecked((byte)unchecked((int)(0x42))), unchecked((byte)unchecked((int
			)(0xE9))), unchecked((byte)unchecked((int)(0xA6))), unchecked((byte)unchecked((int
			)(0x37))), unchecked((byte)unchecked((int)(0xED))), unchecked((byte)unchecked((int
			)(0x6B))), unchecked((byte)unchecked((int)(0x0B))), unchecked((byte)unchecked((int
			)(0xFF))), unchecked((byte)unchecked((int)(0x5C))), unchecked((byte)unchecked((int
			)(0xB6))), unchecked((byte)unchecked((int)(0xF4))), unchecked((byte)unchecked((int
			)(0x06))), unchecked((byte)unchecked((int)(0xB7))), unchecked((byte)unchecked((int
			)(0xED))), unchecked((byte)unchecked((int)(0xEE))), unchecked((byte)unchecked((int
			)(0x38))), unchecked((byte)unchecked((int)(0x6B))), unchecked((byte)unchecked((int
			)(0xFB))), unchecked((byte)unchecked((int)(0x5A))), unchecked((byte)unchecked((int
			)(0x89))), unchecked((byte)unchecked((int)(0x9F))), unchecked((byte)unchecked((int
			)(0xA5))), unchecked((byte)unchecked((int)(0xAE))), unchecked((byte)unchecked((int
			)(0x9F))), unchecked((byte)unchecked((int)(0x24))), unchecked((byte)unchecked((int
			)(0x11))), unchecked((byte)unchecked((int)(0x7C))), unchecked((byte)unchecked((int
			)(0x4B))), unchecked((byte)unchecked((int)(0x1F))), unchecked((byte)unchecked((int
			)(0xE6))), unchecked((byte)unchecked((int)(0x49))), unchecked((byte)unchecked((int
			)(0x28))), unchecked((byte)unchecked((int)(0x66))), unchecked((byte)unchecked((int
			)(0x51))), unchecked((byte)unchecked((int)(0xEC))), unchecked((byte)unchecked((int
			)(0xE6))), unchecked((byte)unchecked((int)(0x53))), unchecked((byte)unchecked((int
			)(0x81))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int
			)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int
			)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int
			)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int
			)(0xFF))) };

		private const int SSH_MSG_KEXDH_INIT = 30;

		private const int SSH_MSG_KEXDH_REPLY = 31;

		internal const int RSA = 0;

		internal const int DSS = 1;

		private int type = 0;

		private int state;

		internal NSch.DH dh;

		internal byte[] V_S;

		internal byte[] V_C;

		internal byte[] I_S;

		internal byte[] I_C;

		internal byte[] e;

		private Buffer buf;

		private Packet packet;

		//  HASH sha;
		//  byte[] K;
		//  byte[] H;
		//  byte[] K_S;
		/// <exception cref="System.Exception"></exception>
		public override void Init(Session session, byte[] V_S, byte[] V_C, byte[] I_S, byte
			[] I_C)
		{
			this.session = session;
			this.V_S = V_S;
			this.V_C = V_C;
			this.I_S = I_S;
			this.I_C = I_C;
			//    sha=new SHA1();
			//    sha.init();
			try
			{
				Type c = Sharpen.Runtime.GetType(session.GetConfig("sha-1"));
				sha = (HASH)(System.Activator.CreateInstance(c));
				sha.Init();
			}
			catch (Exception ex)
			{
				System.Console.Error.WriteLine(ex);
			}
			buf = new Buffer();
			packet = new Packet(buf);
			try
			{
				Type c = Sharpen.Runtime.GetType(session.GetConfig("dh"));
				dh = (NSch.DH)(System.Activator.CreateInstance(c));
				dh.Init();
			}
			catch (Exception ex)
			{
				//System.err.println(e);
				throw;
			}
			dh.SetP(p);
			dh.SetG(g);
			// The client responds with:
			// byte  SSH_MSG_KEXDH_INIT(30)
			// mpint e <- g^x mod p
			//         x is a random number (1 < x < (p-1)/2)
			e = dh.GetE();
			packet.Reset();
			buf.PutByte(unchecked((byte)SSH_MSG_KEXDH_INIT));
			buf.PutMPInt(e);
			session.Write(packet);
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "SSH_MSG_KEXDH_INIT sent");
				JSch.GetLogger().Log(Logger.INFO, "expecting SSH_MSG_KEXDH_REPLY");
			}
			state = SSH_MSG_KEXDH_REPLY;
		}
		
		static byte[] CB (sbyte[] si)
		{
			byte[] s = new byte [si.Length];
			for (int n=0; n<si.Length; n++)
				s[n] = (byte)si[n];
			return s;
		}

		/// <exception cref="System.Exception"></exception>
		public override bool Next(Buffer _buf)
		{
			int i;
			int j;
			switch (state)
			{
				case SSH_MSG_KEXDH_REPLY:
				{
					// The server responds with:
					// byte      SSH_MSG_KEXDH_REPLY(31)
					// string    server public host key and certificates (K_S)
					// mpint     f
					// string    signature of H
					j = _buf.GetInt();
					j = _buf.GetByte();
					j = _buf.GetByte();
					if (j != 31)
					{
						System.Console.Error.WriteLine("type: must be 31 " + j);
						return false;
					}
					K_S = _buf.GetString();
					// K_S is server_key_blob, which includes ....
					// string ssh-dss
					// impint p of dsa
					// impint q of dsa
					// impint g of dsa
					// impint pub_key of dsa
					//System.err.print("K_S: "); //dump(K_S, 0, K_S.length);
					byte[] f = _buf.GetMPInt();
					byte[] sig_of_H = _buf.GetString();
					dh.SetF(f);
					K = dh.GetK();
					//The hash H is computed as the HASH hash of the concatenation of the
					//following:
					// string    V_C, the client's version string (CR and NL excluded)
					// string    V_S, the server's version string (CR and NL excluded)
					// string    I_C, the payload of the client's SSH_MSG_KEXINIT
					// string    I_S, the payload of the server's SSH_MSG_KEXINIT
					// string    K_S, the host key
					// mpint     e, exchange value sent by the client
					// mpint     f, exchange value sent by the server
					// mpint     K, the shared secret
					// This value is called the exchange hash, and it is used to authenti-
					// cate the key exchange.
					buf.Reset();
					buf.PutString(V_C);
					buf.PutString(V_S);
					buf.PutString(I_C);
					buf.PutString(I_S);
					buf.PutString(K_S);
					buf.PutMPInt(e);
					buf.PutMPInt(f);
					buf.PutMPInt(K);
					byte[] foo = new byte[buf.GetLength()];
					buf.GetByte(foo);
					sha.Update(foo, 0, foo.Length);
					H = sha.Digest();
					//System.err.print("H -> "); //dump(H, 0, H.length);
					i = 0;
					j = 0;
					j = ((K_S[i++] << 24) & unchecked((int)(0xff000000))) | ((K_S[i++] << 16) & unchecked(
						(int)(0x00ff0000))) | ((K_S[i++] << 8) & unchecked((int)(0x0000ff00))) | ((K_S[i
						++]) & unchecked((int)(0x000000ff)));
					string alg = Util.Byte2str(K_S, i, j);
					i += j;
					bool result = false;
					if (alg.Equals("ssh-rsa"))
					{
						byte[] tmp;
						byte[] ee;
						byte[] n;
						type = RSA;
						j = ((K_S[i++] << 24) & unchecked((int)(0xff000000))) | ((K_S[i++] << 16) & unchecked(
							(int)(0x00ff0000))) | ((K_S[i++] << 8) & unchecked((int)(0x0000ff00))) | ((K_S[i
							++]) & unchecked((int)(0x000000ff)));
						tmp = new byte[j];
						System.Array.Copy(K_S, i, tmp, 0, j);
						i += j;
						ee = tmp;
						j = ((K_S[i++] << 24) & unchecked((int)(0xff000000))) | ((K_S[i++] << 16) & unchecked(
							(int)(0x00ff0000))) | ((K_S[i++] << 8) & unchecked((int)(0x0000ff00))) | ((K_S[i
							++]) & unchecked((int)(0x000000ff)));
						tmp = new byte[j];
						System.Array.Copy(K_S, i, tmp, 0, j);
						i += j;
						n = tmp;
						//	SignatureRSA sig=new SignatureRSA();
						//	sig.init();
						NSch.SignatureRSA sig = null;
						try
						{
							Type c = Sharpen.Runtime.GetType(session.GetConfig("signature.rsa"));
							sig = (NSch.SignatureRSA)(System.Activator.CreateInstance(c));
							sig.Init();
						}
						catch (Exception ex)
						{
							System.Console.Error.WriteLine(ex);
						}
						sig.SetPubKey(ee, n);
						sig.Update(H);
						result = sig.Verify(sig_of_H);
						if (JSch.GetLogger().IsEnabled(Logger.INFO))
						{
							JSch.GetLogger().Log(Logger.INFO, "ssh_rsa_verify: signature " + result);
						}
					}
					else
					{
						if (alg.Equals("ssh-dss"))
						{
							byte[] q = null;
							byte[] tmp;
							byte[] p;
							byte[] g;
							type = DSS;
							j = ((K_S[i++] << 24) & unchecked((int)(0xff000000))) | ((K_S[i++] << 16) & unchecked(
								(int)(0x00ff0000))) | ((K_S[i++] << 8) & unchecked((int)(0x0000ff00))) | ((K_S[i
								++]) & unchecked((int)(0x000000ff)));
							tmp = new byte[j];
							System.Array.Copy(K_S, i, tmp, 0, j);
							i += j;
							p = tmp;
							j = ((K_S[i++] << 24) & unchecked((int)(0xff000000))) | ((K_S[i++] << 16) & unchecked(
								(int)(0x00ff0000))) | ((K_S[i++] << 8) & unchecked((int)(0x0000ff00))) | ((K_S[i
								++]) & unchecked((int)(0x000000ff)));
							tmp = new byte[j];
							System.Array.Copy(K_S, i, tmp, 0, j);
							i += j;
							q = tmp;
							j = ((K_S[i++] << 24) & unchecked((int)(0xff000000))) | ((K_S[i++] << 16) & unchecked(
								(int)(0x00ff0000))) | ((K_S[i++] << 8) & unchecked((int)(0x0000ff00))) | ((K_S[i
								++]) & unchecked((int)(0x000000ff)));
							tmp = new byte[j];
							System.Array.Copy(K_S, i, tmp, 0, j);
							i += j;
							g = tmp;
							j = ((K_S[i++] << 24) & unchecked((int)(0xff000000))) | ((K_S[i++] << 16) & unchecked(
								(int)(0x00ff0000))) | ((K_S[i++] << 8) & unchecked((int)(0x0000ff00))) | ((K_S[i
								++]) & unchecked((int)(0x000000ff)));
							tmp = new byte[j];
							System.Array.Copy(K_S, i, tmp, 0, j);
							i += j;
							f = tmp;
							//	SignatureDSA sig=new SignatureDSA();
							//	sig.init();
							NSch.SignatureDSA sig = null;
							try
							{
								Type c = Sharpen.Runtime.GetType(session.GetConfig("signature.dss"));
								sig = (NSch.SignatureDSA)(System.Activator.CreateInstance(c));
								sig.Init();
							}
							catch (Exception ex)
							{
								System.Console.Error.WriteLine(ex);
							}
							sig.SetPubKey(f, p, q, g);
							sig.Update(H);
							result = sig.Verify(sig_of_H);
							if (JSch.GetLogger().IsEnabled(Logger.INFO))
							{
								JSch.GetLogger().Log(Logger.INFO, "ssh_dss_verify: signature " + result);
							}
						}
						else
						{
							System.Console.Error.WriteLine("unknown alg");
						}
					}
					state = STATE_END;
					return result;
				}
			}
			return false;
		}

		public override string GetKeyType()
		{
			if (type == DSS)
			{
				return "DSA";
			}
			return "RSA";
		}

		public override int GetState()
		{
			return state;
		}
	}
}
