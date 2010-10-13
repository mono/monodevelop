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
	public class HostKey
	{
		private static readonly byte[] sshdss = Util.Str2byte("ssh-dss");

		private static readonly byte[] sshrsa = Util.Str2byte("ssh-rsa");

		protected internal const int GUESS = 0;

		public const int SSHDSS = 1;

		public const int SSHRSA = 2;

		internal const int UNKNOWN = 3;

		protected internal string host;

		protected internal int type;

		protected internal byte[] key;

		/// <exception cref="NSch.JSchException"></exception>
		public HostKey(string host, byte[] key) : this(host, GUESS, key)
		{
		}

		/// <exception cref="NSch.JSchException"></exception>
		public HostKey(string host, int type, byte[] key)
		{
			this.host = host;
			if (type == GUESS)
			{
				if (key[8] == 'd')
				{
					this.type = SSHDSS;
				}
				else
				{
					if (key[8] == 'r')
					{
						this.type = SSHRSA;
					}
					else
					{
						throw new JSchException("invalid key type");
					}
				}
			}
			else
			{
				this.type = type;
			}
			this.key = key;
		}

		public virtual string GetHost()
		{
			return host;
		}

		public virtual string GetType()
		{
			if (type == SSHDSS)
			{
				return Util.Byte2str(sshdss);
			}
			if (type == SSHRSA)
			{
				return Util.Byte2str(sshrsa);
			}
			return "UNKNOWN";
		}

		public virtual string GetKey()
		{
			return Util.Byte2str(Util.ToBase64(key, 0, key.Length));
		}

		public virtual string GetFingerPrint(JSch jsch)
		{
			HASH hash = null;
			try
			{
				Type c = Sharpen.Runtime.GetType(JSch.GetConfig("md5"));
				hash = (HASH)(System.Activator.CreateInstance(c));
			}
			catch (Exception e)
			{
				System.Console.Error.WriteLine("getFingerPrint: " + e);
			}
			return Util.GetFingerPrint(hash, key);
		}

		internal virtual bool IsMatched(string _host)
		{
			return IsIncluded(_host);
		}

		private bool IsIncluded(string _host)
		{
			int i = 0;
			string hosts = this.host;
			int hostslen = hosts.Length;
			int hostlen = _host.Length;
			int j;
			while (i < hostslen)
			{
				j = hosts.IndexOf(',', i);
				if (j == -1)
				{
					if (hostlen != hostslen - i)
					{
						return false;
					}
					return hosts.RegionMatches(true, i, _host, 0, hostlen);
				}
				if (hostlen == (j - i))
				{
					if (hosts.RegionMatches(true, i, _host, 0, hostlen))
					{
						return true;
					}
				}
				i = j + 1;
			}
			return false;
		}
	}
}
