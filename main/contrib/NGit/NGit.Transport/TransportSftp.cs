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

using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Transport;
using NSch;
using Sharpen;
using System.Collections;

namespace NGit.Transport
{
	/// <summary>Transport over the non-Git aware SFTP (SSH based FTP) protocol.</summary>
	/// <remarks>
	/// Transport over the non-Git aware SFTP (SSH based FTP) protocol.
	/// <p>
	/// The SFTP transport does not require any specialized Git support on the remote
	/// (server side) repository. Object files are retrieved directly through secure
	/// shell's FTP protocol, making it possible to copy objects from a remote
	/// repository that is available over SSH, but whose remote host does not have
	/// Git installed.
	/// <p>
	/// Unlike the HTTP variant (see
	/// <see cref="TransportHttp">TransportHttp</see>
	/// ) we rely upon being able
	/// to list files in directories, as the SFTP protocol supports this function. By
	/// listing files through SFTP we can avoid needing to have current
	/// <code>objects/info/packs</code> or <code>info/refs</code> files on the
	/// remote repository and access the data directly, much as Git itself would.
	/// <p>
	/// Concurrent pushing over this transport is not supported. Multiple concurrent
	/// push operations may cause confusion in the repository state.
	/// </remarks>
	/// <seealso cref="WalkFetchConnection">WalkFetchConnection</seealso>
	public class TransportSftp : SshTransport, WalkTransport
	{
		internal static bool CanHandle(URIish uri)
		{
			return uri.IsRemote() && "sftp".Equals(uri.GetScheme());
		}

