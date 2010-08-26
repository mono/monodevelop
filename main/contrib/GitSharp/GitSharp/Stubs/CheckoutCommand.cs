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
    public class CheckoutCommand : AbstractCommand
    {
        private CheckoutResults results = new CheckoutResults();

        public CheckoutCommand() {
        }

        public CheckoutResults Results
        {
            get { return results; }
            private set { results = value; }
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Quiet, suppress feedback messages.
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When switching branches, proceed even if the index or the
        /// working tree differs from HEAD.  This is used to throw away
        /// local changes.
        /// +
        /// When checking out paths from the index, do not fail upon unmerged
        /// entries; instead, unmerged entries are ignored.
        /// 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When checking out paths from the index, check out stage #2
        /// ('ours') or #3 ('theirs') for unmerged paths.
        /// 
        /// </summary>
        public bool Ours { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When checking out paths from the index, check out stage #2
        /// ('ours') or #3 ('theirs') for unmerged paths.
        /// 
        /// </summary>
        public bool Theirs { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Create a new branch named &lt;new_branch&gt; and start it at
        /// &lt;start_point&gt;; see linkgit:git-branch[1] for details.
        /// 
        /// </summary>
        public string BranchCreate { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When creating a new branch, set up "upstream" configuration. See
        /// "--track" in linkgit:git-branch[1] for details.
        /// +
        /// If no '-b' option is given, the name of the new branch will be
        /// derived from the remote branch.  If "remotes/" or "refs/remotes/"
        /// is prefixed it is stripped away, and then the part up to the
        /// next slash (which would be the nickname of the remote) is removed.
        /// This would tell us to use "hack" as the local branch when branching
        /// off of "origin/hack" (or "remotes/origin/hack", or even
        /// "refs/remotes/origin/hack").  If the given name has no slash, or the above
        /// guessing results in an empty name, the guessing is aborted.  You can
        /// explicitly give a name with '-b' in such a case.
        /// 
        /// </summary>
        public bool Track { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not set up "upstream" configuration, even if the
        /// branch.autosetupmerge configuration variable is true.
        /// 
        /// </summary>
        public bool NoTrack { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Create the new branch's reflog; see linkgit:git-branch[1] for
        /// details.
        /// 
        /// </summary>
        public bool RefLog { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When switching branches,
        /// if you have local modifications to one or more files that
        /// are different between the current branch and the branch to
        /// which you are switching, the command refuses to switch
        /// branches in order to preserve your modifications in context.
        /// However, with this option, a three-way merge between the current
        /// branch, your working tree contents, and the new branch
        /// is done, and you will be on the new branch.
        /// +
        /// When a merge conflict happens, the index entries for conflicting
        /// paths are left unmerged, and you need to resolve the conflicts
        /// and mark the resolved paths with `git add` (or `git rm` if the merge
        /// should result in deletion of the path).
        /// +
        /// When checking out paths from the index, this option lets you recreate
        /// the conflicted merge in the specified paths.
        /// 
        /// </summary>
        public bool Merge { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// The same as --merge option above, but changes the way the
        /// conflicting hunks are presented, overriding the
        /// merge.conflictstyle configuration variable.  Possible values are
        /// "merge" (default) and "diff3" (in addition to what is shown by
        /// "merge" style, shows the original contents).
        /// 
        /// </summary>
        public string Conflict { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Interactively select hunks in the difference between the
        /// &lt;tree-ish&gt; (or the index, if unspecified) and the working
        /// tree.  The chosen hunks are then applied in reverse to the
        /// working tree (and if a &lt;tree-ish&gt; was specified, the index).
        /// +
        /// This means that you can use `git checkout -p` to selectively discard
        /// edits from your current working tree.
        /// 
        /// </summary>
        public bool Patch { get; set; }

        #endregion

        public override void Execute()
        {

            if (Patch && (Track || BranchCreate.Length > 0 || RefLog || Merge || Force))
                throw new ArgumentException("--patch is incompatible with all other options");

            if (Force && Merge)
                throw new ArgumentException("git checkout: -f and -m are incompatible");
            
			if (Track && !(BranchCreate.Length > 0))
				throw new ArgumentException("Missing branch name; try -b");
			
            if (Arguments.Count == 0)
                Arguments.Add("HEAD");

            //Create a new branch and reassign the Repository to the new location before checkout.
            if (BranchCreate.Length > 0)
            {
                Branch.Create(Repository, BranchCreate);
                Arguments[0] = BranchCreate;
            }

            if (Arguments.Count == 1)
            {
                //Checkout the branch using the SHA1 or the name such as HEAD or master/HEAD
                Branch b = new Branch(Repository, Arguments[0]);
                if (b != null)
                {
                    b.Checkout();
                    return;
                }
            }

            //Todo: Add FileNameMatcher support. To be added when fnmatch is completed.
            //For now, pattern matching is not allowed. Please specify the files only.               

            //Gain access to the Git index using the repository determined before command execution
            Index index = new Index(Repository);

            foreach (string arg in Arguments)
            {
                //Use full paths only to eliminate platform-based directory differences
                string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), arg));

                //Perform the validity tests outside of the index to handle the error messages
                if ((new FileInfo(path).Exists) || (new DirectoryInfo(path).Exists))
                    index.Checkout(path);
                else 
                    results.FileNotFoundList.Add(path);
            }
        }
    }

    #region Checkout Results

    public class CheckoutResults
    {
        public List<string> FileNotFoundList = new List<string>();
    }

    #endregion
}
