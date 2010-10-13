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
using NSch;
using Sharpen;

namespace NSch
{
	public class JSch
	{
		internal static Hashtable config = new Hashtable();

		static JSch()
		{
			//  config.put("kex", "diffie-hellman-group-exchange-sha1");
			config.Put("kex", "diffie-hellman-group1-sha1,diffie-hellman-group-exchange-sha1"
				);
			config.Put("server_host_key", "ssh-rsa,ssh-dss");
			//    config.put("server_host_key", "ssh-dss,ssh-rsa");
			config.Put("cipher.s2c", "aes128-ctr,aes128-cbc,3des-ctr,3des-cbc,blowfish-cbc,aes192-cbc,aes256-cbc"
				);
			config.Put("cipher.c2s", "aes128-ctr,aes128-cbc,3des-ctr,3des-cbc,blowfish-cbc,aes192-cbc,aes256-cbc"
				);
			config.Put("mac.s2c", "hmac-md5,hmac-sha1,hmac-sha1-96,hmac-md5-96");
			config.Put("mac.c2s", "hmac-md5,hmac-sha1,hmac-sha1-96,hmac-md5-96");
			config.Put("compression.s2c", "none");
			// config.put("compression.s2c", "zlib@openssh.com,zlib,none");
			config.Put("compression.c2s", "none");
			// config.put("compression.c2s", "zlib@openssh.com,zlib,none");
			config.Put("lang.s2c", string.Empty);
			config.Put("lang.c2s", string.Empty);
			config.Put("compression_level", "6");
			config.Put("diffie-hellman-group-exchange-sha1", "com.jcraft.jsch.DHGEX");
			config.Put("diffie-hellman-group1-sha1", "com.jcraft.jsch.DHG1");
			config.Put("dh", "com.jcraft.jsch.jce.DH");
			config.Put("3des-cbc", "com.jcraft.jsch.jce.TripleDESCBC");
			config.Put("blowfish-cbc", "com.jcraft.jsch.jce.BlowfishCBC");
			config.Put("hmac-sha1", "com.jcraft.jsch.jce.HMACSHA1");
			config.Put("hmac-sha1-96", "com.jcraft.jsch.jce.HMACSHA196");
			config.Put("hmac-md5", "com.jcraft.jsch.jce.HMACMD5");
			config.Put("hmac-md5-96", "com.jcraft.jsch.jce.HMACMD596");
			config.Put("sha-1", "com.jcraft.jsch.jce.SHA1");
			config.Put("md5", "com.jcraft.jsch.jce.MD5");
			config.Put("signature.dss", "com.jcraft.jsch.jce.SignatureDSA");
			config.Put("signature.rsa", "com.jcraft.jsch.jce.SignatureRSA");
			config.Put("keypairgen.dsa", "com.jcraft.jsch.jce.KeyPairGenDSA");
			config.Put("keypairgen.rsa", "com.jcraft.jsch.jce.KeyPairGenRSA");
			config.Put("random", "com.jcraft.jsch.jce.Random");
			config.Put("none", "com.jcraft.jsch.CipherNone");
			config.Put("aes128-cbc", "com.jcraft.jsch.jce.AES128CBC");
			config.Put("aes192-cbc", "com.jcraft.jsch.jce.AES192CBC");
			config.Put("aes256-cbc", "com.jcraft.jsch.jce.AES256CBC");
			config.Put("aes128-ctr", "com.jcraft.jsch.jce.AES128CTR");
			config.Put("aes192-ctr", "com.jcraft.jsch.jce.AES192CTR");
			config.Put("aes256-ctr", "com.jcraft.jsch.jce.AES256CTR");
			config.Put("3des-ctr", "com.jcraft.jsch.jce.TripleDESCTR");
			config.Put("arcfour", "com.jcraft.jsch.jce.ARCFOUR");
			config.Put("arcfour128", "com.jcraft.jsch.jce.ARCFOUR128");
			config.Put("arcfour256", "com.jcraft.jsch.jce.ARCFOUR256");
			config.Put("userauth.none", "com.jcraft.jsch.UserAuthNone");
			config.Put("userauth.password", "com.jcraft.jsch.UserAuthPassword");
			config.Put("userauth.keyboard-interactive", "com.jcraft.jsch.UserAuthKeyboardInteractive"
				);
			config.Put("userauth.publickey", "com.jcraft.jsch.UserAuthPublicKey");
			config.Put("userauth.gssapi-with-mic", "com.jcraft.jsch.UserAuthGSSAPIWithMIC");
			config.Put("gssapi-with-mic.krb5", "com.jcraft.jsch.jgss.GSSContextKrb5");
			config.Put("zlib", "com.jcraft.jsch.jcraft.Compression");
			config.Put("zlib@openssh.com", "com.jcraft.jsch.jcraft.Compression");
			config.Put("StrictHostKeyChecking", "ask");
			config.Put("HashKnownHosts", "no");
			//config.put("HashKnownHosts",  "yes");
			config.Put("PreferredAuthentications", "gssapi-with-mic,publickey,keyboard-interactive,password"
				);
			config.Put("CheckCiphers", "aes256-ctr,aes192-ctr,aes128-ctr,aes256-cbc,aes192-cbc,aes128-cbc,3des-ctr,arcfour,arcfour128,arcfour256"
				);
		}

