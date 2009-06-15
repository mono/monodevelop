using System;
using System.Threading;
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
using Mono.Debugging.Evaluation;

namespace MonoDevelop.Debugger.Win32
{
	public class CorDebuggerSession: DebuggerSession
	{
		object debugLock = new object ();

		CorDebugger dbg;
		CorProcess process;
		CorThread activeThread;
		CorStepper stepper;
		bool terminated;
		bool evaluating;

		static int evaluationTimestamp;

		SymbolBinder symbolBinder = new SymbolBinder ();
		Dictionary<string, DocInfo> documents;
		Dictionary<int, ProcessInfo> processes = new Dictionary<int, ProcessInfo> ();
		Dictionary<int, ThreadInfo> threads = new Dictionary<int,ThreadInfo> ();
		Dictionary<string, ModuleInfo> modules;
		

		public CorObjectAdaptor ObjectAdapter;
		public ExpressionEvaluator<CorValRef, CorType> Evaluator;

		class DocInfo
		{
			public ISymbolReader Reader;
			public ISymbolDocument Document;
			public CorModule Module;
		}

		class ModuleInfo
		{
			public ISymbolReader Reader;
			public CorModule Module;
			public CorMetadataImport Importer;
		}

		public CorDebuggerSession ( )
		{
			documents = new Dictionary<string, DocInfo> (StringComparer.CurrentCultureIgnoreCase);
			modules = new Dictionary<string, ModuleInfo> (StringComparer.CurrentCultureIgnoreCase);

			ObjectAdapter = new CorObjectAdaptor ();
			Evaluator = new NRefactoryEvaluator<CorValRef, CorType> ();
		}

		public new IDebuggerSessionFrontend Frontend {
			get { return base.Frontend; }
		}

		public static int EvaluationTimestamp {
			get { return evaluationTimestamp; }
		}


		public override void Dispose ( )
		{
			if (dbg != null && !terminated)
				dbg.Terminate ();
			base.Dispose ();

			// There is no explicit way of disposing the metadata objects, so we have
			// to rely on the GC to do it.

			modules = null;
			documents = null;
			threads = null;
			process = null;
			processes = null;
			dbg = null;
			activeThread = null;
			GC.Collect ();
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
			process.OnModuleUnload += new CorModuleEventHandler (OnModuleUnload);
			process.OnProcessExit += new CorProcessEventHandler (OnProcessExit);
			process.OnUpdateModuleSymbols += new UpdateModuleSymbolsEventHandler (OnUpdateModuleSymbols);
			process.OnDebuggerError += new DebuggerErrorEventHandler (OnDebuggerError);
			process.OnBreakpoint += new BreakpointEventHandler (OnBreakpoint);
			process.OnStepComplete += new StepCompleteEventHandler (OnStepComplete);
			process.OnBreak += new CorThreadEventHandler (OnBreak);
			process.OnNameChange += new CorThreadEventHandler (OnNameChange);

			process.Continue (false);

			OnStarted ();
		}

		void OnNameChange (object sender, CorThreadEventArgs e)
		{
		}

		void OnStopped ( )
		{
			lock (threads) {
				threads.Clear ();
			}
		}

		void OnBreak (object sender, CorThreadEventArgs e)
		{
			lock (debugLock) {
				if (evaluating) {
					e.Continue = true;
					return;
				}
			}
			OnStopped ();
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
			lock (debugLock) {
				if (evaluating) {
					e.Continue = true;
					return;
				}
			}
			OnStopped ();
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
			lock (debugLock) {
				if (evaluating) {
					e.Continue = true;
					return;
				}
			}
			OnStopped ();
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
			CorMetadataImport mi = new CorMetadataImport (e.Module);

			// Required to avoid the jit to get rid of variables too early
			e.Module.JITCompilerFlags = CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION;

			string file = e.Module.Assembly.Name;
			lock (documents) {
				ISymbolReader reader = null;
				if (file.IndexOfAny (System.IO.Path.InvalidPathChars) == -1 && System.IO.File.Exists (System.IO.Path.ChangeExtension (file, ".pdb"))) {
					try {
						reader = symbolBinder.GetReaderForFile (mi.RawCOMObject, file, ".");
						foreach (ISymbolDocument doc in reader.GetDocuments ()) {
							if (string.IsNullOrEmpty (doc.URL))
								continue;
							string docFile = System.IO.Path.GetFullPath (doc.URL);
							DocInfo di = new DocInfo ();
							di.Document = doc;
							di.Reader = reader;
							di.Module = e.Module;
							documents[docFile] = di;
							NotifySourceFileLoaded (docFile);
						}
					}
					catch (Exception ex) {
						OnDebuggerOutput (true, string.Format ("Debugger Error: {0}\n", ex.Message));
					}
				}
				ModuleInfo moi = new ModuleInfo ();
				moi.Module = e.Module;
				moi.Reader = reader;
				moi.Importer = mi;
				modules[e.Module.Name] = moi;
			}
			e.Continue = true;
		}

