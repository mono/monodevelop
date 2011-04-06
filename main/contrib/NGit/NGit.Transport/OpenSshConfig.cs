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
using System.Collections.Generic;
using System.IO;
using System.Text;
using NGit.Errors;
using NGit.Fnmatch;
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Simple configuration parser for the OpenSSH ~/.ssh/config file.</summary>
	/// <remarks>
	/// Simple configuration parser for the OpenSSH ~/.ssh/config file.
	/// <p>
	/// Since JSch does not (currently) have the ability to parse an OpenSSH
	/// configuration file this is a simple parser to read that file and make the
	/// critical options available to
	/// <see cref="SshSessionFactory">SshSessionFactory</see>
	/// .
	/// </remarks>
	public class OpenSshConfig
	{
		/// <summary>IANA assigned port number for SSH.</summary>
		/// <remarks>IANA assigned port number for SSH.</remarks>
		internal const int SSH_PORT = 22;

		/// <summary>Obtain the user's configuration data.</summary>
		/// <remarks>
		/// Obtain the user's configuration data.
		/// <p>
		/// The configuration file is always returned to the caller, even if no file
		/// exists in the user's home directory at the time the call was made. Lookup
		/// requests are cached and are automatically updated if the user modifies
		/// the configuration file since the last time it was cached.
		/// </remarks>
		/// <param name="fs">
		/// the file system abstraction which will be necessary to
		/// perform certain file system operations.
		/// </param>
		/// <returns>a caching reader of the user's configuration file.</returns>
		public static NGit.Transport.OpenSshConfig Get(FS fs)
		{
			FilePath home = fs.UserHome();
			if (home == null)
			{
				home = new FilePath(".").GetAbsoluteFile();
			}
			FilePath config = new FilePath(new FilePath(home, ".ssh"), "config");
			NGit.Transport.OpenSshConfig osc = new NGit.Transport.OpenSshConfig(home, config);
			osc.Refresh();
			return osc;
		}

		/// <summary>The user's home directory, as key files may be relative to here.</summary>
		/// <remarks>The user's home directory, as key files may be relative to here.</remarks>
		private readonly FilePath home;

		/// <summary>The .ssh/config file we read and monitor for updates.</summary>
		/// <remarks>The .ssh/config file we read and monitor for updates.</remarks>
		private readonly FilePath configFile;

		/// <summary>
		/// Modification time of
		/// <see cref="configFile">configFile</see>
		/// when
		/// <see cref="hosts">hosts</see>
		/// loaded.
		/// </summary>
		private long lastModified;

		/// <summary>Cached entries read out of the configuration file.</summary>
		/// <remarks>Cached entries read out of the configuration file.</remarks>
		private IDictionary<string, OpenSshConfig.Host> hosts;

		internal OpenSshConfig(FilePath h, FilePath cfg)
		{
			home = h;
			configFile = cfg;
			hosts = Sharpen.Collections.EmptyMap<string, OpenSshConfig.Host>();
		}

		/// <summary>Locate the configuration for a specific host request.</summary>
		/// <remarks>Locate the configuration for a specific host request.</remarks>
		/// <param name="hostName">
		/// the name the user has supplied to the SSH tool. This may be a
		/// real host name, or it may just be a "Host" block in the
		/// configuration file.
		/// </param>
		/// <returns>r configuration for the requested name. Never null.</returns>
		public virtual OpenSshConfig.Host Lookup(string hostName)
		{
			IDictionary<string, OpenSshConfig.Host> cache = Refresh();
			OpenSshConfig.Host h = cache.Get(hostName);
			if (h == null)
			{
				h = new OpenSshConfig.Host();
			}
			if (h.patternsApplied)
			{
				return h;
			}
			foreach (KeyValuePair<string, OpenSshConfig.Host> e in cache.EntrySet())
			{
				if (!IsHostPattern(e.Key))
				{
					continue;
				}
				if (!IsHostMatch(e.Key, hostName))
				{
					continue;
				}
				h.CopyFrom(e.Value);
			}
			if (h.hostName == null)
			{
				h.hostName = hostName;
			}
			if (h.user == null)
			{
				h.user = NGit.Transport.OpenSshConfig.UserName();
			}
			if (h.port == 0)
			{
				h.port = NGit.Transport.OpenSshConfig.SSH_PORT;
			}
			h.patternsApplied = true;
			return h;
		}

		private IDictionary<string, OpenSshConfig.Host> Refresh()
		{
			lock (this)
			{
				long mtime = configFile.LastModified();
				if (mtime != lastModified)
				{
					try
					{
						FileInputStream @in = new FileInputStream(configFile);
						try
						{
							hosts = Parse(@in);
						}
						finally
						{
							@in.Close();
						}
					}
					catch (FileNotFoundException)
					{
						hosts = Sharpen.Collections.EmptyMap<string, OpenSshConfig.Host>();
					}
					catch (IOException)
					{
						hosts = Sharpen.Collections.EmptyMap<string, OpenSshConfig.Host>();
					}
					lastModified = mtime;
				}
				return hosts;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IDictionary<string, OpenSshConfig.Host> Parse(InputStream @in)
		{
			IDictionary<string, OpenSshConfig.Host> m = new LinkedHashMap<string, OpenSshConfig.Host
				>();
			BufferedReader br = new BufferedReader(new InputStreamReader(@in));
			IList<OpenSshConfig.Host> current = new AList<OpenSshConfig.Host>(4);
			string line;
			while ((line = br.ReadLine()) != null)
			{
				line = line.Trim();
				if (line.Length == 0 || line.StartsWith("#"))
				{
					continue;
				}
				string[] parts = line.Split("[ \t]*[= \t]", 2);
				string keyword = parts[0].Trim();
				string argValue = parts[1].Trim();
				if (StringUtils.EqualsIgnoreCase("Host", keyword))
				{
					current.Clear();
					foreach (string pattern in argValue.Split("[ \t]"))
					{
						string name = Dequote(pattern);
						OpenSshConfig.Host c = m.Get(name);
						if (c == null)
						{
							c = new OpenSshConfig.Host();
							m.Put(name, c);
						}
						current.AddItem(c);
					}
					continue;
				}
				if (current.IsEmpty())
				{
					// We received an option outside of a Host block. We
					// don't know who this should match against, so skip.
					//
					continue;
				}
				if (StringUtils.EqualsIgnoreCase("HostName", keyword))
				{
					foreach (OpenSshConfig.Host c in current)
					{
						if (c.hostName == null)
						{
							c.hostName = Dequote(argValue);
						}
					}
				}
				else
				{
					if (StringUtils.EqualsIgnoreCase("User", keyword))
					{
						foreach (OpenSshConfig.Host c in current)
						{
							if (c.user == null)
							{
								c.user = Dequote(argValue);
							}
						}
					}
					else
					{
						if (StringUtils.EqualsIgnoreCase("Port", keyword))
						{
							try
							{
								int port = System.Convert.ToInt32(Dequote(argValue));
								foreach (OpenSshConfig.Host c in current)
								{
									if (c.port == 0)
									{
										c.port = port;
									}
								}
							}
							catch (FormatException)
							{
							}
						}
						else
						{
							// Bad port number. Don't set it.
							if (StringUtils.EqualsIgnoreCase("IdentityFile", keyword))
							{
								foreach (OpenSshConfig.Host c in current)
								{
									if (c.identityFile == null)
									{
										c.identityFile = ToFile(Dequote(argValue));
									}
								}
							}
							else
							{
								if (StringUtils.EqualsIgnoreCase("PreferredAuthentications", keyword))
								{
									foreach (OpenSshConfig.Host c in current)
									{
										if (c.preferredAuthentications == null)
										{
											c.preferredAuthentications = Nows(Dequote(argValue));
										}
									}
								}
								else
								{
									if (StringUtils.EqualsIgnoreCase("BatchMode", keyword))
									{
										foreach (OpenSshConfig.Host c in current)
										{
											if (c.batchMode == null)
											{
												c.batchMode = Yesno(Dequote(argValue));
											}
										}
									}
									else
									{
										if (StringUtils.EqualsIgnoreCase("StrictHostKeyChecking", keyword))
										{
											string value = Dequote(argValue);
											foreach (OpenSshConfig.Host c in current)
											{
												if (c.strictHostKeyChecking == null)
												{
													c.strictHostKeyChecking = value;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return m;
		}

		private static bool IsHostPattern(string s)
		{
			return s.IndexOf('*') >= 0 || s.IndexOf('?') >= 0;
		}

		private static bool IsHostMatch(string pattern, string name)
		{
			FileNameMatcher fn;
			try
			{
				fn = new FileNameMatcher(pattern, null);
			}
			catch (InvalidPatternException)
			{
				return false;
			}
			fn.Append(name);
			return fn.IsMatch();
		}

		private static string Dequote(string value)
		{
			if (value.StartsWith("\"") && value.EndsWith("\""))
			{
				return Sharpen.Runtime.Substring(value, 1, value.Length - 1);
			}
			return value;
		}

		private static string Nows(string value)
		{
			StringBuilder b = new StringBuilder();
			for (int i = 0; i < value.Length; i++)
			{
				if (!System.Char.IsWhiteSpace(value[i]))
				{
					b.Append(value[i]);
				}
			}
			return b.ToString();
		}

		private static bool Yesno(string value)
		{
			if (StringUtils.EqualsIgnoreCase("yes", value))
			{
				return true;
			}
			return false;
		}

		private FilePath ToFile(string path)
		{
			if (path.StartsWith("~/"))
			{
				return new FilePath(home, Sharpen.Runtime.Substring(path, 2));
			}
			FilePath ret = new FilePath(path);
			if (ret.IsAbsolute())
			{
				return ret;
			}
			return new FilePath(home, path);
		}

		internal static string UserName()
		{
			return AccessController.DoPrivileged(new _PrivilegedAction_296());
		}

		private sealed class _PrivilegedAction_296 : PrivilegedAction<string>
		{
			public _PrivilegedAction_296()
			{
			}

			public string Run()
			{
				return Runtime.GetProperty("user.name");
			}
		}

		/// <summary>Configuration of one "Host" block in the configuration file.</summary>
		/// <remarks>
		/// Configuration of one "Host" block in the configuration file.
		/// <p>
		/// If returned from
		/// <see cref="OpenSshConfig.Lookup(string)">OpenSshConfig.Lookup(string)</see>
		/// some or all of the
		/// properties may not be populated. The properties which are not populated
		/// should be defaulted by the caller.
		/// <p>
		/// When returned from
		/// <see cref="OpenSshConfig.Lookup(string)">OpenSshConfig.Lookup(string)</see>
		/// any wildcard
		/// entries which appear later in the configuration file will have been
		/// already merged into this block.
		/// </remarks>
		public class Host
		{
			internal bool patternsApplied;

			internal string hostName;

			internal int port;

			internal FilePath identityFile;

			internal string user;

			internal string preferredAuthentications;

			internal bool? batchMode;

			internal string strictHostKeyChecking;

			internal virtual void CopyFrom(OpenSshConfig.Host src)
			{
				if (hostName == null)
				{
					hostName = src.hostName;
				}
				if (port == 0)
				{
					port = src.port;
				}
				if (identityFile == null)
				{
					identityFile = src.identityFile;
				}
				if (user == null)
				{
					user = src.user;
				}
				if (preferredAuthentications == null)
				{
					preferredAuthentications = src.preferredAuthentications;
				}
				if (batchMode == null)
				{
					batchMode = src.batchMode;
				}
				if (strictHostKeyChecking == null)
				{
					strictHostKeyChecking = src.strictHostKeyChecking;
				}
			}

			/// <returns>
			/// the value StrictHostKeyChecking property, the valid values
			/// are "yes" (unknown hosts are not accepted), "no" (unknown
			/// hosts are always accepted), and "ask" (user should be asked
			/// before accepting the host)
			/// </returns>
			public virtual string GetStrictHostKeyChecking()
			{
				return strictHostKeyChecking;
			}

			/// <returns>the real IP address or host name to connect to; never null.</returns>
			public virtual string GetHostName()
			{
				return hostName;
			}

			/// <returns>the real port number to connect to; never 0.</returns>
			public virtual int GetPort()
			{
				return port;
			}

			/// <returns>
			/// path of the private key file to use for authentication; null
			/// if the caller should use default authentication strategies.
			/// </returns>
			public virtual FilePath GetIdentityFile()
			{
				return identityFile;
			}

			/// <returns>the real user name to connect as; never null.</returns>
			public virtual string GetUser()
			{
				return user;
			}

			/// <returns>
			/// the preferred authentication methods, separated by commas if
			/// more than one authentication method is preferred.
			/// </returns>
			public virtual string GetPreferredAuthentications()
			{
				return preferredAuthentications;
			}

			/// <returns>
			/// true if batch (non-interactive) mode is preferred for this
			/// host connection.
			/// </returns>
			public virtual bool IsBatchMode()
			{
				return batchMode != null && batchMode.Value;
			}
		}
	}
}
