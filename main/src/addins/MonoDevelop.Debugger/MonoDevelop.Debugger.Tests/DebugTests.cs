// 
// DebugTests.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;

using Mono.Debugging.Soft;
using Mono.Debugging.Client;

using NUnit.Framework;

namespace MonoDevelop.Debugger.Tests
{
	[TestFixture]
	public abstract partial class DebugTests
	{
		const string TestAppExeName = "MonoDevelop.Debugger.Tests.TestApp.exe";
		const string TestAppProjectDirName = "MonoDevelop.Debugger.Tests.TestApp";

		readonly ManualResetEvent targetStoppedEvent = new ManualResetEvent (false);
		readonly string EngineId;
		string TestName = "";
		TextFile SourceFile;

		SourceLocation lastStoppedPosition;

		public bool AllowTargetInvokes { get; protected set; }

		public DebuggerSession Session { get; private set; }

		public StackFrame Frame { get; private set; }

		protected DebugTests (string engineId)
		{
			EngineId = engineId;
		}

		public void IgnoreCorDebugger (string message = "")
		{
			if (!(Session is SoftDebuggerSession)) {
				Assert.Ignore (message);
			}
		}

		public void IgnoreSoftDebugger (string message = "")
		{
			if (Session is SoftDebuggerSession) {
				Assert.Ignore (message);
			}
		}

		// TODO: implement in another part of the class
		#region Partial Definitions

		/// <summary>
		/// Returns parent directory of target executable
		/// </summary>
		//protected string TargetExeDirectory { get { throw new NotImplementedException (); } }

		/// <summary>
		/// Returns parent directory of target project sources
		/// </summary>
		//protected string TargetProjectSourceDir { get { throw new NotImplementedException (); } }

		/// <summary>
		/// Creates debugger session. The type of session is dependent on <paramref name="engineId"/>
		/// </summary>
		/// <param name="test">test name, usually used as entry point method in target exe</param>
		/// <param name="engineId">the ID of debugger engine</param>
		//protected DebuggerSession CreateSession (string test, string engineId);

		/// <summary>
		/// Creates start info to run the app
		/// </summary>
		/// <param name="test">test name</param>
		//protected DebuggerStartInfo CreateStartInfo (string test);

		#endregion


		[TestFixtureSetUp]
		public virtual void SetUp ()
		{
			SetUpPartial ();
		}

		partial void SetUpPartial();


		[TestFixtureTearDown]
		public virtual void TearDown ()
		{
			TearDownPartial ();
			if (Session != null) {
				Session.Exit ();
				Session.Dispose ();
				Session = null;
			}
		}

		partial void TearDownPartial ();

		protected string TargetExePath
		{
			get{
				return Path.Combine (TargetExeDirectory, TestAppExeName);
			}
		}

		protected void Start (string test)
		{
			TestName = test;
			Session = CreateSession (test, EngineId);

			var dsi = CreateStartInfo (test);
			var soft = dsi as SoftDebuggerStartInfo;

			if (soft != null) {
				var assemblyName = AssemblyName.GetAssemblyName (TargetExePath);

				soft.UserAssemblyNames = new List<AssemblyName> {assemblyName};
			}
			var ops = new DebuggerSessionOptions {
				ProjectAssembliesOnly = true,
				EvaluationOptions = EvaluationOptions.DefaultOptions
			};
			ops.EvaluationOptions.AllowTargetInvoke = AllowTargetInvokes;
			ops.EvaluationOptions.EvaluationTimeout = 100000;


			var sourcePath = Path.Combine (TargetProjectSourceDir, test + ".cs");
			SourceFile = TextFile.ReadFile (sourcePath);
			AddBreakpoint ("break");

			var done = new ManualResetEvent (false);

			Session.TargetHitBreakpoint += (sender, e) => {
				Frame = e.Backtrace.GetFrame (0);
				lastStoppedPosition = Frame.SourceLocation;
				targetStoppedEvent.Set ();
				done.Set ();
			};

			Session.TargetExceptionThrown += (sender, e) => {
				Frame = e.Backtrace.GetFrame (0);
				for (int i = 0; i < e.Backtrace.FrameCount; i++) {
					if (!e.Backtrace.GetFrame (i).IsExternalCode) {
						Frame = e.Backtrace.GetFrame (i);
						break;
					}
				}
				lastStoppedPosition = Frame.SourceLocation;
				targetStoppedEvent.Set ();
			};

			Session.TargetStopped += (sender, e) => {
				//This can be null in case of ForcedStop
				//which is called when exception is thrown
				//when Continue & Stepping is executed
				if (e.Backtrace != null) {
					Frame = e.Backtrace.GetFrame (0);
					lastStoppedPosition = Frame.SourceLocation;
					targetStoppedEvent.Set ();
				} else {
					Console.WriteLine ("e.Backtrace is null");
				}
			};

			var targetExited = new ManualResetEvent (false);
			Session.TargetExited += delegate {
				targetExited.Set ();
			};

			Session.Run (dsi, ops);
			Session.ExceptionHandler = (ex) => {
				Console.WriteLine ("Session.ExceptionHandler:" + Environment.NewLine + ex.ToString ());
				return true;
			};
			switch (WaitHandle.WaitAny (new WaitHandle[]{ done, targetExited }, 30000)) {
			case 0:
				//Breakpoint is hit good... run tests now
				break;
			case 1:
				throw new Exception ("Test application exited before hitting breakpoint");
			default:
				throw new Exception ("Timeout while waiting for initial breakpoint");
			}
			if (Session is SoftDebuggerSession) {
				Console.WriteLine ("SDB protocol version:" + ((SoftDebuggerSession)Session).ProtocolVersion);
			}
		}

