/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using NGit;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>The standard "user" configuration parameters.</summary>
	/// <remarks>The standard "user" configuration parameters.</remarks>
	public class UserConfig
	{
		private sealed class _SectionParser_53 : Config.SectionParser<NGit.UserConfig>
		{
			public _SectionParser_53()
			{
			}

			public NGit.UserConfig Parse(Config cfg)
			{
				return new NGit.UserConfig(cfg);
			}
		}

		/// <summary>
		/// Key for
		/// <see cref="Config.Get{T}(SectionParser{T})">Config.Get&lt;T&gt;(SectionParser&lt;T&gt;)
		/// 	</see>
		/// .
		/// </summary>
		public static readonly Config.SectionParser<NGit.UserConfig> KEY = new _SectionParser_53
			();

		private readonly string authorName;

		private readonly string authorEmail;

		private readonly string committerName;

		private readonly string committerEmail;

		private UserConfig(Config rc)
		{
			authorName = GetNameInternal(rc, Constants.GIT_AUTHOR_NAME_KEY);
			authorEmail = GetEmailInternal(rc, Constants.GIT_AUTHOR_EMAIL_KEY);
			committerName = GetNameInternal(rc, Constants.GIT_COMMITTER_NAME_KEY);
			committerEmail = GetEmailInternal(rc, Constants.GIT_COMMITTER_EMAIL_KEY);
		}

		/// <returns>
		/// the author name as defined in the git variables and
		/// configurations. If no name could be found, try to use the system
		/// user name instead.
		/// </returns>
		public virtual string GetAuthorName()
		{
			return authorName;
		}

		/// <returns>
		/// the committer name as defined in the git variables and
		/// configurations. If no name could be found, try to use the system
		/// user name instead.
		/// </returns>
		public virtual string GetCommitterName()
		{
			return committerName;
		}

		/// <returns>
		/// the author email as defined in git variables and
		/// configurations. If no email could be found, try to
		/// propose one default with the user name and the
		/// host name.
		/// </returns>
		public virtual string GetAuthorEmail()
		{
			return authorEmail;
		}

		/// <returns>
		/// the committer email as defined in git variables and
		/// configurations. If no email could be found, try to
		/// propose one default with the user name and the
		/// host name.
		/// </returns>
		public virtual string GetCommitterEmail()
		{
			return committerEmail;
		}

		private static string GetNameInternal(Config rc, string envKey)
		{
			// try to get the user name from the local and global configurations.
			string username = rc.GetString("user", null, "name");
			if (username == null)
			{
				// try to get the user name for the system property GIT_XXX_NAME
				username = System().Getenv(envKey);
			}
			if (username == null)
			{
				// get the system user name
				username = System().GetProperty(Constants.OS_USER_NAME_KEY);
			}
			if (username == null)
			{
				username = Constants.UNKNOWN_USER_DEFAULT;
			}
			return username;
		}

		private static string GetEmailInternal(Config rc, string envKey)
		{
			// try to get the email from the local and global configurations.
			string email = rc.GetString("user", null, "email");
			if (email == null)
			{
				// try to get the email for the system property GIT_XXX_EMAIL
				email = System().Getenv(envKey);
			}
			if (email == null)
			{
				// try to construct an email
				string username = System().GetProperty(Constants.OS_USER_NAME_KEY);
				if (username == null)
				{
					username = Constants.UNKNOWN_USER_DEFAULT;
				}
				email = username + "@" + System().GetHostname();
			}
			return email;
		}

		private static SystemReader System()
		{
			return SystemReader.GetInstance();
		}
	}
}
