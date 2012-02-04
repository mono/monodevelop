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

using System;
using System.Collections.Generic;
using System.Text;
using NGit;
using Sharpen;

namespace NGit.Errors
{
	/// <summary>An exception detailing multiple reasons for failure.</summary>
	/// <remarks>An exception detailing multiple reasons for failure.</remarks>
	[System.Serializable]
	public class CompoundException : Exception
	{
		private const long serialVersionUID = 1L;

		private static string Format(ICollection<Exception> causes)
		{
			StringBuilder msg = new StringBuilder();
			msg.Append(JGitText.Get().failureDueToOneOfTheFollowing);
			foreach (Exception c in causes)
			{
				msg.Append("  ");
				msg.Append(c.Message);
				msg.Append("\n");
			}
			return msg.ToString();
		}

		private readonly IList<Exception> causeList;

		/// <summary>Constructs an exception detailing many potential reasons for failure.</summary>
		/// <remarks>Constructs an exception detailing many potential reasons for failure.</remarks>
		/// <param name="why">Two or more exceptions that may have been the problem.</param>
		public CompoundException(ICollection<Exception> why) : base(Format(why))
		{
			causeList = Sharpen.Collections.UnmodifiableList(new AList<Exception>(why));
		}

		/// <summary>Get the complete list of reasons why this failure happened.</summary>
		/// <remarks>Get the complete list of reasons why this failure happened.</remarks>
		/// <returns>unmodifiable collection of all possible reasons.</returns>
		public virtual IList<Exception> GetAllCauses()
		{
			return causeList;
		}
	}
}