		void GetLineAndColumn (string breakpointMarker, int offset, string statement, out int line, out int col)
		{
			int i = SourceFile.Text.IndexOf ("/*" + breakpointMarker + "*/", StringComparison.Ordinal);
			if (i == -1)
				Assert.Fail ("Break marker not found: " + breakpointMarker + " in " + SourceFile.Name);
			SourceFile.GetLineColumnFromPosition (i, out line, out col);
			line += offset;
			if (statement != null) {
				int lineStartPosition = SourceFile.GetPositionFromLineColumn (line, 1);
				string lineText = SourceFile.GetText (lineStartPosition, lineStartPosition + SourceFile.GetLineLength (line));
				col = lineText.IndexOf (statement, StringComparison.Ordinal) + 1;
				if (col == 0)
					Assert.Fail ("Failed to find statement:" + statement + " at " + SourceFile.Name + "(" + line + ")");
			} else {
				col = 1;
			}
		}

		public Breakpoint AddBreakpoint (string breakpointMarker, int offset = 0, string statement = null)
		{
			int col, line;
			GetLineAndColumn (breakpointMarker, offset, statement, out line, out col);
			var bp = new Breakpoint (SourceFile.Name, line, col);
			Session.Breakpoints.Add (bp);
			return bp;
		}

		public void RunToCursor (string breakpointMarker, int offset = 0, string statement = null)
		{
			int col, line;
			GetLineAndColumn (breakpointMarker, offset, statement, out line, out col);
			targetStoppedEvent.Reset ();
			Session.Breakpoints.RemoveRunToCursorBreakpoints ();
			var bp = new RunToCursorBreakpoint (SourceFile.Name, line, col);
			Session.Breakpoints.Add (bp);
			Session.Continue ();
			CheckPosition (breakpointMarker, offset, statement);
		}

		public void InitializeTest ()
		{
			Session.Breakpoints.Clear ();
			Session.Options.EvaluationOptions = EvaluationOptions.DefaultOptions;
			Session.Options.ProjectAssembliesOnly = true;
			Session.Options.StepOverPropertiesAndOperators = false;
			AddBreakpoint ("break");
			while (!CheckPosition ("break", 0, silent: true)) {
				targetStoppedEvent.Reset ();
				Session.Continue ();
			}
		}

		public ObjectValue Eval (string exp)
		{
			return Frame.GetExpressionValue (exp, true).Sync ();
		}

		public void WaitStop (int miliseconds)
		{
			if (!targetStoppedEvent.WaitOne (miliseconds)) {
				Assert.Fail ("WaitStop failure: Target stop timeout");
			}
		}

