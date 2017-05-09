//
// Pulsator.cs
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
    class Pulsator<T> where T : class
    {
        private Stage<T> stage;
        public Stage<T> Stage {
            get { return stage; }
            set {
                if (stage == value) {
                    return;
                }

                if (stage != null) {
                    stage.ActorStep -= OnActorStep;
                }

                stage = value;

                if (stage != null) {
                    stage.ActorStep += OnActorStep;
                }
            }
        }

        private T target;
        public T Target {
            get { return target; }
            set { target = value; }
        }

        public double Percent {
            get { return IsPulsing ? stage[Target].Percent : 0; }
        }

        public bool IsPulsing {
            get { return stage != null && stage.Contains (Target); }
        }

        public bool Stopping {
            get { return !IsPulsing ? true : stage[Target].CanExpire; }
        }

        #pragma warning disable 0067
        // FIXME: This is to mute gmcs: https://bugzilla.novell.com/show_bug.cgi?id=360455
        public event EventHandler Pulse;
        #pragma warning restore 0067

        public Pulsator ()
        {
        }

        public Pulsator (Stage<T> stage)
        {
            Stage = stage;
        }

        public void StartPulsing ()
        {
            if (!Stage.Contains (Target)) {
                Stage.Add (Target);
            }

            Stage[Target].CanExpire = false;
        }

        public void StopPulsing ()
        {
            if (Stage.Contains (Target)) {
                Stage[Target].CanExpire = true;
            }
        }

        private bool OnActorStep (Actor<T> actor)
        {
            if (actor.Target == target) {
                OnPulse ();
            }

            return true;
        }

        protected virtual void OnPulse ()
        {
            EventHandler handler = Pulse;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
    }
}
