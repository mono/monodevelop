using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using System.Runtime.InteropServices;
using System.Diagnostics.SymbolStore;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorMetadata;
using Microsoft.Samples.Debugging.CorSymbolStore;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using MonoDevelop.Debugger.Evaluation;

namespace MonoDevelop.Debugger.Win32
{
	class CorDebuggerSession: DebuggerSession
	{
		CorDebugger dbg;
		CorProcess process;
		CorThread activeThread;
		CorStepper stepper;
		SymbolBinder binder = new SymbolBinder ();
		Dictionary<string, DocInfo> documents;
		Dictionary<string, ISymbolReader> readers;
		Dictionary<int, ProcessInfo> processes = new Dictionary<int, ProcessInfo> ();
		Dictionary<int, ThreadInfo> threads = new Dictionary<int,ThreadInfo> ();

		public CorObjectAdaptor ObjectAdapter;
		public ExpressionEvaluator<CorValue, CorType> Evaluator;
		public AsyncEvaluationTracker AsyncEvaluationTracker;

		class DocInfo
		{
			public ISymbolReader Reader;
			public ISymbolDocument Document;
			public CorModule Module;
		}

		public CorDebuggerSession ( )
		{
			documents = new Dictionary<string, DocInfo> (StringComparer.CurrentCultureIgnoreCase);
			readers = new Dictionary<string, ISymbolReader> (StringComparer.CurrentCultureIgnoreCase);

			AsyncEvaluationTracker = new AsyncEvaluationTracker ();
			ObjectAdapter = new CorObjectAdaptor ();
			Evaluator = new ExpressionEvaluator<CorValue, CorType> ();
		}

		public new IDebuggerSessionFrontend Frontend {
			get { return base.Frontend; }
		}

		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			// Create the debugger

			string dversion = CorDebugger.GetDebuggerVersionFromFile (startInfo.Command);
			dbg = new CorDebugger (dversion);

			process = dbg.CreateProcess (startInfo.Command, startInfo.Arguments, startInfo.WorkingDirectory);

			process.OnCreateProcess += new CorProcessEventHandler (OnCreateProcess);
			process.OnCreateAppDomain += new CorAppDomainEventHandler (OnCreateAppDomain);
			process.OnAssemblyLoad += new CorAssemblyEventHandler (OnAssemblyLoad);
			process.OnAssemblyUnload += new CorAssemblyEventHandler (OnAssemblyUnload);
			process.OnCreateThread += new CorThreadEventHandler (OnCreateThread);
			process.OnThreadExit += new CorThreadEventHandler (OnThreadExit);
			process.OnModuleLoad += new CorModuleEventHandler (OnModuleLoad);
			process.OnProcessExit += new CorProcessEventHandler (OnProcessExit);
			process.OnUpdateModuleSymbols += new UpdateModuleSymbolsEventHandler (OnUpdateModuleSymbols);
			process.OnDebuggerError += new DebuggerErrorEventHandler (OnDebuggerError);
			process.OnBreakpoint += new BreakpointEventHandler (OnBreakpoint);
			process.OnStepComplete += new StepCompleteEventHandler (OnStepComplete);
			process.OnBreak += new CorThreadEventHandler (OnBreak);

			process.Continue (false);

			OnStarted ();
		}

