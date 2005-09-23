#if NET_2_0
using System;
using System.Collections;
using Mono.Debugger;
using Mono.Debugger.Languages;

namespace MonoDevelop.Debugger {

	public class FrameHandle
	{
		StackFrame frame;

		public FrameHandle (StackFrame frame)
		{
			this.frame = frame;
		}

		public StackFrame Frame {
			get { return frame; }
		}

		public ILanguage Language {
			get {
				if (frame.Language == null)
					throw new EvaluationException (
						"Stack frame has no source language.");

				return frame.Language;
			}
		}
	}

	public class BacktraceHandle
	{
		FrameHandle[] frames;

		public BacktraceHandle (ProcessHandle process, Backtrace backtrace)
		{
			StackFrame[] bt_frames = backtrace.Frames;
			if (bt_frames != null) {
				frames = new FrameHandle [bt_frames.Length];
				for (int i = 0; i < frames.Length; i++)
					frames [i] = new FrameHandle (bt_frames [i]);
			} else
				frames = new FrameHandle [0];
		}

		public int Length {
			get { return frames.Length; }
		}

		public FrameHandle this [int number] {
			get { return frames [number]; }
		}
	}

	public class ProcessHandle
	{
		ThreadGroup tgroup;
		Process process;
		string name;
		int id;

		public ProcessHandle (Process process)
		{
			this.process = process;
			this.name = process.Name;
			this.id = process.ID;
		}


		public ProcessHandle (Process process, int pid)
			: this (process)
		{
			if (process.HasTarget) {
				if (!process.IsDaemon) {
					StackFrame frame = process.CurrentFrame;
					current_frame = new FrameHandle (frame);
				}
			}
		}

		public Process Process {
			get { return process; }
		}

		public ThreadGroup ThreadGroup {
			get { return tgroup; }
		}

		BacktraceHandle current_backtrace = null;

		int current_frame_idx = -1;
		FrameHandle current_frame = null;
		AssemblerLine current_insn = null;

		public int CurrentFrameIndex {
			get {
				if (current_frame_idx == -1)
					return 0;

				return current_frame_idx;
			}

			set {
				GetBacktrace (-1);
				if ((value < 0) || (value >= current_backtrace.Length))
					throw new EvaluationException ("No such frame.");

				current_frame_idx = value;
				current_frame = current_backtrace [current_frame_idx];
			}
		}


		public BacktraceHandle GetBacktrace (int max_frames)
		{
			if (State == TargetState.NO_TARGET)
				throw new EvaluationException ("No stack.");
			else if (!process.IsStopped)
				throw new EvaluationException ("{0} is not stopped.", Name);

			if ((max_frames == -1) && (current_backtrace != null))
				return current_backtrace;

			current_backtrace = new BacktraceHandle (this, process.GetBacktrace (max_frames));

			if (current_backtrace == null)
				throw new EvaluationException ("No stack.");

			return current_backtrace;
		}

		public FrameHandle CurrentFrame {
			get {
				return GetFrame (current_frame_idx);
			}
		}

		public FrameHandle GetFrame (int number)
		{
			if (State == TargetState.NO_TARGET)
				throw new EvaluationException ("No stack.");
			else if (!process.IsStopped)
				throw new EvaluationException ("{0} is not stopped.", Name);

			if (number == -1) {
				if (current_frame == null)
					current_frame = new FrameHandle (process.CurrentFrame);

				return current_frame;
			}

			GetBacktrace (-1);
			if (number >= current_backtrace.Length)
				throw new EvaluationException ("No such frame: {0}", number);

			return current_backtrace [number];
		}

		public TargetState State {
			get {
				if (process == null)
					return TargetState.NO_TARGET;
				else
					return process.State;
			}
		}

		public string Name {
			get {
				return name;
			}
		}
	}

	public class EvaluationException : Exception
	{
		public EvaluationException (string format, params object[] args)
			: base (String.Format (format, args))
		{ }
	}

	public class EvaluationContext
	{
		ProcessHandle current_process;
		int current_frame_idx = -1;
		ITargetObject this_obj;

		static AddressDomain address_domain = new AddressDomain ("Evaluation");

		public EvaluationContext (ITargetObject this_obj)
		{
			this.this_obj = this_obj;
		}

		public ITargetObject This {
			get {
				return this_obj;
			}
		}

		public ProcessHandle CurrentProcess {
			get { return current_process; }
			set { current_process = value; }
		}

		public FrameHandle CurrentFrame {
			get {
				return current_process.GetFrame (current_frame_idx);
			}
		}

		public int CurrentFrameIndex {
			get {
				return current_frame_idx;
			}

			set {
				current_frame_idx = value;
			}
		}

		public string[] GetNamespaces (FrameHandle frame)
		{
			IMethod method = frame.Frame.Method;
			if ((method == null) || !method.HasSource)
				return null;

			MethodSource msource = method.Source;
			if (msource.IsDynamic)
				return null;

			return msource.GetNamespaces ();
		}

		public string[] GetNamespaces ()
		{
			return GetNamespaces (CurrentFrame);
		}

		public AddressDomain AddressDomain {
			get {
				return address_domain;
			}
		}

	}

}
#endif
