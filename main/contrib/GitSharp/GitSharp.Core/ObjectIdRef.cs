/*
 * Copyright (C) 2010, Google Inc.
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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

using System.Text;

namespace GitSharp.Core
{
    /// <summary>
    /// Any reference whose peeled value is not yet known.
    /// </summary>
    public class Unpeeled : ObjectIdRef
    {

        /// <summary>
        /// Create a new ref pairing.
        /// </summary>
        /// <param name="st">method used to store this ref.</param>
        /// <param name="name">name of this ref.</param>
        /// <param name="id">
        /// current value of the ref. May be null to indicate a ref that
        /// does not exist yet.
        /// </param>
        public Unpeeled(Storage st, string name, ObjectId id)
            : base(st, name, id)
        {

        }

        public override ObjectId PeeledObjectId
        {
            get { return null; }
        }

        public override bool IsPeeled
        {
            get { return false; }
        }
    }

    /// <summary>
    /// An annotated tag whose peeled object has been cached.
    /// </summary>
    public class PeeledTag : ObjectIdRef
    {
        private readonly ObjectId _peeledObjectId;

        /// <summary>
        /// Create a new ref pairing.
        /// </summary>
        /// <param name="st">method used to store this ref.</param>
        /// <param name="name">name of this ref.</param>
        /// <param name="id">
        /// current value of the ref. 
        /// </param>
        /// <param name="p">the first non-tag object that tag {@code id} points to.</param>
        public PeeledTag(Storage st, string name, ObjectId id, ObjectId p)
            : base(st, name, id)
        {

            _peeledObjectId = p;
        }

        public override ObjectId PeeledObjectId
        {
            get { return _peeledObjectId; }
        }

        public override bool IsPeeled
        {
            get { return true; }
        }
    }

    /// <summary>
    /// A reference to a non-tag object coming from a cached source.
    /// </summary>
    public class PeeledNonTag : ObjectIdRef
    {

        /// <summary>
        /// Create a new ref pairing.
        /// </summary>
        /// <param name="st">method used to store this ref.</param>
        /// <param name="name">name of this ref.</param>
        /// <param name="id">
        /// current value of the ref. May be null to indicate a ref that
        /// does not exist yet.
        /// </param>
        public PeeledNonTag(Storage st, string name, ObjectId id)
            : base(st, name, id)
        {

        }

        public override ObjectId PeeledObjectId
        {
            get { return null; }
        }

        public override bool IsPeeled
        {
            get { return true; }
        }
    }

    /// <summary>
    /// A <see cref="Ref"/> that points directly at an <see cref="ObjectId"/>.
    /// </summary>
    public abstract class ObjectIdRef : Ref
    {
        private readonly string _name;
        private readonly Storage _storage;
        private readonly ObjectId _objectId;

        /// <summary>
        /// Create a new ref pairing.
        /// </summary>
        /// <param name="st">method used to store this ref.</param>
        /// <param name="name">name of this ref.</param>
        /// <param name="id">
        /// current value of the ref. May be null to indicate a ref that
        /// does not exist yet.
        /// </param>
        protected ObjectIdRef(Storage st, string name, ObjectId id)
        {
            _name = name;
            _storage = st;
            _objectId = id;
        }

        public string Name
        {
            get { return _name; }
        }

        public bool IsSymbolic
        {
            get { return false; }
        }

        public Ref Leaf
        {
            get { return this; }
        }

        public Ref Target
        {
            get { return this; }
        }

        public ObjectId ObjectId
        {
            get { return _objectId; }
        }

        public abstract ObjectId PeeledObjectId { get; }

        public abstract bool IsPeeled { get; }

        public Storage StorageFormat
        {
            get { return _storage; }
        }

        public override string ToString()
        {
            var r = new StringBuilder();
            r.Append("Ref[");
            r.Append(Name);
            r.Append('=');
            r.Append(ObjectId.ToString(ObjectId));
            r.Append(']');
            return r.ToString();
        }

    }
}