		protected internal TransportSftp(Repository local, URIish uri) : base(local, uri)
		{
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override FetchConnection OpenFetch()
		{
			TransportSftp.SftpObjectDB c = new TransportSftp.SftpObjectDB(this, uri.GetPath()
				);
			WalkFetchConnection r = new WalkFetchConnection(this, c);
			r.Available(c.ReadAdvertisedRefs());
			return r;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override PushConnection OpenPush()
		{
			TransportSftp.SftpObjectDB c = new TransportSftp.SftpObjectDB(this, uri.GetPath()
				);
			WalkPushConnection r = new WalkPushConnection(this, c);
			r.Available(c.ReadAdvertisedRefs());
			return r;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		internal virtual ChannelSftp NewSftp()
		{
			InitSession();
			int tms = GetTimeout() > 0 ? GetTimeout() * 1000 : 0;
			try
			{
				Channel channel = sock.OpenChannel("sftp");
				channel.Connect(tms);
				return (ChannelSftp)channel;
			}
			catch (JSchException je)
			{
				throw new TransportException(uri, je.Message, je);
			}
		}

		internal class SftpObjectDB : WalkRemoteObjectDatabase
		{
			private readonly string objectsPath;

			private ChannelSftp ftp;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal SftpObjectDB(TransportSftp _enclosing, string path)
			{
				this._enclosing = _enclosing;
				if (path.StartsWith("/~"))
				{
					path = Sharpen.Runtime.Substring(path, 1);
				}
				if (path.StartsWith("~/"))
				{
					path = Sharpen.Runtime.Substring(path, 2);
				}
				try
				{
					this.ftp = this._enclosing.NewSftp();
					this.ftp.Cd(path);
					this.ftp.Cd("objects");
					this.objectsPath = this.ftp.Pwd();
				}
				catch (TransportException err)
				{
					this.Close();
					throw;
				}
				catch (SftpException je)
				{
					throw new TransportException("Can't enter " + path + "/objects" + ": " + je.Message
						, je);
				}
			}

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal SftpObjectDB(TransportSftp _enclosing, TransportSftp.SftpObjectDB parent
				, string p)
			{
				this._enclosing = _enclosing;
				try
				{
					this.ftp = this._enclosing.NewSftp();
					this.ftp.Cd(parent.objectsPath);
					this.ftp.Cd(p);
					this.objectsPath = this.ftp.Pwd();
				}
				catch (TransportException err)
				{
					this.Close();
					throw;
				}
				catch (SftpException je)
				{
					throw new TransportException("Can't enter " + p + " from " + parent.objectsPath +
						 ": " + je.Message, je);
				}
			}

			internal override URIish GetURI()
			{
				return this._enclosing.uri.SetPath(this.objectsPath);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override ICollection<WalkRemoteObjectDatabase> GetAlternates()
			{
				try
				{
					return this.ReadAlternates(WalkRemoteObjectDatabase.INFO_ALTERNATES);
				}
				catch (FileNotFoundException)
				{
					return null;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override WalkRemoteObjectDatabase OpenAlternate(string location)
			{
				return new TransportSftp.SftpObjectDB(_enclosing, this, location);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override ICollection<string> GetPackNames()
			{
				IList<string> packs = new AList<string>();
				try
				{
					ArrayList list = this.ftp.Ls("pack");
					Dictionary<string, ChannelSftp.LsEntry> files;
					Dictionary<string, int> mtimes;
					files = new Dictionary<string, ChannelSftp.LsEntry>();
					mtimes = new Dictionary<string, int>();
					foreach (ChannelSftp.LsEntry ent in list)
					{
						files.Put(ent.GetFilename(), ent);
					}
					foreach (ChannelSftp.LsEntry ent_1 in list)
					{
						string n = ent_1.GetFilename();
						if (!n.StartsWith("pack-") || !n.EndsWith(".pack"))
						{
							continue;
						}
						string @in = Sharpen.Runtime.Substring(n, 0, n.Length - 5) + ".idx";
						if (!files.ContainsKey(@in))
						{
							continue;
						}
						mtimes.Put(n, ent_1.GetAttrs().GetMTime());
						packs.AddItem(n);
					}
					packs.Sort(new _IComparer_219(mtimes));
				}
				catch (SftpException je)
				{
					throw new TransportException("Can't ls " + this.objectsPath + "/pack: " + je.Message
						, je);
				}
				return packs;
			}

			private sealed class _IComparer_219 : IComparer<string>
			{
				public _IComparer_219(Dictionary<string, int> mtimes)
				{
					this.mtimes = mtimes;
				}

				public int Compare(string o1, string o2)
				{
					return mtimes.Get(o2) - mtimes.Get(o1);
				}

				private readonly Dictionary<string, int> mtimes;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override WalkRemoteObjectDatabase.FileStream Open(string path)
			{
				try
				{
					SftpATTRS a = this.ftp.Lstat(path);
					return new WalkRemoteObjectDatabase.FileStream(this.ftp.Get(path), a.GetSize());
				}
				catch (SftpException je)
				{
					if (je.id == ChannelSftp.SSH_FX_NO_SUCH_FILE)
					{
						throw new FileNotFoundException(path);
					}
					throw new TransportException("Can't get " + this.objectsPath + "/" + path + ": " 
						+ je.Message, je);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override void DeleteFile(string path)
			{
				try
				{
					this.ftp.Rm(path);
				}
				catch (SftpException je)
				{
					if (je.id == ChannelSftp.SSH_FX_NO_SUCH_FILE)
					{
						return;
					}
					throw new TransportException("Can't delete " + this.objectsPath + "/" + path + ": "
						 + je.Message, je);
				}
				// Prune any now empty directories.
				//
				string dir = path;
				int s = dir.LastIndexOf('/');
				while (s > 0)
				{
					try
					{
						dir = Sharpen.Runtime.Substring(dir, 0, s);
						this.ftp.Rmdir(dir);
						s = dir.LastIndexOf('/');
					}
					catch (SftpException)
					{
						// If we cannot delete it, leave it alone. It may have
						// entries still in it, or maybe we lack write access on
						// the parent. Either way it isn't a fatal error.
						//
						break;
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override OutputStream WriteFile(string path, ProgressMonitor monitor, string
				 monitorTask)
			{
				try
				{
					return this.ftp.Put(path);
				}
				catch (SftpException je)
				{
					if (je.id == ChannelSftp.SSH_FX_NO_SUCH_FILE)
					{
						this.Mkdir_p(path);
						try
						{
							return this.ftp.Put(path);
						}
						catch (SftpException je2)
						{
							je = je2;
						}
					}
					throw new TransportException("Can't write " + this.objectsPath + "/" + path + ": "
						 + je.Message, je);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override void WriteFile(string path, byte[] data)
			{
				string Lock = path + ".lock";
				try
				{
					base.WriteFile(Lock, data);
					try
					{
						this.ftp.Rename(Lock, path);
					}
					catch (SftpException je)
					{
						throw new TransportException("Can't write " + this.objectsPath + "/" + path + ": "
							 + je.Message, je);
					}
				}
				catch (IOException err)
				{
					try
					{
						this.ftp.Rm(Lock);
					}
					catch (SftpException)
					{
					}
					// Ignore deletion failure, we are already
					// failing anyway.
					throw;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void Mkdir_p(string path)
			{
				int s = path.LastIndexOf('/');
				if (s <= 0)
				{
					return;
				}
				path = Sharpen.Runtime.Substring(path, 0, s);
				try
				{
					this.ftp.Mkdir(path);
				}
				catch (SftpException je)
				{
					if (je.id == ChannelSftp.SSH_FX_NO_SUCH_FILE)
					{
						this.Mkdir_p(path);
						try
						{
							this.ftp.Mkdir(path);
							return;
						}
						catch (SftpException je2)
						{
							je = je2;
						}
					}
					throw new TransportException("Can't mkdir " + this.objectsPath + "/" + path + ": "
						 + je.Message, je);
				}
			}

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal virtual IDictionary<string, Ref> ReadAdvertisedRefs()
			{
				SortedDictionary<string, Ref> avail = new SortedDictionary<string, Ref>();
				this.ReadPackedRefs(avail);
				this.ReadRef(avail, WalkRemoteObjectDatabase.ROOT_DIR + Constants.HEAD, Constants
					.HEAD);
				this.ReadLooseRefs(avail, WalkRemoteObjectDatabase.ROOT_DIR + "refs", "refs/");
				return avail;
			}

			/// <exception cref="NGit.Errors.TransportException"></exception>
			private void ReadLooseRefs(SortedDictionary<string, Ref> avail, string dir, string
				 prefix)
			{
				ArrayList list;
				try
				{
					list = this.ftp.Ls(dir);
				}
				catch (SftpException je)
				{
					throw new TransportException("Can't ls " + this.objectsPath + "/" + dir + ": " + 
						je.Message, je);
				}
				foreach (ChannelSftp.LsEntry ent in list)
				{
					string n = ent.GetFilename();
					if (".".Equals(n) || "..".Equals(n))
					{
						continue;
					}
					string nPath = dir + "/" + n;
					if (ent.GetAttrs().IsDir())
					{
						this.ReadLooseRefs(avail, nPath, prefix + n + "/");
					}
					else
					{
						this.ReadRef(avail, nPath, prefix + n);
					}
				}
			}

			/// <exception cref="NGit.Errors.TransportException"></exception>
			private Ref ReadRef(SortedDictionary<string, Ref> avail, string path, string name
				)
			{
				string line;
				try
				{
					BufferedReader br = this.OpenReader(path);
					try
					{
						line = br.ReadLine();
					}
					finally
					{
						br.Close();
					}
				}
				catch (FileNotFoundException)
				{
					return null;
				}
				catch (IOException err)
				{
					throw new TransportException("Cannot read " + this.objectsPath + "/" + path + ": "
						 + err.Message, err);
				}
				if (line == null)
				{
					throw new TransportException("Empty ref: " + name);
				}
				if (line.StartsWith("ref: "))
				{
					string target = Sharpen.Runtime.Substring(line, "ref: ".Length);
					Ref r = avail.Get(target);
					if (r == null)
					{
						r = this.ReadRef(avail, WalkRemoteObjectDatabase.ROOT_DIR + target, target);
					}
					if (r == null)
					{
						r = new ObjectIdRef.Unpeeled(RefStorage.NEW, target, null);
					}
					r = new SymbolicRef(name, r);
					avail.Put(r.GetName(), r);
					return r;
				}
				if (ObjectId.IsId(line))
				{
					Ref r = new ObjectIdRef.Unpeeled(this.Loose(avail.Get(name)), name, ObjectId.FromString
						(line));
					avail.Put(r.GetName(), r);
					return r;
				}
				throw new TransportException("Bad ref: " + name + ": " + line);
			}

			private RefStorage Loose(Ref r)
			{
				if (r != null && r.GetStorage() == RefStorage.PACKED)
				{
					return RefStorage.LOOSE_PACKED;
				}
				return RefStorage.LOOSE;
			}

			internal override void Close()
			{
				if (this.ftp != null)
				{
					try
					{
						if (this.ftp.IsConnected())
						{
							this.ftp.Disconnect();
						}
					}
					finally
					{
						this.ftp = null;
					}
				}
			}

			private readonly TransportSftp _enclosing;
		}
	}
}
