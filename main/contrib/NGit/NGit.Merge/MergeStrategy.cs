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
using System.Collections.Generic;
using NGit;
using NGit.Internal;
using NGit.Merge;
using Sharpen;

namespace NGit.Merge
{
	/// <summary>A method of combining two or more trees together to form an output tree.
	/// 	</summary>
	/// <remarks>
	/// A method of combining two or more trees together to form an output tree.
	/// <p>
	/// Different strategies may employ different techniques for deciding which paths
	/// (and ObjectIds) to carry from the input trees into the final output tree.
	/// </remarks>
	public abstract class MergeStrategy
	{
		/// <summary>Simple strategy that sets the output tree to the first input tree.</summary>
		/// <remarks>Simple strategy that sets the output tree to the first input tree.</remarks>
		public static readonly MergeStrategy OURS = new StrategyOneSided("ours", 0);

		/// <summary>Simple strategy that sets the output tree to the second input tree.</summary>
		/// <remarks>Simple strategy that sets the output tree to the second input tree.</remarks>
		public static readonly MergeStrategy THEIRS = new StrategyOneSided("theirs", 1);

		/// <summary>Simple strategy to merge paths, without simultaneous edits.</summary>
		/// <remarks>Simple strategy to merge paths, without simultaneous edits.</remarks>
		public static readonly ThreeWayMergeStrategy SIMPLE_TWO_WAY_IN_CORE = new StrategySimpleTwoWayInCore
			();

		/// <summary>Simple strategy to merge paths.</summary>
		/// <remarks>Simple strategy to merge paths. It tries to merge also contents. Multiple merge bases are not supported
		/// 	</remarks>
		public static readonly ThreeWayMergeStrategy RESOLVE = new StrategyResolve();

		private static readonly Dictionary<string, MergeStrategy> STRATEGIES = new Dictionary
			<string, MergeStrategy>();

		static MergeStrategy()
		{
			Register(OURS);
			Register(THEIRS);
			Register(SIMPLE_TWO_WAY_IN_CORE);
			Register(RESOLVE);
		}

		/// <summary>Register a merge strategy so it can later be obtained by name.</summary>
		/// <remarks>Register a merge strategy so it can later be obtained by name.</remarks>
		/// <param name="imp">the strategy to register.</param>
		/// <exception cref="System.ArgumentException">a strategy by the same name has already been registered.
		/// 	</exception>
		public static void Register(MergeStrategy imp)
		{
			Register(imp.GetName(), imp);
		}

		/// <summary>Register a merge strategy so it can later be obtained by name.</summary>
		/// <remarks>Register a merge strategy so it can later be obtained by name.</remarks>
		/// <param name="name">name the strategy can be looked up under.</param>
		/// <param name="imp">the strategy to register.</param>
		/// <exception cref="System.ArgumentException">a strategy by the same name has already been registered.
		/// 	</exception>
		public static void Register(string name, MergeStrategy imp)
		{
			lock (typeof(MergeStrategy))
			{
				if (STRATEGIES.ContainsKey(name))
				{
					throw new ArgumentException(MessageFormat.Format(JGitText.Get().mergeStrategyAlreadyExistsAsDefault
						, name));
				}
				STRATEGIES.Put(name, imp);
			}
		}

		/// <summary>Locate a strategy by name.</summary>
		/// <remarks>Locate a strategy by name.</remarks>
		/// <param name="name">name of the strategy to locate.</param>
		/// <returns>the strategy instance; null if no strategy matches the name.</returns>
		public static MergeStrategy Get(string name)
		{
			lock (typeof(MergeStrategy))
			{
				return STRATEGIES.Get(name);
			}
		}

		/// <summary>Get all registered strategies.</summary>
		/// <remarks>Get all registered strategies.</remarks>
		/// <returns>
		/// the registered strategy instances. No inherit order is returned;
		/// the caller may modify (and/or sort) the returned array if
		/// necessary to obtain a reasonable ordering.
		/// </returns>
		public static MergeStrategy[] Get()
		{
			lock (typeof(MergeStrategy))
			{
				MergeStrategy[] r = new MergeStrategy[STRATEGIES.Count];
				Sharpen.Collections.ToArray(STRATEGIES.Values, r);
				return r;
			}
		}

		/// <returns>default name of this strategy implementation.</returns>
		public abstract string GetName();

		/// <summary>Create a new merge instance.</summary>
		/// <remarks>Create a new merge instance.</remarks>
		/// <param name="db">
		/// repository database the merger will read from, and eventually
		/// write results back to.
		/// </param>
		/// <returns>the new merge instance which implements this strategy.</returns>
		public abstract Merger NewMerger(Repository db);

		/// <summary>Create a new merge instance.</summary>
		/// <remarks>Create a new merge instance.</remarks>
		/// <param name="db">
		/// repository database the merger will read from, and eventually
		/// write results back to.
		/// </param>
		/// <param name="inCore">
		/// the merge will happen in memory, working folder will not be
		/// modified, in case of a non-trivial merge that requires manual
		/// resolution, the merger will fail.
		/// </param>
		/// <returns>the new merge instance which implements this strategy.</returns>
		public abstract Merger NewMerger(Repository db, bool inCore);
	}
}
