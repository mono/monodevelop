using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Debugger.Evaluation;

namespace MonoDevelop.Debugger.Win32
{
	class CorMethodCall: AsyncOperation
	{
		public delegate void CallCallback ( );

		public CallCallback OnInvoke;
		public CallCallback OnAbort;

		public ManualResetEvent DoneEvent = new ManualResetEvent (false);

		public override void Invoke ( )
		{
			OnInvoke ();
		}

		public override void Abort ( )
		{
			OnAbort ();
		}

		public override bool WaitForCompleted (int timeout)
		{
			return DoneEvent.WaitOne (timeout, false);
		}
	}
}
