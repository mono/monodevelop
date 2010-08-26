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

using System;
using System.Text;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core.RevWalk
{
    /// <summary>
    /// Base object type accessed during revision walking.
    /// </summary>
    public abstract class RevObject : ObjectId, IEquatable<RevObject>
    {
        protected const int PARSED = 1;

        protected RevObject(AnyObjectId name)
            : base(name)
        {
        }

        public int Flags { get; set; }

        internal virtual void parseHeaders(RevWalk walk)
        {
            loadCanonical(walk);
            Flags |= PARSED;
        }

        internal virtual void parseBody(RevWalk walk)
        {
            if ((Flags & PARSED) == 0)
                parseHeaders(walk);
        }

        internal byte[] loadCanonical(RevWalk walk)
        {
            ObjectLoader ldr = walk.Repository.OpenObject(walk.WindowCursor, this);
            if (ldr == null)
            {
                throw new MissingObjectException(this, Type);
            }

            byte[] data = ldr.CachedBytes;
            if (Type != ldr.Type)
            {
                throw new IncorrectObjectTypeException(this, Type);
            }

            return data;
        }

        /// <summary>
        /// Get Git object type. See <see cref="Constants"/>.
        /// </summary>
        /// <returns></returns>
        public abstract int Type { get; }

        /// <summary>
        /// Get the name of this object.
        /// </summary>
        /// <returns>Unique hash of this object.</returns>
        public ObjectId getId()
        {
            return this;
        }

        public new bool Equals(object obj)
        {
            return ReferenceEquals(this, obj as RevObject);
        }

        public bool Equals(RevObject obj)
        {
            return ReferenceEquals(this, obj);
        }


        /// <summary>
        /// Test to see if the flag has been set on this object.
        /// </summary>
        /// <param name="flag">the flag to test.</param>
        /// <returns>
        /// true if the flag has been added to this object; false if not.
        /// </returns>
        public bool has(RevFlag flag)
        {
            return (Flags & flag.Mask) != 0;
        }

        /// <summary>
        /// Test to see if any flag in the set has been set on this object.
        /// </summary>
        /// <param name="set">the flags to test.</param>
        /// <returns>
        /// true if any flag in the set has been added to this object; false
        /// if not.
        /// </returns>
        public bool hasAny(RevFlagSet set)
        {
            return (Flags & set.Mask) != 0;
        }

        /// <summary>
        /// Test to see if all flags in the set have been set on this object.
        /// </summary>
        /// <param name="set">the flags to test.</param>
        /// <returns>true if all flags of the set have been added to this object;
        /// false if some or none have been added.
        /// </returns>
        public bool hasAll(RevFlagSet set)
        {
            return (Flags & set.Mask) == set.Mask;
        }

        /// <summary>
        /// Add a flag to this object.
        /// <para />
        /// If the flag is already set on this object then the method has no effect.
        /// </summary>
        /// <param name="flag">
        /// The flag to mark on this object, for later testing.
        /// </param>
        public void add(RevFlag flag)
        {
            Flags |= flag.Mask;
        }

        /// <summary>
        /// Add a set of flags to this object.
        /// </summary>
        /// <param name="set">
        /// The set of flags to mark on this object, for later testing.
        /// </param>
        public void add(RevFlagSet set)
        {
            Flags |= set.Mask;
        }

        /// <summary>
        /// Remove a flag from this object.
        /// <para />
        /// If the flag is not set on this object then the method has no effect.
        /// </summary>
        /// <param name="flag">
        /// The flag to remove from this object.
        /// </param>
        public void remove(RevFlag flag)
        {
            Flags &= ~flag.Mask;
        }

        /// <summary>
        /// Remove a set of flags from this object.
        /// </summary>
        /// <param name="set">
        /// The flag to remove from this object.
        /// </param>
        public void remove(RevFlagSet set)
        {
            Flags &= ~set.Mask;
        }

        /// <summary>
        /// Release as much memory as possible from this object.
        /// </summary>
        public virtual void DisposeBody()
        {
            // Nothing needs to be done for most objects.
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            s.Append(Constants.typeString(Type));
            s.Append(' ');
            s.Append(Name);
            s.Append(' ');
            appendCoreFlags(s);
            return s.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s">
        /// Buffer to Append a debug description of core RevFlags onto.
        /// </param>
        internal void appendCoreFlags(StringBuilder s)
        {
            s.Append((Flags & RevWalk.TOPO_DELAY) != 0 ? 'o' : '-');
            s.Append((Flags & RevWalk.TEMP_MARK) != 0 ? 't' : '-');
            s.Append((Flags & RevWalk.REWRITE) != 0 ? 'r' : '-');
            s.Append((Flags & RevWalk.UNINTERESTING) != 0 ? 'u' : '-');
            s.Append((Flags & RevWalk.SEEN) != 0 ? 's' : '-');
            s.Append((Flags & RevWalk.PARSED) != 0 ? 'p' : '-');
        }
    }
}