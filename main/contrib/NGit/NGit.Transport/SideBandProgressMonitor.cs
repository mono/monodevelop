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

using System.Text;
using NGit;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Write progress messages out to the sideband channel.</summary>
	/// <remarks>Write progress messages out to the sideband channel.</remarks>
	internal class SideBandProgressMonitor : ProgressMonitor
	{
		private PrintWriter @out;

		private bool output;

		private long taskBeganAt;

		private long lastOutput;

		private string msg;

		private int lastWorked;

		private int totalWork;

		internal SideBandProgressMonitor(OutputStream os)
		{
			@out = new PrintWriter(new OutputStreamWriter(os, Constants.CHARSET));
		}

		public override void Start(int totalTasks)
		{
			// Ignore the number of tasks.
			taskBeganAt = Runtime.CurrentTimeMillis();
			lastOutput = taskBeganAt;
		}

		public override void BeginTask(string title, int total)
		{
			EndTask();
			msg = title;
			lastWorked = 0;
			totalWork = total;
		}

		public override void Update(int completed)
		{
			if (msg == null)
			{
				return;
			}
			int cmp = lastWorked + completed;
			long now = Runtime.CurrentTimeMillis();
			if (!output && now - taskBeganAt < 500)
			{
				return;
			}
			if (totalWork == UNKNOWN)
			{
				if (now - lastOutput >= 500)
				{
					Display(cmp, null);
					lastOutput = now;
				}
			}
			else
			{
				if ((cmp * 100 / totalWork) != (lastWorked * 100) / totalWork || now - lastOutput
					 >= 500)
				{
					Display(cmp, null);
					lastOutput = now;
				}
			}
			lastWorked = cmp;
			output = true;
		}

		private void Display(int cmp, string eol)
		{
			StringBuilder m = new StringBuilder();
			m.Append(msg);
			m.Append(": ");
			if (totalWork == UNKNOWN)
			{
				m.Append(cmp);
			}
			else
			{
				int pcnt = (cmp * 100 / totalWork);
				if (pcnt < 100)
				{
					m.Append(' ');
				}
				if (pcnt < 10)
				{
					m.Append(' ');
				}
				m.Append(pcnt);
				m.Append("% (");
				m.Append(cmp);
				m.Append("/");
				m.Append(totalWork);
				m.Append(")");
			}
			if (eol != null)
			{
				m.Append(eol);
			}
			else
			{
				m.Append("   \r");
			}
			@out.Write(m);
			@out.Flush();
		}

		public override bool IsCancelled()
		{
			return false;
		}

		public override void EndTask()
		{
			if (output)
			{
				if (totalWork == UNKNOWN)
				{
					Display(lastWorked, ", done\n");
				}
				else
				{
					Display(totalWork, "\n");
				}
			}
			output = false;
			msg = null;
		}
	}
}
