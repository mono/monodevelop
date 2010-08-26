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

using System.Collections.Generic;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// A simple no-op hook.
    /// </summary>
    internal class NULLReceiveHook : IPostReceiveHook
    {
        public void OnPostReceive(ReceivePack rp, ICollection<ReceiveCommand> commands)
        {
        }
    }

    public static class PostReceiveHook
    {
        public static IPostReceiveHook NULL = new NULLReceiveHook();
    }

    /// <summary>
    /// Hook invoked by {@link ReceivePack} after all updates are executed.
    /// <para/>
    /// The hook is called after all commands have been processed. Only commands with
    /// a status of {@link ReceiveCommand.Result#OK} are passed into the hook. To get
    /// all commands within the hook, see {@link ReceivePack#getAllCommands()}.
    /// <para/>
    /// Any post-receive hook implementation should not update the status of a
    /// command, as the command has already completed or failed, and the status has
    /// already been returned to the client.
    /// <para/>
    /// Hooks should execute quickly, as they block the server and the client from
    /// completing the connection.
    /// </summary>
    public interface IPostReceiveHook
    {
        /// <summary>
        /// Invoked after all commands are executed and status has been returned.
        /// </summary>
        /// <param name="rp">
        /// the process handling the current receive. Hooks may obtain
        /// details about the destination repository through this handle.
        /// </param>
        /// <param name="commands">
        /// unmodifiable set of successfully completed commands. May be
        /// the empty set.
        /// </param>
        void OnPostReceive(ReceivePack rp, ICollection<ReceiveCommand> commands);
    }

}