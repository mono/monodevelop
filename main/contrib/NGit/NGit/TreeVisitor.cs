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

using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// A TreeVisitor is invoked depth first for every node in a tree and is expected
	/// to perform different actions.
	/// </summary>
	/// <remarks>
	/// A TreeVisitor is invoked depth first for every node in a tree and is expected
	/// to perform different actions.
	/// </remarks>
	[System.ObsoleteAttribute(@"Use  instead.")]
	public interface TreeVisitor
	{
		/// <summary>Visit to a tree node before child nodes are visited.</summary>
		/// <remarks>Visit to a tree node before child nodes are visited.</remarks>
		/// <param name="t">Tree</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		void StartVisitTree(Tree t);

		/// <summary>Visit to a tree node.</summary>
		/// <remarks>Visit to a tree node. after child nodes have been visited.</remarks>
		/// <param name="t">Tree</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		void EndVisitTree(Tree t);

		/// <summary>Visit to a blob.</summary>
		/// <remarks>Visit to a blob.</remarks>
		/// <param name="f">Blob</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		void VisitFile(FileTreeEntry f);

		/// <summary>Visit to a symlink.</summary>
		/// <remarks>Visit to a symlink.</remarks>
		/// <param name="s">Symlink entry</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		void VisitSymlink(SymlinkTreeEntry s);

		/// <summary>Visit to a gitlink.</summary>
		/// <remarks>Visit to a gitlink.</remarks>
		/// <param name="s">Gitlink entry</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		void VisitGitlink(GitlinkTreeEntry s);
	}
}
