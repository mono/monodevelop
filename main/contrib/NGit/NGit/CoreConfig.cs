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

using ICSharpCode.SharpZipLib.Zip.Compression;
using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>This class keeps git repository core parameters.</summary>
	/// <remarks>This class keeps git repository core parameters.</remarks>
	public class CoreConfig
	{
		private sealed class _SectionParser_59 : Config.SectionParser<NGit.CoreConfig>
		{
			public _SectionParser_59()
			{
			}

			public NGit.CoreConfig Parse(Config cfg)
			{
				return new NGit.CoreConfig(cfg);
			}
		}

		/// <summary>
		/// Key for
		/// <see cref="Config.Get{T}(SectionParser{T})">Config.Get&lt;T&gt;(SectionParser&lt;T&gt;)
		/// 	</see>
		/// .
		/// </summary>
		public static readonly Config.SectionParser<NGit.CoreConfig> KEY = new _SectionParser_59
			();

		/// <summary>
		/// Permissible values for
		/// <code>core.autocrlf</code>
		/// .
		/// </summary>
		public enum AutoCRLF
		{
			FALSE,
			TRUE,
			INPUT
		}

		private readonly int compression;

		private readonly int packIndexVersion;

		private readonly bool logAllRefUpdates;

		private readonly string excludesfile;

		private CoreConfig(Config rc)
		{
			compression = rc.GetInt(ConfigConstants.CONFIG_CORE_SECTION, ConfigConstants.CONFIG_KEY_COMPRESSION
				, Deflater.DEFAULT_COMPRESSION);
			packIndexVersion = rc.GetInt(ConfigConstants.CONFIG_PACK_SECTION, ConfigConstants
				.CONFIG_KEY_INDEXVERSION, 2);
			logAllRefUpdates = rc.GetBoolean(ConfigConstants.CONFIG_CORE_SECTION, ConfigConstants
				.CONFIG_KEY_LOGALLREFUPDATES, true);
			excludesfile = rc.GetString(ConfigConstants.CONFIG_CORE_SECTION, null, ConfigConstants
				.CONFIG_KEY_EXCLUDESFILE);
		}

		/// <returns>The compression level to use when storing loose objects</returns>
		public virtual int GetCompression()
		{
			return compression;
		}

		/// <returns>the preferred pack index file format; 0 for oldest possible.</returns>
		public virtual int GetPackIndexVersion()
		{
			return packIndexVersion;
		}

		/// <returns>whether to log all refUpdates</returns>
		public virtual bool IsLogAllRefUpdates()
		{
			return logAllRefUpdates;
		}

		/// <returns>path of excludesfile</returns>
		public virtual string GetExcludesFile()
		{
			return excludesfile;
		}
	}
}
