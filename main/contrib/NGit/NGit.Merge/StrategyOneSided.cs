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
using NGit.Merge;
using Sharpen;

namespace NGit.Merge
{
	/// <summary>Trivial merge strategy to make the resulting tree exactly match an input.
	/// 	</summary>
	/// <remarks>
	/// Trivial merge strategy to make the resulting tree exactly match an input.
	/// <p>
	/// This strategy can be used to cauterize an entire side branch of history, by
	/// setting the output tree to one of the inputs, and ignoring any of the paths
	/// of the other inputs.
	/// </remarks>
	public class StrategyOneSided : MergeStrategy
	{
		private readonly string strategyName;

		private readonly int treeIndex;

		/// <summary>Create a new merge strategy to select a specific input tree.</summary>
		/// <remarks>Create a new merge strategy to select a specific input tree.</remarks>
		/// <param name="name">name of this strategy.</param>
		/// <param name="index">the position of the input tree to accept as the result.</param>
		protected internal StrategyOneSided(string name, int index)
		{
			strategyName = name;
			treeIndex = index;
		}

		public override string GetName()
		{
			return strategyName;
		}

		public override Merger NewMerger(Repository db)
		{
			return new StrategyOneSided.OneSide(db, treeIndex);
		}

		public override Merger NewMerger(Repository db, bool inCore)
		{
			return new StrategyOneSided.OneSide(db, treeIndex);
		}

		internal class OneSide : Merger
		{
			private readonly int treeIndex;

			protected internal OneSide(Repository local, int index) : base(local)
			{
				treeIndex = index;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override bool MergeImpl()
			{
				return treeIndex < sourceTrees.Length;
			}

			public override ObjectId GetResultTreeId()
			{
				return sourceTrees[treeIndex];
			}
		}
	}
}
