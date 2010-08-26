/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Thad Hughes <thadh@thad.corp.google.com>
 * Copyright (C) 2009, JetBrains s.r.o.
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

using System.IO;
using GitSharp.Core.Util;

namespace GitSharp.Core
{

    public class RepositoryConfig : FileBasedConfig
    {
        public const string BRANCH_SECTION = "branch";

        public RepositoryConfig(Repository repo)
            : this(SystemReader.getInstance().openUserConfig(), new FileInfo(Path.Combine(repo.Directory.FullName, "config")))
        {
			if (repo == null)
				throw new System.ArgumentNullException ("repo");
            
        }

        public RepositoryConfig(Config @base, FileInfo cfgLocation)
            : base(@base, cfgLocation)
        {
            
        }

        public CoreConfig getCore()
        {
            return get(CoreConfig.KEY);
        }

        public TransferConfig getTransfer()
        {
            return get(TransferConfig.KEY);
        }

        public UserConfig getUserConfig()
        {
            return get(UserConfig.KEY);
        }

        public string getAuthorName()
        {
            return getUserConfig().getAuthorName();
        }

        public string getCommitterName()
        {
            return getUserConfig().getCommitterName();
        }

        public string getAuthorEmail()
        {
            return getUserConfig().getAuthorEmail();
        }

        public string getCommitterEmail()
        {
            return getUserConfig().getCommitterEmail();
        }
    }

}