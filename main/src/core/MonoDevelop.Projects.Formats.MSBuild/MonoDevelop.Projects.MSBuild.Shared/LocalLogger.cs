// 
// LocalLogger.cs
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
using System.Collections.Generic;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace MonoDevelop.Projects.MSBuild
{
	class TargetLogger: Logger
	{
		IEventSource eventSource;
		MSBuildEvent eventFilter;
		readonly Action<LogEvent> logger;

		public MSBuildEvent EventFilter {
			get {
				return eventFilter; 
			}
			set {
				if (eventSource != null)
					UnsubscribeAll ();
				
				eventFilter = value;

				if (eventSource != null)
					SubscribeEvents ();
			}
		}

		public TargetLogger (MSBuildEvent eventFilter, Action<LogEvent> logger)
		{
			this.eventFilter = eventFilter;
			this.logger = logger;
		}

		public override void Initialize (IEventSource eventSource)
		{
			this.eventSource = eventSource;
			SubscribeEvents ();
		}

		void SubscribeEvents ()
		{
			if ((eventFilter & MSBuildEvent.BuildStarted) != 0)
				eventSource.BuildStarted += EventSource_BuildStarted;
			
			if ((eventFilter & MSBuildEvent.BuildFinished) != 0)
				eventSource.BuildFinished += EventSource_BuildFinished;
			
			if ((eventFilter & MSBuildEvent.CustomEventRaised) != 0)
				eventSource.CustomEventRaised += EventSource_CustomEventRaised;
			
			if ((eventFilter & MSBuildEvent.ErrorRaised) != 0)
				eventSource.ErrorRaised += EventSource_ErrorRaised;

			if ((eventFilter & MSBuildEvent.MessageRaised) != 0)
				eventSource.MessageRaised += EventSource_MessageRaised;
			
			if ((eventFilter & MSBuildEvent.ProjectStarted) != 0)
				eventSource.ProjectStarted += EventSource_ProjectStarted;

			if ((eventFilter & MSBuildEvent.ProjectFinished) != 0)
				eventSource.ProjectFinished += EventSource_ProjectFinished;

			if ((eventFilter & MSBuildEvent.TargetStarted) != 0)
				eventSource.TargetStarted += EventSource_TargetStarted;

			if ((eventFilter & MSBuildEvent.TargetFinished) != 0)
				eventSource.TargetFinished += EventSource_TargetFinished;

			if ((eventFilter & MSBuildEvent.TaskStarted) != 0)
				eventSource.TaskStarted += EventSource_TaskStarted;

			if ((eventFilter & MSBuildEvent.TargetFinished) != 0)
				eventSource.TaskFinished += EventSource_TaskFinished;

			if ((eventFilter & MSBuildEvent.WarningRaised) != 0)
				eventSource.WarningRaised += EventSource_WarningRaised;
		}

		void EventSource_BuildStarted (object sender, BuildStartedEventArgs e)
		{
			Log (MSBuildEvent.BuildStarted, e.Message);
		}

		void EventSource_BuildFinished (object sender, BuildFinishedEventArgs e)
		{
			Log (MSBuildEvent.BuildFinished, e.Message);
		}

		void EventSource_CustomEventRaised (object sender, CustomBuildEventArgs e)
		{
			Log (MSBuildEvent.CustomEventRaised, e.Message);
		}

		void EventSource_ErrorRaised (object sender, BuildErrorEventArgs e)
		{
			Log (MSBuildEvent.ErrorRaised, e.Message);
		}

		void EventSource_MessageRaised (object sender, BuildMessageEventArgs e)
		{
			Log (MSBuildEvent.MessageRaised, e.Message);
		}

		void EventSource_ProjectStarted (object sender, ProjectStartedEventArgs e)
		{
			Log (MSBuildEvent.ProjectStarted, e.Message);
		}

		void EventSource_ProjectFinished (object sender, ProjectFinishedEventArgs e)
		{
			Log (MSBuildEvent.ProjectFinished, e.Message);
		}

		void EventSource_TargetStarted (object sender, TargetStartedEventArgs e)
		{
			Log (MSBuildEvent.TargetStarted, e.Message);
		}

		void EventSource_TargetFinished (object sender, TargetFinishedEventArgs e)
		{
			Log (MSBuildEvent.TargetFinished, e.Message);
		}

		void EventSource_TaskStarted (object sender, TaskStartedEventArgs e)
		{
			Log (MSBuildEvent.TaskStarted, e.Message);
		}

		void EventSource_TaskFinished (object sender, TaskFinishedEventArgs e)
		{
			Log (MSBuildEvent.TaskFinished, e.Message);
		}

		void EventSource_WarningRaised (object sender, BuildWarningEventArgs e)
		{
			Log (MSBuildEvent.WarningRaised, e.Message);
		}

		void Log (MSBuildEvent ev, string message)
		{
			logger (new LogEvent { Event = ev, Message = message });
		}

		public override void Shutdown ()
		{
			UnsubscribeAll ();
		}

		void UnsubscribeAll ()
		{
			eventSource.BuildStarted -= EventSource_BuildStarted;
			eventSource.BuildFinished -= EventSource_BuildFinished;
			eventSource.CustomEventRaised -= EventSource_CustomEventRaised;
			eventSource.ErrorRaised -= EventSource_ErrorRaised;
			eventSource.MessageRaised -= EventSource_MessageRaised;
			eventSource.ProjectStarted -= EventSource_ProjectStarted;
			eventSource.ProjectFinished -= EventSource_ProjectFinished;
			eventSource.TargetStarted -= EventSource_TargetStarted;
			eventSource.TargetFinished -= EventSource_TargetFinished;
			eventSource.TaskStarted -= EventSource_TaskStarted;
			eventSource.TaskFinished -= EventSource_TaskFinished;
			eventSource.WarningRaised -= EventSource_WarningRaised;
		}
	}
}
