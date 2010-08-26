/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.IO;

namespace GitSharp.Commands
{
    /// <summary>
    /// git-init - Create an empty git repository or reinitialize an existing one 
    /// </summary>
    public class InitCommand : AbstractCommand
    {
        public InitCommand()
        {
            Quiet = true; // <-- [henon] since this command will be used more often programmatically than by CLI quiet=true is the better default.
            Shared = "false";
        }

        #region Properties / Options

        /// <summary>
        /// Get the directory where the Init command will initialize the repository. if GitDirectory is null ActualDirectory is used to initialize the repository.
        /// </summary>
        public override string ActualDirectory
        {
            get
            {
                return FindGitDirectory(GitDirectory, false, Bare);
            }
        }

        /// <summary>
        /// Only print error and warning messages, all other output will be suppressed. Is True by default.
        /// </summary>
        public bool Quiet
        {
            get;
            set;
        }

        /// <summary>
        /// Create a bare repository. If GIT_DIR environment is not set, it is set to the current working directory. Is False by default.
        /// </summary>
        public bool Bare
        {
            get;
            set;
        }

        /// <summary>
        /// NOT IMPLEMENTED!
        /// Provide the directory from which templates will be used. The default template directory is /usr/share/git-core/templates.
        ///     
        /// When specified, <see cref="Template"/> is used as the source of the template files rather than the default. The template files include some directory structure, some suggested "exclude patterns", and copies of non-executing "hook" files. The suggested patterns and hook files are all modifiable and extensible.
        /// </summary>
        public string Template
        {
            get;
            set;
        }

        /// <summary>
        /// NOT IMPLEMENTED!
        ///     Specify that the git repository is to be shared amongst several users. This allows users belonging to the same group to push into that repository. When specified, the config variable "core.sharedRepository" is set so that files and directories under $GIT_DIR are created with the requested permissions. When not specified, git will use permissions reported by umask(2).
        ///     The option can have the following values, defaulting to group if no value is given:
        ///     * umask (or false): Use permissions reported by umask(2). The default, when --shared is not specified.
        ///     * group (or true): Make the repository group-writable, (and g+sx, since the git group may be not the primary group of all users). This is used to loosen the permissions of an otherwise safe umask(2) value. Note that the umask still applies to the other permission bits (e.g. if umask is 0022, using group will not remove read privileges from other (non-group) users). See 0xxx for how to exactly specify the repository permissions.
        ///     * all (or world or everybody): Same as group, but make the repository readable by all users.
        ///     * 0xxx: 0xxx is an octal number and each file will have mode 0xxx. 0xxx will override users' umask(2) value (and not only loosen permissions as group and all does). 0640 will create a repository which is group-readable, but not group-writable or accessible to others. 0660 will create a repo that is readable and writable to the current user and group, but inaccessible to others.
        ///     By default, the configuration flag receive.denyNonFastForwards is enabled in shared repositories, so that you cannot force a non fast-forwarding push into it.
        /// </summary>
        public string Shared
        {
            get;
            set;
        }


        #endregion

        /// <summary>
        /// Execute the command.
        /// </summary>
        public override void Execute()
        {
            using (var repo = new GitSharp.Core.Repository(new DirectoryInfo(ActualDirectory)))
            {
                var reinit = repo.Create(Bare);
                if (!Quiet)
                {
                    OutputStream.WriteLine(String.Format("{0} empty Git repository in {1}", reinit ? "Reinitialized" : "Initialized", repo.Directory.FullName));
                    OutputStream.Flush();
                }
                Repository = new Repository(repo);
            }
        }
    }
}