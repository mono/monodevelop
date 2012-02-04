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

using System.Collections.Generic;
using NGit.Api;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// Encapsulates the result of a
	/// <see cref="CheckoutCommand">CheckoutCommand</see>
	/// </summary>
	public class CheckoutResult
	{
		/// <summary>
		/// The
		/// <see cref="Status.OK">Status.OK</see>
		/// result;
		/// </summary>
		public static readonly NGit.Api.CheckoutResult OK_RESULT = new NGit.Api.CheckoutResult
			(CheckoutResult.Status.OK, null);

		/// <summary>
		/// The
		/// <see cref="Status.ERROR">Status.ERROR</see>
		/// result;
		/// </summary>
		public static readonly NGit.Api.CheckoutResult ERROR_RESULT = new NGit.Api.CheckoutResult
			(CheckoutResult.Status.ERROR, null);

		/// <summary>
		/// The
		/// <see cref="Status.NOT_TRIED">Status.NOT_TRIED</see>
		/// result;
		/// </summary>
		public static readonly NGit.Api.CheckoutResult NOT_TRIED_RESULT = new NGit.Api.CheckoutResult
			(CheckoutResult.Status.NOT_TRIED, null);

		/// <summary>The status</summary>
		public enum Status
		{
			NOT_TRIED,
			OK,
			CONFLICTS,
			NONDELETED,
			ERROR
		}

		private readonly CheckoutResult.Status myStatus;

		private readonly IList<string> conflictList;

		private readonly IList<string> undeletedList;

		internal CheckoutResult(CheckoutResult.Status status, IList<string> fileList)
		{
			myStatus = status;
			if (status == CheckoutResult.Status.CONFLICTS)
			{
				this.conflictList = fileList;
			}
			else
			{
				this.conflictList = new AList<string>(0);
			}
			if (status == CheckoutResult.Status.NONDELETED)
			{
				this.undeletedList = fileList;
			}
			else
			{
				this.undeletedList = new AList<string>(0);
			}
		}

		/// <returns>the status</returns>
		public virtual CheckoutResult.Status GetStatus()
		{
			return myStatus;
		}

		/// <returns>
		/// the list of files that created a checkout conflict, or an empty
		/// list if
		/// <see cref="GetStatus()">GetStatus()</see>
		/// is not
		/// <see cref="Status.CONFLICTS">Status.CONFLICTS</see>
		/// ;
		/// </returns>
		public virtual IList<string> GetConflictList()
		{
			return conflictList;
		}

		/// <returns>
		/// the list of files that could not be deleted during checkout, or
		/// an empty list if
		/// <see cref="GetStatus()">GetStatus()</see>
		/// is not
		/// <see cref="Status.NONDELETED">Status.NONDELETED</see>
		/// ;
		/// </returns>
		public virtual IList<string> GetUndeletedList()
		{
			return undeletedList;
		}
	}
}
