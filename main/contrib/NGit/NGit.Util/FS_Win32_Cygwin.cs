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
using System.Diagnostics;
using System.IO;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	internal class FS_Win32_Cygwin : FS_Win32
	{
		private static string cygpath;

		internal static bool Detect()
		{
			string path = AccessController.DoPrivileged(new _PrivilegedAction_58());
			if (path == null)
			{
				return false;
			}
			foreach (string p in path.Split(";"))
			{
				FilePath e = new FilePath(p, "cygpath.exe");
				if (e.IsFile())
				{
					cygpath = e.GetAbsolutePath();
					return true;
				}
			}
			return false;
		}

		private sealed class _PrivilegedAction_58 : PrivilegedAction<string>
		{
			public _PrivilegedAction_58()
			{
			}

			public string Run()
			{
				return Runtime.GetProperty("java.library.path");
			}
		}

		public override FilePath Resolve(FilePath dir, string pn)
		{
			try
			{
				Process p;
				p = Runtime.GetRuntime().Exec(new string[] { cygpath, "--windows", "--absolute", 
					pn }, null, dir);
				p.GetOutputStream().Close();
				BufferedReader lineRead = new BufferedReader(new InputStreamReader(p.GetInputStream
					(), "UTF-8"));
				string r = null;
				try
				{
					r = lineRead.ReadLine();
				}
				finally
				{
					lineRead.Close();
				}
				for (; ; )
				{
					try
					{
						if (p.WaitFor() == 0 && r != null && r.Length > 0)
						{
							return new FilePath(r);
						}
						break;
					}
					catch (Exception)
					{
					}
				}
			}
			catch (IOException)
			{
			}
			// Stop bothering me, I have a zombie to reap.
			// Fall through and use the default return.
			//
			return base.Resolve(dir, pn);
		}

		protected internal override FilePath UserHomeImpl()
		{
			string home = AccessController.DoPrivileged(new _PrivilegedAction_112());
			if (home == null || home.Length == 0)
			{
				return base.UserHomeImpl();
			}
			return Resolve(new FilePath("."), home);
		}

		private sealed class _PrivilegedAction_112 : PrivilegedAction<string>
		{
			public _PrivilegedAction_112()
			{
			}

			public string Run()
			{
				return Runtime.Getenv("HOME");
			}
		}
	}
}
