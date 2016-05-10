//
// Actor.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace Mono.TextEditor.Theatrics
{
    class Actor<T>
    {
        private T target;

        private DateTime start_time;
        private uint duration;
        private double frames;
        private double percent;
        private bool can_expire = true;

        public Actor (T target, uint duration)
        {
            this.target = target;
            this.duration = duration;
            Reset ();
        }

        public void Reset ()
        {
            Reset (duration);
        }

        public void Reset (uint duration)
        {
            start_time = DateTime.Now;
            frames = 0.0;
            percent = 0.0;
            this.duration = duration;
        }

        public virtual void Step ()
        {
            if (!CanExpire && percent >= 1.0) {
                Reset ();
            }

            percent = (DateTime.Now - start_time).TotalMilliseconds / duration;
            frames++;
        }

        public bool Expired {
            get { return CanExpire && percent >= 1.0; }
        }

        public bool CanExpire {
            get { return can_expire; }
            set { can_expire = value; }
        }

        public T Target {
            get { return target; }
        }

        public double Duration {
            get { return duration; }
        }

        public DateTime StartTime {
            get { return start_time; }
        }

        public double Frames {
            get { return frames; }
        }

        public double FramesPerSecond {
            get { return frames / ((double)duration / 1000.0); }
        }

        public double Percent {
            get { return System.Math.Max (0.0, System.Math.Min (1.0, percent)); }
        }
    }
}
