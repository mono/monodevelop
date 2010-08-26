/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.Collections.ObjectModel;
using System.Text;
using System.Runtime.Serialization;

namespace GitSharp.Core.Exceptions
{
	/// <summary>
	/// An exception detailing multiple reasons for failure.
	/// </summary>
	[Serializable]
	public class CompoundException : GitException
	{
		private readonly IList<Exception> _causeList;

		private static string Format(IEnumerable<Exception> causes)
		{
			var msg = new StringBuilder();
			msg.Append("Failure due to one of the following:");

			foreach (Exception c in causes)
			{
				msg.Append("  ");
				msg.Append(c.Message);
				msg.Append("\n");
			}

			return msg.ToString();
		}

		/// <summary>
		/// Constructs an exception detailing many potential reasons for failure.
		/// </summary>
		/// <param name="why">
		/// Two or more exceptions that may have been the problem. 
		/// </param>
		public CompoundException(IEnumerable<Exception> why)
			: base(Format(why))
		{
			_causeList = new List<Exception>(why);
		}

		///	<summary>
		/// Get the complete list of reasons why this failure happened.
		///	</summary>
		///	<returns>
		/// Unmodifiable collection of all possible reasons.
		/// </returns>
		public IEnumerable<Exception> AllCauses
		{
			get { return new ReadOnlyCollection<Exception>(_causeList); }
		}
		
		protected CompoundException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}
	}
}