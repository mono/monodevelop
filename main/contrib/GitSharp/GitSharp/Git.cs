/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using GitSharp.Commands;

namespace GitSharp
{
	/// <summary>
	/// The static class Git provides everything to interact with git itself, such as the command line interface commands, the git configuration or properties that are affecting git globally.
	/// </summary>
	public static class Git
	{

		#region Version


		/// <summary>
		/// Returns the version of GitSharp.
		/// </summary>
		public static string Version
		{
			get
			{
				Assembly assembly = Assembly.Load("GitSharp");

				Version version = assembly.GetName().Version;
				if (version == null)
					return null;

				object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (attributes.Length == 0)
				{
					// No AssemblyProduct attribute to parse, no commit hash to extract
					return version.ToString();
				}

				string commitHash = ExtractCommitHashFrom(((AssemblyProductAttribute)attributes[0]).Product);
				return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, commitHash);
			}
		}

		private static string ExtractCommitHashFrom(string product)
		{
			// TODO: Maybe should we switch to a regEx capture ?
			string[] parts = product.Split(new[] { '[' });
			return parts[1].TrimEnd(']');
		}


		#endregion

		#region Defaults for git commands


		/// <summary>
		/// Get or set the default output stream that all git commands are writing to. Per default this returns a StreamWriter wrapping the standard output stream.
		/// By setting your own Streamwriter one can capture the output of the commands.
		/// </summary>
		public static StreamWriter DefaultOutputStream
		{
			get
			{
				if (_output == null)
				{
					_output = new StreamWriter(Console.OpenStandardOutput());
					Console.SetOut(_output);
				}
				return _output;
			}
			set
			{
				_output = value;
			}
		}
		private static StreamWriter _output;

		/// <summary>
		/// Get or set the default git repository for all commands. A command can override this by
		/// setting it's own Repository property.
		/// 
		/// Note: Init and Clone do not respect Repository since they create a Repository as a result of Execute.
		/// </summary>
		public static Repository DefaultRepository { get; set; }

		/// <summary>
		/// Get or set the default git directory for all commands. A command can override this, however, 
		/// by setting its own GitDirectory property.
		/// </summary>
		public static string DefaultGitDirectory { get; set; }


		#endregion

		#region Clone

		/// <summary>
		/// Clone a repository and checkout the working directory.
		/// </summary>
		/// <param name="fromUrl"></param>
		/// <param name="toPath"></param>
		/// <returns></returns>
		public static Repository Clone(string fromUrl, string toPath)
		{
			bool bare = false;
			return Clone(fromUrl, toPath, bare);
		}

		/// <summary>
		/// Clone a repository and checkout the working directory only if bare == false
		/// </summary>
		/// <param name="fromUrl"></param>
		/// <param name="toPath"></param>
		/// <param name="bare"></param>
		/// <returns></returns>
		public static Repository Clone(string fromUrl, string toPath, bool bare)
		{
			CloneCommand cmd = new CloneCommand()
			{
				Source = fromUrl,
				Directory = toPath,
				Bare = bare,
			};
			return Clone(cmd);
		}

		public static Repository Clone(CloneCommand command)
		{
			command.Execute();
			return command.Repository;
		}


		#endregion

		#region Init


		public static void Init(string path)
		{
			Repository.Init(path);
		}

		public static void Init(string path, bool bare)
		{
			Repository.Init(path, bare);
		}

		public static void Init(InitCommand command)
		{
			command.Execute();
		}


		#endregion

		#region Merge


		public static MergeResult Merge(MergeOptions options)
		{
			return MergeCommand.Execute(options);
		}


		#endregion

		#region Status

		public static StatusResults Status(StatusCommand command)
		{
			//Populate the command with the status results
			command.Execute();
			return command.Results;
		}

		#endregion

	}
}
