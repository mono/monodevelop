/*
 * Copyright (C) 2009, Stefan Schake <caytchen@gmail.com>
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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
using GitSharp.Core.FnMatch;

namespace GitSharp.Core
{
	public interface IPattern
	{
		bool IsIgnored(string path);
	}

	public class IgnoreHandler
	{
		private readonly Repository _repo;
		private readonly List<IPattern> _commandLinePatterns = new List<IPattern>();
		private readonly List<IPattern> _excludePatterns = new List<IPattern>();
		private readonly Dictionary<string, List<IPattern>> _directoryPatterns = new Dictionary<string, List<IPattern>>();

		public IgnoreHandler(Repository repo)
		{
			if (repo == null)
			{
				throw new ArgumentNullException("repo");
			}

			_repo = repo;

			try
			{
				string excludeFile = repo.Config.getCore().getExcludesFile();
				if (!string.IsNullOrEmpty(excludeFile))
				{
					ReadPatternsFromFile(Path.Combine(repo.WorkingDirectory.FullName, excludeFile), _excludePatterns);
				}
			}
			catch (Exception)
			{
				//optional
			}

			try
			{
				ReadPatternsFromFile(Path.Combine(repo.Directory.FullName, "info/exclude"), _excludePatterns);
			}
			catch (Exception)
			{
				// optional
			}
		}

		private static List<string> GetPathDirectories(string path)
		{
			if (path.StartsWith("/"))
				path = path.Substring(1);

			// always check our repository directory since path is relative to this
			var ret = new List<string> { "." };

			// this ensures top down
			for (int i = 0; i < path.Length; i++)
			{
				char c = path[i];
				if (c == '/')
					ret.Add(path.Substring(0, i));
			}
			return ret;
		}

		private void LoadDirectoryPatterns(IEnumerable<string> dirs)
		{
			foreach (string p in dirs)
			{
				if (_directoryPatterns.ContainsKey(p))
					continue;

				_directoryPatterns.Add(p, new List<IPattern>());
				string ignorePath = Path.Combine(_repo.WorkingDirectory.FullName, p);
				ignorePath = Path.Combine(ignorePath, Constants.GITIGNORE_FILENAME);
				if (File.Exists(ignorePath))
				{
					ReadPatternsFromFile(ignorePath, _directoryPatterns[p]);
				}
			}
		}

		private static void ReadPatternsFromFile(string path, ICollection<IPattern> to)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException("File not found", path);

			try
			{
				using (var s = new FileStream(path, System.IO.FileMode.Open, FileAccess.Read))
				{
					var reader = new StreamReader(s);
					while (!reader.EndOfStream)
						AddPattern(reader.ReadLine(), to);
				}
			}
			catch (IOException inner)
			{
				throw new InvalidOperationException("Can't read from " + path, inner);
			}
		}

		private static bool IsIgnored(string path, IEnumerable<IPattern> patterns, bool ret)
		{
			// if ret is true, path was marked as ignored by a previous pattern, so only NegatedPatterns can still change this
			if (ret)
			{
				return !patterns.Any(p => (p is NegatedPattern) && p.IsIgnored(path));
			}

			return patterns.Any(p => !(p is NegatedPattern) && p.IsIgnored(path));
		}

		public void AddCommandLinePattern(string pattern)
		{
			AddPattern(pattern, _commandLinePatterns);
		}

		/// <summary>
		/// Evaluate if the given path is ignored. If not yet loaded this loads all .gitignore files on the path and respects them.
		/// </summary>
		/// <param name="path">relative path to a file in the repository</param>
		/// <returns></returns>
		public bool IsIgnored(string path)
		{
			bool ret = false;
			string filename = System.IO.Path.GetFileName(path);
			ret = IsIgnored(filename, _excludePatterns, ret);

			var dirs = GetPathDirectories(path);
			LoadDirectoryPatterns(dirs);

			foreach (string p in dirs)
			{
				ret = IsIgnored(filename, _directoryPatterns[p], ret);
			}

			ret = IsIgnored(filename, _commandLinePatterns, ret);

			return ret;
		}

		private static void AddPattern(string line, ICollection<IPattern> to)
		{
			if (line.Length == 0)
				return;

			// Comment
			if (line.StartsWith("#"))
				return;

			// Negated
			if (line.StartsWith("!"))
			{
				line = line.Substring(1);
				to.Add(new NegatedPattern(new FnMatchPattern(line)));
				return;
			}

			to.Add(new FnMatchPattern(line));
		}

		private class FnMatchPattern : IPattern
		{
			private readonly FileNameMatcher _matcher;

			public FnMatchPattern(string line)
			{
				_matcher = new FileNameMatcher(line, null);
			}

			public bool IsIgnored(string path)
			{
				_matcher.Reset();
				_matcher.Append(path);
				return _matcher.IsMatch();
			}
		}

		private class NegatedPattern : IPattern
		{
			private readonly IPattern _original;

			public NegatedPattern(IPattern pattern)
			{
				_original = pattern;
			}

			public bool IsIgnored(string path)
			{
				return _original.IsIgnored(path);
			}
		}
	}
}