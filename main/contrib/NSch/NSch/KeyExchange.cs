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
	public abstract class KeyExchange
	{
		internal const int PROPOSAL_KEX_ALGS = 0;

		internal const int PROPOSAL_SERVER_HOST_KEY_ALGS = 1;

		internal const int PROPOSAL_ENC_ALGS_CTOS = 2;

		internal const int PROPOSAL_ENC_ALGS_STOC = 3;

		internal const int PROPOSAL_MAC_ALGS_CTOS = 4;

		internal const int PROPOSAL_MAC_ALGS_STOC = 5;

		internal const int PROPOSAL_COMP_ALGS_CTOS = 6;

		internal const int PROPOSAL_COMP_ALGS_STOC = 7;

		internal const int PROPOSAL_LANG_CTOS = 8;

		internal const int PROPOSAL_LANG_STOC = 9;

		internal const int PROPOSAL_MAX = 10;

		internal static string kex = "diffie-hellman-group1-sha1";

		internal static string server_host_key = "ssh-rsa,ssh-dss";

		internal static string enc_c2s = "blowfish-cbc";

		internal static string enc_s2c = "blowfish-cbc";

		internal static string mac_c2s = "hmac-md5";

		internal static string mac_s2c = "hmac-md5";

		internal static string lang_c2s = string.Empty;

		internal static string lang_s2c = string.Empty;

		public const int STATE_END = 0;

		protected internal Session session = null;

		protected internal HASH sha = null;

		protected internal byte[] K = null;

		protected internal byte[] H = null;

		protected internal byte[] K_S = null;

		//static String kex_algs="diffie-hellman-group-exchange-sha1"+
		//                       ",diffie-hellman-group1-sha1";
		//static String kex="diffie-hellman-group-exchange-sha1";
		// hmac-md5,hmac-sha1,hmac-ripemd160,
		// hmac-sha1-96,hmac-md5-96
		//static String comp_c2s="none";        // zlib
		//static String comp_s2c="none";
		/// <exception cref="System.Exception"></exception>
		public abstract void Init(Session session, byte[] V_S, byte[] V_C, byte[] I_S, byte
			[] I_C);

		/// <exception cref="System.Exception"></exception>
		public abstract bool Next(Buffer buf);

		public abstract string GetKeyType();

		public abstract int GetState();

		protected internal static string[] Guess(byte[] I_S, byte[] I_C)
		{
			string[] guess = new string[PROPOSAL_MAX];
			Buffer sb = new Buffer(I_S);
			sb.SetOffSet(17);
			Buffer cb = new Buffer(I_C);
			cb.SetOffSet(17);
			for (int i = 0; i < PROPOSAL_MAX; i++)
			{
				byte[] sp = sb.GetString();
				// server proposal
				byte[] cp = cb.GetString();
				// client proposal
				int j = 0;
				int k = 0;
				while (j < cp.Length)
				{
					while (j < cp.Length && cp[j] != ',')
					{
						j++;
					}
					if (k == j)
					{
						return null;
					}
					string algorithm = Util.Byte2str(cp, k, j - k);
					int l = 0;
					int m = 0;
					while (l < sp.Length)
					{
						while (l < sp.Length && sp[l] != ',')
						{
							l++;
						}
						if (m == l)
						{
							return null;
						}
						if (algorithm.Equals(Util.Byte2str(sp, m, l - m)))
						{
							guess[i] = algorithm;
							goto loop_break;
						}
						l++;
						m = l;
					}
					j++;
					k = j;
loop_continue: ;
				}
loop_break: ;
				if (j == 0)
				{
					guess[i] = string.Empty;
				}
				else
				{
					if (guess[i] == null)
					{
						return null;
					}
				}
			}
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "kex: server->client" + " " + guess[PROPOSAL_ENC_ALGS_STOC
					] + " " + guess[PROPOSAL_MAC_ALGS_STOC] + " " + guess[PROPOSAL_COMP_ALGS_STOC]);
				JSch.GetLogger().Log(Logger.INFO, "kex: client->server" + " " + guess[PROPOSAL_ENC_ALGS_CTOS
					] + " " + guess[PROPOSAL_MAC_ALGS_CTOS] + " " + guess[PROPOSAL_COMP_ALGS_CTOS]);
			}
			//    for(int i=0; i<PROPOSAL_MAX; i++){
			//      System.err.println("guess: ["+guess[i]+"]");
			//    }
			return guess;
		}

		public virtual string GetFingerPrint()
		{
			HASH hash = null;
			try
			{
				Type c = Sharpen.Runtime.GetType(session.GetConfig("md5"));
				hash = (HASH)(System.Activator.CreateInstance(c));
			}
			catch (Exception e)
			{
				System.Console.Error.WriteLine("getFingerPrint: " + e);
			}
			return Util.GetFingerPrint(hash, GetHostKey());
		}

		internal virtual byte[] GetK()
		{
			return K;
		}

		internal virtual byte[] GetH()
		{
			return H;
		}

		internal virtual HASH GetHash()
		{
			return sha;
		}

		internal virtual byte[] GetHostKey()
		{
			return K_S;
		}
	}
}
