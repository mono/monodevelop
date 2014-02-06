using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Reflection;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorMetadata;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

namespace MonoDevelop.Debugger.Win32
{
	class CorBacktrace: BaseBacktrace
	{
		CorThread thread;
		readonly int threadId;
		readonly CorDebuggerSession session;
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
			// TODO: Fix remaining.
			uint address = 0;
			//string typeFQN;
			//string typeFullName;
			string addressSpace = "";
			string file = "";
			int line = 0;
			int column = 0;
			string method = "";
			string lang = "";
			string module = "";
			string type = "";
			bool hasDebugInfo = false;
			bool hidden = false;
			bool external = true;

			if (frame.FrameType == CorFrameType.ILFrame) {
				if (frame.Function != null) {
					module = frame.Function.Module.Name;
					CorMetadataImport importer = new CorMetadataImport (frame.Function.Module);
					MethodInfo mi = importer.GetMethodInfo (frame.Function.Token);
					method = mi.DeclaringType.FullName + "." + mi.Name;
					type = mi.DeclaringType.FullName;
					addressSpace = mi.Name;
					ISymbolReader reader = session.GetReaderForModule (frame.Function.Module.Name);
					if (reader != null) {
						ISymbolMethod met = reader.GetMethod (new SymbolToken (frame.Function.Token));
						if (met != null) {
							CorDebugMappingResult mappingResult;
							frame.GetIP (out address, out mappingResult);
							SequencePoint prevSp = null;
							foreach (SequencePoint sp in met.GetSequencePoints ()) {
								if (sp.Offset > address)
									break;
								prevSp = sp;
							}
							if (prevSp != null) {
								line = prevSp.Line;
								column = prevSp.Offset;
								file = prevSp.Document.URL;
								address = (uint)prevSp.Offset;
							}
						}
					}
					// FIXME: Still steps into.
					//hidden = mi.GetCustomAttributes (true).Any (v => v is System.Diagnostics.DebuggerHiddenAttribute);
				}
				lang = "Managed";
				hasDebugInfo = true;
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

			var loc = new SourceLocation (method, file, line, column);
			return new StackFrame ((long) address, addressSpace, loc, lang, external, hasDebugInfo, hidden, null, null);
		}

		#endregion
	}
}
