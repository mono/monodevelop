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
	/// <summary>Abstraction to support various file system operations not in Java.</summary>
	/// <remarks>Abstraction to support various file system operations not in Java.</remarks>
	public abstract class FS
	{
		/// <summary>The auto-detected implementation selected for this operating system and JRE.
		/// 	</summary>
		/// <remarks>The auto-detected implementation selected for this operating system and JRE.
		/// 	</remarks>
		public static readonly NGit.Util.FS DETECTED;

		/// <summary>
		/// Auto-detect the appropriate file system abstraction, taking into account
		/// the presence of a Cygwin installation on the system.
		/// </summary>
		/// <remarks>
		/// Auto-detect the appropriate file system abstraction, taking into account
		/// the presence of a Cygwin installation on the system. Using jgit in
		/// combination with Cygwin requires a more elaborate (and possibly slower)
		/// resolution of file system paths.
		/// </remarks>
		/// <param name="cygwinUsed">
		/// <ul>
		/// <li><code>Boolean.TRUE</code> to assume that Cygwin is used in
		/// combination with jgit</li>
		/// <li><code>Boolean.FALSE</code> to assume that Cygwin is
		/// <b>not</b> used with jgit</li>
		/// <li><code>null</code> to auto-detect whether a Cygwin
		/// installation is present on the system and in this case assume
		/// that Cygwin is used</li>
		/// </ul>
		/// Note: this parameter is only relevant on Windows.
		/// </param>
		/// <returns>detected file system abstraction</returns>
		public static NGit.Util.FS Detect(bool? cygwinUsed)
		{
			if (FS_Win32.Detect())
			{
				bool useCygwin = (cygwinUsed == null && FS_Win32_Cygwin.Detect()) || true.Equals(
					cygwinUsed);
				if (useCygwin)
				{
					return new FS_Win32_Cygwin();
				}
				else
				{
					return new FS_Win32();
				}
			}
			else
			{
				if (FS_POSIX_Java6.Detect())
				{
					return new FS_POSIX_Java6();
				}
				else
				{
					return new FS_POSIX_Java5();
				}
			}
		}

		static FS()
		{
			DETECTED = Detect(null);
		}

		private readonly FilePath userHome;

		/// <summary>Constructs a file system abstraction.</summary>
		/// <remarks>Constructs a file system abstraction.</remarks>
		public FS()
		{
			this.userHome = UserHomeImpl();
		}

		/// <summary>Does this operating system and JRE support the execute flag on files?</summary>
		/// <returns>
		/// true if this implementation can provide reasonably accurate
		/// executable bit information; false otherwise.
		/// </returns>
		public abstract bool SupportsExecute();

		/// <summary>Determine if the file is executable (or not).</summary>
		/// <remarks>
		/// Determine if the file is executable (or not).
		/// <p>
		/// Not all platforms and JREs support executable flags on files. If the
		/// feature is unsupported this method will always return false.
		/// </remarks>
		/// <param name="f">abstract path to test.</param>
		/// <returns>true if the file is believed to be executable by the user.</returns>
		public abstract bool CanExecute(FilePath f);

		/// <summary>Set a file to be executable by the user.</summary>
		/// <remarks>
		/// Set a file to be executable by the user.
		/// <p>
		/// Not all platforms and JREs support executable flags on files. If the
		/// feature is unsupported this method will always return false and no
		/// changes will be made to the file specified.
		/// </remarks>
		/// <param name="f">path to modify the executable status of.</param>
		/// <param name="canExec">true to enable execution; false to disable it.</param>
		/// <returns>true if the change succeeded; false otherwise.</returns>
		public abstract bool SetExecute(FilePath f, bool canExec);

		/// <summary>Resolve this file to its actual path name that the JRE can use.</summary>
		/// <remarks>
		/// Resolve this file to its actual path name that the JRE can use.
		/// <p>
		/// This method can be relatively expensive. Computing a translation may
		/// require forking an external process per path name translated. Callers
		/// should try to minimize the number of translations necessary by caching
		/// the results.
		/// <p>
		/// Not all platforms and JREs require path name translation. Currently only
		/// Cygwin on Win32 require translation for Cygwin based paths.
		/// </remarks>
		/// <param name="dir">directory relative to which the path name is.</param>
		/// <param name="name">path name to translate.</param>
		/// <returns>
		/// the translated path. <code>new File(dir,name)</code> if this
		/// platform does not require path name translation.
		/// </returns>
		public virtual FilePath Resolve(FilePath dir, string name)
		{
			FilePath abspn = new FilePath(name);
			if (abspn.IsAbsolute())
			{
				return abspn;
			}
			return new FilePath(dir, name);
		}

		/// <summary>Determine the user's home directory (location where preferences are).</summary>
		/// <remarks>
		/// Determine the user's home directory (location where preferences are).
		/// <p>
		/// This method can be expensive on the first invocation if path name
		/// translation is required. Subsequent invocations return a cached result.
		/// <p>
		/// Not all platforms and JREs require path name translation. Currently only
		/// Cygwin on Win32 requires translation of the Cygwin HOME directory.
		/// </remarks>
		/// <returns>the user's home directory; null if the user does not have one.</returns>
		public virtual FilePath UserHome()
		{
			return userHome;
		}

		/// <summary>Does this file system have problems with atomic renames?</summary>
		/// <returns>true if the caller should retry a failed rename of a lock file.</returns>
		public abstract bool RetryFailedLockFileCommit();

		/// <summary>Determine the user's home directory (location where preferences are).</summary>
		/// <remarks>Determine the user's home directory (location where preferences are).</remarks>
		/// <returns>the user's home directory; null if the user does not have one.</returns>
		protected internal virtual FilePath UserHomeImpl()
		{
			string home = AccessController.DoPrivileged(new _PrivilegedAction_196());
			if (home == null || home.Length == 0)
			{
				return null;
			}
			return new FilePath(home).GetAbsoluteFile();
		}

		private sealed class _PrivilegedAction_196 : PrivilegedAction<string>
		{
			public _PrivilegedAction_196()
			{
			}

			public string Run()
			{
				return Runtime.GetProperty("user.home");
			}
		}

		internal static FilePath SearchPath(string path, params string[] lookFor)
		{
			foreach (string p in path.Split(FilePath.pathSeparator))
			{
				foreach (string command in lookFor)
				{
					FilePath e = new FilePath(p, command);
					if (e.IsFile())
					{
						return e.GetAbsoluteFile();
					}
				}
			}
			return null;
		}

		/// <summary>Execute a command and return a single line of output as a String</summary>
		/// <param name="dir">Working directory for the command</param>
		/// <param name="command">as component array</param>
		/// <param name="encoding"></param>
		/// <returns>the one-line output of the command</returns>
		protected internal static string ReadPipe(FilePath dir, string[] command, string 
			encoding)
		{
			try
			{
				Process p = Runtime.GetRuntime().Exec(command, null, dir);
				BufferedReader lineRead = new BufferedReader(new InputStreamReader(p.GetInputStream
					(), encoding));
				string r = null;
				try
				{
					r = lineRead.ReadLine();
				}
				finally
				{
					p.GetOutputStream().Close();
					p.GetErrorStream().Close();
					lineRead.Close();
				}
				for (; ; )
				{
					try
					{
						if (p.WaitFor() == 0 && r != null && r.Length > 0)
						{
							return r;
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
			// ignore
			return null;
		}

		/// <returns>the $prefix directory C Git would use.</returns>
		public abstract FilePath GitPrefix();
	}
}
