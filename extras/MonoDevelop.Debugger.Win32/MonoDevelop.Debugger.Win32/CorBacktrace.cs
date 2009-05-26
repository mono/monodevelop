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
using MonoDevelop.Debugger.Evaluation;

namespace MonoDevelop.Debugger.Win32
{
	class CorBacktrace: IBacktrace
	{
		CorThread thread;
		CorDebuggerSession session;
		List<CorFrame> frames;

		public CorBacktrace (CorThread thread, CorDebuggerSession session)
		{
			this.session = session;
			this.thread = thread;
			frames = new List<CorFrame> (GetFrames (thread));
		}

		internal static IEnumerable<CorFrame> GetFrames (CorThread thread)
		{
			foreach (CorChain chain in thread.Chains) {
				foreach (CorFrame frame in chain.Frames)
					yield return frame;
			}
		}

		CorEvaluationContext GetEvaluationContext (int frameIndex, int timeout)
		{
			if (timeout == -1)
				timeout = session.ObjectAdapter.DefaultEvaluationTimeout;
			CorFrame frame = frames[frameIndex];
			CorEvaluationContext ctx = new CorEvaluationContext (session, frame, frameIndex);
			ctx.Thread = thread;
			ctx.Timeout = timeout;
			return ctx;
		}
	
		#region IBacktrace Members

		public AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			return new AssemblyLine[0];
		}

		public int FrameCount
		{
			get { return frames.Count; }
		}

		public ObjectValue[] GetAllLocals (int frameIndex, int timeout)
		{
			List<ObjectValue> locals = new List<ObjectValue> ();

			ObjectValue thisObj = GetThisReference (frameIndex, timeout);
			if (thisObj != null)
				locals.Add (thisObj);

			locals.AddRange (GetLocalVariables (frameIndex, timeout));
			locals.AddRange (GetParameters (frameIndex, timeout));

			return locals.ToArray ();
		}

		public CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			return new CompletionData ();
		}

		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, bool evaluateMethods, int timeout)
		{
			CorEvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			return ctx.Adapter.GetExpressionValuesAsync (ctx, expressions, evaluateMethods, timeout);
		}

		public ObjectValue[] GetLocalVariables (int frameIndex, int timeout)
		{
			CorEvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			List<ObjectValue> list = new List<ObjectValue> ();
			foreach (VariableReference var in ctx.Adapter.GetLocalVariables (ctx))
				list.Add (var.CreateObjectValue (true));
			return list.ToArray ();
		}

		public ObjectValue[] GetParameters (int frameIndex, int timeout)
		{
			CorEvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			List<ObjectValue> vars = new List<ObjectValue> ();
			foreach (VariableReference var in ctx.Adapter.GetParameters (ctx))
				vars.Add (var.CreateObjectValue (true));
			return vars.ToArray ();
		}

		public StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			if (lastIndex >= frames.Count)
				lastIndex = frames.Count - 1;
			StackFrame[] array = new StackFrame[lastIndex - firstIndex + 1];
			for (int n = 0; n < array.Length; n++)
				array[n] = CreateFrame (session, frames[n + firstIndex]);
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

			if (frame.FrameType == CorFrameType.ILFrame) {
				if (frame.Function != null) {
					module = frame.Function.Module.Name;
					CorMetadataImport importer = new CorMetadataImport (frame.Function.Module);
					MethodInfo mi = importer.GetMethodInfo (frame.Function.Token);
					method = mi.DeclaringType.FullName + "." + mi.Name;
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
			else if (frame.FrameType == CorFrameType.NativeFrame) {
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
			return new StackFrame ((long) address, module, method, file, line, lang);
		}

		public ObjectValue GetThisReference (int frameIndex, int timeout)
		{
			CorEvaluationContext ctx = GetEvaluationContext (frameIndex, timeout);
			ValueReference<CorValRef, CorType> var = ctx.Adapter.GetThisReference (ctx);
			if (var != null)
				return var.CreateObjectValue ();
			else
				return null;
		}

		#endregion
	}
}
