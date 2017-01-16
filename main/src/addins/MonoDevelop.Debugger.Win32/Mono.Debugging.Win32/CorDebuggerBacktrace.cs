using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Reflection;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorMetadata;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using System.Linq;

namespace Mono.Debugging.Win32
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

		private const int SpecialSequencePoint = 0xfeefee;

		public static SequencePoint GetSequencePoint(CorDebuggerSession session, CorFrame frame)
		{
			ISymbolReader reader = session.GetReaderForModule (frame.Function.Module);
			if (reader == null)
				return null;

			ISymbolMethod met = reader.GetMethod (new SymbolToken (frame.Function.Token));
			if (met == null)
				return null;

			int SequenceCount = met.SequencePointCount;
			if (SequenceCount <= 0)
				return null;

			CorDebugMappingResult mappingResult;
			uint ip;
			frame.GetIP (out ip, out mappingResult);
			if (mappingResult == CorDebugMappingResult.MAPPING_NO_INFO || mappingResult == CorDebugMappingResult.MAPPING_UNMAPPED_ADDRESS)
				return null;

			int[] offsets = new int[SequenceCount];
			int[] lines = new int[SequenceCount];
			int[] endLines = new int[SequenceCount];
			int[] columns = new int[SequenceCount];
			int[] endColumns = new int[SequenceCount];
			ISymbolDocument[] docs = new ISymbolDocument[SequenceCount];
			met.GetSequencePoints (offsets, docs, lines, columns, endLines, endColumns);

			if ((SequenceCount > 0) && (offsets [0] <= ip)) {
				int i;
				for (i = 0; i < SequenceCount; ++i) {
					if (offsets [i] >= ip) {
						break;
					}
				}

				if ((i == SequenceCount) || (offsets [i] != ip)) {
					--i;
				}

				if (lines [i] == SpecialSequencePoint) {
					int j = i;
					// let's try to find a sequence point that is not special somewhere earlier in the code
					// stream.
					while (j > 0) {
						--j;
						if (lines [j] != SpecialSequencePoint) {
							return new SequencePoint () {
								IsSpecial = true,
								Offset = offsets [j],
								StartLine = lines [j],
								EndLine = endLines [j],
								StartColumn = columns [j],
								EndColumn = endColumns [j],
								Document = docs [j]
							};
						}
					}
					// we didn't find any non-special seqeunce point before current one, let's try to search
					// after.
					j = i;
					while (++j < SequenceCount) {
						if (lines [j] != SpecialSequencePoint) {
							return new SequencePoint () {
								IsSpecial = true,
								Offset = offsets [j],
								StartLine = lines [j],
								EndLine = endLines [j],
								StartColumn = columns [j],
								EndColumn = endColumns [j],
								Document = docs [j]
							};
						}
					}

					// Even if sp is null at this point, it's a valid scenario to have only special sequence 
					// point in a function.  For example, we can have a compiler-generated default ctor which
					// doesn't have any source.
					return null;
				} else {
					return new SequencePoint () {
						IsSpecial = false,
						Offset = offsets [i],
						StartLine = lines [i],
						EndLine = endLines [i],
						StartColumn = columns [i],
						EndColumn = endColumns [i],
						Document = docs [i]
					};
				}
			}
			return null;
		}

		internal static StackFrame CreateFrame (CorDebuggerSession session, CorFrame frame)
		{
			uint address = 0;
			string addressSpace = "";
			string file = "";
			int line = 0;
			int endLine = 0;
			int column = 0;
			int endColumn = 0;
			string method = "[Unknown]";
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
					var declaringType = mi.DeclaringType;
					if (declaringType != null) {
						method = declaringType.FullName + "." + mi.Name;
						type = declaringType.FullName;
					}
					else {
						method = mi.Name;
					}

					addressSpace = mi.Name;
					
					var sp = GetSequencePoint (session, frame);
					if (sp != null) {
						line = sp.StartLine;
						column = sp.StartColumn;
						endLine = sp.EndLine;
						endColumn = sp.EndColumn;
						file = sp.Document.URL;
						address = (uint)sp.Offset;
					}

					if (session.IsExternalCode (file)) {
						external = true;
					} else {
						if (session.Options.ProjectAssembliesOnly) {
							external = mi.GetCustomAttributes (true).Any (v => 
								v is System.Diagnostics.DebuggerHiddenAttribute ||
							v is System.Diagnostics.DebuggerNonUserCodeAttribute);
						} else {
							external = mi.GetCustomAttributes (true).Any (v => 
								v is System.Diagnostics.DebuggerHiddenAttribute);
						}
					}
					hidden = mi.GetCustomAttributes (true).Any (v => v is System.Diagnostics.DebuggerHiddenAttribute);
				}
				lang = "Managed";
				hasDebugInfo = true;
			} else if (frame.FrameType == CorFrameType.NativeFrame) {
				frame.GetNativeIP (out address);
				method = "[Native frame]";
				lang = "Native";
			} else if (frame.FrameType == CorFrameType.InternalFrame) {
				switch (frame.InternalFrameType) {
				case CorDebugInternalFrameType.STUBFRAME_M2U:
					method = "[Managed to Native Transition]";
					break;
				case CorDebugInternalFrameType.STUBFRAME_U2M:
					method = "[Native to Managed Transition]";
					break;
				case CorDebugInternalFrameType.STUBFRAME_LIGHTWEIGHT_FUNCTION:
					method = "[Lightweight Method Call]";
					break;
				case CorDebugInternalFrameType.STUBFRAME_APPDOMAIN_TRANSITION:
					method = "[Application Domain Transition]";
					break;
				case CorDebugInternalFrameType.STUBFRAME_FUNC_EVAL:
					method = "[Function Evaluation]";
					break;
				}
			}

			var loc = new SourceLocation (method, file, line, column, endLine, endColumn);
			return new StackFrame (address, addressSpace, loc, lang, external, hasDebugInfo, hidden, module, type);
		}

		#endregion
	}
}
