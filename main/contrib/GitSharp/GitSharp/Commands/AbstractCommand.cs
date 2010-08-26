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
using GitSharp.Core;

namespace GitSharp.Commands
{
    /// <summary>
    /// Abstract base class of all git commands. It provides basic infrastructure
    /// </summary>
    public abstract class AbstractCommand : IGitCommand
    {
        /// <summary>
        /// Abbreviates a ref-name, used in internal output
        /// </summary>
        /// <param name="dst">long ref</param>
        /// <param name="abbreviateRemote">abbreviate as remote</param>
        /// <returns></returns>
        protected string AbbreviateRef(String dst, bool abbreviateRemote)
        {
            if (dst.StartsWith(Constants.R_HEADS))
                dst = dst.Substring(Constants.R_HEADS.Length);
            else if (dst.StartsWith(Constants.R_TAGS))
                dst = dst.Substring(Constants.R_TAGS.Length);
            else if (abbreviateRemote && dst.StartsWith(Constants.R_REMOTES))
                dst = dst.Substring(Constants.R_REMOTES.Length);
            return dst;
        }

        /// <summary>
        /// Performs upward recursive lookup to return git directory. Honors the environment variable GIT_DIR.
        /// </summary>
        /// <returns></returns>
        public static string FindGitDirectory(string rootDirectory, bool recursive, bool isBare)
        {
            string directory = null;
            string gitDir = null;
            string envGitDir = System.Environment.GetEnvironmentVariable("GIT_DIR");

            //Determine which git directory to use
            if (rootDirectory != null)         	//Directory specified by --git-dir 
                directory = rootDirectory;
            else if (envGitDir != null) 		//Directory specified by $GIT_DIR
                directory = envGitDir;
            else                        		//Current Directory
            {
                directory = Directory.GetCurrentDirectory();
                if (recursive)
                {
                    //Check for non-bare repositories
                    if (!isBare)
                    {
                        while (directory != null)
                        {
                            gitDir = Path.Combine(directory, Constants.DOT_GIT);
                            if (Directory.Exists(gitDir))
                                return directory;

                            //Get parent directory
                            string parentDirectory = Path.Combine(directory, "..");
                            parentDirectory = Path.GetFullPath(parentDirectory);
                            if (parentDirectory == directory)
                                return null;
                            directory = parentDirectory;
                        }
                    }
                    else
                    {
                        //Check for bare repositories
                        while (directory != null)
                        {
                            if (directory.EndsWith(Constants.DOT_GIT_EXT) && Directory.Exists(directory))
                                return directory;

                            //Get parent directory
                            directory = Path.Combine(directory, "..");
                            directory = Path.GetFullPath(directory);
                        }
                    }
                }
            }
            if (!directory.EndsWith(Constants.DOT_GIT_EXT))
            {
                if (!isBare)
                    directory = Path.Combine(directory, Constants.DOT_GIT);
                else
                    directory += Constants.DOT_GIT_EXT;
            }
            return directory;
        }

        /// <summary>
        /// Returns the value of the process' environment variable GIT_DIR
        /// </summary>
        protected string GIT_DIR
        {
            get
            {
                return System.Environment.GetEnvironmentVariable("GIT_DIR");
            }
        }

        /// <summary>
        /// This command's output stream. If not explicitly set, the command writes to Git.OutputStream out.
        /// </summary>
        public StreamWriter OutputStream
        {
            get
            {
                if (_output == null)
                    return Git.DefaultOutputStream;
                return _output;
            }
            set
            {
                _output = value;
            }
        }
        StreamWriter _output = null;

        /// <summary>
        /// The git repository that is either result of the command (init, clone) or subject to alteration (all other commands). 
        /// If not explicitly set, the command uses Git.Commands.Repository.
        /// 
        /// Note: InitCommand and CloneCommand ignore this property and overwrite it as a result of Execute.
        /// </summary>
        public Repository Repository
        {
            get
            {
                if (_repository == null)
                    return Git.DefaultRepository;
                return _repository;
            }
            set // <--- for the time being this is public settable. we need to refactor in order to remove the Repository property from Clone and Init
            {
                _repository = value;
            }
        }
        Repository _repository = null;

        /// <summary>
        /// The git directory. If not explicitly set, the command uses Git.GitDirectory.
        /// </summary>
        public virtual string GitDirectory
        {
            get
            {
                if (_gitDirectory == null)
                    return Git.DefaultGitDirectory;
                return _gitDirectory;
            }
            set
            {
                _gitDirectory = value;
            }
        }
        protected string _gitDirectory = null;

        /// <summary>
        /// Get the directory where the Init command will initialize the repository. if GitDirectory is null ActualDirectory is used to initialize the repository.
        /// </summary>
        public virtual string ActualDirectory
        {
            get
            {
                return FindGitDirectory(GitDirectory, false, false);
            }
        }

        /// <summary>
        /// Execute the git command.
        /// </summary>
        public abstract void Execute();

    }
}