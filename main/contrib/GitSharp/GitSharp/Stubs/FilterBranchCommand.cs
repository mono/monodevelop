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
    public class FilterBranchCommand
        : AbstractCommand
    {

        public FilterBranchCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// This filter may be used if you only need to modify the environment
        /// in which the commit will be performed.  Specifically, you might
        /// want to rewrite the author/committer name/email/time environment
        /// variables (see linkgit:git-commit[1] for details).  Do not forget
        /// to re-export the variables.
        /// 
        /// </summary>
        public string EnvFilter { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This is the filter for rewriting the tree and its contents.
        /// The argument is evaluated in shell with the working
        /// directory set to the root of the checked out tree.  The new tree
        /// is then used as-is (new files are auto-added, disappeared files
        /// are auto-removed - neither .gitignore files nor any other ignore
        /// rules *HAVE ANY EFFECT*!).
        /// 
        /// </summary>
        public string TreeFilter { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This is the filter for rewriting the index.  It is similar to the
        /// tree filter but does not check out the tree, which makes it much
        /// faster.  Frequently used with `git rm \--cached
        /// \--ignore-unmatch ...`, see EXAMPLES below.  For hairy
        /// cases, see linkgit:git-update-index[1].
        /// 
        /// </summary>
        public string IndexFilter { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This is the filter for rewriting the commit's parent list.
        /// It will receive the parent string on stdin and shall output
        /// the new parent string on stdout.  The parent string is in
        /// the format described in linkgit:git-commit-tree[1]: empty for
        /// the initial commit, "-p parent" for a normal commit and
        /// "-p parent1 -p parent2 -p parent3 ..." for a merge commit.
        /// 
        /// </summary>
        public string ParentFilter { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This is the filter for rewriting the commit messages.
        /// The argument is evaluated in the shell with the original
        /// commit message on standard input; its standard output is
        /// used as the new commit message.
        /// 
        /// </summary>
        public string MsgFilter { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This is the filter for performing the commit.
        /// If this filter is specified, it will be called instead of the
        /// 'git-commit-tree' command, with arguments of the form
        /// "&lt;TREE_ID&gt; [-p &lt;PARENT_COMMIT_ID&gt;]..." and the log message on
        /// stdin.  The commit id is expected on stdout.
        /// +
        /// As a special extension, the commit filter may emit multiple
        /// commit ids; in that case, the rewritten children of the original commit will
        /// have all of them as parents.
        /// +
        /// You can use the 'map' convenience function in this filter, and other
        /// convenience functions, too.  For example, calling 'skip_commit "$@"'
        /// will leave out the current commit (but not its changes! If you want
        /// that, use 'git-rebase' instead).
        /// +
        /// You can also use the 'git_commit_non_empty_tree "$@"' instead of
        /// 'git commit-tree "$@"' if you don't wish to keep commits with a single parent
        /// and that makes no change to the tree.
        /// 
        /// </summary>
        public string CommitFilter { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This is the filter for rewriting tag names. When passed,
        /// it will be called for every tag ref that points to a rewritten
        /// object (or to a tag object which points to a rewritten object).
        /// The original tag name is passed via standard input, and the new
        /// tag name is expected on standard output.
        /// +
        /// The original tags are not deleted, but can be overwritten;
        /// use "--tag-name-filter cat" to simply update the tags.  In this
        /// case, be very careful and make sure you have the old tags
        /// backed up in case the conversion has run afoul.
        /// +
        /// Nearly proper rewriting of tag objects is supported. If the tag has
        /// a message attached, a new tag object will be created with the same message,
        /// author, and timestamp. If the tag has a signature attached, the
        /// signature will be stripped. It is by definition impossible to preserve
        /// signatures. The reason this is "nearly" proper, is because ideally if
        /// the tag did not change (points to the same object, has the same name, etc.)
        /// it should retain any signature. That is not the case, signatures will always
        /// be removed, buyer beware. There is also no support for changing the
        /// author or timestamp (or the tag message for that matter). Tags which point
        /// to other tags will be rewritten to point to the underlying commit.
        /// 
        /// </summary>
        public string TagNameFilter { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only look at the history which touches the given subdirectory.
        /// The result will contain that directory (and only that) as its
        /// project root.  Implies --remap-to-ancestor.
        /// 
        /// </summary>
        public string SubdirectoryFilter { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Rewrite refs to the nearest rewritten ancestor instead of
        /// ignoring them.
        /// +
        /// Normally, positive refs on the command line are only changed if the
        /// commit they point to was rewritten.  However, you can limit the extent
        /// of this rewriting by using linkgit:rev-list[1] arguments, e.g., path
        /// limiters.  Refs pointing to such excluded commits would then normally
        /// be ignored.  With this option, they are instead rewritten to point at
        /// the nearest ancestor that was not excluded.
        /// 
        /// </summary>
        public bool RemapToAncestor { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Some kind of filters will generate empty commits, that left the tree
        /// untouched.  This switch allow git-filter-branch to ignore such
        /// commits.  Though, this switch only applies for commits that have one
        /// and only one parent, it will hence keep merges points. Also, this
        /// option is not compatible with the use of '--commit-filter'. Though you
        /// just need to use the function 'git_commit_non_empty_tree "$@"' instead
        /// of the 'git commit-tree "$@"' idiom in your commit filter to make that
        /// happen.
        /// 
        /// </summary>
        public bool PruneEmpty { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use this option to set the namespace where the original commits
        /// will be stored. The default value is 'refs/original'.
        /// 
        /// </summary>
        public string Original { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use this option to set the path to the temporary directory used for
        /// rewriting.  When applying a tree filter, the command needs to
        /// temporarily check out the tree to some directory, which may consume
        /// considerable space in case of large projects.  By default it
        /// does this in the '.git-rewrite/' directory but you can override
        /// that choice by this parameter.
        /// 
        /// </summary>
        public string D { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// 'git-filter-branch' refuses to start with an existing temporary
        /// directory or when there are already refs starting with
        /// 'refs/original/', unless forced.
        /// 
        /// </summary>
        public bool Force { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
