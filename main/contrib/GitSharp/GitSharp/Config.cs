/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace GitSharp
{
	/// <summary>
	/// Represents repository-, user-, and global-configuration for git
	/// </summary>
	public class Config : IEnumerable<KeyValuePair<string, string>>
	{
		private Repository _repo;

		public Config(Repository repo)
		{
			Debug.Assert(repo != null);
			_repo = repo;
		}

		/// <summary>
		/// Direct config access via git style names (i.e. "user.name")
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public string this[string key]
		{
			get
			{
				var config = _repo._internal_repo.Config;
				var token = key.Split('.');

				if (token.Count() == 2)
				{
					return config.getString(token[0], null, token[1]);
				}

				if (token.Count() == 3)
				{
					return config.getString(token[0], token[1], token[2]);
				}

				return null;
			}
			set
			{
				var config = _repo._internal_repo.Config;
				var token = key.Split('.');
				if (token.Count() == 2)
					config.setString(token[0], null, token[1], value);
				else if (token.Count() == 3)
					config.setString(token[0], token[1], token[2], value);
			}
		}

		public IEnumerable<string> Keys
		{
			get
			{
				foreach (var pair in this)
					yield return pair.Key;
			}
		}

		public IEnumerable<string> Values
		{
			get
			{
				foreach (var pair in this)
					yield return pair.Value;
			}
		}

		#region IEnumerable<KeyValuePair<string,string>> Members

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			var config = _repo._internal_repo.Config;
			config.getState();
			foreach (var entry in config._state.get().EntryList)
			{
				if (string.IsNullOrEmpty(entry.name))
					continue;
				var subsec = (entry.subsection != null ? "." + entry.subsection : "");
				yield return new KeyValuePair<string, string>(entry.section + subsec + "." + entry.name, entry.value);
			}
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		/// <summary>
		/// Saves the config to the file system.
		/// </summary>
		public void Persist()
		{
			_repo._internal_repo.Config.save();
		}
	}
}
