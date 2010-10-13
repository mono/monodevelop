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

namespace NGit.Treewalk
{
	/// <summary>Contains options used by the WorkingTreeIterator.</summary>
	/// <remarks>Contains options used by the WorkingTreeIterator.</remarks>
	public class WorkingTreeOptions
	{
		/// <summary>
		/// Creates default options which reflect the original configuration of Git
		/// on Unix systems.
		/// </summary>
		/// <remarks>
		/// Creates default options which reflect the original configuration of Git
		/// on Unix systems.
		/// </remarks>
		/// <returns>created working tree options</returns>
		public static NGit.Treewalk.WorkingTreeOptions CreateDefaultInstance()
		{
			return new NGit.Treewalk.WorkingTreeOptions(CoreConfig.AutoCRLF.FALSE);
		}

		/// <summary>Creates options based on the specified repository configuration.</summary>
		/// <remarks>Creates options based on the specified repository configuration.</remarks>
		/// <param name="config">repository configuration to create options for</param>
		/// <returns>created working tree options</returns>
		public static NGit.Treewalk.WorkingTreeOptions CreateConfigurationInstance(Config
			 config)
		{
			return new NGit.Treewalk.WorkingTreeOptions(config.Get(CoreConfig.KEY).GetAutoCRLF
				());
		}

		/// <summary>
		/// Indicates whether EOLs of text files should be converted to '\n' before
		/// calculating the blob ID.
		/// </summary>
		/// <remarks>
		/// Indicates whether EOLs of text files should be converted to '\n' before
		/// calculating the blob ID.
		/// </remarks>
		private readonly CoreConfig.AutoCRLF autoCRLF;

		/// <summary>Creates new options.</summary>
		/// <remarks>Creates new options.</remarks>
		/// <param name="autoCRLF">
		/// indicates whether EOLs of text files should be converted to
		/// '\n' before calculating the blob ID.
		/// </param>
		public WorkingTreeOptions(CoreConfig.AutoCRLF autoCRLF)
		{
			this.autoCRLF = autoCRLF;
		}

		/// <summary>
		/// Indicates whether EOLs of text files should be converted to '\n' before
		/// calculating the blob ID.
		/// </summary>
		/// <remarks>
		/// Indicates whether EOLs of text files should be converted to '\n' before
		/// calculating the blob ID.
		/// </remarks>
		/// <returns>true if EOLs should be canonicalized.</returns>
		public virtual CoreConfig.AutoCRLF GetAutoCRLF()
		{
			return autoCRLF;
		}
	}
}
