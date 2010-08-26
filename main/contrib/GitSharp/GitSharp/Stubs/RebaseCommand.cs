/*
 * Copyright (C) 2010, Dominique van de Vorle <dvdvorle@gmail.com>
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

namespace GitSharp.Commands
{

    public class RebaseCommand
        : AbstractCommand
    {

        public RebaseCommand()
        {
        }

        // note: the naming of command parameters may not follow .NET conventions in favour of git command line parameter naming conventions.

        public List<string> Arguments { get; set; } // <--- [henon] what is that? the name must be clearer

        /// <summary>
        /// Not implemented
        /// 
        /// Restart the rebasing process after having resolved a merge conflict.
        /// 
        /// </summary>
        public bool Continue { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Restore the original branch and abort the rebase operation.
        /// 
        /// </summary>
        public bool Abort { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Restart the rebasing process by skipping the current patch.
        /// 
        /// </summary>
        public bool Skip { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use merging strategies to rebase.  When the recursive (default) merge
        /// strategy is used, this allows rebase to be aware of renames on the
        /// upstream side.
        /// +
        /// Note that a rebase merge works by replaying each commit from the working
        /// branch on top of the &lt;upstream&gt; branch.  Because of this, when a merge
        /// conflict happens, the side reported as 'ours' is the so-far rebased
        /// series, starting with &lt;upstream&gt;, and 'theirs' is the working branch.  In
        /// other words, the sides are swapped.
        /// 
        /// </summary>
        public bool Merge { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use the given merge strategy.
        /// If there is no `-s` option 'git-merge-recursive' is used
        /// instead.  This implies --merge.
        /// +
        /// Because 'git-rebase' replays each commit from the working branch
        /// on top of the &lt;upstream&gt; branch using the given strategy, using
        /// the 'ours' strategy simply discards all patches from the &lt;branch&gt;,
        /// which makes little sense.
        /// 
        /// </summary>
        public string Strategy { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Be quiet. Implies --no-stat.
        /// 
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Be verbose. Implies --stat.
        /// 
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show a diffstat of what changed upstream since the last rebase. The
        /// diffstat is also controlled by the configuration option rebase.stat.
        /// 
        /// </summary>
        public bool Stat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not show a diffstat as part of the rebase process.
        /// 
        /// </summary>
        public bool NoStat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option bypasses the pre-rebase hook.  See also linkgit:githooks[5].
        /// 
        /// </summary>
        public bool NoVerify { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Ensure at least n lines of surrounding context match before
        /// and after each change.  When fewer lines of surrounding
        /// context exist they all must match.  By default no context is
        /// ever ignored.
        /// 
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Force the rebase even if the current branch is a descendant
        /// of the commit you are rebasing onto.  Normally the command will
        /// exit with the message "Current branch is up to date" in such a
        /// situation.
        /// 
        /// </summary>
        public bool Forcerebase { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These flag are passed to the 'git-apply' program
        /// (see linkgit:git-apply[1]) that applies the patch.
        /// Incompatible with the --interactive option.
        /// 
        /// </summary>
        public string IgnoreWhitespace { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These flag are passed to the 'git-apply' program
        /// (see linkgit:git-apply[1]) that applies the patch.
        /// Incompatible with the --interactive option.
        /// 
        /// </summary>
        public string Whitespace { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These flags are passed to 'git-am' to easily change the dates
        /// of the rebased commits (see linkgit:git-am[1]).
        /// 
        /// </summary>
        public bool CommitterDateIsAuthorDate { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These flags are passed to 'git-am' to easily change the dates
        /// of the rebased commits (see linkgit:git-am[1]).
        /// 
        /// </summary>
        public bool IgnoreDate { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Make a list of the commits which are about to be rebased.  Let the
        /// user edit that list before rebasing.  This mode can also be used to
        /// split commits (see SPLITTING COMMITS below).
        /// 
        /// </summary>
        public bool Interactive { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of ignoring merges, try to recreate them.
        /// 
        /// </summary>
        public bool PreserveMerges { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Rebase all commits reachable from &lt;branch&gt;, instead of
        /// limiting them with an &lt;upstream&gt;.  This allows you to rebase
        /// the root commit(s) on a branch.  Must be used with --onto, and
        /// will skip changes already contained in &lt;newbase&gt; (instead of
        /// &lt;upstream&gt;).  When used together with --preserve-merges, 'all'
        /// root commits will be rewritten to have &lt;newbase&gt; as parent
        /// instead.
        /// 
        /// </summary>
        public string Root { get; set; }

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}


