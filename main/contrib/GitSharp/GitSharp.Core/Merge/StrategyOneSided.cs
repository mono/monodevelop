/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Dan Rigby <dan@danrigby.com>
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

namespace GitSharp.Core.Merge
{
	/// <summary>
	/// Trivial merge strategy to make the resulting tree exactly match an input.
	/// <para />
	/// This strategy can be used to cauterize an entire side branch of history, by
	/// setting the output tree to one of the inputs, and ignoring any of the paths
	/// of the other inputs.
	/// </summary>
	public class StrategyOneSided : MergeStrategy
	{
		private readonly string _strategyName;
		private readonly int _treeIndex;

		///	<summary>
		/// Create a new merge strategy to select a specific input tree.
		/// </summary>
		/// <param name="name">name of this strategy.</param>
		/// <param name="index">
		/// the position of the input tree to accept as the result.
		/// </param>
		public StrategyOneSided(string name, int index)
		{
			_strategyName = name;
			_treeIndex = index;
		}

		public override string Name
		{
			get { return _strategyName; }
		}

		public override Merger NewMerger(Repository db)
		{
			return new OneSide(db, _treeIndex);
		}

		#region Nested Types

		private class OneSide : Merger
		{
			private readonly int _treeIndex;

			public OneSide(Repository local, int index)
				: base(local)
			{
				_treeIndex = index;
			}

			protected override bool MergeImpl()
			{
				return _treeIndex < SourceTrees.Length;
			}

			public override ObjectId GetResultTreeId()
			{
				return SourceTrees[_treeIndex];
			}
		}

		#endregion

	}
}