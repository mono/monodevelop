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

using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Create a remote "session" for executing remote commands.</summary>
	/// <remarks>
	/// Create a remote "session" for executing remote commands.
	/// <p>
	/// Clients should subclass RemoteSession to create an alternate way for JGit to
	/// execute remote commands. (The client application may already have this
	/// functionality available.) Note that this class is just a factory for creating
	/// remote processes. If the application already has a persistent connection to
	/// the remote machine, RemoteSession may do nothing more than return a new
	/// RemoteProcess when exec is called.
	/// </remarks>
	public interface RemoteSession
	{
		/// <summary>Generate a new remote process to execute the given command.</summary>
		/// <remarks>
		/// Generate a new remote process to execute the given command. This function
		/// should also start execution and may need to create the streams prior to
		/// execution.
		/// </remarks>
		/// <param name="commandName">command to execute</param>
		/// <param name="timeout">timeout value, in seconds, for command execution</param>
		/// <returns>a new remote process</returns>
		/// <exception cref="System.IO.IOException">
		/// may be thrown in several cases. For example, on problems
		/// opening input or output streams or on problems connecting or
		/// communicating with the remote host. For the latter two cases,
		/// a TransportException may be thrown (a subclass of
		/// IOException).
		/// </exception>
		SystemProcess Exec(string commandName, int timeout);

		/// <summary>Disconnect the remote session</summary>
		void Disconnect();
	}
}
