/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, JetBrains s.r.o.
 * Copyright (C) 2009, Google, Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// The base session factory that loads known hosts and private keys from
    /// <code>$HOME/.ssh</code>.
    /// <para/>
    /// This is the default implementation used by JGit and provides most of the
    /// compatibility necessary to match OpenSSH, a popular implementation of SSH
    /// used by C Git.
    /// <para/>
    /// The factory does not provide UI behavior. Override the method
    /// <see cref="configure"/>
    /// to supply appropriate {@link UserInfo} to the session.
    /// </summary>
    public abstract class SshConfigSessionFactory : SshSessionFactory
    {
        private OpenSshConfig _config;
        private readonly Dictionary<string, SecureShell> _byIdentityFile = new Dictionary<string, SecureShell>();
        private SecureShell _defaultSecureShell;

        public override ISshSession getSession(string user, string pass, string host, int port)
        {
            OpenSshConfig.Host hc = getConfig().lookup(host);
            host = hc.getHostName();
            if (port <= 0)
                port = hc.getPort();
            if (user == null)
                user = hc.getUser();

            ISshSession session = createSession(hc, user, host, port);
            if (pass != null)
                session.SetPassword(pass);
            string strictHostKeyCheckingPolicy = hc.getStrictHostKeyChecking();
            if (strictHostKeyCheckingPolicy != null)
            {
                var ht = new Hashtable();
                ht.Add("StrictHostKeyChecking", strictHostKeyCheckingPolicy);
                session.SetConfig(ht);
            }
            string pauth = hc.getPreferredAuthentications();
            if (pauth != null)
            {
                var ht = new Hashtable();
                ht.Add("PreferredAuthentications", pauth);
                session.SetConfig(ht);
            }
            configure(hc, session);
            return session;
        }

        /// <summary>
        /// Create a new JSch session for the requested address.
        /// </summary>
        /// <param name="hc">host configuration</param>
        /// <param name="user">login to authenticate as.</param>
        /// <param name="host">server name to connect to.</param>
        /// <param name="port">port number of the SSH daemon (typically 22).</param>
        /// <returns>new session instance, but otherwise unconfigured.</returns>
        protected ISshSession createSession(OpenSshConfig.Host hc, string user, string host, int port)
        {
            return getJSch(hc).GetSession(user, host, port);
        }

        /// <summary>
        /// Provide additional configuration for the session based on the host
        /// information. This method could be used to supply {@link UserInfo}.
        /// </summary>
        /// <param name="hc">host configuration</param>
        /// <param name="session">session to configure</param>
        protected abstract void configure(OpenSshConfig.Host hc, ISshSession session);

        /// <summary>
        /// Obtain the JSch used to create new sessions.
        /// </summary>
        /// <param name="hc">host configuration</param>
        /// <returns>the JSch instance to use.</returns>
        protected SecureShell getJSch(OpenSshConfig.Host hc)
        {
            if (hc == null)
                throw new System.ArgumentNullException("hc");

            SecureShell def = getDefaultJSch();
            FileInfo identityFile = hc.getIdentityFile();
            if (identityFile == null)
                return def;

            string identityKey = identityFile.FullName;
            SecureShell jsch = _byIdentityFile[identityKey];
            if (jsch == null)
            {
                jsch = new SecureShell();
                jsch.SetHostKeyRepository(def.GetHostKeyRepository());
                jsch.AddIdentity(identityKey);
                _byIdentityFile.Add(identityKey, jsch);
            }
            return jsch;
        }

        private SecureShell getDefaultJSch()
        {
            if (_defaultSecureShell == null)
            {
                _defaultSecureShell = createDefaultJSch();
                foreach (object name in _defaultSecureShell.getIdentityNames())
                {
                    _byIdentityFile.put((string)name, _defaultSecureShell);
                }
            }
            return _defaultSecureShell;
        }

        /// <summary>
        /// Returns the new default JSch implementation
        /// </summary>
        /// <returns>the new default JSch implementation</returns>
        protected static SecureShell createDefaultJSch()
        {
            SecureShell jsch = new SecureShell();
            knownHosts(jsch);
            identities(jsch);
            return jsch;
        }

        private OpenSshConfig getConfig()
        {
            if (_config == null)
                _config = OpenSshConfig.get();
            return _config;
        }


        private static void knownHosts(SecureShell sch)
        {
            DirectoryInfo home = FS.userHome();
            if (home == null)
                return;
            var known_hosts = new FileInfo(Path.Combine(home.ToString(), ".ssh/known_hosts"));
            try
            {
                using (var s = new StreamReader(known_hosts.FullName))
                {
                    sch.SetKnownHosts(s);
                }
            }
            catch (FileNotFoundException)
            {
                // Oh well. They don't have a known hosts in home.
            }
            catch (IOException)
            {
                // Oh well. They don't have a known hosts in home.
            }
        }

        private static void identities(SecureShell sch)
        {
            DirectoryInfo home = FS.userHome();
            if (home == null)
                return;
            var sshdir = PathUtil.CombineDirectoryPath(home, ".ssh");
            if (sshdir.IsDirectory())
            {
                loadIdentity(sch, PathUtil.CombineFilePath(sshdir, "identity"));
                loadIdentity(sch, PathUtil.CombineFilePath(sshdir, "id_rsa"));
                loadIdentity(sch, PathUtil.CombineFilePath(sshdir, "id_dsa"));
            }
        }

        private static void loadIdentity(SecureShell sch, FileInfo priv)
        {
            if (!priv.IsFile()) return;
            try
            {
                sch.AddIdentity(priv.FullName);
            }
            catch (Exception)
            {
                // Instead, pretend the key doesn't exist.
            }
        }
    }

    public static class JSchExtensions
    {
        public static IEnumerable<object> getIdentityNames(this SecureShell jSch)
        {
            //TODO: [nulltoken] Implement JSch.getIdentityNames with the help of reflection.
            return new string[]{};
        }
    }

}