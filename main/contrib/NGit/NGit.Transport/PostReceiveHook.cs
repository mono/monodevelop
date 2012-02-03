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
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// Hook invoked by
	/// <see cref="ReceivePack">ReceivePack</see>
	/// after all updates are executed.
	/// <p>
	/// The hook is called after all commands have been processed. Only commands with
	/// a status of
	/// <see cref="Result.OK">Result.OK</see>
	/// are passed into the hook. To get
	/// all commands within the hook, see
	/// <see cref="ReceivePack.GetAllCommands()">ReceivePack.GetAllCommands()</see>
	/// .
	/// <p>
	/// Any post-receive hook implementation should not update the status of a
	/// command, as the command has already completed or failed, and the status has
	/// already been returned to the client.
	/// <p>
	/// Hooks should execute quickly, as they block the server and the client from
	/// completing the connection.
	/// </summary>
	public abstract class PostReceiveHook
	{
		private sealed class _PostReceiveHook_64 : PostReceiveHook
		{
			public _PostReceiveHook_64()
			{
			}

			public override void OnPostReceive(ReceivePack rp, ICollection<ReceiveCommand> commands
				)
			{
			}
		}

		/// <summary>A simple no-op hook.</summary>
		/// <remarks>A simple no-op hook.</remarks>
		public static readonly PostReceiveHook NULL = new _PostReceiveHook_64();

		// Do nothing.
		/// <summary>Invoked after all commands are executed and status has been returned.</summary>
		/// <remarks>Invoked after all commands are executed and status has been returned.</remarks>
		/// <param name="rp">
		/// the process handling the current receive. Hooks may obtain
		/// details about the destination repository through this handle.
		/// </param>
		/// <param name="commands">
		/// unmodifiable set of successfully completed commands. May be
		/// the empty set.
		/// </param>
		public abstract void OnPostReceive(ReceivePack rp, ICollection<ReceiveCommand> commands
			);
	}
}
