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
using System.Net.Sockets;
using System.Text;
using NSch;
using Sharpen;

namespace NSch
{
	internal class Util
	{
		private static readonly byte[] b64 = Util.Str2byte("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/="
			);

		private static byte Val(byte foo)
		{
			if (foo == '=')
			{
				return 0;
			}
			for (int j = 0; j < b64.Length; j++)
			{
				if (foo == b64[j])
				{
					return unchecked((byte)j);
				}
			}
			return 0;
		}

		internal static byte[] FromBase64(byte[] buf, int start, int length)
		{
			byte[] foo = new byte[length];
			int j = 0;
			for (int i = start; i < start + length; i += 4)
			{
				foo[j] = unchecked((byte)((Val(buf[i]) << 2) | ((int)(((uint)(Val(buf[i + 1]) & unchecked(
					(int)(0x30)))) >> 4))));
				if (buf[i + 2] == unchecked((byte)'='))
				{
					j++;
					break;
				}
				foo[j + 1] = unchecked((byte)(((Val(buf[i + 1]) & unchecked((int)(0x0f))) << 4) |
					 ((int)(((uint)(Val(buf[i + 2]) & unchecked((int)(0x3c)))) >> 2))));
				if (buf[i + 3] == unchecked((byte)'='))
				{
					j += 2;
					break;
				}
				foo[j + 2] = unchecked((byte)(((Val(buf[i + 2]) & unchecked((int)(0x03))) << 6) |
					 (Val(buf[i + 3]) & unchecked((int)(0x3f)))));
				j += 3;
			}
			byte[] bar = new byte[j];
			System.Array.Copy(foo, 0, bar, 0, j);
			return bar;
		}

		internal static byte[] ToBase64(byte[] buf, int start, int length)
		{
			byte[] tmp = new byte[length * 2];
			int i;
			int j;
			int k;
			int foo = (length / 3) * 3 + start;
			i = 0;
			for (j = start; j < foo; j += 3)
			{
				k = (buf[j] >> 2) & unchecked((int)(0x3f));
				tmp[i++] = b64[k];
				k = (buf[j] & unchecked((int)(0x03))) << 4 | (buf[j + 1] >> 4) & unchecked((int)(
					0x0f));
				tmp[i++] = b64[k];
				k = (buf[j + 1] & unchecked((int)(0x0f))) << 2 | (buf[j + 2] >> 6) & unchecked((int
					)(0x03));
				tmp[i++] = b64[k];
				k = buf[j + 2] & unchecked((int)(0x3f));
				tmp[i++] = b64[k];
			}
			foo = (start + length) - foo;
			if (foo == 1)
			{
				k = (buf[j] >> 2) & unchecked((int)(0x3f));
				tmp[i++] = b64[k];
				k = ((buf[j] & unchecked((int)(0x03))) << 4) & unchecked((int)(0x3f));
				tmp[i++] = b64[k];
				tmp[i++] = unchecked((byte)(byte)('='));
				tmp[i++] = unchecked((byte)(byte)('='));
			}
			else
			{
				if (foo == 2)
				{
					k = (buf[j] >> 2) & unchecked((int)(0x3f));
					tmp[i++] = b64[k];
					k = (buf[j] & unchecked((int)(0x03))) << 4 | (buf[j + 1] >> 4) & unchecked((int)(
						0x0f));
					tmp[i++] = b64[k];
					k = ((buf[j + 1] & unchecked((int)(0x0f))) << 2) & unchecked((int)(0x3f));
					tmp[i++] = b64[k];
					tmp[i++] = unchecked((byte)(byte)('='));
				}
			}
			byte[] bar = new byte[i];
			System.Array.Copy(tmp, 0, bar, 0, i);
			return bar;
		}

		//    return sun.misc.BASE64Encoder().encode(buf);
		internal static string[] Split(string foo, string split)
		{
			if (foo == null)
			{
				return null;
			}
			byte[] buf = Util.Str2byte(foo);
			ArrayList bar = new ArrayList();
			int start = 0;
			int index;
			while (true)
			{
				index = foo.IndexOf(split, start);
				if (index >= 0)
				{
					bar.Add(Util.Byte2str(buf, start, index - start));
					start = index + 1;
					continue;
				}
				bar.Add(Util.Byte2str(buf, start, buf.Length - start));
				break;
			}
			string[] result = new string[bar.Count];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = (string)(bar[i]);
			}
			return result;
		}

		internal static bool Glob(byte[] pattern, byte[] name)
		{
			return Glob0(pattern, 0, name, 0);
		}

		private static bool Glob0(byte[] pattern, int pattern_index, byte[] name, int name_index
			)
		{
			if (name.Length > 0 && name[0] == '.')
			{
				if (pattern.Length > 0 && pattern[0] == '.')
				{
					if (pattern.Length == 2 && pattern[1] == '*')
					{
						return true;
					}
					return Glob(pattern, pattern_index + 1, name, name_index + 1);
				}
				return false;
			}
			return Glob(pattern, pattern_index, name, name_index);
		}

