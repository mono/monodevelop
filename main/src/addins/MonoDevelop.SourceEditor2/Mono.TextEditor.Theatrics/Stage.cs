//
// Stage.cs
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
using System.Collections.Generic;

namespace Mono.TextEditor.Theatrics
{
    class Stage<T>
    {
        public delegate bool ActorStepHandler (Actor<T> actor);

        private Dictionary<T, Actor<T>> actors = new Dictionary<T, Actor<T>> ();
        private uint timeout_id;

        private uint update_frequency = 30;
        private uint default_duration = 1000;
        private bool playing = true;

        public event ActorStepHandler ActorStep;

        #pragma warning disable 0067
        // FIXME: This is to mute gmcs: https://bugzilla.novell.com/show_bug.cgi?id=360455
        public event EventHandler Iteration;
        #pragma warning restore 0067

        public Stage ()
        {
        }

        public Stage (uint actorDuration)
        {
            default_duration = actorDuration;
        }

        public Actor<T> this[T target] {
            get {
                if (actors.ContainsKey (target)) {
                    return actors[target];
                }

                return null;
            }
        }

        public bool Contains (T target)
        {
            return actors.ContainsKey (target);
        }

        public Actor<T> Add (T target)
        {
            lock (this) {
                return Add (target, default_duration);
            }
        }

        public Actor<T> Add (T target, uint duration)
        {
            lock (this) {
                if (Contains (target)) {
                    throw new InvalidOperationException ("Stage already contains this actor");
                }

                Actor<T> actor = new Actor<T> (target, duration);
                actors.Add (target, actor);

                CheckTimeout ();

                return actor;
            }
        }

        public Actor<T> AddOrReset (T target)
        {
            lock (this) {
                return AddOrResetCore (target, null);
            }
        }

        public Actor<T> AddOrReset (T target, uint duration)
        {
            lock (this) {
                return AddOrResetCore (target, duration);
            }
        }

        private Actor<T> AddOrResetCore (T target, uint? duration)
        {
            lock (this) {
                if (Contains (target)) {
                    Actor<T> actor = this[target];

                    if (duration == null) {
                        actor.Reset ();
                    } else {
                        actor.Reset (duration.Value);
                    }

                    CheckTimeout ();

                    return actor;
                }

				return Add (target, duration.HasValue ? duration.Value : default_duration);
            }
        }

        public void Reset (T target)
        {
            lock (this) {
                ResetCore (target, null);
            }
        }

        public void Reset (T target, uint duration)
        {
            lock (this) {
                ResetCore (target, duration);
            }
        }

        private void ResetCore (T target, uint? duration)
        {
            lock (this) {
                if (!Contains (target)) {
                    throw new InvalidOperationException ("Stage does not contain this actor");
                }

                CheckTimeout ();

                if (duration == null) {
                    this [target].Reset ();
                } else {
                    this [target].Reset (duration.Value);
                }
            }
        }

        private void CheckTimeout ()
        {
            if ((!Playing || actors.Count == 0) && timeout_id > 0) {
                GLib.Source.Remove (timeout_id);
                timeout_id = 0;
                return;
            } else if (Playing && actors.Count > 0 && timeout_id <= 0) {
                timeout_id = GLib.Timeout.Add (update_frequency, OnTimeout);
                return;
            }
        }

        private bool OnTimeout ()
        {
            if (!Playing || this.actors.Count == 0) {
                timeout_id = 0;
                return false;
            }

            Queue<Actor<T>> actors = new Queue<Actor<T>> (this.actors.Values);
            while (actors.Count > 0) {
                Actor<T> actor = actors.Dequeue ();
                actor.Step ();

                if (!OnActorStep (actor) || actor.Expired) {
                    this.actors.Remove (actor.Target);
                }
            }

            OnIteration ();

            return true;
        }

        protected virtual bool OnActorStep (Actor<T> actor)
        {
            ActorStepHandler handler = ActorStep;
            if (handler != null) {
                bool result = true;
                foreach (ActorStepHandler del in handler.GetInvocationList ()) {
                    result &= del (actor);
                }
                return result;
            }
            return false;
        }

        protected virtual void OnIteration ()
        {
            EventHandler handler = Iteration;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public void Play ()
        {
            lock (this) {
                Playing = true;
            }
        }

        public void Pause ()
        {
            lock (this) {
                Playing = false;
            }
        }

        public void Exeunt ()
        {
            lock (this) {
                actors.Clear ();
                CheckTimeout ();
            }
        }

        public uint DefaultActorDuration {
            get { return default_duration; }
            set { lock (this) { default_duration = value; } }
        }

        public bool Playing {
            get { return playing; }
            set {
                lock (this) {
                    if (playing == value) {
                        return;
                    }

                    playing = value;
                    CheckTimeout ();
                }
            }
        }

        public uint UpdateFrequency {
            get { return update_frequency; }
            set {
                lock (this) {
                    bool _playing = Playing;
                    update_frequency = value;
                    Playing = _playing;
                }
            }
        }

        public int ActorCount {
            get { return actors.Count; }
        }
    }
}
