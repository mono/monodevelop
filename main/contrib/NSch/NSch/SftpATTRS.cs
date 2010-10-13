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
using System.Text;
using NSch;
using Sharpen;

namespace NSch
{
	public class SftpATTRS
	{
		internal const int S_ISUID = 0x800;

		internal const int S_ISGID = 0x400;

		internal const int S_ISVTX = 0x200;

		internal const int S_IRUSR = 0x100;

		internal const int S_IWUSR = 0x80;

		internal const int S_IXUSR = 0x40;

		internal const int S_IREAD = 0x100;

		internal const int S_IWRITE = 0x80;

		internal const int S_IEXEC = 0x40;

		internal const int S_IRGRP = 0x20;

		internal const int S_IWGRP = 0x10;

		internal const int S_IXGRP = 0x8;

		internal const int S_IROTH = 0x4;

		internal const int S_IWOTH = 0x2;

		internal const int S_IXOTH = 0x1;

		private const int pmask = unchecked((int)(0xFFF));

		// set user ID on execution
		// set group ID on execution
		// sticky bit   ****** NOT DOCUMENTED *****
		// read by owner
		// write by owner
		// execute/search by owner
		// read by owner
		// write by owner
		// execute/search by owner
		// read by group
		// write by group
		// execute/search by group
		// read by others
		// write by others
		// execute/search by others
		public virtual string GetPermissionsString()
		{
			StringBuilder buf = new StringBuilder(10);
			if (IsDir())
			{
				buf.Append('d');
			}
			else
			{
				if (IsLink())
				{
					buf.Append('l');
				}
				else
				{
					buf.Append('-');
				}
			}
			if ((permissions & S_IRUSR) != 0)
			{
				buf.Append('r');
			}
			else
			{
				buf.Append('-');
			}
			if ((permissions & S_IWUSR) != 0)
			{
				buf.Append('w');
			}
			else
			{
				buf.Append('-');
			}
			if ((permissions & S_ISUID) != 0)
			{
				buf.Append('s');
			}
			else
			{
				if ((permissions & S_IXUSR) != 0)
				{
					buf.Append('x');
				}
				else
				{
					buf.Append('-');
				}
			}
			if ((permissions & S_IRGRP) != 0)
			{
				buf.Append('r');
			}
			else
			{
				buf.Append('-');
			}
			if ((permissions & S_IWGRP) != 0)
			{
				buf.Append('w');
			}
			else
			{
				buf.Append('-');
			}
			if ((permissions & S_ISGID) != 0)
			{
				buf.Append('s');
			}
			else
			{
				if ((permissions & S_IXGRP) != 0)
				{
					buf.Append('x');
				}
				else
				{
					buf.Append('-');
				}
			}
			if ((permissions & S_IROTH) != 0)
			{
				buf.Append('r');
			}
			else
			{
				buf.Append('-');
			}
			if ((permissions & S_IWOTH) != 0)
			{
				buf.Append('w');
			}
			else
			{
				buf.Append('-');
			}
			if ((permissions & S_IXOTH) != 0)
			{
				buf.Append('x');
			}
			else
			{
				buf.Append('-');
			}
			return (buf.ToString());
		}

		public virtual string GetAtimeString()
		{
			SimpleDateFormat locale = new SimpleDateFormat();
			return (locale.Format(Sharpen.Extensions.CreateDate(atime)));
		}

		public virtual string GetMtimeString()
		{
			DateTime date = Sharpen.Extensions.CreateDate(((long)mtime) * 1000);
			return (date.ToString());
		}

		public const int SSH_FILEXFER_ATTR_SIZE = unchecked((int)(0x00000001));

		public const int SSH_FILEXFER_ATTR_UIDGID = unchecked((int)(0x00000002));

		public const int SSH_FILEXFER_ATTR_PERMISSIONS = unchecked((int)(0x00000004));

		public const int SSH_FILEXFER_ATTR_ACMODTIME = unchecked((int)(0x00000008));

		public const int SSH_FILEXFER_ATTR_EXTENDED = unchecked((int)(0x80000000));

		internal const int S_IFDIR = unchecked((int)(0x4000));

		internal const int S_IFLNK = unchecked((int)(0xa000));

		internal int flags = 0;

		internal long size;

		internal int uid;

		internal int gid;

		internal int permissions;

		internal int atime;

		internal int mtime;

		internal string[] extended = null;

		public SftpATTRS()
		{
		}

