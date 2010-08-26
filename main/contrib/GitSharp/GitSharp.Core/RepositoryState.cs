/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com> 
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Core
{
    [Complete]
    public sealed class RepositoryState
    {
        public readonly static RepositoryState Apply = new RepositoryState(false, false, true, "Apply mailbox");
        public readonly static RepositoryState Safe = new RepositoryState(true, true, true, "Normal");
        public readonly static RepositoryState Merging = new RepositoryState(false, false, false, "Conflicts");
        public readonly static RepositoryState Rebasing = new RepositoryState(false, false, true, "Rebase/Apply mailbox");
        public readonly static RepositoryState RebasingRebasing = new RepositoryState(false, false, true, "Rebase");
        public readonly static RepositoryState RebasingMerge = new RepositoryState(false, false, true, "Rebase w/merge");
        public readonly static RepositoryState RebasingInteractive = new RepositoryState(false, false, true, "Rebase interactive");
        public readonly static RepositoryState Bisecting = new RepositoryState(true, false, false, "Bisecting");

        public bool CanCheckout { get; private set; }
        public bool CanResetHead { get; private set; }
        public bool CanCommit { get; private set; }
        public string Description { get; private set; }

        private RepositoryState(bool checkout, bool resetHead, bool commit, string description)
        {
            this.CanCheckout = checkout;
            this.CanResetHead = resetHead;
            this.CanCommit = commit;
            this.Description = description;
        }
    }
}
