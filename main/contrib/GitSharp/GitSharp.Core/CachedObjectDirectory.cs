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


using System;
using System.IO;
using System.Linq;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
    /// <summary>
    /// The cached instance of an <see cref="ObjectDirectory"/>.
    /// <para/>
    /// This class caches the list of loose objects in memory, so the file system is
    /// not queried with stat calls.
    /// </summary>
    public class CachedObjectDirectory : CachedObjectDatabase {
        /// <summary>
        /// The set that contains unpacked objects identifiers, it is created when
        /// the cached instance is created.
        /// </summary>
        private readonly ObjectIdSubclassMap<ObjectId> _unpackedObjects = new ObjectIdSubclassMap<ObjectId>();

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="wrapped">the wrapped database</param>
        public CachedObjectDirectory(ObjectDirectory wrapped) : base(wrapped) {
            DirectoryInfo objects = wrapped.getDirectory();
            string[] fanout = objects.GetDirectories().Select(x => x.FullName).ToArray();
            if (fanout == null)
                fanout = new string[0];
            foreach (string d in fanout) {
                if (d.Length != 2)
                    continue;
                string[] entries = PathUtil.CombineDirectoryPath(objects, d).GetFiles().Select(x => x.FullName).ToArray();
                if (entries == null)
                    continue;
                foreach (string e in entries) {
                    if (e.Length != Constants.OBJECT_ID_STRING_LENGTH - 2)
                        continue;
                    try {
                        _unpackedObjects.Add(ObjectId.FromString(d + e));
                    } catch (ArgumentException) {
                        // ignoring the file that does not represent loose object
                    }
                }
            }
        }

        public override ObjectLoader openObject2(WindowCursor curs, String objectName,
                                                 AnyObjectId objectId) {
            if (_unpackedObjects.Get(objectId) == null)
                return null;
            return base.openObject2(curs, objectName, objectId);
                                                 }

        public override bool hasObject1(AnyObjectId objectId) {
            if (_unpackedObjects.Get(objectId) != null)
                return true; // known to be loose
            return base.hasObject1(objectId);
        }

        public override bool hasObject2(String name) {
            return false; // loose objects were tested by hasObject1
        }
    }
}


