/*
 * Copyright (C) 2009, Stefan Schake <caytchen@gmail.com>
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
using System.IO;
using GitSharp.Core.Transport;

namespace GitSharp.Core
{

    public class SubmoduleEntry
    {
		[Serializable]
        public enum UpdateMethod
        {
            Checkout,
            Rebase,
            Merge
        }

        public string Name { get; private set; }
        public string Path { get; private set; }
        public URIish Url { get; private set; }
        public UpdateMethod Update { get; private set; }

        public SubmoduleEntry(string name, string path, URIish url, UpdateMethod update)
        {
            Name = name;
            Path = path;
            Url = url;
            Update = update;
        }
    }

    public class SubmoduleConfig : FileBasedConfig
    {
        public SubmoduleConfig(Config cfg, Repository db)
            : base(cfg, new FileInfo(Path.Combine(db.WorkingDirectory.FullName, ".gitmodules")))
        {
        }

        public SubmoduleConfig(Repository db)
            : this(null, db)
        {
        }

        public int SubmoduleCount
        {
            get
            {
                return getSubsections("submodule").Count;
            }
        }

        public SubmoduleEntry GetEntry(int index)
        {
            string name = getSubsections("submodule")[index];
            string path = getString("submodule", name, "path");
            string url = getString("submodule", name, "url");
            string update = getString("submodule", name, "update");

            SubmoduleEntry.UpdateMethod method;
            switch (update)
            {
                case "rebase":
                    method = SubmoduleEntry.UpdateMethod.Rebase;
                    break;

                case "merge":
                    method = SubmoduleEntry.UpdateMethod.Merge;
                    break;

                default:
                    method = SubmoduleEntry.UpdateMethod.Checkout;
                    break;
            }

            return new SubmoduleEntry(name, path, new URIish(url), method);
        }

        public void AddEntry(SubmoduleEntry entry)
        {
			if (entry == null)
				throw new System.ArgumentNullException ("entry");
			
            setString("submodule", entry.Name, "path", entry.Path);
            setString("submodule", entry.Name, "url", entry.Url.ToPrivateString());
            if (entry.Update != SubmoduleEntry.UpdateMethod.Checkout)
                setString("submodule", entry.Name, "update", entry.Update.ToString().ToLowerInvariant());
        }
    }

}