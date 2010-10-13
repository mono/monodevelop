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
using Sharpen;

namespace NGit.Api.Errors
{
	/// <summary>
	/// Exception thrown when during command execution a low-level exception from the
	/// JGit library is thrown.
	/// </summary>
	/// <remarks>
	/// Exception thrown when during command execution a low-level exception from the
	/// JGit library is thrown. Also when certain low-level error situations are
	/// reported by JGit through return codes this Exception will be thrown.
	/// <p>
	/// During command execution a lot of exceptions may be thrown. Some of them
	/// represent error situations which can be handled specifically by the caller of
	/// the command. But a lot of exceptions are so low-level that is is unlikely
	/// that the caller of the command can handle them effectively. The huge number
	/// of these low-level exceptions which are thrown by the commands lead to a
	/// complicated and wide interface of the commands. Callers of the API have to
	/// deal with a lot of exceptions they don't understand.
	/// <p>
	/// To overcome this situation this class was introduced. Commands will wrap all
	/// exceptions they declare as low-level in their context into an instance of
	/// this class. Callers of the commands have to deal with one type of low-level
	/// exceptions. Callers will always get access to the original exception (if
	/// available) by calling
	/// <code>#getCause()</code>
	/// .
	/// </remarks>
	[System.Serializable]
	public class JGitInternalException : RuntimeException
	{
		private const long serialVersionUID = 1L;

		/// <param name="message"></param>
		/// <param name="cause"></param>
		public JGitInternalException(string message, Exception cause) : base(message, cause
			)
		{
		}

		/// <param name="message"></param>
		public JGitInternalException(string message) : base(message)
		{
		}
	}
}