		private static bool Glob(byte[] pattern, int pattern_index, byte[] name, int name_index
			)
		{
			//System.err.println("glob: "+new String(pattern)+", "+pattern_index+" "+new String(name)+", "+name_index);
			int patternlen = pattern.Length;
			if (patternlen == 0)
			{
				return false;
			}
			int namelen = name.Length;
			int i = pattern_index;
			int j = name_index;
			while (i < patternlen && j < namelen)
			{
				if (pattern[i] == '\\')
				{
					if (i + 1 == patternlen)
					{
						return false;
					}
					i++;
					if (pattern[i] != name[j])
					{
						return false;
					}
					i += SkipUTF8Char(pattern[i]);
					j += SkipUTF8Char(name[j]);
					continue;
				}
				if (pattern[i] == '*')
				{
					while (i < patternlen)
					{
						if (pattern[i] == '*')
						{
							i++;
							continue;
						}
						break;
					}
					if (patternlen == i)
					{
						return true;
					}
					byte foo = pattern[i];
					if (foo == '?')
					{
						while (j < namelen)
						{
							if (Glob(pattern, i, name, j))
							{
								return true;
							}
							j += SkipUTF8Char(name[j]);
						}
						return false;
					}
					else
					{
						if (foo == '\\')
						{
							if (i + 1 == patternlen)
							{
								return false;
							}
							i++;
							foo = pattern[i];
							while (j < namelen)
							{
								if (foo == name[j])
								{
									if (Glob(pattern, i + SkipUTF8Char(foo), name, j + SkipUTF8Char(name[j])))
									{
										return true;
									}
								}
								j += SkipUTF8Char(name[j]);
							}
							return false;
						}
					}
					while (j < namelen)
					{
						if (foo == name[j])
						{
							if (Glob(pattern, i, name, j))
							{
								return true;
							}
						}
						j += SkipUTF8Char(name[j]);
					}
					return false;
				}
				if (pattern[i] == '?')
				{
					i++;
					j += SkipUTF8Char(name[j]);
					continue;
				}
				if (pattern[i] != name[j])
				{
					return false;
				}
				i += SkipUTF8Char(pattern[i]);
				j += SkipUTF8Char(name[j]);
				if (!(j < namelen))
				{
					// name is end
					if (!(i < patternlen))
					{
						// pattern is end
						return true;
					}
					if (pattern[i] == '*')
					{
						break;
					}
				}
				continue;
			}
			if (i == patternlen && j == namelen)
			{
				return true;
			}
			if (!(j < namelen) && pattern[i] == '*')
			{
				// name is end
				bool ok = true;
				while (i < patternlen)
				{
					if (pattern[i++] != '*')
					{
						ok = false;
						break;
					}
				}
				return ok;
			}
			return false;
		}

		internal static string Quote(string path)
		{
			byte[] _path = Str2byte(path);
			int count = 0;
			for (int i = 0; i < _path.Length; i++)
			{
				byte b = _path[i];
				if (b == '\\' || b == '?' || b == '*')
				{
					count++;
				}
			}
			if (count == 0)
			{
				return path;
			}
			byte[] _path2 = new byte[_path.Length + count];
			for (int i_1 = 0, j = 0; i_1 < _path.Length; i_1++)
			{
				byte b = _path[i_1];
				if (b == '\\' || b == '?' || b == '*')
				{
					_path2[j++] = (byte)('\\');
				}
				_path2[j++] = b;
			}
			return Byte2str(_path2);
		}

		internal static string Unquote(string path)
		{
			byte[] foo = Str2byte(path);
			byte[] bar = Unquote(foo);
			if (foo.Length == bar.Length)
			{
				return path;
			}
			return Byte2str(bar);
		}

		internal static byte[] Unquote(byte[] path)
		{
			int pathlen = path.Length;
			int i = 0;
			while (i < pathlen)
			{
				if (path[i] == '\\')
				{
					if (i + 1 == pathlen)
					{
						break;
					}
					System.Array.Copy(path, i + 1, path, i, path.Length - (i + 1));
					pathlen--;
					i++;
					continue;
				}
				i++;
			}
			if (pathlen == path.Length)
			{
				return path;
			}
			byte[] foo = new byte[pathlen];
			System.Array.Copy(path, 0, foo, 0, pathlen);
			return foo;
		}

		private static string[] chars = new string[] { "0", "1", "2", "3", "4", "5", "6", 
			"7", "8", "9", "a", "b", "c", "d", "e", "f" };

		internal static string GetFingerPrint(HASH hash, byte[] data)
		{
			try
			{
				hash.Init();
				hash.Update(data, 0, data.Length);
				byte[] foo = hash.Digest();
				StringBuilder sb = new StringBuilder();
				int bar;
				for (int i = 0; i < foo.Length; i++)
				{
					bar = foo[i] & unchecked((int)(0xff));
					sb.Append(chars[((int)(((uint)bar) >> 4)) & unchecked((int)(0xf))]);
					sb.Append(chars[(bar) & unchecked((int)(0xf))]);
					if (i + 1 < foo.Length)
					{
						sb.Append(":");
					}
				}
				return sb.ToString();
			}
			catch (Exception)
			{
				return "???";
			}
		}

