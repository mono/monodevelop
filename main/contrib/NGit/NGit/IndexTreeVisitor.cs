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
	/// <summary>Visitor interface for traversing the index and two trees in parallel.</summary>
	/// <remarks>
	/// Visitor interface for traversing the index and two trees in parallel.
	/// When merging we deal with up to two tree nodes and a base node. Then
	/// we figure out what to do.
	/// A File argument is supplied to allow us to check for modifications in
	/// a work tree or update the file.
	/// </remarks>
	[System.ObsoleteAttribute(@"Use NGit.Treewalk.TreeWalk instead, with a NGit.Dircache.DirCacheIterator as a member."
		)]
	public interface IndexTreeVisitor
	{
		/// <summary>Visit a blob, and corresponding tree and index entries.</summary>
		/// <remarks>Visit a blob, and corresponding tree and index entries.</remarks>
		/// <param name="treeEntry"></param>
		/// <param name="indexEntry"></param>
		/// <param name="file"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		void VisitEntry(TreeEntry treeEntry, GitIndex.Entry indexEntry, FilePath file);

		/// <summary>Visit a blob, and corresponding tree nodes and associated index entry.</summary>
		/// <remarks>Visit a blob, and corresponding tree nodes and associated index entry.</remarks>
		/// <param name="treeEntry"></param>
		/// <param name="auxEntry"></param>
		/// <param name="indexEntry"></param>
		/// <param name="file"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		void VisitEntry(TreeEntry treeEntry, TreeEntry auxEntry, GitIndex.Entry indexEntry
			, FilePath file);

		/// <summary>Invoked after handling all child nodes of a tree, during a three way merge
		/// 	</summary>
		/// <param name="tree"></param>
		/// <param name="auxTree"></param>
		/// <param name="curDir"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		void FinishVisitTree(Tree tree, Tree auxTree, string curDir);

		/// <summary>Invoked after handling all child nodes of a tree, during two way merge.</summary>
		/// <remarks>Invoked after handling all child nodes of a tree, during two way merge.</remarks>
		/// <param name="tree"></param>
		/// <param name="i"></param>
		/// <param name="curDir"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		void FinishVisitTree(Tree tree, int i, string curDir);
	}
}
