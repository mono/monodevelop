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
		public static readonly NGit.Util.FS DETECTED = Detect();

		/// <summary>Auto-detect the appropriate file system abstraction.</summary>
		/// <remarks>Auto-detect the appropriate file system abstraction.</remarks>
		/// <returns>detected file system abstraction</returns>
		public static NGit.Util.FS Detect()
		{
			return Detect(null);
		}

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
			if (FS_Win32.IsWin32())
			{
				if (cygwinUsed == null)
				{
					cygwinUsed = Sharpen.Extensions.ValueOf(FS_Win32_Cygwin.IsCygwin());
				}
				if (cygwinUsed.Value)
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
				if (FS_POSIX_Java6.HasExecute())
				{
					return new FS_POSIX_Java6();
				}
				else
				{
					return new FS_POSIX_Java5();
				}
			}
		}

		private volatile FS.Holder<FilePath> userHome;

		private volatile FS.Holder<FilePath> gitPrefix;

		/// <summary>Constructs a file system abstraction.</summary>
		/// <remarks>Constructs a file system abstraction.</remarks>
		public FS()
		{
		}

		/// <summary>Initialize this FS using another's current settings.</summary>
		/// <remarks>Initialize this FS using another's current settings.</remarks>
		/// <param name="src">the source FS to copy from.</param>
		protected internal FS(NGit.Util.FS src)
		{
			// Do nothing by default.
			userHome = src.userHome;
			gitPrefix = src.gitPrefix;
		}

		/// <returns>a new instance of the same type of FS.</returns>
		public abstract NGit.Util.FS NewInstance();

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
			FS.Holder<FilePath> p = userHome;
			if (p == null)
			{
				p = new FS.Holder<FilePath>(UserHomeImpl());
				userHome = p;
			}
			return p.value;
		}

		/// <summary>Set the user's home directory location.</summary>
		/// <remarks>Set the user's home directory location.</remarks>
		/// <param name="path">
		/// the location of the user's preferences; null if there is no
		/// home directory for the current user.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// .
		/// </returns>
		public virtual NGit.Util.FS SetUserHome(FilePath path)
		{
			userHome = new FS.Holder<FilePath>(path);
			return this;
		}

		/// <summary>Does this file system have problems with atomic renames?</summary>
		/// <returns>true if the caller should retry a failed rename of a lock file.</returns>
		public abstract bool RetryFailedLockFileCommit();

		/// <summary>Determine the user's home directory (location where preferences are).</summary>
		/// <remarks>Determine the user's home directory (location where preferences are).</remarks>
		/// <returns>the user's home directory; null if the user does not have one.</returns>
		protected internal virtual FilePath UserHomeImpl()
		{
			string home = AccessController.DoPrivileged(new _PrivilegedAction_237());
			if (home == null || home.Length == 0)
			{
				return null;
			}
			return new FilePath(home).GetAbsoluteFile();
		}

		private sealed class _PrivilegedAction_237 : PrivilegedAction<string>
		{
			public _PrivilegedAction_237()
			{
			}

			public string Run()
			{
				return Runtime.GetProperty("user.home");
			}
		}

		/// <summary>Searches the given path to see if it contains one of the given files.</summary>
		/// <remarks>
		/// Searches the given path to see if it contains one of the given files.
		/// Returns the first it finds. Returns null if not found or if path is null.
		/// </remarks>
		/// <param name="path">List of paths to search separated by File.pathSeparator</param>
		/// <param name="lookFor">Files to search for in the given path</param>
		/// <returns>the first match found, or null</returns>
		internal static FilePath SearchPath(string path, params string[] lookFor)
		{
			if (path == null)
			{
				return null;
			}
			foreach (string p in path.Split(FilePath.pathSeparator))
			{
				foreach (string command in lookFor)
				{
					try {
						FilePath e = new FilePath(p, command);
						if (e.IsFile())
						{
							return e.GetAbsoluteFile();
						}
					} catch {
						Console.WriteLine ("NGit Error: Could not combine: {0} and {1}", p, command);
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
			bool debug = System.Boolean.Parse(SystemReader.GetInstance().GetProperty("jgit.fs.debug"
				));
			try
			{
				if (debug)
				{
					System.Console.Error.WriteLine("readpipe " + Arrays.AsList(command) + "," + dir);
				}
				SystemProcess p = Runtime.GetRuntime().Exec(command, null, dir);
				BufferedReader lineRead = new BufferedReader(new InputStreamReader(p.GetInputStream
					(), encoding));
				p.GetOutputStream().Close();
				AtomicBoolean gooblerFail = new AtomicBoolean(false);
				Sharpen.Thread gobbler = new _Thread_293(p, debug, gooblerFail);
				// ignore
				// Just print on stderr for debugging
				// Just print on stderr for debugging
				gobbler.Start();
				string r = null;
				try
				{
					r = lineRead.ReadLine();
					if (debug)
					{
						System.Console.Error.WriteLine("readpipe may return '" + r + "'");
						System.Console.Error.WriteLine("(ignoring remaing output:");
					}
					string l;
					while ((l = lineRead.ReadLine()) != null)
					{
						if (debug)
						{
							System.Console.Error.WriteLine(l);
						}
					}
				}
				finally
				{
					p.GetErrorStream().Close();
					lineRead.Close();
				}
				for (; ; )
				{
					try
					{
						int rc = p.WaitFor();
						gobbler.Join();
						if (rc == 0 && r != null && r.Length > 0 && !gooblerFail.Get())
						{
							return r;
						}
						if (debug)
						{
							System.Console.Error.WriteLine("readpipe rc=" + rc);
						}
						break;
					}
					catch (Exception)
					{
					}
				}
			}
			catch (IOException e)
			{
				// Stop bothering me, I have a zombie to reap.
				if (debug)
				{
					System.Console.Error.WriteLine(e);
				}
			}
			// Ignore error (but report)
			if (debug)
			{
				System.Console.Error.WriteLine("readpipe returns null");
			}
			return null;
		}

		private sealed class _Thread_293 : Sharpen.Thread
		{
			public _Thread_293(SystemProcess p, bool debug, AtomicBoolean gooblerFail)
			{
				this.p = p;
				this.debug = debug;
				this.gooblerFail = gooblerFail;
			}

			public override void Run()
			{
				InputStream @is = p.GetErrorStream();
				try
				{
					int ch;
					if (debug)
					{
						while ((ch = @is.Read()) != -1)
						{
							System.Console.Error.Write((char)ch);
						}
					}
					else
					{
						while (@is.Read() != -1)
						{
						}
					}
				}
				catch (IOException e)
				{
					if (debug)
					{
						Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
					}
					gooblerFail.Set(true);
				}
				try
				{
					@is.Close();
				}
				catch (IOException e)
				{
					if (debug)
					{
						Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
					}
					gooblerFail.Set(true);
				}
			}

			private readonly SystemProcess p;

			private readonly bool debug;

			private readonly AtomicBoolean gooblerFail;
		}

		/// <returns>the $prefix directory C Git would use.</returns>
		public virtual FilePath GitPrefix()
		{
			FS.Holder<FilePath> p = gitPrefix;
			if (p == null)
			{
				string overrideGitPrefix = SystemReader.GetInstance().GetProperty("jgit.gitprefix"
					);
				if (overrideGitPrefix != null)
				{
					p = new FS.Holder<FilePath>(new FilePath(overrideGitPrefix));
				}
				else
				{
					p = new FS.Holder<FilePath>(DiscoverGitPrefix());
				}
				gitPrefix = p;
			}
			return p.value;
		}

		/// <returns>the $prefix directory C Git would use.</returns>
		protected internal abstract FilePath DiscoverGitPrefix();

		/// <summary>Set the $prefix directory C Git uses.</summary>
		/// <remarks>Set the $prefix directory C Git uses.</remarks>
		/// <param name="path">the directory. Null if C Git is not installed.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Util.FS SetGitPrefix(FilePath path)
		{
			gitPrefix = new FS.Holder<FilePath>(path);
			return this;
		}

		/// <summary>Initialize a ProcesssBuilder to run a command using the system shell.</summary>
		/// <remarks>Initialize a ProcesssBuilder to run a command using the system shell.</remarks>
		/// <param name="cmd">
		/// command to execute. This string should originate from the
		/// end-user, and thus is platform specific.
		/// </param>
		/// <param name="args">
		/// arguments to pass to command. These should be protected from
		/// shell evaluation.
		/// </param>
		/// <returns>
		/// a partially completed process builder. Caller should finish
		/// populating directory, environment, and then start the process.
		/// </returns>
		public abstract ProcessStartInfo RunInShell(string cmd, string[] args);

		private class Holder<V>
		{
			internal readonly V value;

			internal Holder(V value)
			{
				this.value = value;
			}
		}
	}
}
