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
using NGit;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Treewalk.Filter
{
	/// <summary>Includes tree entries only if they match the configured path.</summary>
	/// <remarks>
	/// Includes tree entries only if they match the configured path.
	/// <p>
	/// Applications should use
	/// <see cref="PathFilterGroup">PathFilterGroup</see>
	/// to connect these into a tree
	/// filter graph, as the group supports breaking out of traversal once it is
	/// known the path can never match.
	/// </remarks>
	public class PathFilter : TreeFilter
	{
		/// <summary>Create a new tree filter for a user supplied path.</summary>
		/// <remarks>
		/// Create a new tree filter for a user supplied path.
		/// <p>
		/// Path strings are relative to the root of the repository. If the user's
		/// input should be assumed relative to a subdirectory of the repository the
		/// caller must prepend the subdirectory's path prior to creating the filter.
		/// <p>
		/// Path strings use '/' to delimit directories on all platforms.
		/// </remarks>
		/// <param name="path">
		/// the path to filter on. Must not be the empty string. All
		/// trailing '/' characters will be trimmed before string's length
		/// is checked or is used as part of the constructed filter.
		/// </param>
		/// <returns>a new filter for the requested path.</returns>
		/// <exception cref="System.ArgumentException">the path supplied was the empty string.
		/// 	</exception>
		public static NGit.Treewalk.Filter.PathFilter Create(string path)
		{
			while (path.EndsWith("/"))
			{
				path = Sharpen.Runtime.Substring(path, 0, path.Length - 1);
			}
			if (path.Length == 0)
			{
				throw new ArgumentException(JGitText.Get().emptyPathNotPermitted);
			}
			return new NGit.Treewalk.Filter.PathFilter(path);
		}

		internal readonly string pathStr;

		internal readonly byte[] pathRaw;

		private PathFilter(string s)
		{
			pathStr = s;
			pathRaw = Constants.Encode(pathStr);
		}

		/// <returns>the path this filter matches.</returns>
		public virtual string GetPath()
		{
			return pathStr;
		}

		public override bool Include(TreeWalk walker)
		{
			return walker.IsPathPrefix(pathRaw, pathRaw.Length) == 0;
		}

		public override bool ShouldBeRecursive()
		{
			foreach (byte b in pathRaw)
			{
				if (b == '/')
				{
					return true;
				}
			}
			return false;
		}

		public override TreeFilter Clone()
		{
			return this;
		}

		public override string ToString()
		{
			return "PATH(\"" + pathStr + "\")";
		}
	}
}