		public bool CheckPosition (string guid, int offset = 0, string statement = null, bool silent = false)
		{
			if (!targetStoppedEvent.WaitOne (6000)) {
				if (!silent)
					Assert.Fail ("CheckPosition failure: Target stop timeout");
				return false;
			}
			if (lastStoppedPosition.FileName == SourceFile.Name) {
				int i = SourceFile.Text.IndexOf ("/*" + guid + "*/", StringComparison.Ordinal);
				if (i == -1) {
					if (!silent)
						Assert.Fail ("CheckPosition failure: Guid marker not found:" + guid + " in file:" + SourceFile.Name);
					return false;
				}
				int line, col;
				SourceFile.GetLineColumnFromPosition (i, out line, out col);
				if ((line + offset) != lastStoppedPosition.Line) {
					if (!silent)
						Assert.Fail ("CheckPosition failure: Wrong line Expected:" + (line + offset) + " Actual:" + lastStoppedPosition.Line + " in file:" + SourceFile.Name);
					return false;
				}
				if (!string.IsNullOrEmpty (statement)) {
					int position = SourceFile.GetPositionFromLineColumn (lastStoppedPosition.Line, lastStoppedPosition.Column);
					string actualStatement = SourceFile.GetText (position, position + statement.Length);
					if (statement != actualStatement) {
						if (!silent)
							Assert.AreEqual (statement, actualStatement);
						return false;
					}
				}
			} else {
				if (!silent)
					Assert.Fail ("CheckPosition failure: Wrong file Excpected:" + SourceFile.Name + " Actual:" + lastStoppedPosition.FileName);
				return false;
			}
			return true;
		}

		public void StepIn (string guid, string statement)
		{
			StepIn (guid, 0, statement);
		}

		public void StepIn (string guid, int offset = 0, string statement = null)
		{
			targetStoppedEvent.Reset ();
			Session.StepInstruction ();
			CheckPosition (guid, offset, statement);
		}

		public void StepOver (string guid, string statement)
		{
			StepOver (guid, 0, statement);
		}

		public void StepOver (string guid, int offset = 0, string statement = null)
		{
			targetStoppedEvent.Reset ();
			Session.NextInstruction ();
			CheckPosition (guid, offset, statement);
		}

		public void StepOut (string guid, string statement)
		{
			StepOut (guid, 0, statement);
		}

		public void StepOut (string guid, int offset = 0, string statement = null)
		{
			targetStoppedEvent.Reset ();
			Session.Finish ();
			CheckPosition (guid, offset, statement);
		}

		public void Continue (string guid, string statement)
		{
			Continue (guid, 0, statement);
		}

		public void Continue (string guid, int offset = 0, string statement = null)
		{
			targetStoppedEvent.Reset ();
			Session.Continue ();
			CheckPosition (guid, offset, statement);
		}

		public void StartTest (string methodName)
		{
			if (!targetStoppedEvent.WaitOne (3000)) {
				Assert.Fail ("StartTest failure: Target stop timeout");
			}
			Assert.AreEqual ('"' + methodName + '"', Eval ("NextMethodToCall = \"" + methodName + "\";").Value);
			targetStoppedEvent.Reset ();
			Session.Continue ();
		}

		public void SetNextStatement (string guid, int offset = 0, string statement = null)
		{
			int line, column;
			GetLineAndColumn (guid, offset, statement, out line, out column);
			Session.SetNextStatement (SourceFile.Name, line, column);
		}

		public void AddCatchpoint (string exceptionName, bool includeSubclasses)
		{
			Session.Breakpoints.Add (new Catchpoint (exceptionName, includeSubclasses));
		}
	}

	static class EvalHelper
	{
		public static bool AtLeast (this Version ver, int major, int minor) {
			if ((ver.Major > major) || ((ver.Major == major && ver.Minor >= minor)))
				return true;
			else
				return false;
		}

		public static ObjectValue Sync (this ObjectValue val)
		{
			if (!val.IsEvaluating)
				return val;

			object locker = new object ();
			EventHandler h = delegate {
				lock (locker) {
					Monitor.PulseAll (locker);
				}
			};

			val.ValueChanged += h;

			lock (locker) {
				while (val.IsEvaluating) {
					if (!Monitor.Wait (locker, 8000))
						throw new Exception ("Timeout while waiting for value evaluation");
				}
			}

			val.ValueChanged -= h;
			return val;
		}

		public static ObjectValue GetChildSync (this ObjectValue val, string name, EvaluationOptions ops)
		{
			var result = val.GetChild (name, ops);

			return result != null ? result.Sync () : null;
		}

		public static ObjectValue[] GetAllChildrenSync (this ObjectValue val)
		{
			var children = val.GetAllChildren ();
			foreach (var child in children) {
				child.Sync ();
			}
			return children;
		}
	}
}
