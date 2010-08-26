/*
 * Copyright (C) 2010, JetBrains s.r.o.
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

using System.Collections.Generic;

namespace GitSharp.Core
{
    /// <summary>
    /// <see cref="ObjectDatabase"/> wrapper providing temporary lookup caching.
    /// <para/>
    /// The base class for {@code ObjectDatabase}s that wrap other database instances
    /// and optimize querying for objects by caching some database dependent
    /// information. Instances of this class (or any of its subclasses) can be
    /// returned from the method <see cref="ObjectDatabase.newCachedDatabase"/>. This
    /// class can be used in scenarios where the database does not change, or when
    /// changes in the database while some operation is in progress is an acceptable
    /// risk.
    /// <para/>
    /// The default implementation delegates all requests to the wrapped database.
    /// The instance might be indirectly invalidated if the wrapped instance is
    /// closed. Closing the delegating instance does not implies closing the wrapped
    /// instance. For alternative databases, cached instances are used as well.
    /// </summary>
    public class CachedObjectDatabase : ObjectDatabase {
        /// <summary>
        /// The wrapped database instance
        /// </summary>
        protected ObjectDatabase wrapped;

        /// <summary>
        /// Create the delegating database instance
        /// </summary>
        /// <param name="wrapped">the wrapped object database</param>
        public CachedObjectDatabase(ObjectDatabase wrapped) {
            this.wrapped = wrapped;
        }

        public override bool hasObject1(AnyObjectId objectId) {
            return wrapped.hasObject1(objectId);
        }

        public override ObjectLoader openObject1(WindowCursor curs, AnyObjectId objectId)
        {
            return wrapped.openObject1(curs, objectId);
        }

        public override bool hasObject2(string objectName) {
            return wrapped.hasObject2(objectName);
        }

        protected override ObjectDatabase[] loadAlternates() {
            ObjectDatabase[] loaded = wrapped.getAlternates();
            var result = new ObjectDatabase[loaded.Length];
            for (int i = 0; i < loaded.Length; i++) {
                result[i] = loaded[i].newCachedDatabase();
            }
            return result;
        }

        public override ObjectLoader openObject2(WindowCursor curs, string objectName,
                                                 AnyObjectId objectId) {
            return wrapped.openObject2(curs, objectName, objectId);
                                                 }

        public override void OpenObjectInAllPacks(ICollection<PackedObjectLoader> @out,
                                                  WindowCursor curs, AnyObjectId objectId)  {
            wrapped.OpenObjectInAllPacks(@out, curs, objectId);
                                                  }

        public override bool tryAgain1() {
            return wrapped.tryAgain1();
        }

        public override ObjectDatabase newCachedDatabase() {
            // Note that "this" is not returned since subclasses might actually do something,
            // on closeSelf() (for example closing database connections or open repositories).
            // The situation might become even more tricky if we will consider alternates.
            return wrapped.newCachedDatabase();
        }
    }
}


