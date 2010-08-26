/*
 * Copyright (C) 2009, Google Inc.
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
using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
    public class RepositoryCache
    {
        private static readonly RepositoryCache Cache = new RepositoryCache();

        public static RepositoryCache Instance
        {
            get { return Cache; }
        }

        public static Repository open(Key location)
        {
            return open(location, true);
        }

        public static Repository open(Key location, bool mustExist)
        {
            return Cache.openRepository(location, mustExist);
        }

        public static void register(Repository db)
        {
			if (db == null)
				throw new System.ArgumentNullException ("db");
			
            Cache.registerRepository(FileKey.exact(db.Directory), db);
        }

        public static void close(Repository db)
        {
			if (db == null)
				throw new System.ArgumentNullException ("db");
			
            Cache.unregisterRepository(FileKey.exact(db.Directory));
        }

        public static void clear()
        {
            Cache.clearAll();
        }

        private readonly Dictionary<Key, WeakReference<Repository>> cacheMap;
        private readonly Lock[] openLocks;

        public RepositoryCache()
        {
            cacheMap = new Dictionary<Key, WeakReference<Repository>>();
            openLocks = new Lock[4];
            for (int i = 0; i < openLocks.Length; i++)
                openLocks[i] = new Lock();
        }

        private Repository openRepository(Key location, bool mustExist)
        {
            WeakReference<Repository> @ref = cacheMap.GetValue(location);
            Repository db = @ref != null ? @ref.get() : null;

            if (db == null)
            {
                lock (lockFor(location))
                {
                    @ref = cacheMap.GetValue(location);
                    db = @ref != null ? @ref.get() : null;
                    if (db == null)
                    {
                        db = location.open(mustExist);
                        @ref = new WeakReference<Repository>(db);
                        cacheMap.AddOrReplace(location, @ref);
                    }
                }
            }

            db.IncrementOpen();
            return db;
        }

        private void registerRepository(Key location, Repository db)
        {
            db.IncrementOpen();
            WeakReference<Repository> newRef = new WeakReference<Repository>(db);
            WeakReference<Repository> oldRef = cacheMap.put(location, newRef);
            Repository oldDb = oldRef != null ? oldRef.get() : null;
            if (oldDb != null)
                oldDb.Dispose();

        }

        private void unregisterRepository(Key location)
        {
            WeakReference<Repository> oldRef = cacheMap.GetValue(location);
            cacheMap.Remove(location);
            Repository oldDb = oldRef != null ? oldRef.get() : null;
            if (oldDb != null)
                oldDb.Dispose();
        }

        private void clearAll()
        {
            for (int stage = 0; stage < 2; stage++)
            {
                var keysToRemove = new List<Key>();

                foreach (KeyValuePair<Key, WeakReference<Repository>> e in cacheMap)
                {
                    Repository db = e.Value.get();
                    if (db != null)
                        db.Dispose();

                    keysToRemove.Add(e.Key);
                }

                foreach (Key key in keysToRemove)
                {
                    cacheMap.Remove(key);
                }
            }
        }

        private Lock lockFor(Key location)
        {
            return openLocks[(((uint) location.GetHashCode()) >> 1)%openLocks.Length];
        }

        private class Lock
        {
        } ;

        public interface Key
        {
            Repository open(bool mustExist);
        }

        public class FileKey : Key
        {
            public static FileKey exact(DirectoryInfo dir)
            {
                return new FileKey(dir);
            }

            public static FileKey lenient(DirectoryInfo dir)
            {
                DirectoryInfo gitdir = resolve(dir);
                return new FileKey(gitdir ?? dir);
            }

            private readonly DirectoryInfo path;

            public FileKey(DirectoryInfo dir)
            {
                path = dir;
            }

            public DirectoryInfo getFile()
            {
                return path;
            }

            public Repository open(bool mustExist)
            {
                if (mustExist && !isGitRepository(path))
                    throw new RepositoryNotFoundException(path);
                return new Repository(path);
            }

            public override int GetHashCode()
            {
                return path.FullName.GetHashCode();
            }

            public override bool Equals(object obj)
            {
				FileKey fk = (obj as FileKey);
				if ( fk != null)
				{
					return path.FullName.Equals(fk.path.FullName);
				}
				
                return false;
            }

            public override string ToString()
            {
                return path.FullName;
            }

            public static bool isGitRepository(DirectoryInfo dir)
            {
                return FS.resolve(dir, "objects").Exists && FS.resolve(dir, "refs").Exists &&
                       isValidHead(new FileInfo(Path.Combine(dir.FullName, Constants.HEAD)));
            }

            private static bool isValidHead(FileInfo head)
            {
                string r = readFirstLine(head);
                return head.Exists && r != null && (r.StartsWith("ref: refs/") || ObjectId.IsId(r));
            }

            private static string readFirstLine(FileInfo head)
            {
                try
                {
                    byte[] buf = IO.ReadFully(head, 4096);
                    int n = buf.Length;
                    if (n == 0)
                        return null;
                    if (buf[n - 1] == '\n')
                        n--;
                    return RawParseUtils.decode(buf, 0, n);
                }
                catch (IOException)
                {
                    return null;
                }
            }

        	private static DirectoryInfo resolve(DirectoryInfo directory)
            {
                if (isGitRepository(directory))
                {
                    return directory;
                }

                if (isGitRepository(PathUtil.CombineDirectoryPath(directory, Constants.DOT_GIT)))
				{
                    return PathUtil.CombineDirectoryPath(directory, Constants.DOT_GIT);
				}

                string name = directory.Name;
                DirectoryInfo parent = directory.Parent;
                if (isGitRepository(new DirectoryInfo(Path.Combine(parent.FullName, name + Constants.DOT_GIT_EXT))))
                {
                    return new DirectoryInfo(Path.Combine(parent.FullName, name + Constants.DOT_GIT_EXT));
                }

                return null;
            }
        }
    }

}