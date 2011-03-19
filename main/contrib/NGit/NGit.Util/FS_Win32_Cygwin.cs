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
using System.Diagnostics;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	internal class FS_Win32_Cygwin : FS_Win32
	{
		private static string cygpath;

		internal static bool IsCygwin()
		{
			string path = AccessController.DoPrivileged(new _PrivilegedAction_58());
			if (path == null)
			{
				return false;
			}
			FilePath found = FS.SearchPath(path, "cygpath.exe");
			if (found != null)
			{
				cygpath = found.GetPath();
			}
			return cygpath != null;
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

		public FS_Win32_Cygwin() : base()
		{
		}

		protected internal FS_Win32_Cygwin(FS src) : base(src)
		{
		}

		public override FS NewInstance()
		{
			return new NGit.Util.FS_Win32_Cygwin(this);
		}

		public override FilePath Resolve(FilePath dir, string pn)
		{
			string w = ReadPipe(dir, new string[] { cygpath, "--windows", "--absolute", pn }, 
				"UTF-8");
			//
			//
			if (w != null)
			{
				return new FilePath(w);
			}
			return base.Resolve(dir, pn);
		}

		protected internal override FilePath UserHomeImpl()
		{
			string home = AccessController.DoPrivileged(new _PrivilegedAction_95());
			if (home == null || home.Length == 0)
			{
				return base.UserHomeImpl();
			}
			return Resolve(new FilePath("."), home);
		}

		private sealed class _PrivilegedAction_95 : PrivilegedAction<string>
		{
			public _PrivilegedAction_95()
			{
			}

			public string Run()
			{
				return Runtime.Getenv("HOME");
			}
		}

		public override ProcessStartInfo RunInShell(string cmd, string[] args)
		{
			IList<string> argv = new AList<string>(4 + args.Length);
			argv.AddItem("sh.exe");
			argv.AddItem("-c");
			argv.AddItem(cmd + " \"$@\"");
			argv.AddItem(cmd);
			Sharpen.Collections.AddAll(argv, Arrays.AsList(args));
			ProcessStartInfo proc = new ProcessStartInfo();
			proc.SetCommand(argv);
			return proc;
		}
	}
}
