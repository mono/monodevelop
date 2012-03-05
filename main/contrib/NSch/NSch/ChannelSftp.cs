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
	public class ChannelSftp : ChannelSession
	{
		private const int LOCAL_MAXIMUM_PACKET_SIZE = 32 * 1024;

		private const int LOCAL_WINDOW_SIZE_MAX = (64 * LOCAL_MAXIMUM_PACKET_SIZE);

		private const byte SSH_FXP_INIT = 1;

		private const byte SSH_FXP_VERSION = 2;

		private const byte SSH_FXP_OPEN = 3;

		private const byte SSH_FXP_CLOSE = 4;

		private const byte SSH_FXP_READ = 5;

		private const byte SSH_FXP_WRITE = 6;

		private const byte SSH_FXP_LSTAT = 7;

		private const byte SSH_FXP_FSTAT = 8;

		private const byte SSH_FXP_SETSTAT = 9;

		private const byte SSH_FXP_FSETSTAT = 10;

		private const byte SSH_FXP_OPENDIR = 11;

		private const byte SSH_FXP_READDIR = 12;

		private const byte SSH_FXP_REMOVE = 13;

		private const byte SSH_FXP_MKDIR = 14;

		private const byte SSH_FXP_RMDIR = 15;

		private const byte SSH_FXP_REALPATH = 16;

		private const byte SSH_FXP_STAT = 17;

		private const byte SSH_FXP_RENAME = 18;

		private const byte SSH_FXP_READLINK = 19;

		private const byte SSH_FXP_SYMLINK = 20;

		private const byte SSH_FXP_STATUS = 101;

		private const byte SSH_FXP_HANDLE = 102;

		private const byte SSH_FXP_DATA = 103;

		private const byte SSH_FXP_NAME = 104;

		private const byte SSH_FXP_ATTRS = 105;

		private const byte SSH_FXP_EXTENDED = unchecked((byte)200);

		private const byte SSH_FXP_EXTENDED_REPLY = unchecked((byte)201);

		private const int SSH_FXF_READ = unchecked((int)(0x00000001));

		private const int SSH_FXF_WRITE = unchecked((int)(0x00000002));

		private const int SSH_FXF_APPEND = unchecked((int)(0x00000004));

		private const int SSH_FXF_CREAT = unchecked((int)(0x00000008));

		private const int SSH_FXF_TRUNC = unchecked((int)(0x00000010));

		private const int SSH_FXF_EXCL = unchecked((int)(0x00000020));

		private const int SSH_FILEXFER_ATTR_SIZE = unchecked((int)(0x00000001));

		private const int SSH_FILEXFER_ATTR_UIDGID = unchecked((int)(0x00000002));

		private const int SSH_FILEXFER_ATTR_PERMISSIONS = unchecked((int)(0x00000004));

		private const int SSH_FILEXFER_ATTR_ACMODTIME = unchecked((int)(0x00000008));

		private const int SSH_FILEXFER_ATTR_EXTENDED = unchecked((int)(0x80000000));

		public const int SSH_FX_OK = 0;

		public const int SSH_FX_EOF = 1;

		public const int SSH_FX_NO_SUCH_FILE = 2;

		public const int SSH_FX_PERMISSION_DENIED = 3;

		public const int SSH_FX_FAILURE = 4;

		public const int SSH_FX_BAD_MESSAGE = 5;

		public const int SSH_FX_NO_CONNECTION = 6;

		public const int SSH_FX_CONNECTION_LOST = 7;

		public const int SSH_FX_OP_UNSUPPORTED = 8;

		private const int MAX_MSG_LENGTH = 256 * 1024;

		public const int OVERWRITE = 0;

		public const int RESUME = 1;

		public const int APPEND = 2;

		private bool interactive = false;

		private int seq = 1;

		private int[] ackid = new int[1];

		private Buffer buf;

		private Packet packet;

		private Buffer obuf;

		private Packet opacket;

		private int client_version = 3;

		private int server_version = 3;

		//private string version = client_version.ToString();

		private Hashtable extensions = null;

		private InputStream io_in = null;

		private static readonly string file_separator = FilePath.separator;

		private static readonly char file_separatorc = FilePath.separatorChar;

		private static bool fs_is_bs = unchecked((byte)FilePath.separatorChar) == '\\';

		private string cwd;

		private string home;

		private string lcwd;

		private static readonly string UTF8 = "UTF-8";

		private string fEncoding = UTF8;

		private bool fEncoding_is_utf8 = true;

		private ChannelSftp.RequestQueue rq;

		// pflags
		// The followings will be used in file uploading.
		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetBulkRequests(int bulk_requests)
		{
			if (bulk_requests > 0)
			{
				rq = new ChannelSftp.RequestQueue(this, bulk_requests);
			}
			else
			{
				throw new JSchException("setBulkRequests: " + bulk_requests + " must be greater than 0."
					);
			}
		}

		public virtual int GetBulkRequests()
		{
			return rq.Size();
		}

		public ChannelSftp() : base()
		{
			rq = new ChannelSftp.RequestQueue(this, 10);
			SetLocalWindowSizeMax(LOCAL_WINDOW_SIZE_MAX);
			SetLocalWindowSize(LOCAL_WINDOW_SIZE_MAX);
			SetLocalPacketSize(LOCAL_MAXIMUM_PACKET_SIZE);
		}

		internal override void Init()
		{
		}

		/// <exception cref="NSch.JSchException"></exception>
		public override void Start()
		{
			try
			{
				PipedOutputStream pos = new PipedOutputStream();
				io.SetOutputStream(pos);
				PipedInputStream pis = new Channel.MyPipedInputStream(this, pos, rmpsize);
				io.SetInputStream(pis);
				io_in = io.@in;
				if (io_in == null)
				{
					throw new JSchException("channel is down");
				}
				Request request = new RequestSftp();
				request.DoRequest(GetSession(), this);
				buf = new Buffer(lmpsize);
				packet = new Packet(buf);
				obuf = new Buffer(rmpsize);
				opacket = new Packet(obuf);
				int i = 0;
				int length;
				int type;
				byte[] str;
				// send SSH_FXP_INIT
				SendINIT();
				// receive SSH_FXP_VERSION
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				length = header.length;
				if (length > MAX_MSG_LENGTH)
				{
					throw new SftpException(SSH_FX_FAILURE, "Received message is too long: " + length
						);
				}
				type = header.type;
				// 2 -> SSH_FXP_VERSION
				server_version = header.rid;
				//System.err.println("SFTP protocol server-version="+server_version);
				if (length > 0)
				{
					extensions = new Hashtable();
					// extension data
					Fill(buf, length);
					byte[] extension_name = null;
					byte[] extension_data = null;
					while (length > 0)
					{
						extension_name = buf.GetString();
						length -= (4 + extension_name.Length);
						extension_data = buf.GetString();
						length -= (4 + extension_data.Length);
						extensions.Put(Util.Byte2str(extension_name), Util.Byte2str(extension_data));
					}
				}
				lcwd = new FilePath(".").GetCanonicalPath();
			}
			catch (Exception e)
			{
				//System.err.println(e);
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

		public virtual void Quit()
		{
			Disconnect();
		}

		public virtual void Exit()
		{
			Disconnect();
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Lcd(string path)
		{
			path = LocalAbsolutePath(path);
			if ((new FilePath(path)).IsDirectory())
			{
				try
				{
					path = (new FilePath(path)).GetCanonicalPath();
				}
				catch (Exception)
				{
				}
				lcwd = path;
				return;
			}
			throw new SftpException(SSH_FX_NO_SUCH_FILE, "No such directory");
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Cd(string path)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				path = IsUnique(path);
				byte[] str = _realpath(path);
				SftpATTRS attr = _stat(str);
				if ((attr.GetFlags() & SftpATTRS.SSH_FILEXFER_ATTR_PERMISSIONS) == 0)
				{
					throw new SftpException(SSH_FX_FAILURE, "Can't change directory: " + path);
				}
				if (!attr.IsDir())
				{
					throw new SftpException(SSH_FX_FAILURE, "Can't change directory: " + path);
				}
				SetCwd(Util.Byte2str(str, fEncoding));
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Put(string src, string dst)
		{
			Put(src, dst, null, OVERWRITE);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Put(string src, string dst, int mode)
		{
			Put(src, dst, null, mode);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Put(string src, string dst, SftpProgressMonitor monitor)
		{
			Put(src, dst, monitor, OVERWRITE);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Put(string src, string dst, SftpProgressMonitor monitor, int 
			mode)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				src = LocalAbsolutePath(src);
				dst = RemoteAbsolutePath(dst);
				ArrayList v = Glob_remote(dst);
				int vsize = v.Count;
				if (vsize != 1)
				{
					if (vsize == 0)
					{
						if (IsPattern(dst))
						{
							throw new SftpException(SSH_FX_FAILURE, dst);
						}
						else
						{
							dst = Util.Unquote(dst);
						}
					}
					throw new SftpException(SSH_FX_FAILURE, v.ToString());
				}
				else
				{
					dst = (string)(v[0]);
				}
				bool isRemoteDir = IsRemoteDir(dst);
				v = Glob_local(src);
				vsize = v.Count;
				StringBuilder dstsb = null;
				if (isRemoteDir)
				{
					if (!dst.EndsWith("/"))
					{
						dst += "/";
					}
					dstsb = new StringBuilder(dst);
				}
				else
				{
					if (vsize > 1)
					{
						throw new SftpException(SSH_FX_FAILURE, "Copying multiple files, but the destination is missing or a file."
							);
					}
				}
				for (int j = 0; j < vsize; j++)
				{
					string _src = (string)(v[j]);
					string _dst = null;
					if (isRemoteDir)
					{
						int i = _src.LastIndexOf(file_separatorc);
						if (fs_is_bs)
						{
							int ii = _src.LastIndexOf('/');
							if (ii != -1 && ii > i)
							{
								i = ii;
							}
						}
						if (i == -1)
						{
							dstsb.Append(_src);
						}
						else
						{
							dstsb.Append(Sharpen.Runtime.Substring(_src, i + 1));
						}
						_dst = dstsb.ToString();
						dstsb.Delete(dst.Length, _dst.Length);
					}
					else
					{
						_dst = dst;
					}
					//System.err.println("_dst "+_dst);
					long size_of_dst = 0;
					if (mode == RESUME)
					{
						try
						{
							SftpATTRS attr = _stat(_dst);
							size_of_dst = attr.GetSize();
						}
						catch (Exception)
						{
						}
						//System.err.println(eee);
						long size_of_src = new FilePath(_src).Length();
						if (size_of_src < size_of_dst)
						{
							throw new SftpException(SSH_FX_FAILURE, "failed to resume for " + _dst);
						}
						if (size_of_src == size_of_dst)
						{
							return;
						}
					}
					if (monitor != null)
					{
						monitor.Init(SftpProgressMonitor.PUT, _src, _dst, (new FilePath(_src)).Length());
						if (mode == RESUME)
						{
							monitor.Count(size_of_dst);
						}
					}
					FileInputStream fis = null;
					try
					{
						fis = new FileInputStream(_src);
						_put(fis, _dst, monitor, mode);
					}
					finally
					{
						if (fis != null)
						{
							fis.Close();
						}
					}
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, e.ToString(), (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, e.ToString());
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Put(InputStream src, string dst)
		{
			Put(src, dst, null, OVERWRITE);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Put(InputStream src, string dst, int mode)
		{
			Put(src, dst, null, mode);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Put(InputStream src, string dst, SftpProgressMonitor monitor)
		{
			Put(src, dst, monitor, OVERWRITE);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Put(InputStream src, string dst, SftpProgressMonitor monitor, 
			int mode)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				dst = RemoteAbsolutePath(dst);
				ArrayList v = Glob_remote(dst);
				int vsize = v.Count;
				if (vsize != 1)
				{
					if (vsize == 0)
					{
						if (IsPattern(dst))
						{
							throw new SftpException(SSH_FX_FAILURE, dst);
						}
						else
						{
							dst = Util.Unquote(dst);
						}
					}
					throw new SftpException(SSH_FX_FAILURE, v.ToString());
				}
				else
				{
					dst = (string)(v[0]);
				}
				if (IsRemoteDir(dst))
				{
					throw new SftpException(SSH_FX_FAILURE, dst + " is a directory");
				}
				if (monitor != null)
				{
					monitor.Init(SftpProgressMonitor.PUT, "-", dst, SftpProgressMonitor.UNKNOWN_SIZE);
				}
				_put(src, dst, monitor, mode);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, e.ToString(), (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, e.ToString());
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void _put(InputStream src, string dst, SftpProgressMonitor monitor
			, int mode)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				byte[] dstb = Util.Str2byte(dst, fEncoding);
				long skip = 0;
				if (mode == RESUME || mode == APPEND)
				{
					try
					{
						SftpATTRS attr = _stat(dstb);
						skip = attr.GetSize();
					}
					catch (Exception)
					{
					}
				}
				//System.err.println(eee);
				if (mode == RESUME && skip > 0)
				{
					long skipped = src.Skip(skip);
					if (skipped < skip)
					{
						throw new SftpException(SSH_FX_FAILURE, "failed to resume for " + dst);
					}
				}
				if (mode == OVERWRITE)
				{
					SendOPENW(dstb);
				}
				else
				{
					SendOPENA(dstb);
				}
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
				{
					throw new SftpException(SSH_FX_FAILURE, "invalid type=" + type);
				}
				if (type == SSH_FXP_STATUS)
				{
					int i = buf.GetInt();
					ThrowStatusError(buf, i);
				}
				byte[] handle = buf.GetString();
				// handle
				byte[] data = null;
				bool dontcopy = true;
				if (!dontcopy)
				{
					// This case will not work anymore.
					data = new byte[obuf.buffer.Length - (5 + 13 + 21 + handle.Length + Session.buffer_margin
						)];
				}
				long offset = 0;
				if (mode == RESUME || mode == APPEND)
				{
					offset += skip;
				}
				int startid = seq;
				int ackcount = 0;
				int _s = 0;
				int _datalen = 0;
				if (!dontcopy)
				{
					// This case will not work anymore.
					_datalen = data.Length;
				}
				else
				{
					data = obuf.buffer;
					_s = 5 + 13 + 21 + handle.Length;
					_datalen = obuf.buffer.Length - _s - Session.buffer_margin;
				}
				int bulk_requests = rq.Size();
				while (true)
				{
					int nread = 0;
					int count = 0;
					int s = _s;
					int datalen = _datalen;
					do
					{
						nread = src.Read(data, s, datalen);
						if (nread > 0)
						{
							s += nread;
							datalen -= nread;
							count += nread;
						}
					}
					while (datalen > 0 && nread > 0);
					if (count <= 0)
					{
						break;
					}
					int foo = count;
					while (foo > 0)
					{
						if ((seq - 1) == startid || ((seq - startid) - ackcount) >= bulk_requests)
						{
							while (((seq - startid) - ackcount) >= bulk_requests)
							{
								if (this.rwsize >= foo)
								{
									break;
								}
								if (CheckStatus(ackid, header))
								{
									int _ackid = ackid[0];
									if (startid > _ackid || _ackid > seq - 1)
									{
										if (_ackid == seq)
										{
											System.Console.Error.WriteLine("ack error: startid=" + startid + " seq=" + seq + 
												" _ackid=" + _ackid);
										}
										else
										{
											throw new SftpException(SSH_FX_FAILURE, "ack error: startid=" + startid + " seq="
												 + seq + " _ackid=" + _ackid);
										}
									}
									ackcount++;
								}
								else
								{
									break;
								}
							}
						}
						foo -= SendWRITE(handle, offset, data, 0, foo);
					}
					offset += count;
					if (monitor != null && !monitor.Count(count))
					{
						break;
					}
				}
				int _ackcount = seq - startid;
				while (_ackcount > ackcount)
				{
					if (!CheckStatus(null, header))
					{
						break;
					}
					ackcount++;
				}
				if (monitor != null)
				{
					monitor.End();
				}
				_sendCLOSE(handle, header);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, e.ToString(), (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, e.ToString());
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual OutputStream Put(string dst)
		{
			return Put(dst, (SftpProgressMonitor)null, OVERWRITE);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual OutputStream Put(string dst, int mode)
		{
			return Put(dst, (SftpProgressMonitor)null, mode);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual OutputStream Put(string dst, SftpProgressMonitor monitor, int mode
			)
		{
			return Put(dst, monitor, mode, 0);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual OutputStream Put(string dst, SftpProgressMonitor monitor, int mode
			, long offset)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				dst = RemoteAbsolutePath(dst);
				dst = IsUnique(dst);
				if (IsRemoteDir(dst))
				{
					throw new SftpException(SSH_FX_FAILURE, dst + " is a directory");
				}
				byte[] dstb = Util.Str2byte(dst, fEncoding);
				long skip = 0;
				if (mode == RESUME || mode == APPEND)
				{
					try
					{
						SftpATTRS attr = _stat(dstb);
						skip = attr.GetSize();
					}
					catch (Exception)
					{
					}
				}
				//System.err.println(eee);
				if (mode == OVERWRITE)
				{
					SendOPENW(dstb);
				}
				else
				{
					SendOPENA(dstb);
				}
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				if (type == SSH_FXP_STATUS)
				{
					int i = buf.GetInt();
					ThrowStatusError(buf, i);
				}
				byte[] handle = buf.GetString();
				// handle
				if (mode == RESUME || mode == APPEND)
				{
					offset += skip;
				}
				long[] _offset = new long[1];
				_offset[0] = offset;
				OutputStream @out = new _OutputStream_686(this, handle, _offset, monitor);
				return @out;
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		private sealed class _OutputStream_686 : OutputStream
		{
			public _OutputStream_686(ChannelSftp _enclosing, byte[] handle, long[] _offset, SftpProgressMonitor
				 monitor)
			{
				this._enclosing = _enclosing;
				this.handle = handle;
				this._offset = _offset;
				this.monitor = monitor;
				this.init = true;
				this.isClosed = false;
				this.ackid = new int[1];
				this.startid = 0;
				this._ackid = 0;
				this.ackcount = 0;
				this.writecount = 0;
				this.header = new ChannelHeader(_enclosing);
				this._data = new byte[1];
			}

			private bool init;

			private bool isClosed;

			private int[] ackid;

			private int startid;

			private int _ackid;

			private int ackcount;

			private int writecount;

			private ChannelHeader header;

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(byte[] d)
			{
				this.Write(d, 0, d.Length);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(byte[] d, int s, int len)
			{
				if (this.init)
				{
					this.startid = this._enclosing.seq;
					this._ackid = this._enclosing.seq;
					this.init = false;
				}
				if (this.isClosed)
				{
					throw new IOException("stream already closed");
				}
				try
				{
					int _len = len;
					while (_len > 0)
					{
						int sent = this._enclosing.SendWRITE(handle, _offset[0], d, s, _len);
						this.writecount++;
						_offset[0] += sent;
						s += sent;
						_len -= sent;
						if ((this._enclosing.seq - 1) == this.startid || this._enclosing.io_in.Available(
							) >= 1024)
						{
							while (this._enclosing.io_in.Available() > 0)
							{
								if (this._enclosing.CheckStatus(this.ackid, this.header))
								{
									this._ackid = this.ackid[0];
									if (this.startid > this._ackid || this._ackid > this._enclosing.seq - 1)
									{
										throw new SftpException(NSch.ChannelSftp.SSH_FX_FAILURE, string.Empty);
									}
									this.ackcount++;
								}
								else
								{
									break;
								}
							}
						}
					}
					if (monitor != null && !monitor.Count(len))
					{
						this.Close();
						throw new IOException("canceled");
					}
				}
				catch (IOException e)
				{
					throw;
				}
				catch (Exception e)
				{
					throw new IOException(e.ToString());
				}
			}

			internal byte[] _data;

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(int foo)
			{
				this._data[0] = unchecked((byte)foo);
				this.Write(this._data, 0, 1);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Flush()
			{
				if (this.isClosed)
				{
					throw new IOException("stream already closed");
				}
				if (!this.init)
				{
					try
					{
						while (this.writecount > this.ackcount)
						{
							if (!this._enclosing.CheckStatus(null, this.header))
							{
								break;
							}
							this.ackcount++;
						}
					}
					catch (SftpException e)
					{
						throw new IOException(e.ToString());
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				if (this.isClosed)
				{
					return;
				}
				this.Flush();
				if (monitor != null)
				{
					monitor.End();
				}
				try
				{
					this._enclosing._sendCLOSE(handle, this.header);
				}
				catch (IOException e)
				{
					throw;
				}
				catch (Exception e)
				{
					throw new IOException(e.ToString());
				}
				this.isClosed = true;
			}

			private readonly ChannelSftp _enclosing;

			private readonly byte[] handle;

			private readonly long[] _offset;

			private readonly SftpProgressMonitor monitor;
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Get(string src, string dst)
		{
			Get(src, dst, null, OVERWRITE);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Get(string src, string dst, SftpProgressMonitor monitor)
		{
			Get(src, dst, monitor, OVERWRITE);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Get(string src, string dst, SftpProgressMonitor monitor, int 
			mode)
		{
			// System.out.println("get: "+src+" "+dst);
			bool _dstExist = false;
			string _dst = null;
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				src = RemoteAbsolutePath(src);
				dst = LocalAbsolutePath(dst);
				ArrayList v = Glob_remote(src);
				int vsize = v.Count;
				if (vsize == 0)
				{
					throw new SftpException(SSH_FX_NO_SUCH_FILE, "No such file");
				}
				FilePath dstFile = new FilePath(dst);
				bool isDstDir = dstFile.IsDirectory();
				StringBuilder dstsb = null;
				if (isDstDir)
				{
					if (!dst.EndsWith(file_separator))
					{
						dst += file_separator;
					}
					dstsb = new StringBuilder(dst);
				}
				else
				{
					if (vsize > 1)
					{
						throw new SftpException(SSH_FX_FAILURE, "Copying multiple files, but destination is missing or a file."
							);
					}
				}
				for (int j = 0; j < vsize; j++)
				{
					string _src = (string)(v[j]);
					SftpATTRS attr = _stat(_src);
					if (attr.IsDir())
					{
						throw new SftpException(SSH_FX_FAILURE, "not supported to get directory " + _src);
					}
					_dst = null;
					if (isDstDir)
					{
						int i = _src.LastIndexOf('/');
						if (i == -1)
						{
							dstsb.Append(_src);
						}
						else
						{
							dstsb.Append(Sharpen.Runtime.Substring(_src, i + 1));
						}
						_dst = dstsb.ToString();
						dstsb.Delete(dst.Length, _dst.Length);
					}
					else
					{
						_dst = dst;
					}
					FilePath _dstFile = new FilePath(_dst);
					if (mode == RESUME)
					{
						long size_of_src = attr.GetSize();
						long size_of_dst = _dstFile.Length();
						if (size_of_dst > size_of_src)
						{
							throw new SftpException(SSH_FX_FAILURE, "failed to resume for " + _dst);
						}
						if (size_of_dst == size_of_src)
						{
							return;
						}
					}
					if (monitor != null)
					{
						monitor.Init(SftpProgressMonitor.GET, _src, _dst, attr.GetSize());
						if (mode == RESUME)
						{
							monitor.Count(_dstFile.Length());
						}
					}
					FileOutputStream fos = null;
					_dstExist = _dstFile.Exists();
					try
					{
						if (mode == OVERWRITE)
						{
							fos = new FileOutputStream(_dst);
						}
						else
						{
							fos = new FileOutputStream(_dst, true);
						}
						// append
						// System.err.println("_get: "+_src+", "+_dst);
						_get(_src, fos, monitor, mode, new FilePath(_dst).Length());
					}
					finally
					{
						if (fos != null)
						{
							fos.Close();
						}
					}
				}
			}
			catch (Exception e)
			{
				if (!_dstExist && _dst != null)
				{
					FilePath _dstFile = new FilePath(_dst);
					if (_dstFile.Exists() && _dstFile.Length() == 0)
					{
						_dstFile.Delete();
					}
				}
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Get(string src, OutputStream dst)
		{
			Get(src, dst, null, OVERWRITE, 0);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Get(string src, OutputStream dst, SftpProgressMonitor monitor
			)
		{
			Get(src, dst, monitor, OVERWRITE, 0);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Get(string src, OutputStream dst, SftpProgressMonitor monitor
			, int mode, long skip)
		{
			//System.err.println("get: "+src+", "+dst);
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				src = RemoteAbsolutePath(src);
				src = IsUnique(src);
				if (monitor != null)
				{
					SftpATTRS attr = _stat(src);
					monitor.Init(SftpProgressMonitor.GET, src, "??", attr.GetSize());
					if (mode == RESUME)
					{
						monitor.Count(skip);
					}
				}
				_get(src, dst, monitor, mode, skip);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		private void _get(string src, OutputStream dst, SftpProgressMonitor monitor, int 
			mode, long skip)
		{
			//System.err.println("_get: "+src+", "+dst);
			byte[] srcb = Util.Str2byte(src, fEncoding);
			try
			{
				SendOPENR(srcb);
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				if (type == SSH_FXP_STATUS)
				{
					int i = buf.GetInt();
					ThrowStatusError(buf, i);
				}
				byte[] handle = buf.GetString();
				// filename
				long offset = 0;
				if (mode == RESUME)
				{
					offset += skip;
				}
				int request_max = 1;
				rq.Init();
				long request_offset = offset;
				int request_len = buf.buffer.Length - 13;
				if (server_version == 0)
				{
					request_len = 1024;
				}
				while (true)
				{
					while (rq.Count() < request_max)
					{
						SendREAD(handle, request_offset, request_len, rq);
						request_offset += request_len;
					}
					header = Header(buf, header);
					length = header.length;
					type = header.type;
					ChannelSftp.RequestQueue.Request rr = rq.Get(header.rid);
					if (type == SSH_FXP_STATUS)
					{
						Fill(buf, length);
						int i = buf.GetInt();
						if (i == SSH_FX_EOF)
						{
							goto loop_break;
						}
						ThrowStatusError(buf, i);
					}
					if (type != SSH_FXP_DATA)
					{
						goto loop_break;
					}
					buf.Rewind();
					Fill(buf.buffer, 0, 4);
					length -= 4;
					int length_of_data = buf.GetInt();
					// length of data 
					int optional_data = length - length_of_data;
					int foo = length_of_data;
					while (foo > 0)
					{
						int bar = foo;
						if (bar > buf.buffer.Length)
						{
							bar = buf.buffer.Length;
						}
						int data_len = io_in.Read(buf.buffer, 0, bar);
						if (data_len < 0)
						{
							goto loop_break;
						}
						dst.Write(buf.buffer, 0, data_len);
						offset += data_len;
						foo -= data_len;
						if (monitor != null)
						{
							if (!monitor.Count(data_len))
							{
								Skip(foo);
								if (optional_data > 0)
								{
									Skip(optional_data);
								}
								goto loop_break;
							}
						}
					}
					//System.err.println("length: "+length);  // length should be 0
					if (optional_data > 0)
					{
						Skip(optional_data);
					}
					if (length_of_data < rr.length)
					{
						//
						rq.Cancel(header, buf);
						SendREAD(handle, rr.offset + length_of_data, (int)(rr.length - length_of_data), rq
							);
						request_offset = rr.offset + rr.length;
					}
					if (request_max < rq.Size())
					{
						request_max++;
					}
loop_continue: ;
				}
loop_break: ;
				dst.Flush();
				if (monitor != null)
				{
					monitor.End();
				}
				rq.Cancel(header, buf);
				_sendCLOSE(handle, header);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		private class RequestQueue
		{
			internal class Request
			{
				internal int id;

				internal long offset;

				internal long length;

				internal Request(RequestQueue _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly RequestQueue _enclosing;
			}

			internal ChannelSftp.RequestQueue.Request[] rrq = null;

			internal int head;

			internal int count;

			internal RequestQueue(ChannelSftp _enclosing, int size)
			{
				this._enclosing = _enclosing;
				this.rrq = new ChannelSftp.RequestQueue.Request[size];
				for (int i = 0; i < this.rrq.Length; i++)
				{
					this.rrq[i] = new ChannelSftp.RequestQueue.Request(this);
				}
				this.Init();
			}

			internal virtual void Init()
			{
				this.head = this.count = 0;
			}

			internal virtual void Add(int id, long offset, int length)
			{
				if (this.count == 0)
				{
					this.head = 0;
				}
				int tail = this.head + this.count;
				if (tail >= this.rrq.Length)
				{
					tail -= this.rrq.Length;
				}
				this.rrq[tail].id = id;
				this.rrq[tail].offset = offset;
				this.rrq[tail].length = length;
				this.count++;
			}

			internal virtual ChannelSftp.RequestQueue.Request Get(int id)
			{
				this.count -= 1;
				int i = this.head;
				this.head++;
				if (this.head == this.rrq.Length)
				{
					this.head = 0;
				}
				if (this.rrq[i].id != id)
				{
					System.Console.Error.WriteLine("The request is not in order.");
				}
				this.rrq[i].id = 0;
				return this.rrq[i];
			}

			internal virtual int Count()
			{
				return this.count;
			}

			internal virtual int Size()
			{
				return this.rrq.Length;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal virtual void Cancel(ChannelHeader header, Buffer buf)
			{
				int _count = this.count;
				for (int i = 0; i < _count; i++)
				{
					header = this._enclosing.Header(buf, header);
					int length = header.length;
					this.Get(header.rid);
					this._enclosing.Skip(length);
				}
			}

			private readonly ChannelSftp _enclosing;
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual InputStream Get(string src)
		{
			return Get(src, null, 0L);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual InputStream Get(string src, SftpProgressMonitor monitor)
		{
			return Get(src, monitor, 0L);
		}

		/// <exception cref="NSch.SftpException"></exception>
		[System.ObsoleteAttribute(@"This method will be deleted in the future.")]
		public virtual InputStream Get(string src, int mode)
		{
			return Get(src, null, 0L);
		}

		/// <exception cref="NSch.SftpException"></exception>
		[System.ObsoleteAttribute(@"This method will be deleted in the future.")]
		public virtual InputStream Get(string src, SftpProgressMonitor monitor, int mode)
		{
			return Get(src, monitor, 0L);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual InputStream Get(string src, SftpProgressMonitor monitor, long skip
			)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				src = RemoteAbsolutePath(src);
				src = IsUnique(src);
				byte[] srcb = Util.Str2byte(src, fEncoding);
				SftpATTRS attr = _stat(srcb);
				if (monitor != null)
				{
					monitor.Init(SftpProgressMonitor.GET, src, "??", attr.GetSize());
				}
				SendOPENR(srcb);
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				if (type == SSH_FXP_STATUS)
				{
					int i = buf.GetInt();
					ThrowStatusError(buf, i);
				}
				byte[] handle = buf.GetString();
				// handle
				InputStream @in = new _InputStream_1195(this, skip, monitor, handle);
				//throwStatusError(buf, i);
				// ??
				return @in;
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		private sealed class _InputStream_1195 : InputStream
		{
			public _InputStream_1195(ChannelSftp _enclosing, long skip, SftpProgressMonitor monitor
				, byte[] handle)
			{
				this._enclosing = _enclosing;
				this.skip = skip;
				this.monitor = monitor;
				this.handle = handle;
				this.offset = skip;
				this.closed = false;
				this.rest_length = 0;
				this._data = new byte[1];
				this.rest_byte = new byte[1024];
				this.header = new ChannelHeader(_enclosing);
			}

			internal long offset;

			internal bool closed;

			internal int rest_length;

			internal byte[] _data;

			internal byte[] rest_byte;

			internal ChannelHeader header;

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read()
			{
				if (this.closed)
				{
					return -1;
				}
				int i = this.Read(this._data, 0, 1);
				if (i == -1)
				{
					return -1;
				}
				else
				{
					return this._data[0] & unchecked((int)(0xff));
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read(byte[] d)
			{
				if (this.closed)
				{
					return -1;
				}
				return this.Read(d, 0, d.Length);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read(byte[] d, int s, int len)
			{
				if (this.closed)
				{
					return -1;
				}
				if (d == null)
				{
					throw new ArgumentNullException();
				}
				if (s < 0 || len < 0 || s + len > d.Length)
				{
					throw new IndexOutOfRangeException();
				}
				if (len == 0)
				{
					return 0;
				}
				if (this.rest_length > 0)
				{
					int foo = this.rest_length;
					if (foo > len)
					{
						foo = len;
					}
					System.Array.Copy(this.rest_byte, 0, d, s, foo);
					if (foo != this.rest_length)
					{
						System.Array.Copy(this.rest_byte, foo, this.rest_byte, 0, this.rest_length - foo);
					}
					if (monitor != null)
					{
						if (!monitor.Count(foo))
						{
							this.Close();
							return -1;
						}
					}
					this.rest_length -= foo;
					return foo;
				}
				if (this._enclosing.buf.buffer.Length - 13 < len)
				{
					len = this._enclosing.buf.buffer.Length - 13;
				}
				if (this._enclosing.server_version == 0 && len > 1024)
				{
					len = 1024;
				}
				try
				{
					this._enclosing.SendREAD(handle, this.offset, len);
				}
				catch (Exception)
				{
					throw new IOException("error");
				}
				this.header = this._enclosing.Header(this._enclosing.buf, this.header);
				this.rest_length = this.header.length;
				int type = this.header.type;
				int id = this.header.rid;
				if (type != ChannelSftp.SSH_FXP_STATUS && type != ChannelSftp.SSH_FXP_DATA)
				{
					throw new IOException("error");
				}
				if (type == ChannelSftp.SSH_FXP_STATUS)
				{
					this._enclosing.Fill(this._enclosing.buf, this.rest_length);
					int i = this._enclosing.buf.GetInt();
					this.rest_length = 0;
					if (i == ChannelSftp.SSH_FX_EOF)
					{
						this.Close();
						return -1;
					}
					throw new IOException("error");
				}
				this._enclosing.buf.Rewind();
				this._enclosing.Fill(this._enclosing.buf.buffer, 0, 4);
				int length_of_data = this._enclosing.buf.GetInt();
				this.rest_length -= 4;
				int optional_data = this.rest_length - length_of_data;
				this.offset += length_of_data;
				int foo_1 = length_of_data;
				if (foo_1 > 0)
				{
					int bar = foo_1;
					if (bar > len)
					{
						bar = len;
					}
					int i = this._enclosing.io_in.Read(d, s, bar);
					if (i < 0)
					{
						return -1;
					}
					foo_1 -= i;
					this.rest_length = foo_1;
					if (foo_1 > 0)
					{
						if (this.rest_byte.Length < foo_1)
						{
							this.rest_byte = new byte[foo_1];
						}
						int _s = 0;
						int _len = foo_1;
						int j;
						while (_len > 0)
						{
							j = this._enclosing.io_in.Read(this.rest_byte, _s, _len);
							if (j <= 0)
							{
								break;
							}
							_s += j;
							_len -= j;
						}
					}
					if (optional_data > 0)
					{
						this._enclosing.io_in.Skip(optional_data);
					}
					if (monitor != null)
					{
						if (!monitor.Count(i))
						{
							this.Close();
							return -1;
						}
					}
					return i;
				}
				return 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				if (this.closed)
				{
					return;
				}
				this.closed = true;
				if (monitor != null)
				{
					monitor.End();
				}
				try
				{
					this._enclosing._sendCLOSE(handle, this.header);
				}
				catch (Exception)
				{
					throw new IOException("error");
				}
			}

			private readonly ChannelSftp _enclosing;

			private readonly long skip;

			private readonly SftpProgressMonitor monitor;

			private readonly byte[] handle;
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual ArrayList Ls(string path)
		{
			//System.out.println("ls: "+path);
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				byte[] pattern = null;
				ArrayList v = new ArrayList();
				int foo = path.LastIndexOf('/');
				string dir = Sharpen.Runtime.Substring(path, 0, ((foo == 0) ? 1 : foo));
				string _pattern = Sharpen.Runtime.Substring(path, foo + 1);
				dir = Util.Unquote(dir);
				// If pattern has included '*' or '?', we need to convert
				// to UTF-8 string before globbing.
				byte[][] _pattern_utf8 = new byte[1][];
				bool pattern_has_wildcard = IsPattern(_pattern, _pattern_utf8);
				if (pattern_has_wildcard)
				{
					pattern = _pattern_utf8[0];
				}
				else
				{
					string upath = Util.Unquote(path);
					//SftpATTRS attr=_lstat(upath);
					SftpATTRS attr = _stat(upath);
					if (attr.IsDir())
					{
						pattern = null;
						dir = upath;
					}
					else
					{
						if (fEncoding_is_utf8)
						{
							pattern = _pattern_utf8[0];
							pattern = Util.Unquote(pattern);
						}
						else
						{
							_pattern = Util.Unquote(_pattern);
							pattern = Util.Str2byte(_pattern, fEncoding);
						}
					}
				}
				SendOPENDIR(Util.Str2byte(dir, fEncoding));
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				if (type == SSH_FXP_STATUS)
				{
					int i = buf.GetInt();
					ThrowStatusError(buf, i);
				}
				byte[] handle = buf.GetString();
				// handle
				while (true)
				{
					SendREADDIR(handle);
					header = Header(buf, header);
					length = header.length;
					type = header.type;
					if (type != SSH_FXP_STATUS && type != SSH_FXP_NAME)
					{
						throw new SftpException(SSH_FX_FAILURE, string.Empty);
					}
					if (type == SSH_FXP_STATUS)
					{
						Fill(buf, length);
						int i = buf.GetInt();
						if (i == SSH_FX_EOF)
						{
							break;
						}
						ThrowStatusError(buf, i);
					}
					buf.Rewind();
					Fill(buf.buffer, 0, 4);
					length -= 4;
					int count = buf.GetInt();
					byte[] str;
					int flags;
					buf.Reset();
					while (count > 0)
					{
						if (length > 0)
						{
							buf.Shift();
							int j = (buf.buffer.Length > (buf.index + length)) ? length : (buf.buffer.Length 
								- buf.index);
							int i = Fill(buf.buffer, buf.index, j);
							buf.index += i;
							length -= i;
						}
						byte[] filename = buf.GetString();
						byte[] longname = null;
						if (server_version <= 3)
						{
							longname = buf.GetString();
						}
						SftpATTRS attrs = SftpATTRS.GetATTR(buf);
						bool find = false;
						string f = null;
						if (pattern == null)
						{
							find = true;
						}
						else
						{
							if (!pattern_has_wildcard)
							{
								find = Util.Array_equals(pattern, filename);
							}
							else
							{
								byte[] _filename = filename;
								if (!fEncoding_is_utf8)
								{
									f = Util.Byte2str(_filename, fEncoding);
									_filename = Util.Str2byte(f, UTF8);
								}
								find = Util.Glob(pattern, _filename);
							}
						}
						if (find)
						{
							if (f == null)
							{
								f = Util.Byte2str(filename, fEncoding);
							}
							string l = null;
							if (longname == null)
							{
								// TODO: we need to generate long name from attrs
								//       for the sftp protocol 4(and later).
								l = attrs.ToString() + " " + f;
							}
							else
							{
								l = Util.Byte2str(longname, fEncoding);
							}
							v.Add(new ChannelSftp.LsEntry(this, f, l, attrs));
						}
						count--;
					}
				}
				_sendCLOSE(handle, header);
				return v;
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual string Readlink(string path)
		{
			try
			{
				if (server_version < 3)
				{
					throw new SftpException(SSH_FX_OP_UNSUPPORTED, "The remote sshd is too old to support symlink operation."
						);
				}
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				path = IsUnique(path);
				SendREADLINK(Util.Str2byte(path, fEncoding));
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_STATUS && type != SSH_FXP_NAME)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				if (type == SSH_FXP_NAME)
				{
					int count = buf.GetInt();
					// count
					byte[] filename = null;
					for (int i = 0; i < count; i++)
					{
						filename = buf.GetString();
						if (server_version <= 3)
						{
							byte[] longname = buf.GetString();
						}
						SftpATTRS.GetATTR(buf);
					}
					return Util.Byte2str(filename, fEncoding);
				}
				int i_1 = buf.GetInt();
				ThrowStatusError(buf, i_1);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
			return null;
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Symlink(string oldpath, string newpath)
		{
			if (server_version < 3)
			{
				throw new SftpException(SSH_FX_OP_UNSUPPORTED, "The remote sshd is too old to support symlink operation."
					);
			}
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				oldpath = RemoteAbsolutePath(oldpath);
				newpath = RemoteAbsolutePath(newpath);
				oldpath = IsUnique(oldpath);
				if (IsPattern(newpath))
				{
					throw new SftpException(SSH_FX_FAILURE, newpath);
				}
				newpath = Util.Unquote(newpath);
				SendSYMLINK(Util.Str2byte(oldpath, fEncoding), Util.Str2byte(newpath, fEncoding));
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_STATUS)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				int i = buf.GetInt();
				if (i == SSH_FX_OK)
				{
					return;
				}
				ThrowStatusError(buf, i);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Rename(string oldpath, string newpath)
		{
			if (server_version < 2)
			{
				throw new SftpException(SSH_FX_OP_UNSUPPORTED, "The remote sshd is too old to support rename operation."
					);
			}
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				oldpath = RemoteAbsolutePath(oldpath);
				newpath = RemoteAbsolutePath(newpath);
				oldpath = IsUnique(oldpath);
				ArrayList v = Glob_remote(newpath);
				int vsize = v.Count;
				if (vsize >= 2)
				{
					throw new SftpException(SSH_FX_FAILURE, v.ToString());
				}
				if (vsize == 1)
				{
					newpath = (string)(v[0]);
				}
				else
				{
					// vsize==0
					if (IsPattern(newpath))
					{
						throw new SftpException(SSH_FX_FAILURE, newpath);
					}
					newpath = Util.Unquote(newpath);
				}
				SendRENAME(Util.Str2byte(oldpath, fEncoding), Util.Str2byte(newpath, fEncoding));
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_STATUS)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				int i = buf.GetInt();
				if (i == SSH_FX_OK)
				{
					return;
				}
				ThrowStatusError(buf, i);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Rm(string path)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				ArrayList v = Glob_remote(path);
				int vsize = v.Count;
				ChannelHeader header = new ChannelHeader(this);
				for (int j = 0; j < vsize; j++)
				{
					path = (string)(v[j]);
					SendREMOVE(Util.Str2byte(path, fEncoding));
					header = Header(buf, header);
					int length = header.length;
					int type = header.type;
					Fill(buf, length);
					if (type != SSH_FXP_STATUS)
					{
						throw new SftpException(SSH_FX_FAILURE, string.Empty);
					}
					int i = buf.GetInt();
					if (i != SSH_FX_OK)
					{
						ThrowStatusError(buf, i);
					}
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		private bool IsRemoteDir(string path)
		{
			try
			{
				SendSTAT(Util.Str2byte(path, fEncoding));
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_ATTRS)
				{
					return false;
				}
				SftpATTRS attr = SftpATTRS.GetATTR(buf);
				return attr.IsDir();
			}
			catch (Exception)
			{
			}
			return false;
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Chgrp(int gid, string path)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				ArrayList v = Glob_remote(path);
				int vsize = v.Count;
				for (int j = 0; j < vsize; j++)
				{
					path = (string)(v[j]);
					SftpATTRS attr = _stat(path);
					attr.SetFLAGS(0);
					attr.SetUIDGID(attr.uid, gid);
					_setStat(path, attr);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Chown(int uid, string path)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				ArrayList v = Glob_remote(path);
				int vsize = v.Count;
				for (int j = 0; j < vsize; j++)
				{
					path = (string)(v[j]);
					SftpATTRS attr = _stat(path);
					attr.SetFLAGS(0);
					attr.SetUIDGID(uid, attr.gid);
					_setStat(path, attr);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Chmod(int permissions, string path)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				ArrayList v = Glob_remote(path);
				int vsize = v.Count;
				for (int j = 0; j < vsize; j++)
				{
					path = (string)(v[j]);
					SftpATTRS attr = _stat(path);
					attr.SetFLAGS(0);
					attr.SetPERMISSIONS(permissions);
					_setStat(path, attr);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void SetMtime(string path, int mtime)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				ArrayList v = Glob_remote(path);
				int vsize = v.Count;
				for (int j = 0; j < vsize; j++)
				{
					path = (string)(v[j]);
					SftpATTRS attr = _stat(path);
					attr.SetFLAGS(0);
					attr.SetACMODTIME(attr.GetATime(), mtime);
					_setStat(path, attr);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Rmdir(string path)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				ArrayList v = Glob_remote(path);
				int vsize = v.Count;
				ChannelHeader header = new ChannelHeader(this);
				for (int j = 0; j < vsize; j++)
				{
					path = (string)(v[j]);
					SendRMDIR(Util.Str2byte(path, fEncoding));
					header = Header(buf, header);
					int length = header.length;
					int type = header.type;
					Fill(buf, length);
					if (type != SSH_FXP_STATUS)
					{
						throw new SftpException(SSH_FX_FAILURE, string.Empty);
					}
					int i = buf.GetInt();
					if (i != SSH_FX_OK)
					{
						ThrowStatusError(buf, i);
					}
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void Mkdir(string path)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				SendMKDIR(Util.Str2byte(path, fEncoding), null);
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_STATUS)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				int i = buf.GetInt();
				if (i == SSH_FX_OK)
				{
					return;
				}
				ThrowStatusError(buf, i);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual SftpATTRS Stat(string path)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				path = IsUnique(path);
				return _stat(path);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		//return null;
		/// <exception cref="NSch.SftpException"></exception>
		private SftpATTRS _stat(byte[] path)
		{
			try
			{
				SendSTAT(path);
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_ATTRS)
				{
					if (type == SSH_FXP_STATUS)
					{
						int i = buf.GetInt();
						ThrowStatusError(buf, i);
					}
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				SftpATTRS attr = SftpATTRS.GetATTR(buf);
				return attr;
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		//return null;
		/// <exception cref="NSch.SftpException"></exception>
		private SftpATTRS _stat(string path)
		{
			return _stat(Util.Str2byte(path, fEncoding));
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual SftpATTRS Lstat(string path)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				path = IsUnique(path);
				return _lstat(path);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		private SftpATTRS _lstat(string path)
		{
			try
			{
				SendLSTAT(Util.Str2byte(path, fEncoding));
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_ATTRS)
				{
					if (type == SSH_FXP_STATUS)
					{
						int i = buf.GetInt();
						ThrowStatusError(buf, i);
					}
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				SftpATTRS attr = SftpATTRS.GetATTR(buf);
				return attr;
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		private byte[] _realpath(string path)
		{
			SendREALPATH(Util.Str2byte(path, fEncoding));
			ChannelHeader header = new ChannelHeader(this);
			header = Header(buf, header);
			int length = header.length;
			int type = header.type;
			Fill(buf, length);
			if (type != SSH_FXP_STATUS && type != SSH_FXP_NAME)
			{
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
			int i;
			if (type == SSH_FXP_STATUS)
			{
				i = buf.GetInt();
				ThrowStatusError(buf, i);
			}
			i = buf.GetInt();
			// count
			byte[] str = null;
			while (i-- > 0)
			{
				str = buf.GetString();
				// absolute path;
				if (server_version <= 3)
				{
					byte[] lname = buf.GetString();
				}
				// long filename
				SftpATTRS attr = SftpATTRS.GetATTR(buf);
			}
			// dummy attribute
			return str;
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void SetStat(string path, SftpATTRS attr)
		{
			try
			{
				((Channel.MyPipedInputStream)io_in).UpdateReadSide();
				path = RemoteAbsolutePath(path);
				ArrayList v = Glob_remote(path);
				int vsize = v.Count;
				for (int j = 0; j < vsize; j++)
				{
					path = (string)(v[j]);
					_setStat(path, attr);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		private void _setStat(string path, SftpATTRS attr)
		{
			try
			{
				SendSETSTAT(Util.Str2byte(path, fEncoding), attr);
				ChannelHeader header = new ChannelHeader(this);
				header = Header(buf, header);
				int length = header.length;
				int type = header.type;
				Fill(buf, length);
				if (type != SSH_FXP_STATUS)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				int i = buf.GetInt();
				if (i != SSH_FX_OK)
				{
					ThrowStatusError(buf, i);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual string Pwd()
		{
			return GetCwd();
		}

		public virtual string Lpwd()
		{
			return lcwd;
		}

//		public virtual string Version()
//		{
//			return version;
//		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual string GetHome()
		{
			if (home == null)
			{
				try
				{
					((Channel.MyPipedInputStream)io_in).UpdateReadSide();
					byte[] _home = _realpath(string.Empty);
					home = Util.Byte2str(_home, fEncoding);
				}
				catch (Exception e)
				{
					if (e is SftpException)
					{
						throw (SftpException)e;
					}
					if (e is Exception)
					{
						throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
					}
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
			}
			return home;
		}

		/// <exception cref="NSch.SftpException"></exception>
		private string GetCwd()
		{
			if (cwd == null)
			{
				cwd = GetHome();
			}
			return cwd;
		}

		private void SetCwd(string cwd)
		{
			this.cwd = cwd;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NSch.SftpException"></exception>
		private void Read(byte[] buf, int s, int l)
		{
			int i = 0;
			while (l > 0)
			{
				i = io_in.Read(buf, s, l);
				if (i <= 0)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				s += i;
				l -= i;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NSch.SftpException"></exception>
		private bool CheckStatus(int[] ackid, ChannelHeader header)
		{
			header = Header(buf, header);
			int length = header.length;
			int type = header.type;
			if (ackid != null)
			{
				ackid[0] = header.rid;
			}
			Fill(buf, length);
			if (type != SSH_FXP_STATUS)
			{
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
			int i = buf.GetInt();
			if (i != SSH_FX_OK)
			{
				ThrowStatusError(buf, i);
			}
			return true;
		}

		/// <exception cref="System.Exception"></exception>
		private bool _sendCLOSE(byte[] handle, ChannelHeader header)
		{
			SendCLOSE(handle);
			return CheckStatus(null, header);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendINIT()
		{
			packet.Reset();
			PutHEAD(SSH_FXP_INIT, 5);
			buf.PutInt(3);
			// version 3
			GetSession().Write(packet, this, 5 + 4);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendREALPATH(byte[] path)
		{
			SendPacketPath(SSH_FXP_REALPATH, path);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendSTAT(byte[] path)
		{
			SendPacketPath(SSH_FXP_STAT, path);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendLSTAT(byte[] path)
		{
			SendPacketPath(SSH_FXP_LSTAT, path);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendFSTAT(byte[] handle)
		{
			SendPacketPath(SSH_FXP_FSTAT, handle);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendSETSTAT(byte[] path, SftpATTRS attr)
		{
			packet.Reset();
			PutHEAD(SSH_FXP_SETSTAT, 9 + path.Length + attr.Length());
			buf.PutInt(seq++);
			buf.PutString(path);
			// path
			attr.Dump(buf);
			GetSession().Write(packet, this, 9 + path.Length + attr.Length() + 4);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendREMOVE(byte[] path)
		{
			SendPacketPath(SSH_FXP_REMOVE, path);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendMKDIR(byte[] path, SftpATTRS attr)
		{
			packet.Reset();
			PutHEAD(SSH_FXP_MKDIR, 9 + path.Length + (attr != null ? attr.Length() : 4));
			buf.PutInt(seq++);
			buf.PutString(path);
			// path
			if (attr != null)
			{
				attr.Dump(buf);
			}
			else
			{
				buf.PutInt(0);
			}
			GetSession().Write(packet, this, 9 + path.Length + (attr != null ? attr.Length() : 
				4) + 4);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendRMDIR(byte[] path)
		{
			SendPacketPath(SSH_FXP_RMDIR, path);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendSYMLINK(byte[] p1, byte[] p2)
		{
			SendPacketPath(SSH_FXP_SYMLINK, p1, p2);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendREADLINK(byte[] path)
		{
			SendPacketPath(SSH_FXP_READLINK, path);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendOPENDIR(byte[] path)
		{
			SendPacketPath(SSH_FXP_OPENDIR, path);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendREADDIR(byte[] path)
		{
			SendPacketPath(SSH_FXP_READDIR, path);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendRENAME(byte[] p1, byte[] p2)
		{
			SendPacketPath(SSH_FXP_RENAME, p1, p2);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendCLOSE(byte[] path)
		{
			SendPacketPath(SSH_FXP_CLOSE, path);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendOPENR(byte[] path)
		{
			SendOPEN(path, SSH_FXF_READ);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendOPENW(byte[] path)
		{
			SendOPEN(path, SSH_FXF_WRITE | SSH_FXF_CREAT | SSH_FXF_TRUNC);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendOPENA(byte[] path)
		{
			SendOPEN(path, SSH_FXF_WRITE | SSH_FXF_CREAT);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendOPEN(byte[] path, int mode)
		{
			packet.Reset();
			PutHEAD(SSH_FXP_OPEN, 17 + path.Length);
			buf.PutInt(seq++);
			buf.PutString(path);
			buf.PutInt(mode);
			buf.PutInt(0);
			// attrs
			GetSession().Write(packet, this, 17 + path.Length + 4);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendPacketPath(byte fxp, byte[] path)
		{
			packet.Reset();
			PutHEAD(fxp, 9 + path.Length);
			buf.PutInt(seq++);
			buf.PutString(path);
			// path
			GetSession().Write(packet, this, 9 + path.Length + 4);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendPacketPath(byte fxp, byte[] p1, byte[] p2)
		{
			packet.Reset();
			PutHEAD(fxp, 13 + p1.Length + p2.Length);
			buf.PutInt(seq++);
			buf.PutString(p1);
			buf.PutString(p2);
			GetSession().Write(packet, this, 13 + p1.Length + p2.Length + 4);
		}

		/// <exception cref="System.Exception"></exception>
		private int SendWRITE(byte[] handle, long offset, byte[] data, int start, int length
			)
		{
			int _length = length;
			opacket.Reset();
			if (obuf.buffer.Length < obuf.index + 13 + 21 + handle.Length + length + Session.
				buffer_margin)
			{
				_length = obuf.buffer.Length - (obuf.index + 13 + 21 + handle.Length + Session.buffer_margin
					);
			}
			// System.err.println("_length="+_length+" length="+length);
			PutHEAD(obuf, SSH_FXP_WRITE, 21 + handle.Length + _length);
			// 14
			obuf.PutInt(seq++);
			//  4
			obuf.PutString(handle);
			//  4+handle.length
			obuf.PutLong(offset);
			//  8
			if (obuf.buffer != data)
			{
				obuf.PutString(data, start, _length);
			}
			else
			{
				//  4+_length
				obuf.PutInt(_length);
				obuf.Skip(_length);
			}
			GetSession().Write(opacket, this, 21 + handle.Length + _length + 4);
			return _length;
		}

		/// <exception cref="System.Exception"></exception>
		private void SendREAD(byte[] handle, long offset, int length)
		{
			SendREAD(handle, offset, length, null);
		}

		/// <exception cref="System.Exception"></exception>
		private void SendREAD(byte[] handle, long offset, int length, ChannelSftp.RequestQueue
			 rrq)
		{
			packet.Reset();
			PutHEAD(SSH_FXP_READ, 21 + handle.Length);
			buf.PutInt(seq++);
			buf.PutString(handle);
			buf.PutLong(offset);
			buf.PutInt(length);
			GetSession().Write(packet, this, 21 + handle.Length + 4);
			if (rrq != null)
			{
				rrq.Add(seq - 1, offset, length);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void PutHEAD(Buffer buf, byte type, int length)
		{
			buf.PutByte(unchecked((byte)Session.SSH_MSG_CHANNEL_DATA));
			buf.PutInt(recipient);
			buf.PutInt(length + 4);
			buf.PutInt(length);
			buf.PutByte(type);
		}

		/// <exception cref="System.Exception"></exception>
		private void PutHEAD(byte type, int length)
		{
			PutHEAD(buf, type, length);
		}

		/// <exception cref="System.Exception"></exception>
		private ArrayList Glob_remote(string _path)
		{
			ArrayList v = new ArrayList();
			int i = 0;
			int foo = _path.LastIndexOf('/');
			if (foo < 0)
			{
				// it is not absolute path.
				v.Add(Util.Unquote(_path));
				return v;
			}
			string dir = Sharpen.Runtime.Substring(_path, 0, ((foo == 0) ? 1 : foo));
			string _pattern = Sharpen.Runtime.Substring(_path, foo + 1);
			dir = Util.Unquote(dir);
			byte[] pattern = null;
			byte[][] _pattern_utf8 = new byte[1][];
			bool pattern_has_wildcard = IsPattern(_pattern, _pattern_utf8);
			if (!pattern_has_wildcard)
			{
				if (!dir.Equals("/"))
				{
					dir += "/";
				}
				v.Add(dir + Util.Unquote(_pattern));
				return v;
			}
			pattern = _pattern_utf8[0];
			SendOPENDIR(Util.Str2byte(dir, fEncoding));
			ChannelHeader header = new ChannelHeader(this);
			header = Header(buf, header);
			int length = header.length;
			int type = header.type;
			Fill(buf, length);
			if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
			{
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
			if (type == SSH_FXP_STATUS)
			{
				i = buf.GetInt();
				ThrowStatusError(buf, i);
			}
			byte[] handle = buf.GetString();
			// filename
			string pdir = null;
			// parent directory
			while (true)
			{
				SendREADDIR(handle);
				header = Header(buf, header);
				length = header.length;
				type = header.type;
				if (type != SSH_FXP_STATUS && type != SSH_FXP_NAME)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty);
				}
				if (type == SSH_FXP_STATUS)
				{
					Fill(buf, length);
					break;
				}
				buf.Rewind();
				Fill(buf.buffer, 0, 4);
				length -= 4;
				int count = buf.GetInt();
				byte[] str;
				int flags;
				buf.Reset();
				while (count > 0)
				{
					if (length > 0)
					{
						buf.Shift();
						int j = (buf.buffer.Length > (buf.index + length)) ? length : (buf.buffer.Length 
							- buf.index);
						i = io_in.Read(buf.buffer, buf.index, j);
						if (i <= 0)
						{
							break;
						}
						buf.index += i;
						length -= i;
					}
					byte[] filename = buf.GetString();
					//System.err.println("filename: "+new String(filename));
					if (server_version <= 3)
					{
						str = buf.GetString();
					}
					// longname
					SftpATTRS attrs = SftpATTRS.GetATTR(buf);
					byte[] _filename = filename;
					string f = null;
					bool found = false;
					if (!fEncoding_is_utf8)
					{
						f = Util.Byte2str(filename, fEncoding);
						_filename = Util.Str2byte(f, UTF8);
					}
					found = Util.Glob(pattern, _filename);
					if (found)
					{
						if (f == null)
						{
							f = Util.Byte2str(filename, fEncoding);
						}
						if (pdir == null)
						{
							pdir = dir;
							if (!pdir.EndsWith("/"))
							{
								pdir += "/";
							}
						}
						v.Add(pdir + f);
					}
					count--;
				}
			}
			if (_sendCLOSE(handle, header))
			{
				return v;
			}
			return null;
		}

		private bool IsPattern(byte[] path)
		{
			int length = path.Length;
			int i = 0;
			while (i < length)
			{
				if (path[i] == '*' || path[i] == '?')
				{
					return true;
				}
				if (path[i] == '\\' && (i + 1) < length)
				{
					i++;
				}
				i++;
			}
			return false;
		}

		/// <exception cref="System.Exception"></exception>
		private ArrayList Glob_local(string _path)
		{
			//System.err.println("glob_local: "+_path);
			ArrayList v = new ArrayList();
			byte[] path = Util.Str2byte(_path, UTF8);
			int i = path.Length - 1;
			while (i >= 0)
			{
				if (path[i] != '*' && path[i] != '?')
				{
					i--;
					continue;
				}
				if (!fs_is_bs && i > 0 && path[i - 1] == '\\')
				{
					i--;
					if (i > 0 && path[i - 1] == '\\')
					{
						i--;
						i--;
						continue;
					}
				}
				break;
			}
			if (i < 0)
			{
				v.Add(fs_is_bs ? _path : Util.Unquote(_path));
				return v;
			}
			while (i >= 0)
			{
				if (path[i] == file_separatorc || (fs_is_bs && path[i] == '/'))
				{
					// On Windows, '/' is also the separator.
					break;
				}
				i--;
			}
			if (i < 0)
			{
				v.Add(fs_is_bs ? _path : Util.Unquote(_path));
				return v;
			}
			byte[] dir;
			if (i == 0)
			{
				dir = new byte[] { unchecked((byte)file_separatorc) };
			}
			else
			{
				dir = new byte[i];
				System.Array.Copy(path, 0, dir, 0, i);
			}
			byte[] pattern = new byte[path.Length - i - 1];
			System.Array.Copy(path, i + 1, pattern, 0, pattern.Length);
			//System.err.println("dir: "+new String(dir)+" pattern: "+new String(pattern));
			try
			{
				string[] children = (new FilePath(Util.Byte2str(dir, UTF8))).List();
				string pdir = Util.Byte2str(dir) + file_separator;
				for (int j = 0; j < children.Length; j++)
				{
					//System.err.println("children: "+children[j]);
					if (Util.Glob(pattern, Util.Str2byte(children[j], UTF8)))
					{
						v.Add(pdir + children[j]);
					}
				}
			}
			catch (Exception)
			{
			}
			return v;
		}

		/// <exception cref="NSch.SftpException"></exception>
		private void ThrowStatusError(Buffer buf, int i)
		{
			if (server_version >= 3 && buf.GetLength() >= 4)
			{
				// WindRiver's sftp will send invalid 
				// SSH_FXP_STATUS packet.
				byte[] str = buf.GetString();
				//byte[] tag=buf.getString();
				throw new SftpException(i, Util.Byte2str(str, UTF8));
			}
			else
			{
				throw new SftpException(i, "Failure");
			}
		}

		private static bool IsLocalAbsolutePath(string path)
		{
			return (new FilePath(path)).IsAbsolute();
		}

		public override void Disconnect()
		{
			base.Disconnect();
		}

		private bool IsPattern(string path, byte[][] utf8)
		{
			byte[] _path = Util.Str2byte(path, UTF8);
			if (utf8 != null)
			{
				utf8[0] = _path;
			}
			return IsPattern(_path);
		}

		private bool IsPattern(string path)
		{
			return IsPattern(path, null);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Fill(Buffer buf, int len)
		{
			buf.Reset();
			Fill(buf.buffer, 0, len);
			buf.Skip(len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int Fill(byte[] buf, int s, int len)
		{
			int i = 0;
			int foo = s;
			while (len > 0)
			{
				i = io_in.Read(buf, s, len);
				if (i <= 0)
				{
					throw new IOException("inputstream is closed");
				}
				//return (s-foo)==0 ? i : s-foo;
				s += i;
				len -= i;
			}
			return s - foo;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Skip(long foo)
		{
			while (foo > 0)
			{
				long bar = io_in.Skip(foo);
				if (bar <= 0)
				{
					break;
				}
				foo -= bar;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ChannelHeader Header(Buffer buf, ChannelHeader header)
		{
			buf.Rewind();
			int i = Fill(buf.buffer, 0, 9);
			header.length = buf.GetInt() - 5;
			header.type = buf.GetByte() & unchecked((int)(0xff));
			header.rid = buf.GetInt();
			return header;
		}

		/// <exception cref="NSch.SftpException"></exception>
		private string RemoteAbsolutePath(string path)
		{
			if (path[0] == '/')
			{
				return path;
			}
			string cwd = GetCwd();
			//    if(cwd.equals(getHome())) return path;
			if (cwd.EndsWith("/"))
			{
				return cwd + path;
			}
			return cwd + "/" + path;
		}

		private string LocalAbsolutePath(string path)
		{
			if (IsLocalAbsolutePath(path))
			{
				return path;
			}
			if (lcwd.EndsWith(file_separator))
			{
				return lcwd + path;
			}
			return lcwd + file_separator + path;
		}

		/// <summary>
		/// This method will check if the given string can be expanded to the
		/// unique string.
		/// </summary>
		/// <remarks>
		/// This method will check if the given string can be expanded to the
		/// unique string.  If it can be expanded to mutiple files, SftpException
		/// will be thrown.
		/// </remarks>
		/// <returns>the returned string is unquoted.</returns>
		/// <exception cref="NSch.SftpException"></exception>
		/// <exception cref="System.Exception"></exception>
		private string IsUnique(string path)
		{
			ArrayList v = Glob_remote(path);
			if (v.Count != 1)
			{
				throw new SftpException(SSH_FX_FAILURE, path + " is not unique: " + v.ToString());
			}
			return (string)(v[0]);
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual int GetServerVersion()
		{
			if (!IsConnected())
			{
				throw new SftpException(SSH_FX_FAILURE, "The channel is not connected.");
			}
			return server_version;
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual void SetFilenameEncoding(string encoding)
		{
			int sversion = GetServerVersion();
			if (3 <= sversion && sversion <= 5 && !encoding.Equals(UTF8))
			{
				throw new SftpException(SSH_FX_FAILURE, "The encoding can not be changed for this sftp server."
					);
			}
			if (encoding.Equals(UTF8))
			{
				encoding = UTF8;
			}
			fEncoding = encoding;
			fEncoding_is_utf8 = fEncoding.Equals(UTF8);
		}

		public virtual string GetExtension(string key)
		{
			if (extensions == null)
			{
				return null;
			}
			return (string)extensions[key];
		}

		/// <exception cref="NSch.SftpException"></exception>
		public virtual string Realpath(string path)
		{
			try
			{
				byte[] _path = _realpath(RemoteAbsolutePath(path));
				return Util.Byte2str(_path, fEncoding);
			}
			catch (Exception e)
			{
				if (e is SftpException)
				{
					throw (SftpException)e;
				}
				if (e is Exception)
				{
					throw new SftpException(SSH_FX_FAILURE, string.Empty, (Exception)e);
				}
				throw new SftpException(SSH_FX_FAILURE, string.Empty);
			}
		}

		public class LsEntry : IComparable
		{
			private string filename;

			private string longname;

			private SftpATTRS attrs;

			internal LsEntry(ChannelSftp _enclosing, string filename, string longname, SftpATTRS
				 attrs)
			{
				this._enclosing = _enclosing;
				this.SetFilename(filename);
				this.SetLongname(longname);
				this.SetAttrs(attrs);
			}

			public virtual string GetFilename()
			{
				return this.filename;
			}

			internal virtual void SetFilename(string filename)
			{
				this.filename = filename;
			}

			public virtual string GetLongname()
			{
				return this.longname;
			}

			internal virtual void SetLongname(string longname)
			{
				this.longname = longname;
			}

			public virtual SftpATTRS GetAttrs()
			{
				return this.attrs;
			}

			internal virtual void SetAttrs(SftpATTRS attrs)
			{
				this.attrs = attrs;
			}

			public override string ToString()
			{
				return this.longname;
			}

			/// <exception cref="System.InvalidCastException"></exception>
			public virtual int CompareTo(object o)
			{
				if (o is ChannelSftp.LsEntry)
				{
					return Sharpen.Runtime.CompareOrdinal(this.filename, ((ChannelSftp.LsEntry)o).GetFilename
						());
				}
				throw new InvalidCastException("a decendent of LsEntry must be given.");
			}

			private readonly ChannelSftp _enclosing;
		}
	}

	internal class ChannelHeader
	{
		internal int length;

		internal int type;

		internal int rid;

		internal ChannelHeader(ChannelSftp _enclosing)
		{
			this._enclosing = _enclosing;
		}

		private readonly ChannelSftp _enclosing;
	}
}
