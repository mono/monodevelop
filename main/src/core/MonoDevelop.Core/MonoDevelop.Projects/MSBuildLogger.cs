//
// MSBuildLogger.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Projects.MSBuild;
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	public class MSBuildLogger
	{
		public MSBuildLogger ()
		{
		}

		internal void NotifyEvent (LogEvent e)
		{
			OnEventRaised (new MSBuildLoggerEventArgs { Event = e.Event, Message = e.Message });
		}

		internal void NotifyEvent (MSBuildLoggerEventArgs args)
		{
			OnEventRaised (args);
		}

		public MSBuildEvent EnabledEvents { get; set; }

		protected virtual void OnEventRaised (MSBuildLoggerEventArgs args)
		{
			EventRaised?.Invoke (this, args);
		}

		public event EventHandler<MSBuildLoggerEventArgs> EventRaised;
	}

	public class MSBuildLoggerEventArgs: EventArgs
	{
		public Project Project { get; set; }
		public MSBuildEvent Event { get; set; }
		public string Message { get; set; }
	}

	class ProxyLogger: MSBuildLogger
	{
		IEnumerable<MSBuildLogger> loggers;
		Project project;

		public ProxyLogger (Project project, IEnumerable<MSBuildLogger> loggers)
		{
			this.project = project;
			this.loggers = loggers;
			foreach (var lo in loggers)
				EnabledEvents |= lo.EnabledEvents;
		}

		protected override void OnEventRaised (MSBuildLoggerEventArgs args)
		{
			args.Project = project;
			foreach (var lo in loggers)
				if ((lo.EnabledEvents & args.Event) != 0)
					lo.NotifyEvent (args);
		}
	}
}
