using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.SymbolStore;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorMetadata;
using Mono.Debugging.Evaluation;

namespace MonoDevelop.Debugger.Win32
{
	class CorBacktrace: BaseBacktrace
	{
		CorThread thread;
		int threadId;
		CorDebuggerSession session;
		List<CorFrame> frames;
		int evalTimestamp;

		public CorBacktrace (CorThread thread, CorDebuggerSession session): base (session.ObjectAdapter)
		{
			this.session = session;
			this.thread = thread;
			threadId = thread.Id;
			frames = new List<CorFrame> (GetFrames (thread));
			evalTimestamp = CorDebuggerSession.EvaluationTimestamp;
		}

		internal static IEnumerable<CorFrame> GetFrames (CorThread thread)
		{
			foreach (CorChain chain in thread.Chains) {
                if (!chain.IsManaged)
                    continue;
                foreach (CorFrame frame in chain.Frames)
                    yield return frame;
			}
		}

		internal List<CorFrame> FrameList {
			get {
				if (evalTimestamp != CorDebuggerSession.EvaluationTimestamp) {
					thread = session.GetThread (threadId);
					frames = new List<CorFrame> (GetFrames (thread));
					evalTimestamp = CorDebuggerSession.EvaluationTimestamp;
				}
				return frames;
			}
		}

		protected override EvaluationContext GetEvaluationContext (int frameIndex, EvaluationOptions options)
		{
			CorEvaluationContext ctx = new CorEvaluationContext (session, this, frameIndex, options);
			ctx.Thread = thread;
			return ctx;
		}
	
		#region IBacktrace Members

		public override AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			return new AssemblyLine[0];
		}

		public override int FrameCount
		{
			get { return FrameList.Count; }
		}

		public override StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			if (lastIndex >= FrameList.Count)
				lastIndex = FrameList.Count - 1;
			StackFrame[] array = new StackFrame[lastIndex - firstIndex + 1];
			for (int n = 0; n < array.Length; n++)
				array[n] = CreateFrame (session, FrameList[n + firstIndex]);
			return array;
		}

		internal static StackFrame CreateFrame (CorDebuggerSession session, CorFrame frame)
		{
			uint address = 0;
			string file = "";
			int line = 0;
			string method = "";
			string lang = "";
			string module = "";
			string type = "";

			if (frame.FrameType == CorFrameType.ILFrame) {
				if (frame.Function != null) {
					module = frame.Function.Module.Name;
					CorMetadataImport importer = new CorMetadataImport (frame.Function.Module);
					MethodInfo mi = importer.GetMethodInfo (frame.Function.Token);
					method = mi.DeclaringType.FullName + "." + mi.Name;
					type = mi.DeclaringType.FullName;
					ISymbolReader reader = session.GetReaderForModule (frame.Function.Module.Name);
					if (reader != null) {
						ISymbolMethod met = reader.GetMethod (new SymbolToken (frame.Function.Token));
						if (met != null) {
							uint offset;
							CorDebugMappingResult mappingResult;
							frame.GetIP (out offset, out mappingResult);
							SequencePoint prevSp = null;
							foreach (SequencePoint sp in met.GetSequencePoints ()) {
								if (sp.Offset > offset)
									break;
								prevSp = sp;
							}
							if (prevSp != null) {
								line = prevSp.Line;
								file = prevSp.Document.URL;
							}
						}
					}
				}
				lang = "Managed";
			}
			else if (frame.FrameType == CorFrameType.NativeFrame) {
				frame.GetNativeIP (out address);
				method = "<Unknown>";
				lang = "Native";
			}
			else if (frame.FrameType == CorFrameType.InternalFrame) {
				switch (frame.InternalFrameType) {
					case CorDebugInternalFrameType.STUBFRAME_M2U: method = "[Managed to Native Transition]"; break;
					case CorDebugInternalFrameType.STUBFRAME_U2M: method = "[Native to Managed Transition]"; break;
					case CorDebugInternalFrameType.STUBFRAME_LIGHTWEIGHT_FUNCTION: method = "[Lightweight Method Call]"; break;
					case CorDebugInternalFrameType.STUBFRAME_APPDOMAIN_TRANSITION: method = "[Application Domain Transition]"; break;
					case CorDebugInternalFrameType.STUBFRAME_FUNC_EVAL: method = "[Function Evaluation]"; break;
				}
			}
			if (method == null)
				method = "<Unknown>";
			var loc = new SourceLocation (method, file, line);
			return new StackFrame ((long) address, loc, lang);
		}

		#endregion
	}
}
