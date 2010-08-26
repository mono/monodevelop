/*
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

namespace GitSharp.Core.RevWalk
{
	/// <summary>
	/// Case insensitive key for a <see cref="FooterLine"/>.
	/// </summary>
	public class FooterKey
	{
        /// <summary>
		/// Standard <code>Signed-off-by</code>
        /// </summary>
        public static FooterKey SIGNED_OFF_BY = new FooterKey("Signed-off-by");

		/// <summary>
		/// Standard <code>Acked-by</code>
		/// </summary>
        public static FooterKey ACKED_BY = new FooterKey("Acked-by");

        /// <summary>
		/// Standard <code>CC</code>
        /// </summary>
        public static FooterKey CC = new FooterKey("CC");

        private readonly string _name;
        private readonly byte[] _raw;

		/// <summary>
		/// Create a key for a specific footer line.
		/// </summary>
		/// <param _name="keyName">Name of the footer line.</param>
		public FooterKey(string keyName)
		{
			if (keyName == null)
				throw new System.ArgumentNullException ("keyName");
			
			_name = keyName;
			_raw = Constants.encode(keyName.ToLowerInvariant());
		}

		public override string ToString()
		{
			return "FooterKey[" + _name + "]";
		}

		public string Name
		{
			get { return _name; }
		}

		public byte[] Raw
		{
			get { return _raw; }
		}
	}
}
