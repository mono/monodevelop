/*
 * Copyright (C) 2008, Google Inc.
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

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// A command being processed by <see cref="ReceivePack"/>.
    /// <para/>
    /// This command instance roughly translates to the server side representation of
    /// the <see cref="RemoteRefUpdate"/> created by the client.
    /// </summary>
    public class ReceiveCommand
    {
        /// <summary>
        /// Type of operation requested.
        /// </summary>
        [Serializable]
        public enum Type
        {
            /// <summary>
            /// Create a new ref; the ref must not already exist.
            /// </summary>
            CREATE,

            /// <summary>
            /// Update an existing ref with a fast-forward update.
            /// <para/>
            /// During a fast-forward update no changes will be lost; only new
            /// commits are inserted into the ref.
            /// </summary>
            UPDATE,

            /// <summary>
            /// Update an existing ref by potentially discarding objects.
            /// <para/>
            /// The current value of the ref is not fully reachable from the new
            /// value of the ref, so a successful command may result in one or more
            /// objects becoming unreachable.
            /// </summary>
            UPDATE_NONFASTFORWARD,

            /// <summary>
            /// Delete an existing ref; the ref should already exist.
            /// </summary>
            DELETE
        }

        /// <summary>
        /// Result of the update command.
        /// </summary>
        [Serializable]
        public enum Result
        {
            /// <summary>
            /// The command has not yet been attempted by the server.
            /// </summary>
            NOT_ATTEMPTED,

            /// <summary>
            /// The server is configured to deny creation of this ref.
            /// </summary>
            REJECTED_NOCREATE,

            /// <summary>
            /// The server is configured to deny deletion of this ref.
            /// </summary>
            REJECTED_NODELETE,

            /// <summary>
            /// The update is a non-fast-forward update and isn't permitted.
            /// </summary>
            REJECTED_NONFASTFORWARD,

            /// <summary>
            /// The update affects <code>HEAD</code> and cannot be permitted.
            /// </summary>
            REJECTED_CURRENT_BRANCH,

            /// <summary>
            /// One or more objects aren't in the repository.
            /// <para/>
            /// This is severe indication of either repository corruption on the
            /// server side, or a bug in the client wherein the client did not supply
            /// all required objects during the pack transfer.
            /// </summary>
            REJECTED_MISSING_OBJECT,

            /// <summary>
            /// Other failure; see <see cref="ReceiveCommand.getMessage"/>.
            /// </summary>
            REJECTED_OTHER_REASON,

            /// <summary>
            /// The ref could not be locked and updated atomically; try again.
            /// </summary>
            LOCK_FAILURE,

            /// <summary>
            /// The change was completed successfully.
            /// </summary>
            OK
        }

        private readonly ObjectId _oldId;
        private readonly ObjectId _newId;
        private readonly string _name;
        private Type _type;
        private Ref _refc;
        private Result _status;
        private string _message;

        /// <summary>
        /// Create a new command for <see cref="ReceivePack"/>.
        /// </summary>
        /// <param name="oldId">
        /// the old object id; must not be null. Use
        /// <see cref="ObjectId.ZeroId"/> to indicate a ref creation.
        /// </param>
        /// <param name="newId">
        /// the new object id; must not be null. Use
        /// <see cref="ObjectId.ZeroId"/> to indicate a ref deletion.
        /// </param>
        /// <param name="name">name of the ref being affected.</param>
        public ReceiveCommand(ObjectId oldId, ObjectId newId, string name)
        {
            _oldId = oldId;
            _newId = newId;
            _name = name;

            _type = Type.UPDATE;
            if (ObjectId.ZeroId.Equals(oldId))
                _type = Type.CREATE;
            if (ObjectId.ZeroId.Equals(newId))
                _type = Type.DELETE;
            _status = Result.NOT_ATTEMPTED;
        }

        /// <returns>the old value the client thinks the ref has.</returns>
        public ObjectId getOldId()
        {
            return _oldId;
        }

        /// <returns>the requested new value for this ref.</returns>
        public ObjectId getNewId()
        {
            return _newId;
        }

        /// <returns>the name of the ref being updated.</returns>
        public string getRefName()
        {
            return _name;
        }

        /// <returns>the type of this command; see <see cref="Type"/>.</returns>
        public Type getType()
        {
            return _type;
        }

        /// <returns>the ref, if this was advertised by the connection.</returns>
        public Ref getRef()
        {
            return _refc;
        }

        /// <returns>the current status code of this command.</returns>
        public Result getResult()
        {
            return _status;
        }

        /// <returns>the message associated with a failure status.</returns>
        public string getMessage()
        {
            return _message;
        }

        /// <summary>
        /// Set the status of this command.
        /// </summary>
        /// <param name="s">the new status code for this command.</param>
        public void setResult(Result s)
        {
            setResult(s, null);
        }

        /// <summary>
        /// Set the status of this command.
        /// </summary>
        /// <param name="s">the new status code for this command.</param>
        /// <param name="m">optional message explaining the new status.</param>
        public void setResult(Result s, string m)
        {
            _status = s;
            _message = m;
        }

        public void setRef(Ref r)
        {
            _refc = r;
        }

        public void setType(Type t)
        {
            _type = t;
        }

        public override string ToString()
        {
            return Enum.GetName(typeof(Type), getType()) + ": " + getOldId().Name + " " + getNewId().Name + " " + getRefName();
        }
    }

}