		internal ArrayList pool = new ArrayList();

		internal ArrayList identities = new ArrayList();

		private HostKeyRepository known_hosts = null;

		private sealed class _Logger_115 : Logger
		{
			public _Logger_115()
			{
			}

			public override bool IsEnabled(int level)
			{
				return false;
			}

			public override void Log(int level, string message)
			{
			}
		}

		private static readonly Logger DEVNULL = new _Logger_115();

		internal static Logger logger = DEVNULL;

		public JSch()
		{
			try
			{
				string osname = (string)(Runtime.GetProperties()["os.name"]);
				if (osname != null && osname.Equals("Mac OS X"))
				{
					config.Put("hmac-sha1", "com.jcraft.jsch.jcraft.HMACSHA1");
					config.Put("hmac-md5", "com.jcraft.jsch.jcraft.HMACMD5");
					config.Put("hmac-md5-96", "com.jcraft.jsch.jcraft.HMACMD596");
					config.Put("hmac-sha1-96", "com.jcraft.jsch.jcraft.HMACSHA196");
				}
			}
			catch (Exception)
			{
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual Session GetSession(string username, string host)
		{
			return GetSession(username, host, 22);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual Session GetSession(string username, string host, int port)
		{
			if (username == null)
			{
				throw new JSchException("username must not be null.");
			}
			if (host == null)
			{
				throw new JSchException("host must not be null.");
			}
			Session s = new Session(this);
			s.SetUserName(username);
			s.SetHost(host);
			s.SetPort(port);
			//pool.addElement(s);
			return s;
		}

		protected internal virtual void AddSession(Session session)
		{
			lock (pool)
			{
				pool.Add(session);
			}
		}

		protected internal virtual bool RemoveSession(Session session)
		{
			lock (pool)
			{
				return pool.RemoveElement(session);
			}
		}

		public virtual void SetHostKeyRepository(HostKeyRepository hkrepo)
		{
			known_hosts = hkrepo;
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetKnownHosts(string filename)
		{
			if (known_hosts == null)
			{
				known_hosts = new KnownHosts(this);
			}
			if (known_hosts is KnownHosts)
			{
				lock (known_hosts)
				{
					((KnownHosts)known_hosts).SetKnownHosts(filename);
				}
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetKnownHosts(InputStream stream)
		{
			if (known_hosts == null)
			{
				known_hosts = new KnownHosts(this);
			}
			if (known_hosts is KnownHosts)
			{
				lock (known_hosts)
				{
					((KnownHosts)known_hosts).SetKnownHosts(stream);
				}
			}
		}

		public virtual HostKeyRepository GetHostKeyRepository()
		{
			if (known_hosts == null)
			{
				known_hosts = new KnownHosts(this);
			}
			return known_hosts;
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void AddIdentity(string prvkey)
		{
			AddIdentity(prvkey, (byte[])null);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void AddIdentity(string prvkey, string passphrase)
		{
			byte[] _passphrase = null;
			if (passphrase != null)
			{
				_passphrase = Util.Str2byte(passphrase);
			}
			AddIdentity(prvkey, _passphrase);
			if (_passphrase != null)
			{
				Util.Bzero(_passphrase);
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void AddIdentity(string prvkey, byte[] passphrase)
		{
			Identity identity = IdentityFile.NewInstance(prvkey, null, this);
			AddIdentity(identity, passphrase);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void AddIdentity(string prvkey, string pubkey, byte[] passphrase)
		{
			Identity identity = IdentityFile.NewInstance(prvkey, pubkey, this);
			AddIdentity(identity, passphrase);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void AddIdentity(string name, byte[] prvkey, byte[] pubkey, byte[]
			 passphrase)
		{
			Identity identity = IdentityFile.NewInstance(name, prvkey, pubkey, this);
			AddIdentity(identity, passphrase);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void AddIdentity(Identity identity, byte[] passphrase)
		{
			if (passphrase != null)
			{
				try
				{
					byte[] goo = new byte[passphrase.Length];
					System.Array.Copy(passphrase, 0, goo, 0, passphrase.Length);
					passphrase = goo;
					identity.SetPassphrase(passphrase);
				}
				finally
				{
					Util.Bzero(passphrase);
				}
			}
			lock (identities)
			{
				if (!identities.Contains(identity))
				{
					identities.Add(identity);
				}
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void RemoveIdentity(string name)
		{
			lock (identities)
			{
				for (int i = 0; i < identities.Count; i++)
				{
					Identity identity = (Identity)(identities[i]);
					if (!identity.GetName().Equals(name))
					{
						continue;
					}
					identities.RemoveElement(identity);
					identity.Clear();
					break;
				}
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual ArrayList GetIdentityNames()
		{
			ArrayList foo = new ArrayList();
			lock (identities)
			{
				for (int i = 0; i < identities.Count; i++)
				{
					Identity identity = (Identity)(identities[i]);
					foo.Add(identity.GetName());
				}
			}
			return foo;
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void RemoveAllIdentity()
		{
			lock (identities)
			{
				ArrayList foo = GetIdentityNames();
				for (int i = 0; i < foo.Count; i++)
				{
					string name = ((string)foo[i]);
					RemoveIdentity(name);
				}
			}
		}

		public static string GetConfig(string key)
		{
			lock (config)
			{
				string s = (string)(config[key]);
				s = s.Replace ("com.jcraft.jsch.jce","NSch.Jce");
				s = s.Replace ("com.jcraft.jsch.jcraft","NSch.Jcraft");
				s = s.Replace ("com.jcraft.jsch.jgss","NSch.Jgss");
				s = s.Replace ("com.jcraft.jsch","NSch");
				return s;
			}
		}

		public static void SetConfig(Hashtable newconf)
		{
			lock (config)
			{
				for (IEnumerator e = newconf.Keys.GetEnumerator (); e.MoveNext(); )
				{
					string key = (string)(e.Current);
					config.Put(key, (string)(newconf[key]));
				}
			}
		}

		public static void SetConfig(string key, string value)
		{
			config.Put(key, value);
		}

		public static void SetLogger(Logger logger)
		{
			if (logger == null)
			{
				NSch.JSch.logger = DEVNULL;
			}
			NSch.JSch.logger = logger;
		}

		internal static Logger GetLogger()
		{
			return logger;
		}
	}
}
