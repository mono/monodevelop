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

using GitSharp.Core.Util;

namespace GitSharp.Core
{
    /// <summary>
    /// Updates any reference stored by <see cref="RefDirectory"/>.
    /// </summary>
    public class RefDirectoryUpdate : RefUpdate
    {
        private readonly RefDirectory _database;

        private LockFile _lock;

        public RefDirectoryUpdate(RefDirectory r, Ref @ref)
            : base(@ref)
        {
            _database = r;
        }

        public override RefDatabase getRefDatabase()
        {
            return _database;
        }

        public override Repository getRepository()
        {
            return _database.getRepository();
        }

        protected override bool tryLock(bool deref)
        {
            Ref dst = Ref;
            if (deref)
                dst = dst.getLeaf();
            string name = dst.getName();
            _lock = new LockFile(_database.fileFor(name));
            if (_lock.Lock())
            {
                dst = _database.getRef(name);
                OldObjectId = (dst != null ? dst.getObjectId() : null);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void unlock()
        {
            if (_lock != null)
            {
                _lock.Unlock();
                _lock = null;
            }
        }

        protected override RefUpdateResult doUpdate(RefUpdateResult status)
        {
            _lock.setNeedStatInformation(true);
            _lock.Write(NewObjectId);

            string msg = getRefLogMessage();
            if (msg != null)
            {
                if (isRefLogIncludingResult())
                {
                    string strResult = toResultString(status);
                    if (strResult != null)
                    {
                        if (msg.Length > 0)
                            msg = msg + ": " + strResult;
                        else
                            msg = strResult;
                    }
                }
                _database.log(this, msg, true);
            }
            if (!_lock.Commit())
                return RefUpdateResult.LOCK_FAILURE;
            _database.stored(this, _lock.CommitLastModified);
            return status;
        }

        private static string toResultString(RefUpdateResult status)
        {
            switch (status)
            {
                case RefUpdateResult.FORCED:
                    return "forced-update";
                case RefUpdateResult.FAST_FORWARD:
                    return "fast forward";
                case RefUpdateResult.NEW:
                    return "created";
                default:
                    return null;
            }
        }

        protected override RefUpdateResult doDelete(RefUpdateResult status)
        {
            if (Ref.getLeaf().getStorage() != Storage.New)
                _database.delete(this);
            return status;
        }

        protected override RefUpdateResult doLink(string target)
        {
            _lock.setNeedStatInformation(true);
            _lock.Write(Constants.encode(RefDirectory.SYMREF + target + '\n'));

            string msg = getRefLogMessage();
            if (msg != null)
                _database.log(this, msg, false);
            if (!_lock.Commit())
                return RefUpdateResult.LOCK_FAILURE;
            _database.storedSymbolicRef(this, _lock.CommitLastModified, target);

            if (Ref.getStorage() == Storage.New)
                return RefUpdateResult.NEW;
            return RefUpdateResult.FORCED;
        }
    }
}


