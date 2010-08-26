/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using System.Collections.Generic;
using System.Linq;

namespace GitSharp.Core
{
	/// <summary>
	/// Fast, efficient map specifically for {@link ObjectId} subclasses.
	/// <para />
	/// This map provides an efficient translation from any ObjectId instance to a
	/// cached subclass of ObjectId that has the same value.
	/// <para />
	/// Raw value equality is tested when comparing two ObjectIds (or subclasses),
	/// not reference equality and not <code>.Equals(Object)</code> equality. This
	/// allows subclasses to override <code>Equals</code> to supply their own
	/// extended semantics.
	/// </summary>
	/// <typeparam name="TObject">
	/// Type of subclass of ObjectId that will be stored in the map.
	/// </typeparam>
	public class ObjectIdSubclassMap<TObject> : HashSet<TObject>
		where TObject : AnyObjectId
	{
		private static readonly IEqualityComparer<TObject> EqualityComparer = new AnyObjectId.AnyObjectIdEqualityComparer<TObject>();

		public ObjectIdSubclassMap()
			: base(EqualityComparer)
		{
		}

		/// <summary>
		/// Lookup an existing mapping.
		/// </summary>
		/// <param name="toFind">the object identifier to find.</param>
		/// <returns>the instance mapped to toFind, or null if no mapping exists.</returns>
		public TObject Get(AnyObjectId toFind)
		{
			return this.SingleOrDefault(x => AnyObjectId.equals(toFind.ToObjectId(), x.ToObjectId()));
		}
	}
}