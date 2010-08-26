/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

using ICSharpCode.SharpZipLib.Zip.Compression;

namespace GitSharp.Core
{
    public class CoreConfig
    {
        private class SectionParser : Config.SectionParser<CoreConfig>
        {
            public CoreConfig parse(Config cfg)
            {
                return new CoreConfig(cfg);
            }
        }

        public static Config.SectionParser<CoreConfig> KEY = new SectionParser();

        private readonly int compression;
        private readonly int packIndexVersion;
        private readonly bool logAllRefUpdates;
        private readonly string excludesFile;

        private CoreConfig(Config rc)
        {
            compression = rc.getInt("core", "compression", Deflater.DEFAULT_COMPRESSION);
            packIndexVersion = rc.getInt("pack", "indexversion", 2);
            logAllRefUpdates = rc.getBoolean("core", "logallrefupdates", true);
            excludesFile = rc.getString("core", null, "excludesfile");
        }

        public string getExcludesFile()
        {
            return excludesFile;
        }

        public int getCompression()
        {
            return compression;
        }

        public int getPackIndexVersion()
        {
            return packIndexVersion;
        }

        ///<summary>
        ///Return whether to log all refUpdates
        ///</summary>
        public bool isLogAllRefUpdates()
        {
            return logAllRefUpdates;
        }
    }
}
