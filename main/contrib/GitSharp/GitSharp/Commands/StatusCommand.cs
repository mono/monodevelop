/*
 * Copyright (C) 2010, Rolenun <rolenun@gmail.com>
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
using GitSharp.Core;

namespace GitSharp.Commands
{
    #region Status Command
    public class StatusCommand : AbstractCommand
    {
    	private StatusResults results = new StatusResults();

        public StatusCommand()
        {
        }

        public Boolean AnyDifferences { get { return Repository.Status.AnyDifferences; } }
        public int IndexSize { get; private set; }
        public StatusResults Results 
        {
        	get { return results; } 
        	private set { results = value; }
        }
        public Boolean IsEmptyRepository { get { return IndexSize <= 0; } }

        public override void Execute()
        {
            RepositoryStatus status = new RepositoryStatus(Repository);
            
            IgnoreRules rules;

            //Read ignore file list and remove from the untracked list
            try
            {
                rules = new IgnoreRules(Path.Combine(Repository.WorkingDirectory, ".gitignore"));
            }
            catch (FileNotFoundException) 
            {
                //.gitignore file does not exist for a newly initialized repository.
                string[] lines = {};
                rules = new IgnoreRules(lines);
            } 

            foreach (string hash in status.Untracked)
            {
                string path = Path.Combine(Repository.WorkingDirectory, hash);
                if (!rules.IgnoreFile(Repository.WorkingDirectory, path) && !rules.IgnoreDir(Repository.WorkingDirectory, path))
                {
                    results.UntrackedList.Add(hash);
                }
            }
            
            if (status.AnyDifferences || results.UntrackedList.Count > 0)
            {
                // Files use the following StatusTypes: removed, missing, added, and modified, modified w/staged, and merge conflict.
                // The following StatusStates are defined for each type:
                //              Modified -> Unstaged
                //         MergeConflict -> Unstaged
                //                 Added -> Staged
                //        ModifiedStaged -> Staged
                //               Removed -> Staged
                //               Missing -> Staged
                // The StatusState known as "Untracked" is determined by what is *not* defined in any state.
                // It is then intersected with the .gitignore list to determine what should be listed as untracked.

                HashSet<string> hset = new HashSet<string>(status.MergeConflict);
                foreach (string hash in hset)
                {
                    results.ModifiedList.Add(hash, StatusType.MergeConflict);
                    status.Staged.Remove(hash);
                    status.Modified.Remove(hash);
                }

                hset = new HashSet<string>(status.Missing);
                foreach (string hash in hset)
                    results.ModifiedList.Add(hash, StatusType.Missing);

                hset = new HashSet<string>(status.Modified);
                foreach (string hash in hset)
                    results.ModifiedList.Add(hash, StatusType.Modified);

                hset = new HashSet<string>(status.Staged);
                foreach (string hash in hset)
                    results.StagedList.Add(hash, StatusType.ModifiedStaged);

                hset = new HashSet<string>(status.Added);
                foreach (string hash in hset)
                    results.StagedList.Add(hash, StatusType.Added);

                hset = new HashSet<string>(status.Removed);
                foreach (string hash in hset)
                    results.StagedList.Add(hash, StatusType.Removed);

                results.UntrackedList.Sort();
                results.ModifiedList.OrderBy(v => v.Key);
                results.StagedList.OrderBy(v => v.Key);
            }

            IndexSize = Repository.Index.Size;
        }

    }
    #endregion

    #region Status Results

    public class StatusResults
    {
        public List<string> UntrackedList = new List<string>();
        public Dictionary<string, int> StagedList = new Dictionary<string, int>();
        public Dictionary<string, int> ModifiedList = new Dictionary<string, int>();

        public void Clear()
        {
        	UntrackedList.Clear();
        	StagedList.Clear();
        	ModifiedList.Clear();
        }
        
        // Returns all available matches of a file and its status based on filename.
        public Dictionary<string,int> Search(string filepattern)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (string hash in UntrackedList)
            {
                if (hash.Contains(filepattern))
                {
                    result.Add(hash, StatusState.Untracked);
                }
            }
            foreach (string key in ModifiedList.Keys)
            {
                if (key.Contains(filepattern))
                {
                    result.Add(key, StatusState.Modified);
                }
            }
            foreach (string key in StagedList.Keys)
            {
                if (key.Contains(filepattern))
                {
                    result.Add(key, StatusState.Staged);
                }
            }

            return result;
        }
        
        public bool Contains(string filepattern, int state)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            if (state == StatusState.Untracked)
            {
                foreach (string hash in UntrackedList)
                {
                    if (hash.Contains(filepattern))
                    {
                        return true;
                    }
                }
            }

            if (state == StatusState.Modified)
            {
                foreach (string key in ModifiedList.Keys)
                {
                    if (key.Contains(filepattern))
                    {
                        return true;
                    }
                }
            }

            if (state == StatusState.Staged)
            {
                foreach (string key in StagedList.Keys)
                {
                    if (key.Contains(filepattern))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }

    #endregion

    #region Status Types
    
    public struct StatusState
    {
        public const int Staged = 1;
        public const int Modified = 2;
        public const int Untracked = 3;
    }

    public struct StatusType
    {
        public const int Added = 1;
        public const int Modified = 2;
        public const int Removed = 3;
        public const int Missing = 4;
        public const int MergeConflict = 5;
        public const int ModifiedStaged = 6;
    }

    #endregion

}