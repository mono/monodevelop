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

namespace NGit
{
	/// <summary>Persistent configuration that can be stored and loaded from a location.</summary>
	/// <remarks>Persistent configuration that can be stored and loaded from a location.</remarks>
	public abstract class StoredConfig : Config
	{
		/// <summary>Create a configuration with no default fallback.</summary>
		/// <remarks>Create a configuration with no default fallback.</remarks>
		public StoredConfig() : base()
		{
		}

		/// <summary>Create an empty configuration with a fallback for missing keys.</summary>
		/// <remarks>Create an empty configuration with a fallback for missing keys.</remarks>
		/// <param name="defaultConfig">
		/// the base configuration to be consulted when a key is missing
		/// from this configuration instance.
		/// </param>
		public StoredConfig(Config defaultConfig) : base(defaultConfig)
		{
		}

		/// <summary>Load the configuration from the persistent store.</summary>
		/// <remarks>
		/// Load the configuration from the persistent store.
		/// <p>
		/// If the configuration does not exist, this configuration is cleared, and
		/// thus behaves the same as though the backing store exists, but is empty.
		/// </remarks>
		/// <exception cref="System.IO.IOException">the configuration could not be read (but does exist).
		/// 	</exception>
		/// <exception cref="NGit.Errors.ConfigInvalidException">the configuration is not properly formatted.
		/// 	</exception>
		public abstract void Load();

		/// <summary>Save the configuration to the persistent store.</summary>
		/// <remarks>Save the configuration to the persistent store.</remarks>
		/// <exception cref="System.IO.IOException">the configuration could not be written.</exception>
		public abstract void Save();
	}
}
