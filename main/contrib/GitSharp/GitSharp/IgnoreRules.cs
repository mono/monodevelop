/*
 * Copyright (C) 2009, Matt DeKrey <mattdekrey@gmail.com>
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
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace GitSharp
{
	public class IgnoreRules
	{
		private struct Rule
		{
			public Regex pattern;
			public bool exclude;
			public bool isDirectoryOnly;
		}

		private IEnumerable<Rule> rules;

		public IgnoreRules(string ignorePath)
			: this(File.ReadAllLines(ignorePath))
		{

		}

		public IgnoreRules(string[] lines)
		{
			List<Rule> rules = new List<Rule>();
			BuildRules(rules, lines);

			this.rules = rules;
		}

		private void BuildRules(List<Rule> rules, string[] lines)
		{
			foreach (string line in lines)
			{
				string workingLine = line.Trim();
				if (workingLine.StartsWith("#") || workingLine.Length == 0)
					continue;

				Rule r;
				r.exclude = !workingLine.StartsWith("!");
				if (!r.exclude)
					workingLine = workingLine.Substring(1);
				r.isDirectoryOnly = !workingLine.Contains(".");

				const string regexCharMatch = @"[^/\\]";
				StringBuilder pattern = new StringBuilder();
				int i = 0;
				if (workingLine[0] == '/')
				{
					pattern.Append("^/");
					i++;
				}
				else
				{
					pattern.Append("/");
				}
				for (; i < workingLine.Length; i++)
				{
					switch (workingLine[i])
					{
						case '?':
							pattern.Append(regexCharMatch).Append("?");
							break;
						case '\\':
							i++;
							pattern.Append("\\");
							break;
						case '*':
							pattern.Append(regexCharMatch).Append("*");
							break;
						case '[':
							for (; i < workingLine.Length && workingLine[i] != ']'; i++)
							{
								if (i == 0 && workingLine[i] == '!')
									pattern.Append("^");
								else
									pattern.Append(workingLine[i]);
							}
							pattern.Append(workingLine[i]);
							break;
						case '.':
							pattern.Append("\\.");
							break;
						default:
							pattern.Append(workingLine[i]);
							break;
					}
				}
				if (!r.isDirectoryOnly)
				{
					pattern.Append("$");
				}
				r.pattern = new System.Text.RegularExpressions.Regex(pattern.ToString());
				rules.Add(r);
			}
		}

		public bool IgnoreDir(string workingDirectory, string fullDirectory)
		{
			string path;
			workingDirectory = workingDirectory.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/');
			path = fullDirectory.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/');

			if (path.StartsWith(workingDirectory))
			{
				path = path.Substring(workingDirectory.Length);
			}
			else
			{
				throw new ArgumentException("fullDirectory must be a subdirectory of workingDirectory", "fullDirectory", null);
			}
			string dirPath = Path.GetDirectoryName(path).Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/');

			bool ignore = false;
			foreach (Rule rule in rules)
			{
				if (rule.exclude != ignore)
				{
					if (rule.isDirectoryOnly && rule.pattern.IsMatch(dirPath))
						ignore = rule.exclude;
				}
			}
			return ignore;
		}

		public bool IgnoreFile(string workingDirectory, string filePath)
		{
			string path;
			workingDirectory = workingDirectory.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/');
			path = filePath.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/');


			if (path.StartsWith(workingDirectory))
			{
				path = path.Substring(workingDirectory.Length);
			}
			else
			{
				throw new ArgumentException("filePath must be a subpath of workingDirectory", "filePath", null);
			}
			string dirPath = Path.GetDirectoryName(path).Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/');

			bool ignore = false;
			foreach (Rule rule in rules)
			{
				if (rule.exclude != ignore)
				{
					if (!rule.isDirectoryOnly && rule.pattern.IsMatch(path))
						ignore = rule.exclude;
				}
			}
			return ignore;
		}
	}
}
