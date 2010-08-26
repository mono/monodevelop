/*
 * Copyright (C) 2008-2010, Google Inc.
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Write progress messages out to the sideband channel.
    /// </summary>
    public class SideBandProgressMonitor : ProgressMonitor, IDisposable
    {
        private readonly StreamWriter _writer;
        private bool _output;
        private long _taskBeganAt;
        private long _lastOutput;
        private string _msg;
        private int _lastWorked;
        private int _totalWork;

		  public SideBandProgressMonitor(Stream os)
        {
				_writer = new StreamWriter(os, Constants.CHARSET);
		  }

        public override void Start(int totalTasks)
        {
            _taskBeganAt = SystemReader.getInstance().getCurrentTime();
            _lastOutput = _taskBeganAt;
        }

        public override void BeginTask(string title, int totalWork)
        {
            EndTask();
            _msg = title;
            _lastWorked = 0;
            _totalWork = totalWork;
        }

        public override void Update(int completed)
        {
            if (_msg == null)
                return;

            int cmp = _lastWorked + completed;
            long now = SystemReader.getInstance().getCurrentTime();
            if (!_output && now - _taskBeganAt < 500)
                return;
            if (_totalWork == UNKNOWN)
            {
                if (now - _lastOutput >= 500)
                {
                    display(cmp, null);
                    _lastOutput = now;
                }
            }
            else
            {
                if ((cmp * 100 / _totalWork) != (_lastWorked * 100) / _totalWork || now - _lastOutput >= 500)
                {
                    display(cmp, null);
                    _lastOutput = now;
                }
            }
            _lastWorked = cmp;
            _output = true;
        }

        private void display(int cmp, string eol)
        {
            var m = new StringBuilder();
            m.Append(_msg);
            m.Append(": ");

            if (_totalWork == UNKNOWN)
            {
                m.Append(cmp);
            }
            else
            {
                int pcnt = (cmp * 100 / _totalWork);
                if (pcnt < 100)
                    m.Append(' ');
                if (pcnt < 10)
                    m.Append(' ');
                m.Append(pcnt);
                m.Append("% (");
                m.Append(cmp);
                m.Append("/");
                m.Append(_totalWork);
                m.Append(")");
            }
            if (eol != null)
                m.Append(eol);
            else
            {
                m.Append("   \r");
            }
            _writer.Write(m.ToString());
            _writer.Flush();
        }

        public override bool IsCancelled
        {
            get { return false; }
        }

        public override void EndTask()
        {
            if (_output)
            {
                if (_totalWork == UNKNOWN)
                    display(_lastWorked, ", done\n");
                else
                    display(_totalWork, "\n");
            }
            _output = false;
            _msg = null;
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

    }

}