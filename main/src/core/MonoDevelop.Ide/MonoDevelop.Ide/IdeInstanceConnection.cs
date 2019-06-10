//
// IdeInstanceConnection.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Mono.Unix;
using MonoDevelop.Core;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide
{
	class IdeInstanceConnection
	{
		const int ipcBasePort = 40000;

		string socket_filename;
		Socket listen_socket = null;
		EndPoint ep;

		public event EventHandler<FileEventArgs> FileOpenRequested;

		public void Initialize (bool ipcTcp)
		{
			if (ipcTcp) {
				listen_socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
				ep = new IPEndPoint (IPAddress.Loopback, ipcBasePort + HashSdbmBounded (Environment.UserName));
			} else {
				socket_filename = "/tmp/md-" + Environment.GetEnvironmentVariable ("USER") + "-socket";
				listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
				ep = new UnixEndPoint (socket_filename);
			}
		}

		public bool TryConnect (StartupInfo startupInfo)
		{
			try {
				StringBuilder builder = new StringBuilder ();
				foreach (var file in startupInfo.RequestedFileList) {
					builder.AppendFormat ("{0};{1};{2}\n", file.FileName, file.Line, file.Column);
				}
				listen_socket.Connect (ep);
				listen_socket.Send (Encoding.UTF8.GetBytes (builder.ToString ()));
				return true;
			} catch {
				// Reset the socket
				if (null != socket_filename && File.Exists (socket_filename))
					File.Delete (socket_filename);
				return false;
			}
		}

		public void StartListening ()
		{
			// FIXME: we should probably track the last 'selected' one
			// and do this more cleanly
			try {
				listen_socket.Bind (ep);
				listen_socket.Listen (5);
				listen_socket.BeginAccept (new AsyncCallback (ListenCallback), listen_socket);
			} catch {
				// Socket already in use
			}
		}

		void ListenCallback (IAsyncResult state)
		{
			var files = new List<FilePath> ();

			Socket sock = (Socket)state.AsyncState;

			Socket client = sock.EndAccept (state);
			((Socket)state.AsyncState).BeginAccept (new AsyncCallback (ListenCallback), sock);
			byte [] buf = new byte [1024];
			client.Receive (buf);
			foreach (string filename in Encoding.UTF8.GetString (buf).Split ('\n')) {
				string trimmed = filename.Trim ();
				string file = "";
				foreach (char c in trimmed) {
					if (c == 0x0000)
						continue;
					file += c;
				}
				files.Add (file);
			}
			if (files.Count > 0) {
				GLib.Idle.Add (() => {
					FileOpenRequested?.Invoke (this, new FileEventArgs (files, false));
					return false; 
				});
			}
		}

		public void Dispose ()
		{
			// unloading services
			if (null != socket_filename)
				File.Delete (socket_filename);
		}

		/// <summary>SDBM-style hash, bounded to a range of 1000.</summary>
		static int HashSdbmBounded (string input)
		{
			ulong hash = 0;
			for (int i = 0; i < input.Length; i++) {
				unchecked {
					hash = ((ulong)input [i]) + (hash << 6) + (hash << 16) - hash;
				}
			}

			return (int)(hash % 1000);
		}

	}
}
