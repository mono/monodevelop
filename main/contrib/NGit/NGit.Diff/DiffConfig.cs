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
using Sharpen;

namespace NGit.Diff
{
	/// <summary>Keeps track of diff related configuration options.</summary>
	/// <remarks>Keeps track of diff related configuration options.</remarks>
	public class DiffConfig
	{
		private sealed class _SectionParser_52 : Config.SectionParser<NGit.Diff.DiffConfig
			>
		{
			public _SectionParser_52()
			{
			}

			public NGit.Diff.DiffConfig Parse(Config cfg)
			{
				return new NGit.Diff.DiffConfig(cfg);
			}
		}

		/// <summary>
		/// Key for
		/// <see cref="NGit.Config.Get{T}(NGit.Config.SectionParser{T})">NGit.Config.Get&lt;T&gt;(NGit.Config.SectionParser&lt;T&gt;)
		/// 	</see>
		/// .
		/// </summary>
		public static readonly Config.SectionParser<NGit.Diff.DiffConfig> KEY = new _SectionParser_52
			();

		private readonly bool noPrefix;

		private readonly bool renames;

		private readonly int renameLimit;

		private DiffConfig(Config rc)
		{
			noPrefix = rc.GetBoolean("diff", "noprefix", false);
			renames = rc.GetBoolean("diff", "renames", false);
			renameLimit = rc.GetInt("diff", "renamelimit", 200);
		}

		/// <returns>true if the prefix "a/" and "b/" should be suppressed.</returns>
		public virtual bool IsNoPrefix()
		{
			return noPrefix;
		}

		/// <returns>true if rename detection is enabled by default.</returns>
		public virtual bool IsRenameDetectionEnabled()
		{
			return renames;
		}

		/// <returns>limit on number of paths to perform inexact rename detection.</returns>
		public virtual int GetRenameLimit()
		{
			return renameLimit;
		}
	}
}