		void OnBreak (object sender, CorThreadEventArgs e)
		{
			e.Continue = false;
			SetActiveThread (e.Thread);
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetInterrupted);
			args.Process = GetPocess (process);
			args.Thread = GetThread (e.Thread);
			args.Backtrace = new Backtrace (new CorBacktrace (e.Thread, this));
			OnTargetEvent (args);
		}

		void OnStepComplete (object sender, CorStepCompleteEventArgs e)
		{
			e.Continue = false;
			SetActiveThread (e.Thread);
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetStopped);
			args.Process = GetPocess (process);
			args.Thread = GetThread (e.Thread);
			args.Backtrace = new Backtrace (new CorBacktrace (e.Thread, this));
			OnTargetEvent (args);
		}

		void OnThreadExit (object sender, CorThreadEventArgs e)
		{
			lock (threads) {
				threads.Remove (e.Thread.Id);
			}
		}

		void OnBreakpoint (object sender, CorBreakpointEventArgs e)
		{
			e.Continue = false;
			SetActiveThread (e.Thread);
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetHitBreakpoint);
			args.Process = GetPocess (process);
			args.Thread = GetThread (e.Thread);
			args.Backtrace = new Backtrace (new CorBacktrace (e.Thread, this));
			OnTargetEvent (args);
		}

		void OnDebuggerError (object sender, CorDebuggerErrorEventArgs e)
		{
			Exception ex = Marshal.GetExceptionForHR (e.HResult);
			OnDebuggerOutput (true, string.Format ("Debugger Error: {0}\n", ex.Message));
		}

		void OnUpdateModuleSymbols (object sender, CorUpdateModuleSymbolsEventArgs e)
		{
			SymbolBinder binder = new SymbolBinder ();
			CorMetadataImport mi = new CorMetadataImport (e.Module);
			ISymbolReader reader = binder.GetReaderFromStream (mi.RawCOMObject, e.Stream);
			foreach (ISymbolDocument doc in reader.GetDocuments ()) {
				Console.WriteLine (doc.URL);
			}
			e.Continue = true;
		}

		void OnProcessExit (object sender, CorProcessEventArgs e)
		{
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetExited);
			OnTargetEvent (args);
		}

		void OnAssemblyUnload (object sender, CorAssemblyEventArgs e)
		{
			OnDebuggerOutput (false, string.Format ("Unloaded Module '{0}'\n", e.Assembly.Name));
			e.Continue = true;
		}

		void OnModuleLoad (object sender, CorModuleEventArgs e)
		{
			SymbolBinder binder = new SymbolBinder ();
			CorMetadataImport mi = new CorMetadataImport (e.Module);
			string file = e.Module.Assembly.Name;
			try {
				lock (documents) {
					ISymbolReader reader = binder.GetReaderForFile (mi.RawCOMObject, file, ".");
					foreach (ISymbolDocument doc in reader.GetDocuments ()) {
						string docFile = System.IO.Path.GetFullPath (doc.URL);
						DocInfo di = new DocInfo ();
						di.Document = doc;
						di.Reader = reader;
						di.Module = e.Module;
						di.Module.SymbolReader = reader;
						documents[docFile] = di;
						readers[di.Module.Name] = reader;
						NotifySourceFileLoaded (docFile);
					}
				}
			}
			catch {
			}

			e.Continue = true;
		}

		void OnCreateAppDomain (object sender, CorAppDomainEventArgs e)
		{
			e.AppDomain.Attach ();
			e.Continue = true;
		}

		void OnCreateProcess (object sender, CorProcessEventArgs e)
		{
			e.Continue = true;
		}

		void OnCreateThread (object sender, CorThreadEventArgs e)
		{
			OnDebuggerOutput (false, string.Format ("Started Thread {0}", e.Thread.Id));
			e.Continue = true;
		}

		void OnAssemblyLoad (object sender, CorAssemblyEventArgs e)
		{
			OnDebuggerOutput (false, string.Format ("Loaded Module '{0}'", e.Assembly.Name));
			e.Continue = true;
		}

		protected override void OnAttachToProcess (int processId)
		{
		}

		protected override void OnContinue ( )
		{
			process.Continue (false);
		}

		protected override void OnDetach ( )
		{
			process.Detach ();
		}

		protected override void OnEnableBreakEvent (object handle, bool enable)
		{
			CorBreakpoint bp = handle as CorFunctionBreakpoint;
			if (bp != null)
				bp.Activate (enable);
		}

		protected override void OnExit ( )
		{
			try {
				process.Terminate (1);
			}
			catch (Exception ex) {
				OnDebuggerOutput (true, ex.Message + "\n");
			}
		}

		protected override void OnFinish ( )
		{
			if (stepper != null)
				stepper.StepOut ();
		}

		protected override ProcessInfo[] OnGetPocesses ( )
		{
			return new ProcessInfo[] { GetPocess (process) };
		}

		protected override Backtrace OnGetThreadBacktrace (int processId, int threadId)
		{
			foreach (CorThread t in process.Threads) {
				if (t.Id == threadId) {
					return new Backtrace (new CorBacktrace (t, this));
				}
			}
			return null;
		}

		protected override ThreadInfo[] OnGetThreads (int processId)
		{
			List<ThreadInfo> list = new List<ThreadInfo> ();
			foreach (CorThread t in process.Threads)
				list.Add (GetThread (t));
			return list.ToArray ();
		}

		internal ISymbolReader GetReaderForModule (string file)
		{
			ISymbolReader doc;
			if (!readers.TryGetValue (System.IO.Path.GetFullPath (file), out doc))
				return null;
			return doc;
		}

		protected override object OnInsertBreakEvent (BreakEvent be, bool activate)
		{
			lock (documents) {
				Breakpoint bp = be as Breakpoint;
				if (bp != null) {
					DocInfo doc;
					if (!documents.TryGetValue (System.IO.Path.GetFullPath (bp.FileName), out doc))
						return null;

					int line = doc.Document.FindClosestLine (bp.Line);
					ISymbolMethod met = doc.Reader.GetMethodFromDocumentPosition (doc.Document, line, 0);
					if (met == null)
						return null;

					int offset = -1;
					foreach (SequencePoint sp in met.GetSequencePoints ()) {
						if (sp.Line == line && sp.Document.URL == doc.Document.URL) {
							offset = sp.Offset;
							break;
						}
					}
					if (offset == -1)
						return null;

					CorFunction func = doc.Module.GetFunctionFromToken (met.Token.GetToken ());
					CorFunctionBreakpoint corBp = func.ILCode.CreateBreakpoint (offset);
					corBp.Activate (activate);
					return corBp;
				}
			}
			return null;
		}

		protected override void OnNextInstruction ( )
		{
		}

		protected override void OnNextLine ( )
		{
			if (stepper != null) {
				CorFrame frame = activeThread.ActiveFrame;
				ISymbolReader reader = GetReaderForModule (frame.Function.Module.Name);
				if (reader == null)
					return;
				ISymbolMethod met = reader.GetMethod (new SymbolToken (frame.Function.Token));
				if (met == null)
					return;

				uint offset;
				CorDebugMappingResult mappingResult;
				frame.GetIP (out offset, out mappingResult);

				SequencePoint nextSeq = null;
				foreach (SequencePoint sp in met.GetSequencePoints ()) {
					if (sp.Offset > offset) {
						nextSeq = sp;
						break;
					}
				}

				if (nextSeq == null)
					return;

				COR_DEBUG_STEP_RANGE[] ranges = new COR_DEBUG_STEP_RANGE[1];
				ranges[0] = new COR_DEBUG_STEP_RANGE ();
				ranges[0].startOffset = offset;
				ranges[0].endOffset = (uint) nextSeq.Offset;
				stepper.StepRange (false, ranges);
				process.Continue (false);
			}
		}

		protected override void OnRemoveBreakEvent (object handle)
		{
		}


		protected override void OnSetActiveThread (int processId, int threadId)
		{
			activeThread = null;
			stepper = null;
			foreach (CorThread t in process.Threads) {
				if (t.Id == threadId) {
					SetActiveThread (t);
					break;
				}
			}
		}

		void SetActiveThread (CorThread t)
		{
			activeThread = t;
			stepper = activeThread.CreateStepper ();
			stepper.SetUnmappedStopMask (CorDebugUnmappedStop.STOP_NONE);
		}

		protected override void OnStepInstruction ( )
		{
		}

		protected override void OnStepLine ( )
		{
			if (stepper != null) {
				stepper.Step (true);
				process.Continue (false);
			}
		}

		protected override void OnStop ( )
		{
			process.Stop (0);
		}

		protected override object OnUpdateBreakEvent (object handle, BreakEvent be)
		{
			return null;
		}

		ProcessInfo GetPocess (CorProcess proc)
		{
			ProcessInfo info;
			lock (processes) {
				if (!processes.TryGetValue (proc.Id, out info)) {
					info = new ProcessInfo (proc.Id, "");
					processes[proc.Id] = info;
				}
			}
			return info;
		}

		ThreadInfo GetThread (CorThread thread)
		{
			ThreadInfo info;
			lock (threads) {
				if (!threads.TryGetValue (thread.Id, out info)) {
					info = new ThreadInfo (thread.Process.Id, thread.Id, "", "");
					threads[thread.Id] = info;
				}
				return info;
			}
		}
	}

	class SequencePoint
	{
		public int Line;
		public int Offset;
		public ISymbolDocument Document;
	}

	static class SequencePointExt
	{
		public static IEnumerable<SequencePoint> GetSequencePoints (this ISymbolMethod met)
		{
			int sc = met.SequencePointCount;
			int[] offsets = new int[sc];
			int[] lines = new int[sc];
			int[] endLines = new int[sc];
			int[] columns = new int[sc];
			int[] endColumns = new int[sc];
			ISymbolDocument[] docs = new ISymbolDocument[sc];
			met.GetSequencePoints (offsets, docs, lines, columns, endLines, endColumns);

			for (int n = 0; n < sc; n++) {
				if (columns[n] == 0)
					continue;
				SequencePoint sp = new SequencePoint ();
				sp.Document = docs[n];
				sp.Line = lines[n];
				sp.Offset = offsets[n];
				yield return sp;
			}
		}

		public static Type GetTypeInfo (this CorClass type)
		{
			CorMetadataImport mi = new CorMetadataImport (type.Module);
			return mi.GetType (type.Token);
		}

		public static ISymbolMethod GetSymbolMethod (this CorFunction func, CorDebuggerSession session)
		{
			ISymbolReader reader = session.GetReaderForModule (func.Module.Name);
			if (reader == null)
				return null;
			return reader.GetMethod (new SymbolToken (func.Token));
		}

		public static System.Reflection.MethodInfo GetMethodInfo (this CorFunction func, CorDebuggerSession session)
		{
			CorMetadataImport mi = new CorMetadataImport (func.Module);
			return mi.GetMethodInfo (func.Token);
		}
	}
}
