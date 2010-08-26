/*
 * Copyright (C) 2008, Google Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GitSharp.Core.Exceptions;
using GitSharp.Core.FnMatch;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Simple configuration parser for the OpenSSH ~/.ssh/config file.
    /// <para/>
    /// Since JSch does not (currently) have the ability to parse an OpenSSH
    /// configuration file this is a simple parser to read that file and make the
    /// critical options available to {@link SshSessionFactory}.
    /// </summary>
    public class OpenSshConfig
    {
        /// <summary>
        /// IANA assigned port number for SSH.
        /// </summary>
        public const int SSH_PORT = 22;
        private readonly Object _locker = new Object();

        /// <summary>
        /// Obtain the user's configuration data.
        /// <para/>
        /// The configuration file is always returned to the caller, even if no file
        /// exists in the user's home directory at the time the call was made. Lookup
        /// requests are cached and are automatically updated if the user modifies
        /// the configuration file since the last time it was cached.
        /// </summary>
        /// <returns>a caching reader of the user's configuration file.</returns>
        public static OpenSshConfig get()
        {
            DirectoryInfo home = FS.userHome() ?? new DirectoryInfo(Path.GetFullPath("."));

            FileInfo config = PathUtil.CombineFilePath(home, ".ssh" + Path.DirectorySeparatorChar + "config");
            var osc = new OpenSshConfig(home, config);
            osc.refresh();
            return osc;
        }

        /// <summary>
        /// The user's home directory, as key files may be relative to here.
        /// </summary>
        private readonly DirectoryInfo _home;

        /// <summary>
        /// The .ssh/config file we read and monitor for updates.
        /// </summary>
        private readonly FileInfo _configFile;

        /// <summary>
        /// Modification time of <see cref="_configFile"/> when <see cref="_hosts"/> loaded.
        /// </summary>
        private long _lastModified;

        /// <summary>
        /// Cached entries read out of the configuration file.
        /// </summary>
        private Dictionary<string, Host> _hosts;

        public OpenSshConfig(DirectoryInfo home, FileInfo cfg)
        {
            _home = home;
            _configFile = cfg;
            _hosts = new Dictionary<string, Host>();
        }

        /// <summary>
        /// Locate the configuration for a specific host request.
        /// </summary>
        /// <param name="hostName">
        /// the name the user has supplied to the SSH tool. This may be a
        /// real host name, or it may just be a "Host" block in the
        /// configuration file.
        /// </param>
        /// <returns>configuration for the requested name. Never null.</returns>
        public Host lookup(string hostName)
        {
            Dictionary<string, Host> cache = refresh();
            Host h = cache.get(hostName);
            if (h == null)
                h = new Host();
            if (h.patternsApplied)
                return h;

            foreach (KeyValuePair<string, Host> e in cache)
            {
                if (!isHostPattern(e.Key))
                    continue;
                if (!isHostMatch(e.Key, hostName))
                    continue;
                h.copyFrom(e.Value);
            }

            if (h.hostName == null)
                h.hostName = hostName;
            if (h.user == null)
                h.user = userName();
            if (h.port == 0)
                h.port = SSH_PORT;
            h.patternsApplied = true;
            return h;
        }

        private Dictionary<string, Host> refresh()
        {
            lock (_locker)
            {
                long mtime = _configFile.lastModified();
                if (mtime != _lastModified)
                {
                    try
                    {
                        using (var s = new FileStream(_configFile.FullName, System.IO.FileMode.Open, FileAccess.Read))
                        {
                            _hosts = parse(s);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        _hosts = new Dictionary<string, Host>();
                    }
                    catch (IOException)
                    {
                        _hosts = new Dictionary<string, Host>();
                    }
                    _lastModified = mtime;
                }
                return _hosts;
            }
        }

        private Dictionary<string, Host> parse(Stream stream)
        {
            var m = new Dictionary<string, Host>();
            var sr = new StreamReader(stream);
            var current = new List<Host>(4);
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                var regex = new Regex("[ \t]*[= \t]");

                string[] parts = regex.Split(line, 2);
                string keyword = parts[0].Trim();
                string argValue = parts[1].Trim();

                var regex2 = new Regex("[ \t]");
                if (StringUtils.equalsIgnoreCase("Host", keyword))
                {
                    current.Clear();
                    foreach (string pattern in regex2.Split(argValue))
                    {
                        string name = dequote(pattern);
                        Host c = m.get(name);
                        if (c == null)
                        {
                            c = new Host();
                            m.put(name, c);
                        }
                        current.Add(c);
                    }
                    continue;
                }

                if (current.isEmpty())
                {
                    // We received an option outside of a Host block. We
                    // don't know who this should match against, so skip.
                    //
                    continue;
                }

                if (StringUtils.equalsIgnoreCase("HostName", keyword))
                {
                    foreach (Host c in current)
                        if (c.hostName == null)
                            c.hostName = dequote(argValue);
                }
                else if (StringUtils.equalsIgnoreCase("User", keyword))
                {
                    foreach (Host c in current)
                        if (c.user == null)
                            c.user = dequote(argValue);
                }
                else if (StringUtils.equalsIgnoreCase("Port", keyword))
                {
                    try
                    {
                        int port = int.Parse(dequote(argValue));
                        foreach (Host c in current)
                            if (c.port == 0)
                                c.port = port;
                    }
                    catch (FormatException)
                    {
                        // Bad port number. Don't set it.
                    }
                }
                else if (StringUtils.equalsIgnoreCase("IdentityFile", keyword))
                {
                    foreach (Host c in current)
                        if (c.identityFile == null)
                            c.identityFile = toFile(dequote(argValue));
                }
                else if (StringUtils.equalsIgnoreCase("PreferredAuthentications", keyword))
                {
                    foreach (Host c in current)
                        if (c.preferredAuthentications == null)
                            c.preferredAuthentications = nows(dequote(argValue));
                }
                else if (StringUtils.equalsIgnoreCase("BatchMode", keyword))
                {
                    foreach (Host c in current)
                        if (c.batchMode == null)
                            c.batchMode = yesno(dequote(argValue));
                }
                else if (StringUtils.equalsIgnoreCase("StrictHostKeyChecking", keyword))
                {
                    string value = dequote(argValue);
                    foreach (Host c in current)
                        if (c.strictHostKeyChecking == null)
                            c.strictHostKeyChecking = value;
                }
            }

            return m;
        }

        private static bool isHostPattern(string s)
        {
            return s.IndexOf('*') >= 0 || s.IndexOf('?') >= 0;
        }

        private static bool isHostMatch(string pattern, string name)
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

        private static string dequote(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value.Slice(1, value.Length - 1);
            return value;
        }

        private static string nows(string value)
        {
            var b = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
                if (!Char.IsSeparator(value[i]))
                    b.Append(value[i]);
            return b.ToString();
        }

        private static bool yesno(string value)
        {
            if (StringUtils.equalsIgnoreCase("yes", value))
                return true;
            return false;
        }

        private FileInfo toFile(string path)
        {
            if (path.StartsWith("~/"))
            {
                return PathUtil.CombineFilePath(_home, path.Substring(2));
            }

            if (Path.IsPathRooted(path))
            {
                return new FileInfo(path);
            }

            return PathUtil.CombineFilePath(_home, path);
        }

        public static string userName()
        {
            return Environment.UserName;
        }

        /// <summary>
        /// Configuration of one "Host" block in the configuration file.
        /// <para/>
        /// If returned from <see cref="OpenSshConfig.lookup"/> some or all of the
        /// properties may not be populated. The properties which are not populated
        /// should be defaulted by the caller.
        /// <para/>
        /// When returned from <see cref="OpenSshConfig.lookup"/> any wildcard
        /// entries which appear later in the configuration file will have been
        /// already merged into this block.
        /// 
        /// </summary>
        public class Host
        {
            public bool patternsApplied;
            public string hostName;
            public int port;
            public FileInfo identityFile;
            public string user;
            public string preferredAuthentications;
            public bool? batchMode;
            public string strictHostKeyChecking;

            public void copyFrom(Host src)
            {
                if (src == null)
                    throw new ArgumentNullException("src");

                if (hostName == null)
                    hostName = src.hostName;
                if (port == 0)
                    port = src.port;
                if (identityFile == null)
                    identityFile = src.identityFile;
                if (user == null)
                    user = src.user;
                if (preferredAuthentications == null)
                    preferredAuthentications = src.preferredAuthentications;
                if (batchMode == null)
                    batchMode = src.batchMode;
                if (strictHostKeyChecking == null)
                    strictHostKeyChecking = src.strictHostKeyChecking;
            }

            /// <returns>
            /// the value StrictHostKeyChecking property, the valid values
            /// are "yes" (unknown hosts are not accepted), "no" (unknown
            /// hosts are always accepted), and "ask" (user should be asked
            /// before accepting the host)
            /// </returns>
            public string getStrictHostKeyChecking()
            {
                return strictHostKeyChecking;
            }

            /// <returns>the real IP address or host name to connect to; never null.</returns>
            public string getHostName()
            {
                return hostName;
            }

            /// <returns>the real port number to connect to; never 0.</returns>
            public int getPort()
            {
                return port;
            }

            /// <returns>
            /// path of the private key file to use for authentication; null
            /// if the caller should use default authentication strategies.
            /// </returns>
            public FileInfo getIdentityFile()
            {
                return identityFile;
            }

            /// <returns>the real user name to connect as; never null.</returns>
            public string getUser()
            {
                return user;
            }

            /// <returns>
            /// the preferred authentication methods, separated by commas if
            /// more than one authentication method is preferred.
            /// </returns>
            public string getPreferredAuthentications()
            {
                return preferredAuthentications;
            }

            /// <returns>
            /// true if batch (non-interactive) mode is preferred for this
            /// host connection.
            /// </returns>
            public bool isBatchMode()
            {
                return batchMode != null && batchMode.Value;
            }
        }
    }

}