		void OnModuleUnload (object sender, CorModuleEventArgs e)
		{
			lock (documents) {
				modules.Remove (e.Module.Name);
				List<string> toRemove = new List<string> ();
				foreach (KeyValuePair<string, DocInfo> di in documents) {
					if (di.Value.Module.Name == e.Module.Name)
						toRemove.Add (di.Key);
				}
				foreach (string file in toRemove) {
					documents.Remove (file);
					NotifySourceFileUnloaded (file);
				}
			}
		}

		void OnCreateAppDomain (object sender, CorAppDomainEventArgs e)
		{
			e.AppDomain.Attach ();
			e.Continue = true;
		}

		void OnCreateProcess (object sender, CorProcessEventArgs e)
		{
			// Required to avoid the jit to get rid of variables too early
			e.Process.DesiredNGENCompilerFlags = CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION;
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
			ClearEvalStatus ();
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
				terminated = true;
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
			lock (documents) {
				ModuleInfo mod;
				if (!modules.TryGetValue (System.IO.Path.GetFullPath (file), out mod))
					return null;
				return mod.Reader;
			}
		}

		internal CorMetadataImport GetMetadataForModule (string file)
		{
			lock (documents) {
				ModuleInfo mod;
				if (!modules.TryGetValue (System.IO.Path.GetFullPath (file), out mod))
					return null;
				return mod.Importer;
			}
		}

		internal IEnumerable<CorModule> GetModules ( )
		{
			List<CorModule> mods = new List<CorModule> ();
			lock (documents) {
				foreach (ModuleInfo mod in modules.Values)
					mods.Add (mod.Module);
			}
			return mods;
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
				ClearEvalStatus ();
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
				ClearEvalStatus ();
				process.Continue (false);
			}
		}

		protected override void OnStop ( )
		{
			process.Stop (0);
			OnStopped ();
			CorThread currentThread = null;
			foreach (CorThread t in process.Threads) {
				currentThread = t;
				break;
			}
			TargetEventArgs args = new TargetEventArgs (TargetEventType.TargetStopped);
			args.Process = GetPocess (process);
			args.Thread = GetThread (currentThread);
			args.Backtrace = new Backtrace (new CorBacktrace (currentThread, this));
			OnTargetEvent (args);
		}

		protected override object OnUpdateBreakEvent (object handle, BreakEvent be)
		{
			return null;
		}

		public CorValue RuntimeInvoke (CorEvaluationContext ctx, CorFunction function, CorType[] typeArgs, CorValue thisObj, CorValue[] arguments)
		{
			if (!ctx.Thread.ActiveChain.IsManaged)
				throw new EvaluatorException ("Cannot evaluate expression because the thread is stopped in native code.");

			lock (debugLock) {
				evaluating = true;
			}

			CorValue[] args;
			if (thisObj == null)
				args = arguments;
			else {
				args = new CorValue[arguments.Length + 1];
				args[0] = thisObj;
				arguments.CopyTo (args, 1);
			}

			CorMethodCall mc = new CorMethodCall ();
			CorValue exception = null;

			EvalEventHandler completeHandler = delegate (object o, CorEvalEventArgs eargs) {
				mc.DoneEvent.Set ();
				eargs.Continue = false;
			};

			EvalEventHandler exceptionHandler = delegate (object o, CorEvalEventArgs eargs) {
				exception = eargs.Eval.Result;
				mc.DoneEvent.Set ();
				eargs.Continue = false;
			};

			process.OnEvalComplete += completeHandler;
			process.OnEvalException += exceptionHandler;

			mc.OnInvoke = delegate {
				if (function.GetMethodInfo (this).Name == ".ctor")
					ctx.Eval.NewParameterizedObject (function, typeArgs, args);
				else
					ctx.Eval.CallParameterizedFunction (function, typeArgs, args);
				process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_SUSPEND, ctx.Thread);
				ClearEvalStatus ();
				process.Continue (false);
			};
			mc.OnAbort = delegate {
				ctx.Eval.Abort ();
			};

