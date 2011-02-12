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

using System.Text;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	internal class FS_Win32 : FS
	{
		internal static bool Detect()
		{
			string osDotName = AccessController.DoPrivileged(new _PrivilegedAction_55());
			return osDotName != null && StringUtils.ToLowerCase(osDotName).IndexOf("windows")
				 != -1;
		}

		private sealed class _PrivilegedAction_55 : PrivilegedAction<string>
		{
			public _PrivilegedAction_55()
			{
			}

			public string Run()
			{
				return Runtime.GetProperty("os.name");
			}
		}

		public override bool SupportsExecute()
		{
			return false;
		}

		public override bool CanExecute(FilePath f)
		{
			return false;
		}

		public override bool SetExecute(FilePath f, bool canExec)
		{
			return false;
		}

		public override bool RetryFailedLockFileCommit()
		{
			return true;
		}

		public override FilePath GitPrefix()
		{
			string path = SystemReader.GetInstance().Getenv("PATH");
			FilePath gitExe = SearchPath(path, "git.exe", "git.cmd");
			if (gitExe != null)
			{
				return gitExe.GetParentFile().GetParentFile();
			}
			// This isn't likely to work, if bash is in $PATH, git should
			// also be in $PATH. But its worth trying.
			//
			string w = ReadPipe(UserHome(), new string[] { "bash", "--login", "-c", "which git"
				 }, Encoding.Default.Name());
			//
			//
			if (w != null)
			{
				return new FilePath(w).GetParentFile().GetParentFile();
			}
			return null;
		}

		protected internal override FilePath UserHomeImpl()
		{
			string home = SystemReader.GetInstance().Getenv("HOME");
			if (home != null)
			{
				return Resolve(null, home);
			}
			string homeDrive = SystemReader.GetInstance().Getenv("HOMEDRIVE");
			if (homeDrive != null)
			{
				string homePath = SystemReader.GetInstance().Getenv("HOMEPATH");
				return new FilePath(homeDrive, homePath);
			}
			string homeShare = SystemReader.GetInstance().Getenv("HOMESHARE");
			if (homeShare != null)
			{
				return new FilePath(homeShare);
			}
			return base.UserHomeImpl();
		}
	}
}
