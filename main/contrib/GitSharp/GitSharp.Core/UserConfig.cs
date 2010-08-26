/*
 * Copyright (C) 2009, Yann Simon <yann.simon.fr@gmail.com>
 * Copyright (C) 2009, Google Inc.
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

using GitSharp.Core.Util;

namespace GitSharp.Core
{

    public class UserConfig
    {
        private class SectionParser : Config.SectionParser<UserConfig>
        {
            public UserConfig parse(Config cfg)
            {
                return new UserConfig(cfg);
            }
        }

        public static Config.SectionParser<UserConfig> KEY = new SectionParser();

        private readonly string authorName;
        private readonly string authorEmail;
        private readonly string committerName;
        private readonly string committerEmail;

        public UserConfig(Config rc)
        {
            authorName = getNameInternal(rc, Constants.GIT_AUTHOR_NAME_KEY);
            authorEmail = getEmailInternal(rc, Constants.GIT_AUTHOR_EMAIL_KEY);

            committerName = getNameInternal(rc, Constants.GIT_COMMITTER_NAME_KEY);
            committerEmail = getEmailInternal(rc, Constants.GIT_COMMITTER_EMAIL_KEY);
        }

        public string getAuthorName()
        {
            return authorName;
        }

        public string getCommitterName()
        {
            return committerName;
        }

        public string getAuthorEmail()
        {
            return authorEmail;
        }
        
        public string getCommitterEmail()
        {
            return committerEmail;
        }

        private string getNameInternal(Config rc, string envKey)
        {
            string username = rc.getString("user", null, "name");

            if (username == null)
            {
                username = system().getenv(envKey);
            }
            if (username == null)
            {
                username = system().getProperty(Constants.OS_USER_NAME_KEY);
            }
            if (username == null)
            {
                username = Constants.UNKNOWN_USER_DEFAULT;
            }

            return username;
        }

        private string getEmailInternal(Config rc, string envKey)
        {
            string email = rc.getString("user", null, "email");

            if (email == null)
            {
                email = system().getenv(envKey);
            }
            if (email == null)
            {
                string username = system().getProperty(Constants.OS_USER_NAME_KEY);
                if (username == null)
                {
                    username = Constants.UNKNOWN_USER_DEFAULT;
                }
                email = username + "@" + system().getHostname();
            }

            return email;
        }

        private SystemReader system()
        {
            return SystemReader.getInstance();
        }
    }

}