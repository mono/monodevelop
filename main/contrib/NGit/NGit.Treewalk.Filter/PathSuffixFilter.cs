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
	/// <remarks>Includes tree entries only if they match the configured path.</remarks>
	public class PathSuffixFilter : TreeFilter
	{
		/// <summary>Create a new tree filter for a user supplied path.</summary>
		/// <remarks>
		/// Create a new tree filter for a user supplied path.
		/// <p>
		/// Path strings use '/' to delimit directories on all platforms.
		/// </remarks>
		/// <param name="path">the path (suffix) to filter on. Must not be the empty string.</param>
		/// <returns>a new filter for the requested path.</returns>
		/// <exception cref="System.ArgumentException">the path supplied was the empty string.
		/// 	</exception>
		public static NGit.Treewalk.Filter.PathSuffixFilter Create(string path)
		{
			if (path.Length == 0)
			{
				throw new ArgumentException(JGitText.Get().emptyPathNotPermitted);
			}
			return new NGit.Treewalk.Filter.PathSuffixFilter(path);
		}

		internal readonly string pathStr;

		internal readonly byte[] pathRaw;

		private PathSuffixFilter(string s)
		{
			pathStr = s;
			pathRaw = Constants.Encode(pathStr);
		}

		public override TreeFilter Clone()
		{
			return this;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override bool Include(TreeWalk walker)
		{
			if (walker.IsSubtree)
			{
				return true;
			}
			else
			{
				return walker.IsPathSuffix(pathRaw, pathRaw.Length);
			}
		}

		public override bool ShouldBeRecursive()
		{
			return true;
		}
	}
}
