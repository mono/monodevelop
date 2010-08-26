/*
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
    /// A RefUpdate combination for renaming a reference.
    /// <para/>
    /// If the source reference is currently pointed to by {@code HEAD}, then the
    /// HEAD symbolic reference is updated to point to the new destination.
    /// </summary>
    public abstract class RefRename {
        /// <summary>
        /// Update operation to read and delete the source reference.
        /// </summary>
        protected RefUpdate source;

        /// <summary>
        /// Update operation to create/overwrite the destination reference.
        /// </summary>
        protected RefUpdate destination;

        private RefUpdate.RefUpdateResult result = RefUpdate.RefUpdateResult.NOT_ATTEMPTED;

        /// <summary>
        /// Initialize a new rename operation.
        /// </summary>
        /// <param name="src">operation to read and delete the source.</param>
        /// <param name="dst">operation to create (or overwrite) the destination.</param>
        protected RefRename(RefUpdate src, RefUpdate dst) {
            source = src;
            destination = dst;

            Repository repo = destination.getRepository();
            string cmd = "";
            if (source.getName().StartsWith(Constants.R_HEADS)
                && destination.getName().StartsWith(Constants.R_HEADS))
                cmd = "Branch: ";
            setRefLogMessage(cmd + "renamed "
                             + repo.ShortenRefName(source.getName()) + " to "
                             + repo.ShortenRefName(destination.getName()));
        }

        /// <returns>identity of the user making the change in the reflog.</returns>
        public PersonIdent getRefLogIdent() {
            return destination.getRefLogIdent();
        }

        /// <summary>
        /// Set the identity of the user appearing in the reflog.
        /// <para/>
        /// The timestamp portion of the identity is ignored. A new identity with the
        /// current timestamp will be created automatically when the rename occurs
        /// and the log record is written.
        /// </summary>
        /// <param name="pi">
        /// identity of the user. If null the identity will be
        /// automatically determined based on the repository
        /// configuration.
        /// </param>
        public void setRefLogIdent(PersonIdent pi) {
            destination.setRefLogIdent(pi);
        }

        /// <summary>
        /// Get the message to include in the reflog.
        /// </summary>
        /// <returns>
        /// message the caller wants to include in the reflog; null if the
        /// rename should not be logged.
        /// </returns>
        public string getRefLogMessage() {
            return destination.getRefLogMessage();
        }

        /// <summary>
        /// Set the message to include in the reflog.
        /// </summary>
        /// <param name="msg">the message to describe this change.</param>
        public void setRefLogMessage(string msg) {
            if (msg == null)
                disableRefLog();
            else
                destination.setRefLogMessage(msg, false);
        }

        /// <summary>
        /// Don't record this rename in the ref's associated reflog.
        /// </summary>
        public void disableRefLog() {
            destination.setRefLogMessage("", false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>result of rename operation</returns>
        public RefUpdate.RefUpdateResult getResult() {
            return result;
        }

        /// <returns>the result of the new ref update</returns>
        public RefUpdate.RefUpdateResult rename()  {
            try {
                result = doRename();
                return result;
            } catch (IOException) {
                result = RefUpdate.RefUpdateResult.IO_FAILURE;
                throw;
            }
        }

        /// <returns>the result of the rename operation.</returns>
        protected abstract RefUpdate.RefUpdateResult doRename();

        /// <returns>
        /// true if the {@code Constants#HEAD} reference needs to be linked
        /// to the new destination name.
        /// </returns>
        protected bool needToUpdateHEAD(){
            Ref head = source.getRefDatabase().getRef(Constants.HEAD);
            if (head.isSymbolic()) {
                head = head.getTarget();
                return head.getName().Equals(source.getName());
            }
            return false;
        }
    }
}


