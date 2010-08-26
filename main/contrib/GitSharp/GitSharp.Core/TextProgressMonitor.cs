/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.IO;
using System.Text;

namespace GitSharp.Core
{
	public class TextProgressMonitor : ProgressMonitor, IDisposable
	{
		private readonly TextWriter _writer;
		private DateTime _taskBeganAt;
		private string _message;
		private int _lastWorked;
		private int _totalWork;
		private bool _output;

		public TextProgressMonitor()
			: this(Console.Error)
		{
		}

		public TextProgressMonitor(TextWriter writer)
		{
			_writer = writer;
			_taskBeganAt = DateTime.Now;
		}

		#region ProgressMonitor Members

		public override void Start(int totalTasks)
		{
			_taskBeganAt = DateTime.Now;
		}

		public override void BeginTask(string title, int totalWork)
		{
			EndTask();
			_message = title;
			_lastWorked = 0;
			_totalWork = totalWork;
		}

		public override void Update(int completed)
		{
			if (_message == null) return;
			int cmp = _lastWorked + completed;
			if (!_output && ((DateTime.Now - _taskBeganAt).TotalMilliseconds < 500)) return;

			if (_totalWork == UNKNOWN)
			{
				Display(cmp);
				_writer.Flush();
			}
			else if ((cmp * 100 / _totalWork) != (_lastWorked * 100) / _totalWork)
			{
				Display(cmp);
				_writer.Flush();
			}

			_lastWorked = cmp;
			_output = true;
		}

		private void Display(int cmp)
		{
			var m = new StringBuilder();
			m.Append('\r');
			m.Append(_message);
			m.Append(": ");
			while (m.Length < 25)
			{
				m.Append(' ');
			}

			if (_totalWork == UNKNOWN)
			{
				m.Append(cmp);
			}
			else
			{
				string twstr = _totalWork.ToString();
				string cmpstr = cmp.ToString();

				while (cmpstr.Length < twstr.Length)
				{
					cmpstr = " " + cmpstr;
				}

				int pcnt = (cmp * 100 / _totalWork);
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
				m.Append(cmpstr);
				m.Append("/");
				m.Append(twstr);
				m.Append(")");
			}

			_writer.Write(m);
		}

		public override void EndTask()
		{
			if (_output)
			{
				if (_totalWork != UNKNOWN)
				{
					Display(_totalWork);
				}

				_writer.WriteLine();
			}

			_output = false;
			_message = null;
		}

		public override bool IsCancelled
		{
			get { return false; }
		}

		#endregion
		
		public void Dispose ()
		{
			_writer.Dispose();
		}
		
	}
}