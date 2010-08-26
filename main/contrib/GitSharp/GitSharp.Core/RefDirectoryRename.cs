/*
 * Copyright (C) 2010, Google Inc.
 * Copyright (C) 2009, Robin Rosenberg
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

using System.IO;

namespace GitSharp.Core
{
    /// <summary>
    /// Rename any reference stored by {@link RefDirectory}.
    /// <para/>
    /// This class works by first renaming the source reference to a temporary name,
    /// then renaming the temporary name to the destination reference.
    /// <para/>
    /// This strategy permits switching a reference like {@code refs/heads/foo},
    /// which is a file, to {@code refs/heads/foo/bar}, which is stored inside a
    /// directory that happens to match the source name.
    /// </summary>
    public class RefDirectoryRename : RefRename
    {
        private readonly RefDirectory _refdb;

        /// <summary>
        /// The value of the source reference at the start of the rename.
        /// <para/>
        /// At the end of the rename the destination reference must have this same
        /// value, otherwise we have a concurrent update and the rename must fail
        /// without making any changes.
        /// </summary>
        private ObjectId _objId;

        /// <summary>
        /// True if HEAD must be moved to the destination reference.
        /// </summary>
        private bool _updateHead;

        /// <summary>
        /// A reference we backup {@link #objId} into during the rename.
        /// </summary>
        private RefDirectoryUpdate _tmp;

        public RefDirectoryRename(RefDirectoryUpdate src, RefDirectoryUpdate dst)
            : base(src, dst)
        {
            _refdb = (RefDirectory)src.getRefDatabase();
        }

        protected override RefUpdate.RefUpdateResult doRename()
        {
            if (source.getRef().isSymbolic())
                return RefUpdate.RefUpdateResult.IO_FAILURE; // not supported

            var rw = new RevWalk.RevWalk(_refdb.getRepository());
            _objId = source.getOldObjectId();
            _updateHead = needToUpdateHEAD();
            _tmp = _refdb.newTemporaryUpdate();
            try
            {
                // First backup the source so its never unreachable.
                _tmp.setNewObjectId(_objId);
                _tmp.setForceUpdate(true);
                _tmp.disableRefLog();
                switch (_tmp.update(rw))
                {
                    case RefUpdate.RefUpdateResult.NEW:
                    case RefUpdate.RefUpdateResult.FORCED:
                    case RefUpdate.RefUpdateResult.NO_CHANGE:
                        break;
                    default:
                        return _tmp.getResult();
                }

                // Save the source's log under the temporary name, we must do
                // this before we delete the source, otherwise we lose the log.
                if (!renameLog(source, _tmp))
                    return RefUpdate.RefUpdateResult.IO_FAILURE;

                // If HEAD has to be updated, link it now to destination.
                // We have to link before we delete, otherwise the delete
                // fails because its the current branch.
                RefUpdate dst = destination;
                if (_updateHead)
                {
                    if (!linkHEAD(destination))
                    {
                        renameLog(_tmp, source);
                        return RefUpdate.RefUpdateResult.LOCK_FAILURE;
                    }

                    // Replace the update operation so HEAD will log the rename.
                    dst = _refdb.newUpdate(Constants.HEAD, false);
                    dst.setRefLogIdent(destination.getRefLogIdent());
                    dst.setRefLogMessage(destination.getRefLogMessage(), false);
                }

                // Delete the source name so its path is free for replacement.
                source.setExpectedOldObjectId(_objId);
                source.setForceUpdate(true);
                source.disableRefLog();
                if (source.delete(rw) != RefUpdate.RefUpdateResult.FORCED)
                {
                    renameLog(_tmp, source);
                    if (_updateHead)
                        linkHEAD(source);
                    return source.getResult();
                }

                // Move the log to the destination.
                if (!renameLog(_tmp, destination))
                {
                    renameLog(_tmp, source);
                    source.setExpectedOldObjectId(ObjectId.ZeroId);
                    source.setNewObjectId(_objId);
                    source.update(rw);
                    if (_updateHead)
                        linkHEAD(source);
                    return RefUpdate.RefUpdateResult.IO_FAILURE;
                }

                // Create the destination, logging the rename during the creation.
                dst.setExpectedOldObjectId(ObjectId.ZeroId);
                dst.setNewObjectId(_objId);
                if (dst.update(rw) != RefUpdate.RefUpdateResult.NEW)
                {
                    // If we didn't create the destination we have to undo
                    // our work. Put the log back and restore source.
                    if (renameLog(destination, _tmp))
                        renameLog(_tmp, source);
                    source.setExpectedOldObjectId(ObjectId.ZeroId);
                    source.setNewObjectId(_objId);
                    source.update(rw);
                    if (_updateHead)
                        linkHEAD(source);
                    return dst.getResult();
                }

                return RefUpdate.RefUpdateResult.RENAMED;
            }
            finally
            {
                // Always try to free the temporary name.
                try
                {
                    _refdb.delete(_tmp);
                }
                catch (IOException)
                {
                    _refdb.fileFor(_tmp.getName()).Delete();
                }
            }
        }

        private bool renameLog(RefUpdate src, RefUpdate dst)
        {
            FileInfo srcLog = _refdb.logFor(src.getName());
            FileInfo dstLog = _refdb.logFor(dst.getName());

            if (!srcLog.Exists)
                return true;

            if (!rename(srcLog.FullName, dstLog.FullName))
                return false;

            // There be dragons



            try
            {
                int levels = RefDirectory.levelsIn(src.getName()) - 2;
                RefDirectory.delete(srcLog, levels);
                return true;
            }
            catch (IOException)
            {
                rename(dstLog.FullName, srcLog.FullName);
                return false;
            }
        }

        private static bool rename(string src, string dst)
        {
            if (new FileInfo(src).RenameTo(dst))
                return true;

            DirectoryInfo dir = new FileInfo(dst).Directory;
            if ((dir.Exists || !dir.Mkdirs()) && !dir.IsDirectory())
                return false;
            return new FileInfo(src).RenameTo(dst);
        }

        private bool linkHEAD(RefUpdate target)
        {
            try
            {
                RefUpdate u = _refdb.newUpdate(Constants.HEAD, false);
                u.disableRefLog();
                switch (u.link(target.getName()))
                {
                    case RefUpdate.RefUpdateResult.NEW:
                    case RefUpdate.RefUpdateResult.FORCED:
                    case RefUpdate.RefUpdateResult.NO_CHANGE:
                        return true;
                    default:
                        return false;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}