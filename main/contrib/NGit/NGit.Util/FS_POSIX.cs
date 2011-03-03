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
using System.Text;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	internal abstract class FS_POSIX : FS
	{
		public override FilePath GitPrefix()
		{
			string path = SystemReader.GetInstance().Getenv("PATH");
			FilePath gitExe = SearchPath(path, "git");
			if (gitExe != null)
			{
				return gitExe.GetParentFile().GetParentFile();
			}
			if (IsMacOS())
			{
				// On MacOSX, PATH is shorter when Eclipse is launched from the
				// Finder than from a terminal. Therefore try to launch bash as a
				// login shell and search using that.
				//
				string w = ReadPipe(UserHome(), new string[] { "bash", "--login", "-c", "which git"
					 }, Encoding.Default.Name());
				//
				//
				if (w == null || w.Length == 0)
				{
					return null;
				}
				FilePath parentFile = new FilePath(w).GetParentFile();
				if (parentFile == null)
				{
					return null;
				}
				return parentFile.GetParentFile();
			}
			return null;
		}

		public override ProcessStartInfo RunInShell(string cmd, string[] args)
		{
			IList<string> argv = new AList<string>(4 + args.Length);
			argv.AddItem("sh");
			argv.AddItem("-c");
			argv.AddItem(cmd + " \"$@\"");
			argv.AddItem(cmd);
			Sharpen.Collections.AddAll(argv, Arrays.AsList(args));
			ProcessStartInfo proc = new ProcessStartInfo();
			proc.SetCommand(argv);
			return proc;
		}

		private static bool IsMacOS()
		{
			string osDotName = AccessController.DoPrivileged(new _PrivilegedAction_95());
			return "Mac OS X".Equals(osDotName);
		}

		private sealed class _PrivilegedAction_95 : PrivilegedAction<string>
		{
			public _PrivilegedAction_95()
			{
			}

			public string Run()
			{
				return Runtime.GetProperty("os.name");
			}
		}
	}
}
