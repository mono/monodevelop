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
	/// <summary>Asynchronous operation handle.</summary>
	/// <remarks>
	/// Asynchronous operation handle.
	/// Callers that start an asynchronous operation are supplied with a handle that
	/// may be used to attempt cancellation of the operation if the caller does not
	/// wish to continue.
	/// </remarks>
	public interface AsyncOperation
	{
		/// <summary>Cancels the running task.</summary>
		/// <remarks>
		/// Cancels the running task.
		/// Attempts to cancel execution of this task. This attempt will fail if the
		/// task has already completed, already been cancelled, or could not be
		/// cancelled for some other reason. If successful, and this task has not
		/// started when cancel is called, this task should never run. If the task
		/// has already started, then the mayInterruptIfRunning parameter determines
		/// whether the thread executing this task should be interrupted in an
		/// attempt to stop the task.
		/// </remarks>
		/// <param name="mayInterruptIfRunning">
		/// true if the thread executing this task should be interrupted;
		/// otherwise, in-progress tasks are allowed to complete
		/// </param>
		/// <returns>
		/// false if the task could not be cancelled, typically because it
		/// has already completed normally; true otherwise
		/// </returns>
		bool Cancel(bool mayInterruptIfRunning);

		/// <summary>Release resources used by the operation, including cancellation.</summary>
		/// <remarks>Release resources used by the operation, including cancellation.</remarks>
		void Release();
	}
}
