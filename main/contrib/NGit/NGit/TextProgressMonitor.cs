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

using System.IO;
using System.Text;
using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>A simple progress reporter printing on a stream.</summary>
	/// <remarks>A simple progress reporter printing on a stream.</remarks>
	public class TextProgressMonitor : BatchingProgressMonitor
	{
		private readonly TextWriter @out;

		private bool write;

		/// <summary>Initialize a new progress monitor.</summary>
		/// <remarks>Initialize a new progress monitor.</remarks>
		public TextProgressMonitor() : this(new PrintWriter(System.Console.Error))
		{
		}

		/// <summary>Initialize a new progress monitor.</summary>
		/// <remarks>Initialize a new progress monitor.</remarks>
		/// <param name="out">the stream to receive messages on.</param>
		public TextProgressMonitor(TextWriter @out)
		{
			this.@out = @out;
			this.write = true;
		}

		protected internal override void OnUpdate(string taskName, int workCurr)
		{
			StringBuilder s = new StringBuilder();
			Format(s, taskName, workCurr);
			Send(s);
		}

		protected internal override void OnEndTask(string taskName, int workCurr)
		{
			StringBuilder s = new StringBuilder();
			Format(s, taskName, workCurr);
			s.Append("\n");
			Send(s);
		}

		private void Format(StringBuilder s, string taskName, int workCurr)
		{
			s.Append("\r");
			s.Append(taskName);
			s.Append(": ");
			while (s.Length < 25)
			{
				s.Append(' ');
			}
			s.Append(workCurr);
		}

		protected internal override void OnUpdate(string taskName, int cmp, int totalWork
			, int pcnt)
		{
			StringBuilder s = new StringBuilder();
			Format(s, taskName, cmp, totalWork, pcnt);
			Send(s);
		}

		protected internal override void OnEndTask(string taskName, int cmp, int totalWork
			, int pcnt)
		{
			StringBuilder s = new StringBuilder();
			Format(s, taskName, cmp, totalWork, pcnt);
			s.Append("\n");
			Send(s);
		}

		private void Format(StringBuilder s, string taskName, int cmp, int totalWork, int
			 pcnt)
		{
			s.Append("\r");
			s.Append(taskName);
			s.Append(": ");
			while (s.Length < 25)
			{
				s.Append(' ');
			}
			string endStr = totalWork.ToString();
			string curStr = cmp.ToString();
			while (curStr.Length < endStr.Length)
			{
				curStr = " " + curStr;
			}
			if (pcnt < 100)
			{
				s.Append(' ');
			}
			if (pcnt < 10)
			{
				s.Append(' ');
			}
			s.Append(pcnt);
			s.Append("% (");
			s.Append(curStr);
			s.Append("/");
			s.Append(endStr);
			s.Append(")");
		}

		private void Send(StringBuilder s)
		{
			if (write)
			{
				try
				{
					@out.Write(s.ToString());
					@out.Flush();
				}
				catch (IOException)
				{
					write = false;
				}
			}
		}
	}
}
