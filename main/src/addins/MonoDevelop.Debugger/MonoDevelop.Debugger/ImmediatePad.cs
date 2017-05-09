// 
// ImmediatePad.cs
//  
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2013 Xamarin Inc. (http://www.xamarin.com)
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
using System.Linq;
using System.Collections.Generic;

using Mono.Debugging.Client;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.Debugger
{
	public class ImmediatePad: PadContent
	{
		static readonly object mutex = new object();
		DebuggerConsoleView view;
		readonly Dictionary<DebuggerSession, List<uint>> timersList = new Dictionary<DebuggerSession, List<uint>> ();
		
		protected override void Initialize (IPadWindow container)
		{
			view = new DebuggerConsoleView ();
			view.ConsoleInput += OnViewConsoleInput;
			view.ShadowType = Gtk.ShadowType.None;
			view.ShowAll ();
			view.Editable = DebuggingService.IsPaused;
			DebuggingService.PausedEvent += DebuggerPaused;
			DebuggingService.ResumedEvent += DebuggerResumed;
			DebuggingService.StoppedEvent += DebuggerStopped;
		}

		void OnViewConsoleInput (object sender, ConsoleInputEventArgs e)
		{
			if (!DebuggingService.IsDebugging) {
				view.WriteOutput (GettextCatalog.GetString ("Debug session not started."));
				FinishPrinting ();
			} else if (DebuggingService.IsRunning || DebuggingService.CurrentFrame == null) {
				view.WriteOutput (GettextCatalog.GetString ("The expression can't be evaluated while the application is running."));
				FinishPrinting ();
			} else {
				var frame = DebuggingService.CurrentFrame;
				var ops = GetEvaluationOptions (false);
				var expression = e.Text;

				var vres = frame.ValidateExpression (expression, ops);
				if (!vres) {
					view.WriteOutput (vres.Message);
					FinishPrinting ();
					return;
				}

				var val = frame.GetExpressionValue (expression, ops);
				if (val.IsEvaluating) {
					WaitForCompleted (val, frame.DebuggerSession);
					return;
				}

				PrintValue (val);
			}
		}	

		static EvaluationOptions GetEvaluationOptions (bool membersPrint)
		{
			var ops = DebuggingService.GetUserOptions ().EvaluationOptions;
			if (!membersPrint) {
				ops.AllowMethodEvaluation = true;
				ops.AllowToStringCalls = true;
				ops.AllowTargetInvoke = true;
			}
			ops.EvaluationTimeout = 20000;
			ops.EllipsizeStrings = false;
			ops.MemberEvaluationTimeout = 20000;
			ops.GroupPrivateMembers = false;
			return ops;
		}

		static string GetErrorText (ObjectValue val)
		{
			if (val.IsNotSupported)
				return string.IsNullOrEmpty(val.Value) ? GettextCatalog.GetString ("Expression not supported.") : val.Value;

			if (val.IsError || val.IsUnknown)
				return string.IsNullOrEmpty(val.Value) ? GettextCatalog.GetString ("Evaluation failed.") : val.Value;

			return string.Empty;
		}

		void PrintValue (ObjectValue val) 
		{
			string result = val.Value;

			if (string.IsNullOrEmpty (result) || val.IsError || val.IsUnknown || val.IsNotSupported) {
				view.WriteOutput (GetErrorText (val));
				FinishPrinting ();
			} else {
				var ops = GetEvaluationOptions (true);
				var children = val.GetAllChildren (ops);
				var hasMore = false;

				view.WriteOutput (result);

				if (children.Length > 0 && string.Equals (children[0].Name, "[0..99]")) {
					// Big Arrays Hack
					children = children[0].GetAllChildren ();
					hasMore = true;
				}

				var evaluating = new Dictionary<ObjectValue, bool> ();
				foreach (var child in children) {
					if (child.IsEvaluating) {
						evaluating.Add (child, false);
					} else {
						PrintChildValue (child);
					}
				}

				if (evaluating.Count > 0) {
					foreach (var eval in evaluating)
						WaitChildForCompleted (eval.Key, evaluating, hasMore);
				} else {
					FinishPrinting (hasMore);
				}
			}
		}

		void PrintChildValue (ObjectValue val)
		{
			view.WriteOutput (Environment.NewLine);

			if (val.IsError || val.IsUnknown) {
				view.WriteOutput (string.Format ("\t{0}", GetErrorText (val)));
			} else if (!val.IsNotSupported) {
				view.WriteOutput (string.Format ("\t{0}: {1}", val.Name, val.Value));
			}
		}

		void PrintChildValueAtMark (ObjectValue val, Gtk.TextMark mark) 
		{
			string prefix = "\t" + val.Name + ": ";
			string result = val.Value; 

			if (string.IsNullOrEmpty (result) || val.IsError || val.IsUnknown || val.IsNotSupported) {
				SetLineText (prefix + GetErrorText (val), mark);
			} else {
				SetLineText (prefix + result, mark);
			}
		}

		void FinishPrinting (bool hasMore = false)
		{
			if (hasMore)
				view.WriteOutput ("\n\t" + GettextCatalog.GetString ("< More... (The first {0} items were displayed.) >", 100));

			view.Prompt (true);
		}

		Gtk.TextIter DeleteLineAtMark (Gtk.TextMark mark)
		{
			var start = view.Buffer.GetIterAtMark (mark);
			var end = view.Buffer.GetIterAtMark (mark);
			end.ForwardLine ();

			view.Buffer.Delete (ref start, ref end);

			return start;
		}

		void SetLineText (string text, Gtk.TextMark mark)
		{
			var start = DeleteLineAtMark (mark);

			view.Buffer.Insert (ref start, text + "\n");
		}

		void WaitForCompleted (ObjectValue val, DebuggerSession session)
		{
			var mark = view.Buffer.CreateMark (null, view.InputLineEnd, true);
			var iteration = 0;
			uint timerId = 0;

			timerId = GLib.Timeout.Add (100, () => {
				List<uint> list;
				if (!timersList.TryGetValue (session, out list) || !list.Contains (timerId)) {
					SetLineText (GettextCatalog.GetString ("Debugging stopped"), mark);
					FinishPrinting ();
					return false;
				}
				if (!val.IsEvaluating) {
					if (iteration >= 5)
						DeleteLineAtMark (mark);
					list.Remove (timerId);

					PrintValue (val);

					return false;
				}

				if (++iteration == 5) {
					SetLineText (GettextCatalog.GetString ("Evaluating"), mark);
				} else if (iteration > 5 && (iteration - 5) % 10 == 0) {
					string points = string.Join ("", Enumerable.Repeat (".", iteration / 10));
					SetLineText (GettextCatalog.GetString ("Evaluating") + " " + points, mark);
				}

				return true;
			});
			List<uint> tList;
			if (!timersList.TryGetValue (session, out tList))
				timersList [session] = tList = new List<uint> ();
			tList.Add (timerId);
		}

		void WaitChildForCompleted (ObjectValue val, IDictionary<ObjectValue, bool> evaluatingList, bool hasMore)
		{
			view.WriteOutput ("\n ");

			var mark = view.Buffer.CreateMark (null, view.InputLineEnd, true);
			var iteration = 0;

			GLib.Timeout.Add (100, () => {
				if (!val.IsEvaluating) {
					PrintChildValueAtMark (val, mark);

					lock (mutex) {
						// Maybe We don't need this lock because children evaluation is done synchronously
						evaluatingList[val] = true;
						if (evaluatingList.All (x => x.Value))
							FinishPrinting (hasMore);
					}

					return false;
				}

				string prefix = "\t" + val.Name + ": ";

				if (++iteration == 5) {
					SetLineText (prefix + GettextCatalog.GetString ("Evaluating"), mark);
				} else if (iteration > 5 && (iteration - 5) % 10 == 0) {
					string points = string.Join ("", Enumerable.Repeat (".", iteration / 10));
					SetLineText (prefix + GettextCatalog.GetString ("Evaluating") + " " + points, mark);
				}

				return true;
			});
		}	

		public override Control Control {
			get {
				return view;
			}
		}

		public override void Dispose ()
		{
			DebuggingService.PausedEvent -= DebuggerPaused;
			DebuggingService.ResumedEvent -= DebuggerResumed;
			DebuggingService.StoppedEvent -= DebuggerStopped;
			base.Dispose ();
		}

		void DebuggerResumed (object sender, EventArgs e)
		{
			view.Editable = DebuggingService.IsPaused;
		}

		void DebuggerPaused (object sender, EventArgs e)
		{
			view.Editable = DebuggingService.IsPaused;
		}

		void DebuggerStopped (object sender, EventArgs e)
		{
			timersList.Remove ((DebuggerSession)sender);
			view.Editable = DebuggingService.IsPaused;
		}
	}
}