		internal static bool Array_equals(byte[] foo, byte[] bar)
		{
			int i = foo.Length;
			if (i != bar.Length)
			{
				return false;
			}
			for (int j = 0; j < i; j++)
			{
				if (foo[j] != bar[j])
				{
					return false;
				}
			}
			//try{while(true){i--; if(foo[i]!=bar[i])return false;}}catch(Exception e){}
			return true;
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal static Socket CreateSocket(string host, int port, int timeout)
		{
			Socket socket = null;
			if (timeout == 0)
			{
				try
				{
					socket = Sharpen.Extensions.CreateSocket(host, port);
					return socket;
				}
				catch (Exception e)
				{
					string message = e.ToString();
					if (e is Exception)
					{
						throw new JSchException(message, (Exception)e);
					}
					throw new JSchException(message);
				}
			}
			string _host = host;
			int _port = port;
			Socket[] sockp = new Socket[1];
			Exception[] ee = new Exception[1];
			string message_1 = string.Empty;
			Sharpen.Thread tmp = new Sharpen.Thread(new _Runnable_350(sockp, _host, _port, ee
				));
			tmp.SetName("Opening Socket " + host);
			tmp.Start();
			try
			{
				tmp.Join(timeout);
				message_1 = "timeout: ";
			}
			catch (Exception)
			{
			}
			if (sockp[0] != null && sockp[0].Connected)
			{
				socket = sockp[0];
			}
			else
			{
				message_1 += "socket is not established";
				if (ee[0] != null)
				{
					message_1 = ee[0].ToString();
				}
				tmp.Interrupt();
				tmp = null;
				throw new JSchException(message_1);
			}
			return socket;
		}

		private sealed class _Runnable_350 : Runnable
		{
			public _Runnable_350(Socket[] sockp, string _host, int _port, Exception[] ee)
			{
				this.sockp = sockp;
				this._host = _host;
				this._port = _port;
				this.ee = ee;
			}

			public void Run()
			{
				sockp[0] = null;
				try
				{
					sockp[0] = Sharpen.Extensions.CreateSocket(_host, _port);
				}
				catch (Exception e)
				{
					ee[0] = e;
					if (sockp[0] != null && sockp[0].Connected)
					{
						try
						{
							sockp[0].Close();
						}
						catch (Exception)
						{
						}
					}
					sockp[0] = null;
				}
			}

			private readonly Socket[] sockp;

			private readonly string _host;

			private readonly int _port;

			private readonly Exception[] ee;
		}

		internal static byte[] Str2byte(string str, string encoding)
		{
			if (str == null)
			{
				return null;
			}
			try
			{
				return Sharpen.Runtime.GetBytesForString(str, encoding);
			}
			catch (UnsupportedEncodingException)
			{
				return Sharpen.Runtime.GetBytesForString(str);
			}
		}

		internal static byte[] Str2byte(string str)
		{
			return Str2byte(str, "UTF-8");
		}

		internal static string Byte2str(byte[] str, string encoding)
		{
			return Byte2str(str, 0, str.Length, encoding);
		}

		internal static string Byte2str(byte[] str, int s, int l, string encoding)
		{
			try
			{
				return Sharpen.Extensions.CreateString(str, s, l, encoding);
			}
			catch (UnsupportedEncodingException)
			{
				return Sharpen.Extensions.CreateString(str, s, l);
			}
		}

		internal static string Byte2str(byte[] str)
		{
			return Byte2str(str, 0, str.Length, "UTF-8");
		}

		internal static string Byte2str(byte[] str, int s, int l)
		{
			return Byte2str(str, s, l, "UTF-8");
		}

		internal static readonly byte[] empty = Str2byte(string.Empty);

		internal static void Bzero(byte[] foo)
		{
			if (foo == null)
			{
				return;
			}
			for (int i = 0; i < foo.Length; i++)
			{
				foo[i] = 0;
			}
		}

		internal static string DiffString(string str, string[] not_available)
		{
			string[] stra = Util.Split(str, ",");
			string result = null;
			for (int i = 0; i < stra.Length; i++)
			{
				for (int j = 0; j < not_available.Length; j++)
				{
					if (stra[i].Equals(not_available[j]))
					{
						goto loop_continue;
					}
				}
				if (result == null)
				{
					result = stra[i];
				}
				else
				{
					result = result + "," + stra[i];
				}
loop_continue: ;
			}
loop_break: ;
			return result;
		}

		private static int SkipUTF8Char(byte b)
		{
			if (unchecked((byte)(b & unchecked((int)(0x80)))) == 0)
			{
				return 1;
			}
			if (unchecked((byte)(b & unchecked((int)(0xe0)))) == unchecked((byte)unchecked((int
				)(0xc0))))
			{
				return 2;
			}
			if (unchecked((byte)(b & unchecked((int)(0xf0)))) == unchecked((byte)unchecked((int
				)(0xe0))))
			{
				return 3;
			}
			return 1;
		}
	}
}
