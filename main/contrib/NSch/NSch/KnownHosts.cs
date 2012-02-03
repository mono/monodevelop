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
using System.Collections;
using System.IO;
using System.Text;
using NSch;
using Sharpen;

namespace NSch
{
	public class KnownHosts : HostKeyRepository
	{
		private static readonly string _known_hosts = "known_hosts";

		private JSch jsch = null;

		private string known_hosts = null;

		private ArrayList pool = null;

		private MAC hmacsha1 = null;

		internal KnownHosts(JSch jsch) : base()
		{
			this.jsch = jsch;
			pool = new ArrayList();
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal virtual void SetKnownHosts(string foo)
		{
			try
			{
				known_hosts = foo;
				FileInputStream fis = new FileInputStream(foo);
				SetKnownHosts(fis);
			}
			catch (FileNotFoundException)
			{
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal virtual void SetKnownHosts(InputStream foo)
		{
			pool.Clear();
			StringBuilder sb = new StringBuilder();
			byte i;
			int j;
			bool error = false;
			try
			{
				InputStream fis = foo;
				string host;
				string key = null;
				int type;
				byte[] buf = new byte[1024];
				int bufl = 0;
				while (true)
				{
					bufl = 0;
					while (true)
					{
						j = fis.Read();
						if (j == -1)
						{
							if (bufl == 0)
							{
								goto loop_break;
							}
							else
							{
								break;
							}
						}
						if (j == unchecked((int)(0x0d)))
						{
							continue;
						}
						if (j == unchecked((int)(0x0a)))
						{
							break;
						}
						if (buf.Length <= bufl)
						{
							if (bufl > 1024 * 10)
							{
								break;
							}
							// too long...
							byte[] newbuf = new byte[buf.Length * 2];
							System.Array.Copy(buf, 0, newbuf, 0, buf.Length);
							buf = newbuf;
						}
						buf[bufl++] = unchecked((byte)j);
					}
					j = 0;
					while (j < bufl)
					{
						i = buf[j];
						if (i == ' ' || i == '\t')
						{
							j++;
							continue;
						}
						if (i == '#')
						{
							AddInvalidLine(Util.Byte2str(buf, 0, bufl));
							goto loop_continue;
						}
						break;
					}
					if (j >= bufl)
					{
						AddInvalidLine(Util.Byte2str(buf, 0, bufl));
						goto loop_continue;
					}
					sb.Length = 0;
					while (j < bufl)
					{
						i = buf[j++];
						if (i == unchecked((int)(0x20)) || i == '\t')
						{
							break;
						}
						sb.Append((char)i);
					}
					host = sb.ToString();
					if (j >= bufl || host.Length == 0)
					{
						AddInvalidLine(Util.Byte2str(buf, 0, bufl));
						goto loop_continue;
					}
					sb.Length = 0;
					type = -1;
					while (j < bufl)
					{
						i = buf[j++];
						if (i == unchecked((int)(0x20)) || i == '\t')
						{
							break;
						}
						sb.Append((char)i);
					}
					if (sb.ToString().Equals("ssh-dss"))
					{
						type = HostKey.SSHDSS;
					}
					else
					{
						if (sb.ToString().Equals("ssh-rsa"))
						{
							type = HostKey.SSHRSA;
						}
						else
						{
							j = bufl;
						}
					}
					if (j >= bufl)
					{
						AddInvalidLine(Util.Byte2str(buf, 0, bufl));
						goto loop_continue;
					}
					sb.Length = 0;
					while (j < bufl)
					{
						i = buf[j++];
						if (i == unchecked((int)(0x0d)))
						{
							continue;
						}
						if (i == unchecked((int)(0x0a)))
						{
							break;
						}
						sb.Append((char)i);
					}
					key = sb.ToString();
					if (key.Length == 0)
					{
						AddInvalidLine(Util.Byte2str(buf, 0, bufl));
						goto loop_continue;
					}
					//System.err.println(host);
					//System.err.println("|"+key+"|");
					HostKey hk = null;
					hk = new KnownHosts.HashedHostKey(this, host, type, Util.FromBase64(Util.Str2byte
						(key), 0, key.Length));
					pool.Add(hk);
loop_continue: ;
				}
loop_break: ;
				fis.Close();
				if (error)
				{
					throw new JSchException("KnownHosts: invalid format");
				}
			}
			catch (Exception e)
			{
				if (e is JSchException)
				{
					throw (JSchException)e;
				}
				if (e is Exception)
				{
					throw new JSchException(e.ToString(), (Exception)e);
				}
				throw new JSchException(e.ToString());
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		private void AddInvalidLine(string line)
		{
			HostKey hk = new HostKey(line, HostKey.UNKNOWN, null);
			pool.Add(hk);
		}

		internal virtual string GetKnownHostsFile()
		{
			return known_hosts;
		}

		public override string GetKnownHostsRepositoryID()
		{
			return known_hosts;
		}

		public override int Check(string host, byte[] key)
		{
			int result = NOT_INCLUDED;
			if (host == null)
			{
				return result;
			}
			int type = GetType(key);
			HostKey hk;
			lock (pool)
			{
				for (int i = 0; i < pool.Count; i++)
				{
					hk = (HostKey)(pool[i]);
					if (hk.IsMatched(host) && hk.type == type)
					{
						if (Util.Array_equals(hk.key, key))
						{
							return OK;
						}
						else
						{
							result = CHANGED;
						}
					}
				}
			}
			if (result == NOT_INCLUDED && host.StartsWith("[") && host.IndexOf("]:") > 1)
			{
				return Check(Sharpen.Runtime.Substring(host, 1, host.IndexOf("]:")), key);
			}
			return result;
		}

		public override void Add(HostKey hostkey, UserInfo userinfo)
		{
			int type = hostkey.type;
			string host = hostkey.GetHost();
			byte[] key = hostkey.key;
			HostKey hk = null;
			lock (pool)
			{
				for (int i = 0; i < pool.Count; i++)
				{
					hk = (HostKey)(pool[i]);
					if (hk.IsMatched(host) && hk.type == type)
					{
					}
				}
			}
			hk = hostkey;
			pool.Add(hk);
			string bar = GetKnownHostsRepositoryID();
			if (bar != null)
			{
				bool foo = true;
				FilePath goo = new FilePath(bar);
				if (!goo.Exists())
				{
					foo = false;
					if (userinfo != null)
					{
						foo = userinfo.PromptYesNo(bar + " does not exist.\n" + "Are you sure you want to create it?"
							);
						goo = goo.GetParentFile();
						if (foo && goo != null && !goo.Exists())
						{
							foo = userinfo.PromptYesNo("The parent directory " + goo + " does not exist.\n" +
								 "Are you sure you want to create it?");
							if (foo)
							{
								if (!goo.Mkdirs())
								{
									userinfo.ShowMessage(goo + " has not been created.");
									foo = false;
								}
								else
								{
									userinfo.ShowMessage(goo + " has been succesfully created.\nPlease check its access permission."
										);
								}
							}
						}
						if (goo == null)
						{
							foo = false;
						}
					}
				}
				if (foo)
				{
					try
					{
						Sync(bar);
					}
					catch (Exception e)
					{
						System.Console.Error.WriteLine("sync known_hosts: " + e);
					}
				}
			}
		}

		public override HostKey[] GetHostKey()
		{
			return GetHostKey(null, null);
		}

		public override HostKey[] GetHostKey(string host, string type)
		{
			lock (pool)
			{
				int count = 0;
				for (int i = 0; i < pool.Count; i++)
				{
					HostKey hk = (HostKey)pool[i];
					if (hk.type == HostKey.UNKNOWN)
					{
						continue;
					}
					if (host == null || (hk.IsMatched(host) && (type == null || hk.GetType().Equals(type
						))))
					{
						count++;
					}
				}
				if (count == 0)
				{
					return null;
				}
				HostKey[] foo = new HostKey[count];
				int j = 0;
				for (int i_1 = 0; i_1 < pool.Count; i_1++)
				{
					HostKey hk = (HostKey)pool[i_1];
					if (hk.type == HostKey.UNKNOWN)
					{
						continue;
					}
					if (host == null || (hk.IsMatched(host) && (type == null || hk.GetType().Equals(type
						))))
					{
						foo[j++] = hk;
					}
				}
				return foo;
			}
		}

		public override void Remove(string host, string type)
		{
			Remove(host, type, null);
		}

		public override void Remove(string host, string type, byte[] key)
		{
			bool sync = false;
			lock (pool)
			{
				for (int i = 0; i < pool.Count; i++)
				{
					HostKey hk = (HostKey)(pool[i]);
					if (host == null || (hk.IsMatched(host) && (type == null || (hk.GetType().Equals(
						type) && (key == null || Util.Array_equals(key, hk.key))))))
					{
						string hosts = hk.GetHost();
						if (hosts.Equals(host) || ((hk is KnownHosts.HashedHostKey) && ((KnownHosts.HashedHostKey
							)hk).IsHashed()))
						{
							pool.RemoveElement(hk);
						}
						else
						{
							hk.host = DeleteSubString(hosts, host);
						}
						sync = true;
					}
				}
			}
			if (sync)
			{
				try
				{
					Sync();
				}
				catch (Exception)
				{
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void Sync()
		{
			if (known_hosts != null)
			{
				Sync(known_hosts);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void Sync(string foo)
		{
			lock (this)
			{
				if (foo == null)
				{
					return;
				}
				FileOutputStream fos = new FileOutputStream(foo);
				Dump(fos);
				fos.Close();
			}
		}

		private static readonly byte[] space = new byte[] { unchecked((byte)unchecked((int
			)(0x20))) };

		private static readonly byte[] cr = Util.Str2byte("\n");

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Dump(OutputStream @out)
		{
			try
			{
				HostKey hk;
				lock (pool)
				{
					for (int i = 0; i < pool.Count; i++)
					{
						hk = (HostKey)(pool[i]);
						//hk.dump(out);
						string host = hk.GetHost();
						string type = hk.GetType();
						if (type.Equals("UNKNOWN"))
						{
							@out.Write(Util.Str2byte(host));
							@out.Write(cr);
							continue;
						}
						@out.Write(Util.Str2byte(host));
						@out.Write(space);
						@out.Write(Util.Str2byte(type));
						@out.Write(space);
						@out.Write(Util.Str2byte(hk.GetKey()));
						@out.Write(cr);
					}
				}
			}
			catch (Exception e)
			{
				System.Console.Error.WriteLine(e);
			}
		}

		private int GetType(byte[] key)
		{
			if (key[8] == 'd')
			{
				return HostKey.SSHDSS;
			}
			if (key[8] == 'r')
			{
				return HostKey.SSHRSA;
			}
			return HostKey.UNKNOWN;
		}

		private string DeleteSubString(string hosts, string host)
		{
			int i = 0;
			int hostlen = host.Length;
			int hostslen = hosts.Length;
			int j;
			while (i < hostslen)
			{
				j = hosts.IndexOf(',', i);
				if (j == -1)
				{
					break;
				}
				if (!host.Equals(Sharpen.Runtime.Substring(hosts, i, j)))
				{
					i = j + 1;
					continue;
				}
				return Sharpen.Runtime.Substring(hosts, 0, i) + Sharpen.Runtime.Substring(hosts, 
					j + 1);
			}
			if (hosts.EndsWith(host) && hostslen - i == hostlen)
			{
				return Sharpen.Runtime.Substring(hosts, 0, (hostlen == hostslen) ? 0 : hostslen -
					 hostlen - 1);
			}
			return hosts;
		}

		private MAC GetHMACSHA1()
		{
			lock (this)
			{
				if (hmacsha1 == null)
				{
					try
					{
						Type c = Sharpen.Runtime.GetType(JSch.GetConfig("hmac-sha1"));
						hmacsha1 = (MAC)(System.Activator.CreateInstance(c));
					}
					catch (Exception e)
					{
						System.Console.Error.WriteLine("hmacsha1: " + e);
					}
				}
				return hmacsha1;
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal virtual HostKey CreateHashedHostKey(string host, byte[] key)
		{
			KnownHosts.HashedHostKey hhk = new KnownHosts.HashedHostKey(this, host, key);
			hhk.Hash();
			return hhk;
		}

		internal class HashedHostKey : HostKey
		{
			private static readonly string HASH_MAGIC = "|1|";

			private static readonly string HASH_DELIM = "|";

			private bool hashed = false;

			internal byte[] salt = null;

			internal byte[] hash = null;

			/// <exception cref="NSch.JSchException"></exception>
			public HashedHostKey(KnownHosts _enclosing, string host, byte[] key) : this(_enclosing, host, 
				HostKey.GUESS, key)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="NSch.JSchException"></exception>
			public HashedHostKey(KnownHosts _enclosing, string host, int type, byte[] key) : 
				base(host, type, key)
			{
				this._enclosing = _enclosing;
				if (this.host.StartsWith(KnownHosts.HashedHostKey.HASH_MAGIC) && Sharpen.Runtime.Substring
					(this.host, KnownHosts.HashedHostKey.HASH_MAGIC.Length).IndexOf(KnownHosts.HashedHostKey
					.HASH_DELIM) > 0)
				{
					string data = Sharpen.Runtime.Substring(this.host, KnownHosts.HashedHostKey.HASH_MAGIC
						.Length);
					string _salt = Sharpen.Runtime.Substring(data, 0, data.IndexOf(KnownHosts.HashedHostKey
						.HASH_DELIM));
					string _hash = Sharpen.Runtime.Substring(data, data.IndexOf(KnownHosts.HashedHostKey
						.HASH_DELIM) + 1);
					this.salt = Util.FromBase64(Util.Str2byte(_salt), 0, _salt.Length);
					this.hash = Util.FromBase64(Util.Str2byte(_hash), 0, _hash.Length);
					if (this.salt.Length != 20 || this.hash.Length != 20)
					{
						// block size of hmac-sha1
						this.salt = null;
						this.hash = null;
						return;
					}
					this.hashed = true;
				}
			}

			internal override bool IsMatched(string _host)
			{
				if (!this.hashed)
				{
					return base.IsMatched(_host);
				}
				MAC macsha1 = this._enclosing.GetHMACSHA1();
				try
				{
					lock (macsha1)
					{
						macsha1.Init(this.salt);
						byte[] foo = Util.Str2byte(_host);
						macsha1.Update(foo, 0, foo.Length);
						byte[] bar = new byte[macsha1.GetBlockSize()];
						macsha1.DoFinal(bar, 0);
						return Util.Array_equals(this.hash, bar);
					}
				}
				catch (Exception e)
				{
					System.Console.Out.WriteLine(e);
				}
				return false;
			}

			internal virtual bool IsHashed()
			{
				return this.hashed;
			}

			internal virtual void Hash()
			{
				if (this.hashed)
				{
					return;
				}
				MAC macsha1 = this._enclosing.GetHMACSHA1();
				if (this.salt == null)
				{
					Random random = Session.random;
					lock (random)
					{
						this.salt = new byte[macsha1.GetBlockSize()];
						random.Fill(this.salt, 0, this.salt.Length);
					}
				}
				try
				{
					lock (macsha1)
					{
						macsha1.Init(this.salt);
						byte[] foo = Util.Str2byte(this.host);
						macsha1.Update(foo, 0, foo.Length);
						this.hash = new byte[macsha1.GetBlockSize()];
						macsha1.DoFinal(this.hash, 0);
					}
				}
				catch (Exception)
				{
				}
				this.host = KnownHosts.HashedHostKey.HASH_MAGIC + Util.Byte2str(Util.ToBase64(this
					.salt, 0, this.salt.Length)) + KnownHosts.HashedHostKey.HASH_DELIM + Util.Byte2str
					(Util.ToBase64(this.hash, 0, this.hash.Length));
				this.hashed = true;
			}

			private readonly KnownHosts _enclosing;
		}
	}
}
