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

using NGit.Errors;
using NGit.Fnmatch;
using Sharpen;

namespace NGit.Ignore
{
	/// <summary>
	/// A single ignore rule corresponding to one line in a .gitignore or
	/// ignore file.
	/// </summary>
	/// <remarks>
	/// A single ignore rule corresponding to one line in a .gitignore or
	/// ignore file. Parses the ignore pattern
	/// Inspiration from: Ferry Huberts
	/// </remarks>
	public class IgnoreRule
	{
		private string pattern;

		private bool negation;

		private bool nameOnly;

		private bool dirOnly;

		private FileNameMatcher matcher;

		/// <summary>Create a new ignore rule with the given pattern.</summary>
		/// <remarks>
		/// Create a new ignore rule with the given pattern. Assumes that
		/// the pattern is already trimmed.
		/// </remarks>
		/// <param name="pattern">
		/// Base pattern for the ignore rule. This pattern will
		/// be parsed to generate rule parameters.
		/// </param>
		public IgnoreRule(string pattern)
		{
			this.pattern = pattern;
			negation = false;
			nameOnly = false;
			dirOnly = false;
			matcher = null;
			Setup();
		}

		/// <summary>Remove leading/trailing characters as needed.</summary>
		/// <remarks>
		/// Remove leading/trailing characters as needed. Set up
		/// rule variables for later matching.
		/// </remarks>
		private void Setup()
		{
			int startIndex = 0;
			int endIndex = pattern.Length;
			if (pattern.StartsWith("!"))
			{
				startIndex++;
				negation = true;
			}
			if (pattern.EndsWith("/"))
			{
				endIndex--;
				dirOnly = true;
			}
			pattern = Sharpen.Runtime.Substring(pattern, startIndex, endIndex);
			bool hasSlash = pattern.Contains("/");
			if (!hasSlash)
			{
				nameOnly = true;
			}
			else
			{
				if (!pattern.StartsWith("/"))
				{
					//Contains "/" but does not start with one
					//Adding / to the start should not interfere with matching
					pattern = "/" + pattern;
				}
			}
			if (pattern.Contains("*") || pattern.Contains("?") || pattern.Contains("["))
			{
				try
				{
					matcher = new FileNameMatcher(pattern, '/');
				}
				catch (InvalidPatternException)
				{
				}
			}
		}

		// Ignore pattern exceptions
		/// <returns>True if the pattern is just a file name and not a path</returns>
		public virtual bool GetNameOnly()
		{
			return nameOnly;
		}

		/// <returns>True if the pattern should match directories only</returns>
		public virtual bool DirOnly()
		{
			return dirOnly;
		}

		/// <returns>True if the pattern had a "!" in front of it</returns>
		public virtual bool GetNegation()
		{
			return negation;
		}

		/// <returns>The blob pattern to be used as a matcher</returns>
		public virtual string GetPattern()
		{
			return pattern;
		}

		/// <summary>Returns true if a match was made.</summary>
		/// <remarks>
		/// Returns true if a match was made.
		/// <br />
		/// This function does NOT return the actual ignore status of the
		/// target! Please consult
		/// <see cref="GetResult()">GetResult()</see>
		/// for the ignore status. The actual
		/// ignore status may be true or false depending on whether this rule is
		/// an ignore rule or a negation rule.
		/// </remarks>
		/// <param name="target">Name pattern of the file, relative to the base directory of this rule
		/// 	</param>
		/// <param name="isDirectory">Whether the target file is a directory or not</param>
		/// <returns>
		/// True if a match was made. This does not necessarily mean that
		/// the target is ignored. Call
		/// <see cref="GetResult()">getResult()</see>
		/// for the result.
		/// </returns>
		public virtual bool IsMatch(string target, bool isDirectory)
		{
			if (!target.StartsWith("/"))
			{
				target = "/" + target;
			}
			if (matcher == null)
			{
				if (target.Equals(pattern))
				{
					//Exact match
					if (dirOnly && !isDirectory)
					{
						//Directory expectations not met
						return false;
					}
					else
					{
						//Directory expectations met
						return true;
					}
				}
				if ((target).StartsWith(pattern + "/"))
				{
					return true;
				}
				if (nameOnly)
				{
					//Iterate through each sub-name
					string[] segments = target.Split("/");
					for (int idx = 0; idx < segments.Length; idx++)
					{
						string segmentName = segments[idx];
						if (segmentName.Equals(pattern) && DoesMatchDirectoryExpectations(isDirectory, idx
							, segments.Length))
						{
							return true;
						}
					}
				}
			}
			else
			{
				matcher.Append(target);
				if (matcher.IsMatch())
				{
					return true;
				}
				string[] segments = target.Split("/");
				if (nameOnly)
				{
					for (int idx = 0; idx < segments.Length; idx++)
					{
						string segmentName = segments[idx];
						//Iterate through each sub-directory
						matcher.Reset();
						matcher.Append(segmentName);
						if (matcher.IsMatch() && DoesMatchDirectoryExpectations(isDirectory, idx, segments
							.Length))
						{
							return true;
						}
					}
				}
				else
				{
					//TODO: This is the slowest operation
					//This matches e.g. "/src/ne?" to "/src/new/file.c"
					matcher.Reset();
					for (int idx = 0; idx < segments.Length; idx++)
					{
						string segmentName = segments[idx];
						if (segmentName.Length > 0)
						{
							matcher.Append("/" + segmentName);
						}
						if (matcher.IsMatch() && DoesMatchDirectoryExpectations(isDirectory, idx, segments
							.Length))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// If a call to <code>isMatch(String, boolean)</code> was previously
		/// made, this will return whether or not the target was ignored.
		/// </summary>
		/// <remarks>
		/// If a call to <code>isMatch(String, boolean)</code> was previously
		/// made, this will return whether or not the target was ignored. Otherwise
		/// this just indicates whether the rule is non-negation or negation.
		/// </remarks>
		/// <returns>True if the target is to be ignored, false otherwise.</returns>
		public virtual bool GetResult()
		{
			return !negation;
		}

		private bool DoesMatchDirectoryExpectations(bool isDirectory, int segmentIdx, int
			 segmentLength)
		{
			// The segment we are checking is a directory, expectations are met.
			if (segmentIdx < segmentLength - 1)
			{
				return true;
			}
			// We are checking the last part of the segment for which isDirectory has to be considered.
			return !dirOnly || isDirectory;
		}
	}
}
