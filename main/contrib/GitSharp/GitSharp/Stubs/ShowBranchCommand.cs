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
using System.IO;
using System.Linq;
using System.Text;

namespace GitSharp.Commands
{
    public class ShowBranchCommand
        : AbstractCommand
    {

        public ShowBranchCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Show the remote-tracking branches.
        /// 
        /// </summary>
        public bool Remotes { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show both remote-tracking branches and local branches.
        /// 
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// With this option, the command includes the current
        /// branch to the list of revs to be shown when it is not
        /// given on the command line.
        /// 
        /// </summary>
        public bool Current { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///         By default, the branches and their commits are shown in
        ///         reverse chronological order.  This option makes them
        ///         appear in topological order (i.e., descendant commits
        ///         are shown before their parents).
        /// 
        /// </summary>
        public bool TopoOrder { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option is similar to '--topo-order' in the sense that no
        /// parent comes before all of its children, but otherwise commits
        /// are ordered according to their commit date.
        /// 
        /// </summary>
        public bool DateOrder { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// By default, the output omits merges that are reachable
        /// from only one tip being shown.  This option makes them
        /// visible.
        /// 
        /// </summary>
        public bool Sparse { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually the command stops output upon showing the commit
        /// that is the common ancestor of all the branches.  This
        /// flag tells the command to go &lt;n&gt; more common commits
        /// beyond that.  When &lt;n&gt; is negative, display only the
        /// &lt;reference&gt;s given, without showing the commit ancestry
        /// tree.
        /// 
        /// </summary>
        public string More { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Synonym to `--more=-1`
        /// 
        /// </summary>
        public bool List { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of showing the commit list, determine possible
        /// merge bases for the specified commits. All merge bases
        /// will be contained in all specified commits. This is
        /// different from how linkgit:git-merge-base[1] handles
        /// the case of three or more commits.
        /// 
        /// </summary>
        public bool MergeBase { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Among the &lt;reference&gt;s given, display only the ones that
        /// cannot be reached from any other &lt;reference&gt;.
        /// 
        /// </summary>
        public string Independent { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not show naming strings for each commit.
        /// 
        /// </summary>
        public bool NoName { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of naming the commits using the path to reach
        /// them from heads (e.g. "master~2" to mean the grandparent
        /// of "master"), name them with the unique prefix of their
        /// object names.
        /// 
        /// </summary>
        public bool Sha1Name { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Shows only commits that are NOT on the first branch given.
        /// This helps track topic branches by hiding any commit that
        /// is already in the main line of development.  When given
        /// "git show-branch --topics master topic1 topic2", this
        /// will show the revisions given by "git rev-list {caret}master
        /// topic1 topic2"
        /// 
        /// </summary>
        public bool Topics { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Shows &lt;n&gt; most recent ref-log entries for the given
        /// ref.  If &lt;base&gt; is given, &lt;n&gt; entries going back from
        /// that entry.  &lt;base&gt; can be specified as count or date.
        /// When no explicit &lt;ref&gt; parameter is given, it defaults to the
        /// current branch (or `HEAD` if it is detached).
        /// 
        /// </summary>
        public string Reflog { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Color the status sign (one of these: `*` `!` `+` `-`) of each commit
        /// corresponding to the branch it's in.
        /// 
        /// </summary>
        public bool Color { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Turn off colored output, even when the configuration file gives the
        /// default to color output.
        /// 
        /// </summary>
        public bool NoColor { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
