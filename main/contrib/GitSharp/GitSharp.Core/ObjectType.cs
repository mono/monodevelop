/*
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

namespace GitSharp.Core
{
	[Serializable]
	public enum ObjectType
	{
		/// <summary>
		/// An unknown or invalid object type code.
		/// </summary>
		Bad = -1,

		/// <summary>
		/// In-pack object type: extended types.
		/// <para />
		/// This header code is reserved for future expansion. It is currently
		/// undefined/unsupported.
		/// </summary>
		Extension = 0,

		/// <summary>
		/// In-pack object type: commit.
		/// <para />
		/// Indicates the associated object is a commit.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// <seealso cref="Constants.TYPE_COMMIT"/>
		/// </summary>
		Commit = 1,

		/// <summary>
		/// In-pack object type: tree.
		/// <para />
		/// Indicates the associated object is a tree.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </summary>
		/// <seealso cref="Constants.TYPE_BLOB"/>
		Tree = 2,

		/// <summary>
		/// In-pack object type: blob.
		/// <para />
		/// Indicates the associated object is a blob.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </summary>
		/// <seealso cref="Constants.TYPE_BLOB"/>
		Blob = 3,

		/// <summary>
		/// In-pack object type: annotated tag.
		/// <para />
		/// Indicates the associated object is an annotated tag.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </summary>
		/// <seealso cref="Constants.TYPE_TAG"/>
		Tag = 4,

		/// <summary>
		/// In-pack object type: reserved for future use.
		/// </summary>
		ObjectType5 = 5,

		/// <summary>
		/// In-pack object type: offset delta
		/// <para />
		/// Objects stored with this type actually have a different type which must
		/// be obtained from their delta base object. Delta objects store only the
		/// changes needed to apply to the base object in order to recover the
		/// original object.
		/// <para />
		/// An offset delta uses a negative offset from the start of this object to
		/// refer to its delta base. The base object must exist in this packfile
		/// (even in the case of a thin pack).
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </summary>
		OffsetDelta = 6,

		/// <summary>
		/// In-pack object type: reference delta
		/// <para />
		/// Objects stored with this type actually have a different type which must
		/// be obtained from their delta base object. Delta objects store only the
		/// changes needed to apply to the base object in order to recover the
		/// original object.
		/// <para />
		/// A reference delta uses a full object id (hash) to reference the delta
		/// base. The base object is allowed to be omitted from the packfile, but
		/// only in the case of a thin pack being transferred over the network.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </summary>
		ReferenceDelta = 7,

		DeltaBase = 254,

		Unknown = 255
	}
}