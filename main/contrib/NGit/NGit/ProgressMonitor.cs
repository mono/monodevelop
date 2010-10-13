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
	/// <summary>A progress reporting interface.</summary>
	/// <remarks>A progress reporting interface.</remarks>
	public abstract class ProgressMonitor
	{
		/// <summary>Constant indicating the total work units cannot be predicted.</summary>
		/// <remarks>Constant indicating the total work units cannot be predicted.</remarks>
		public const int UNKNOWN = 0;

		/// <summary>Advise the monitor of the total number of subtasks.</summary>
		/// <remarks>
		/// Advise the monitor of the total number of subtasks.
		/// <p>
		/// This should be invoked at most once per progress monitor interface.
		/// </remarks>
		/// <param name="totalTasks">
		/// the total number of tasks the caller will need to complete
		/// their processing.
		/// </param>
		public abstract void Start(int totalTasks);

		/// <summary>Begin processing a single task.</summary>
		/// <remarks>Begin processing a single task.</remarks>
		/// <param name="title">
		/// title to describe the task. Callers should publish these as
		/// stable string constants that implementations could match
		/// against for translation support.
		/// </param>
		/// <param name="totalWork">
		/// total number of work units the application will perform;
		/// <see cref="UNKNOWN">UNKNOWN</see>
		/// if it cannot be predicted in advance.
		/// </param>
		public abstract void BeginTask(string title, int totalWork);

		/// <summary>Denote that some work units have been completed.</summary>
		/// <remarks>
		/// Denote that some work units have been completed.
		/// <p>
		/// This is an incremental update; if invoked once per work unit the correct
		/// value for our argument is <code>1</code>, to indicate a single unit of
		/// work has been finished by the caller.
		/// </remarks>
		/// <param name="completed">the number of work units completed since the last call.</param>
		public abstract void Update(int completed);

		/// <summary>Finish the current task, so the next can begin.</summary>
		/// <remarks>Finish the current task, so the next can begin.</remarks>
		public abstract void EndTask();

		/// <summary>Check for user task cancellation.</summary>
		/// <remarks>Check for user task cancellation.</remarks>
		/// <returns>true if the user asked the process to stop working.</returns>
		public abstract bool IsCancelled();
	}
}
