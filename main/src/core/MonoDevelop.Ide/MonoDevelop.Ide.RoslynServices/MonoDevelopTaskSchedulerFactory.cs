//
// MonoDevelopTaskSchedulerFactory.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Roslyn.Utilities;
using System.Threading;
using Microsoft.VisualStudio.Threading;

namespace MonoDevelop.Ide.RoslynServices
{
	[ExportWorkspaceService(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Host), Shared]
	class MonoDevelopTaskSchedulerFactory : EditorTaskSchedulerFactory
	{
		readonly IThreadingContext _threadingContext;

		[ImportingConstructor]
		[Obsolete (MefConstruction.ImportingConstructorMessage, error: true)]
		public MonoDevelopTaskSchedulerFactory (IThreadingContext threadingContext, IAsynchronousOperationListenerProvider listenerProvider) : base (listenerProvider)
		{
			_threadingContext = threadingContext;
		}

		public override IWorkspaceTaskScheduler CreateEventingTaskQueue ()
		{
			return new WorkspaceTaskQueue(this, new JoinableTaskFactoryTaskScheduler(_threadingContext.JoinableTaskFactory));
		}

		class JoinableTaskFactoryTaskScheduler : TaskScheduler
		{
			readonly JoinableTaskFactory _joinableTaskFactory;

			public JoinableTaskFactoryTaskScheduler (JoinableTaskFactory joinableTaskFactory)
			{
				_joinableTaskFactory = joinableTaskFactory;
			}

			public override int MaximumConcurrencyLevel => 1;

			protected override IEnumerable<Task> GetScheduledTasks () => null;

			protected override void QueueTask (Task task)
			{
				_joinableTaskFactory.RunAsync (async () => {
					await _joinableTaskFactory.SwitchToMainThreadAsync (alwaysYield: true);
					TryExecuteTask (task);
				});
			}

			protected override bool TryExecuteTaskInline (Task task, bool taskWasPreviouslyQueued)
			{
				if (_joinableTaskFactory.Context.IsOnMainThread) {
					return TryExecuteTask (task);
				}

				return false;
			}
		}
	}
}
