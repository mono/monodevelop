/*
 * Copyright (C) 2010, Google Inc.
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
    /// A reference that indirectly points at another <see cref="Ref"/>.
    /// <para/>
    /// A symbolic reference always derives its current value from the target
    /// reference.
    /// </summary>
    public class SymbolicRef : Ref
    {
        private readonly string _name;
        private readonly Ref _target;

        /// <summary>
        /// Create a new ref pairing.
        /// </summary>
        /// <param name="refName">name of this ref.</param>
        /// <param name="target">the ref we reference and derive our value from.</param>
        public SymbolicRef(string refName, Ref target)
        {
            _name = refName;
            _target = target;
        }

        public string Name
        {
            get { return _name; }
        }

        public bool IsSymbolic
        {
            get { return true; }
        }

        public Ref Leaf
        {
            get
            {
                Ref dst = Target;
                while (dst.IsSymbolic)
                    dst = dst.Target;
                return dst;
            }
        }

        public Ref Target
        {
            get { return _target; }
        }

        public ObjectId ObjectId
        {
            get { return Leaf.ObjectId; }
        }

        public ObjectId PeeledObjectId
        {
            get { return Leaf.PeeledObjectId; }
        }

        public bool IsPeeled
        {
            get { return Leaf.IsPeeled; }
        }

        public Storage StorageFormat
        {
            get { return Storage.Loose; }
        }

        public override string ToString()
        {
            var r = new StringBuilder();
            r.Append("SymbolicRef[");
            Ref cur = this;
            while (cur.isSymbolic())
            {
                r.Append(cur.getName());
                r.Append(" -> ");
                cur = cur.getTarget();
            }
            r.Append(cur.getName());
            r.Append('=');
            r.Append(ObjectId.ToString(cur.getObjectId()));
            r.Append("]");
            return r.ToString();
        }
    }
}