		internal static NSch.SftpATTRS GetATTR(Buffer buf)
		{
			NSch.SftpATTRS attr = new NSch.SftpATTRS();
			attr.flags = buf.GetInt();
			if ((attr.flags & SSH_FILEXFER_ATTR_SIZE) != 0)
			{
				attr.size = buf.GetLong();
			}
			if ((attr.flags & SSH_FILEXFER_ATTR_UIDGID) != 0)
			{
				attr.uid = buf.GetInt();
				attr.gid = buf.GetInt();
			}
			if ((attr.flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0)
			{
				attr.permissions = buf.GetInt();
			}
			if ((attr.flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0)
			{
				attr.atime = buf.GetInt();
			}
			if ((attr.flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0)
			{
				attr.mtime = buf.GetInt();
			}
			if ((attr.flags & SSH_FILEXFER_ATTR_EXTENDED) != 0)
			{
				int count = buf.GetInt();
				if (count > 0)
				{
					attr.extended = new string[count * 2];
					for (int i = 0; i < count; i++)
					{
						attr.extended[i * 2] = Util.Byte2str(buf.GetString());
						attr.extended[i * 2 + 1] = Util.Byte2str(buf.GetString());
					}
				}
			}
			return attr;
		}

		internal virtual int Length()
		{
			int len = 4;
			if ((flags & SSH_FILEXFER_ATTR_SIZE) != 0)
			{
				len += 8;
			}
			if ((flags & SSH_FILEXFER_ATTR_UIDGID) != 0)
			{
				len += 8;
			}
			if ((flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0)
			{
				len += 4;
			}
			if ((flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0)
			{
				len += 8;
			}
			if ((flags & SSH_FILEXFER_ATTR_EXTENDED) != 0)
			{
				len += 4;
				int count = extended.Length / 2;
				if (count > 0)
				{
					for (int i = 0; i < count; i++)
					{
						len += 4;
						len += extended[i * 2].Length;
						len += 4;
						len += extended[i * 2 + 1].Length;
					}
				}
			}
			return len;
		}

		internal virtual void Dump(Buffer buf)
		{
			buf.PutInt(flags);
			if ((flags & SSH_FILEXFER_ATTR_SIZE) != 0)
			{
				buf.PutLong(size);
			}
			if ((flags & SSH_FILEXFER_ATTR_UIDGID) != 0)
			{
				buf.PutInt(uid);
				buf.PutInt(gid);
			}
			if ((flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0)
			{
				buf.PutInt(permissions);
			}
			if ((flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0)
			{
				buf.PutInt(atime);
			}
			if ((flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0)
			{
				buf.PutInt(mtime);
			}
			if ((flags & SSH_FILEXFER_ATTR_EXTENDED) != 0)
			{
				int count = extended.Length / 2;
				if (count > 0)
				{
					for (int i = 0; i < count; i++)
					{
						buf.PutString(Util.Str2byte(extended[i * 2]));
						buf.PutString(Util.Str2byte(extended[i * 2 + 1]));
					}
				}
			}
		}

		internal virtual void SetFLAGS(int flags)
		{
			this.flags = flags;
		}

		public virtual void SetSIZE(long size)
		{
			flags |= SSH_FILEXFER_ATTR_SIZE;
			this.size = size;
		}

		public virtual void SetUIDGID(int uid, int gid)
		{
			flags |= SSH_FILEXFER_ATTR_UIDGID;
			this.uid = uid;
			this.gid = gid;
		}

		public virtual void SetACMODTIME(int atime, int mtime)
		{
			flags |= SSH_FILEXFER_ATTR_ACMODTIME;
			this.atime = atime;
			this.mtime = mtime;
		}

		public virtual void SetPERMISSIONS(int permissions)
		{
			flags |= SSH_FILEXFER_ATTR_PERMISSIONS;
			permissions = (this.permissions & ~pmask) | (permissions & pmask);
			this.permissions = permissions;
		}

		public virtual bool IsDir()
		{
			return ((flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0 && ((permissions & S_IFDIR) 
				== S_IFDIR));
		}

		public virtual bool IsLink()
		{
			return ((flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0 && ((permissions & S_IFLNK) 
				== S_IFLNK));
		}

		public virtual int GetFlags()
		{
			return flags;
		}

		public virtual long GetSize()
		{
			return size;
		}

		public virtual int GetUId()
		{
			return uid;
		}

		public virtual int GetGId()
		{
			return gid;
		}

		public virtual int GetPermissions()
		{
			return permissions;
		}

		public virtual int GetATime()
		{
			return atime;
		}

		public virtual int GetMTime()
		{
			return mtime;
		}

		public virtual string[] GetExtended()
		{
			return extended;
		}

		public override string ToString()
		{
			return (GetPermissionsString() + " " + GetUId() + " " + GetGId() + " " + GetSize(
				) + " " + GetMtimeString());
		}
	}
}
