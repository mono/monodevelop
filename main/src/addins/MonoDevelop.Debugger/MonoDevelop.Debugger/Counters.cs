//
// Counters.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Debugger
{
	static class Counters
	{
		public static Counter DebugSession = InstrumentationService.CreateCounter ("Debug Session", "Debugger", id: "Debugger.DebugSession");
		public static Counter EvaluationStats = InstrumentationService.CreateCounter ("Evaluation Statistics", "Debugger", id: "Debugger.EvaluationStatistics");
		public static Counter StepInStats = InstrumentationService.CreateCounter ("Step In Statistics", "Debugger", id: "Debugger.StepInStatistics");
		public static Counter StepOutStats = InstrumentationService.CreateCounter ("Step Out Statistics", "Debugger", id: "Debugger.StepOutStatistics");
		public static Counter StepOverStats = InstrumentationService.CreateCounter ("Step Over Statistics", "Debugger", id: "Debugger.StepOverStatistics");
		public static Counter StepInstructionStats = InstrumentationService.CreateCounter ("Step Instruction Statistics", "Debugger", id: "Debugger.StepInstructionStatistics");
		public static Counter NextInstructionStats = InstrumentationService.CreateCounter ("Next Instruction Statistics", "Debugger", id: "Debugger.NextInstructionStatistics");
		public static TimerCounter<DebuggerStartMetadata> DebuggerStart = InstrumentationService.CreateTimerCounter<DebuggerStartMetadata> ("Debugger Start", "Debugger", id: "Debugger.Start");
		public static TimerCounter<DebuggerActionMetadata> DebuggerAction = InstrumentationService.CreateTimerCounter<DebuggerActionMetadata> ("Debugger Action", "Debugger", id: "Debugger.Action");
		public static Counter LocalVariableStats = InstrumentationService.CreateCounter ("Local Variable Statistics", "Debugger", id: "Debugger.LocalVariableStatistics");
		public static Counter WatchExpressionStats = InstrumentationService.CreateCounter ("Watch Expression Statistics", "Debugger", id: "Debugger.WatchExpressionStatistics");
		public static Counter StackTraceStats = InstrumentationService.CreateCounter ("Stack Trace Statistics", "Debugger", id: "Debugger.StackTraceStatistics");
		public static Counter TooltipStats = InstrumentationService.CreateCounter ("Tooltip Statistics", "Debugger", id: "Debugger.TooltipStatistics");
		public static Counter DebuggerBusy = InstrumentationService.CreateCounter ("Debugger Busy", "Debugger", id: "Debugger.Busy");
		public static Counter LocalsPadFrameChanged = InstrumentationService.CreateCounter ("The StackFrame changed in the Locals Pad", "Debugger", id: "Debugger.LocalsPadFrameChanged");
		public static Counter AddedWatchFromLocals = InstrumentationService.CreateCounter ("Added a Watch Expression from the Locals Pad", "Debugger", id: "Debugger.AddedWatchFromLocals");
		public static Counter ManuallyAddedWatch = InstrumentationService.CreateCounter ("User Manually Added Watch Expression", "Debugger", id: "Debugger.ManuallyAddedWatch");
		public static Counter PinnedWatch = InstrumentationService.CreateCounter ("Pinned Watch Expression", "Debugger", id: "Debugger.PinnedWatch");
		public static Counter EditedValue = InstrumentationService.CreateCounter ("User Edited Variable Value", "Debugger", id: "Debugger.EditedValue");
		public static Counter ExpandedNode = InstrumentationService.CreateCounter ("User Expanded ObjectValue Node", "Debugger", id: "Debugger.ExpandedObjectValueNode");
		public static Counter OpenedPreviewer = InstrumentationService.CreateCounter ("User opened the value in the previewer", "Debugger", id: "Debugger.OpenedPreviewer");
		public static Counter OpenedVisualizer = InstrumentationService.CreateCounter ("User opened the value in a Visualizer", "Debugger", id: "Debugger.OpenedVisualizer");
	}

	class DebuggerStartMetadata : CounterMetadata
	{
		public DebuggerStartMetadata ()
		{
		}

		public string Name {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}
	}

	class DebuggerActionMetadata : CounterMetadata
	{
		public enum ActionType {
			Unknown,
			StepOver,
			StepInto,
			StepOut
		};

		public DebuggerActionMetadata ()
		{
		}

		public ActionType Type {
			get {
				var result = GetProperty<string> ();
				if (Enum.TryParse<ActionType> (result, out var eResult)) {
					return eResult;
				}

				return ActionType.Unknown;
			}

			set => SetProperty (value.ToString ());
		}
	}
}