			try {
				ObjectAdapter.AsyncExecute (mc, ctx.Timeout);
			}
			finally {
				process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_RUN, ctx.Thread);
				process.OnEvalComplete -= completeHandler;
				process.OnEvalException -= exceptionHandler;

				lock (debugLock) {
					evaluating = false;
				}
			}

			if (exception != null) {
/*				ValueReference<CorValue, CorType> msg = ctx.Adapter.GetMember (ctx, val, "Message");
				if (msg != null) {
					string s = msg.ObjectValue as string;
					mc.ExceptionMessage = s;
				}
				else
					mc.ExceptionMessage = "Evaluation failed.";*/
				CorValRef vref = new CorValRef (exception);
				throw new EvaluatorException ("Evaluation failed: " + ObjectAdapter.GetValueTypeName (ctx, vref));
			}

			return ctx.Eval.Result;
		}

		public CorValue NewString (CorEvaluationContext ctx, string value)
		{
			lock (debugLock) {
				evaluating = true;
			}

			ManualResetEvent doneEvent = new ManualResetEvent (false);

			EvalEventHandler completeHandler = delegate (object o, CorEvalEventArgs eargs) {
				doneEvent.Set ();
				eargs.Continue = false;
			};

			EvalEventHandler exceptionHandler = delegate (object o, CorEvalEventArgs eargs) {
				doneEvent.Set ();
				eargs.Continue = false;
			};

			try {
				process.OnEvalComplete += completeHandler;
				process.OnEvalException += exceptionHandler;

				ctx.Eval.NewString (value);
				process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_SUSPEND, ctx.Thread);
				ClearEvalStatus ();
				process.Continue (false);

				if (doneEvent.WaitOne (ctx.Timeout))
					return ctx.Eval.Result;
				else
					return null;
			} finally {
				process.SetAllThreadsDebugState (CorDebugThreadState.THREAD_RUN, ctx.Thread);
				process.OnEvalComplete -= completeHandler;
				process.OnEvalException -= exceptionHandler;

				lock (debugLock) {
					evaluating = false;
				}
			}
		}

		void ClearEvalStatus ( )
		{
			evaluationTimestamp++;
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
					string loc = string.Empty;
					if (thread.ActiveFrame != null) {
						StackFrame frame = CorBacktrace.CreateFrame (this, thread.ActiveFrame);
						loc = frame.ToString ();
					} else {
						loc = "<Unknown>";
					}
					
					info = new ThreadInfo (thread.Process.Id, thread.Id, GetThreadName (thread), loc);
					threads[thread.Id] = info;
				}
				return info;
			}
		}

		string GetThreadName (CorThread thread)
		{
			return string.Empty;
/*			CorValue to = thread.ThreadVariable;
			if (to == null)
				return string.Empty;

			CorEvaluationContext ctx = new CorEvaluationContext (this);
			ctx.Thread = thread;
			ctx.Frame = thread.ActiveFrame;

			LiteralValueReference<CorValRef, CorType> val = new LiteralValueReference<CorValRef, CorType> (ctx, "", new CorValRef (to));
			ValueReference<CorValRef, CorType> prop = val.GetChild ("Name");
			if (prop != null)
				return prop.ObjectValue as string;
			else
				return string.Empty;*/
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

		public static Type GetTypeInfo (this CorClass type, CorDebuggerSession session)
		{
			CorMetadataImport mi = session.GetMetadataForModule (type.Module.Name);
			if (mi != null)
				return mi.GetType (type.Token);
			else
				return null;
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
			CorMetadataImport mi = session.GetMetadataForModule (func.Module.Name);
			if (mi != null)
				return mi.GetMethodInfo (func.Token);
			else
				return null;
		}

		public static void SetValue (this CorValRef thisVal, EvaluationContext<CorValRef, CorType> ctx, CorValRef val)
		{
			CorReferenceValue s = thisVal.Val.CastToReferenceValue ();
			if (s != null) {
				CorReferenceValue v = val.Val.CastToReferenceValue ();
				if (v != null) {
					s.Value = v.Value;
					return;
				}
			}
			CorGenericValue gv = CorObjectAdaptor.GetRealObject (thisVal.Val) as CorGenericValue;
			if (gv != null)
				gv.SetValue (ctx.Adapter.TargetObjectToObject (ctx, val));
		}
	}
}
