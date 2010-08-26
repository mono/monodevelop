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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GitSharp.Core.Merge
{
	/// <summary>
	/// A method of combining two or more trees together to form an output tree.
	/// <para />
	/// Different strategies may employ different techniques for deciding which paths
	/// (and ObjectIds) to carry from the input trees into the final output tree.
	/// </summary>
	public abstract class MergeStrategy
	{
		/// <summary>
		/// Simple strategy that sets the output tree to the first input tree.
		/// </summary>
		public static readonly MergeStrategy Ours = new StrategyOneSided("ours", 0);

		/// <summary>
		/// Simple strategy that sets the output tree to the second input tree.
		/// </summary>
		public static readonly MergeStrategy Theirs = new StrategyOneSided("theirs", 1);

		/// <summary>
		/// Simple strategy to merge paths, without simultaneous edits.
		/// </summary>
		public static readonly ThreeWayMergeStrategy SimpleTwoWayInCore = new StrategySimpleTwoWayInCore();

		private static readonly Dictionary<String, MergeStrategy> Strategies = new Dictionary<String, MergeStrategy>();
		
		private static Object locker = new Object();

		static MergeStrategy()
		{
			Register(Ours);
			Register(Theirs);
			Register(SimpleTwoWayInCore);
		}

		///	<summary>
		/// Register a merge strategy so it can later be obtained by name.
		///	</summary>
		///	<param name="imp">the strategy to register.</param>
		///	<exception cref="ArgumentException">
		/// a strategy by the same name has already been registered.
		/// </exception>
		public static void Register(MergeStrategy imp)
		{
			if (imp == null)
				throw new ArgumentNullException ("imp");
			
			Register(imp.Name, imp);
		}

		///	<summary>
		/// Register a merge strategy so it can later be obtained by name.
		/// </summary>
		/// <param name="name">
		/// name the strategy can be looked up under.</param>
		/// <param name="imp">the strategy to register.</param>
		/// <exception cref="ArgumentException">
		/// a strategy by the same name has already been registered.
		/// </exception>
		public static void Register(string name, MergeStrategy imp)
		{
			lock(locker)
			{
				if (Strategies.ContainsKey(name))
				{
					throw new ArgumentException("Merge strategy \"" + name + "\" already exists as a default strategy");
				}
	
				Strategies.Add(name, imp);
			}
		}

		///	<summary>
		/// Locate a strategy by name.
		///	</summary>
		///	<param name="name">name of the strategy to locate.</param>
		/// <returns>
		/// The strategy instance; null if no strategy matches the name.
		/// </returns>
		public static MergeStrategy Get(string name)
		{
			lock(locker)
			{
				return Strategies[name];
			}
		}

		///	<summary>
		/// Get all registered strategies.
		/// </summary>
		/// <returns>
		/// The registered strategy instances. No inherit order is returned;
		/// the caller may modify (and/or sort) the returned array if
		/// necessary to obtain a reasonable ordering.
		/// </returns>
		public static MergeStrategy[] Get()
		{
			lock(locker)
			{
				return Strategies.Values.ToArray();
			}
		}

		/// <summary>
		/// default name of this strategy implementation.
		/// </summary>
		/// <returns></returns>
		public abstract string Name { get; }

		///	<summary>
		/// Create a new merge instance.
		/// </summary>
		/// <param name="db">
		/// repository database the merger will read from, and eventually
		/// write results back to.
		/// </param>
		/// <returns> the new merge instance which implements this strategy.</returns>
		public abstract Merger NewMerger(Repository db);
	}
}