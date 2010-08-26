/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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
using System.Net.Sockets;
using System.Text;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Transport through a git-daemon waiting for anonymous TCP connections.
    /// <para/>
    /// This transport supports the <code>git://</code> protocol, usually run on
    /// the IANA registered port 9418. It is a popular means for distributing open
    /// source projects, as there are no authentication or authorization overheads.
    /// </summary>
    public class TransportGitAnon : TcpTransport, IPackTransport
    {
        public const int GIT_PORT = Daemon.DEFAULT_PORT;

        public static bool canHandle(URIish uri)
        {
            if (uri == null)
                throw new System.ArgumentNullException("uri");

            return "git".Equals(uri.Scheme);
        }

        public TransportGitAnon(Repository local, URIish uri)
            : base(local, uri)
        {
        }

        public override IFetchConnection openFetch()
        {
            return new TcpFetchConnection(this);
        }

        public override IPushConnection openPush()
        {
            return new TcpPushConnection(this);
        }

        public override void close()
        {
            // Resources must be established per-connection.
        }

        private Socket OpenConnection()
        {
            int port = Uri.Port > 0 ? Uri.Port : GIT_PORT;
            Socket ret = null;
            try
            {
                ret = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ret.Connect(Uri.Host, port);
                return ret;
            }
            catch (SocketException e)
            {
                try
                {
                    if (ret != null) ret.Close();
                }
                catch (Exception)
                {
                    // ignore a failure during close, we're already failing
                }
                throw new TransportException(Uri, e.Message, e);
            }
        }

        private void Service(string name, PacketLineOut pckOut)
        {
            var cmd = new StringBuilder();
            cmd.Append(name);
            cmd.Append(' ');
            cmd.Append(Uri.Path);
            cmd.Append('\0');
            cmd.Append("host=");
            cmd.Append(Uri.Host);
            if (Uri.Port > 0 && Uri.Port != GIT_PORT)
            {
                cmd.Append(":");
                cmd.Append(Uri.Port);
            }
            cmd.Append('\0');
            pckOut.WriteString(cmd.ToString());
            pckOut.Flush();
        }

        #region Nested Types

        private class TcpFetchConnection : BasePackFetchConnection
        {
            private Socket _sock;

            public TcpFetchConnection(TransportGitAnon instance)
                : base(instance)
            {
                _sock = instance.OpenConnection();
                try
                {
                    init(new NetworkStream(_sock));
                    instance.Service("git-upload-pack", pckOut);
                }
                catch (SocketException err)
                {
                    Close();
                    throw new TransportException(uri, "remote hung up unexpectedly", err);
                }
                readAdvertisedRefs();
            }

            public override void Close()
            {
                base.Close();

                if (_sock == null) return;

                try
                {
                    _sock.Close();
                }
                catch (Exception)
                {
                    // Ignore errors during close.
                }
                finally
                {
                    _sock = null;
                }
            }
        }

        private class TcpPushConnection : BasePackPushConnection
        {
            private Socket _sock;

            public TcpPushConnection(TransportGitAnon instance)
                : base(instance)
            {
                _sock = instance.OpenConnection();
                try
                {
                    init(new NetworkStream(_sock));
                    instance.Service("git-receive-pack", pckOut);
                }
                catch (SocketException err)
                {
                    Close();
                    throw new TransportException(uri, "remote hung up unexpectedly", err);
                }
                readAdvertisedRefs();
            }

            public override void Close()
            {
                base.Close();

                if (_sock == null) return;

                try
                {
                    _sock.Close();
                }
                catch (Exception)
                {
                    // Ignore errors during close.
                }
                finally
                {
                    _sock = null;
                }
            }
        }

        #endregion
    }
}
