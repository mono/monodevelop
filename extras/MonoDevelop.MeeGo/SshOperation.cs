// 
// SshOperation.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using Tamir.SharpSsh;
using MonoDevelop.Core;
using System.Threading;

namespace MonoDevelop.MeeGo
{
	public abstract class SshOperation<T> : IAsyncOperation where T : SshBase
	{
		T ssh;
		ManualResetEvent wait = new ManualResetEvent (false);
		
		public SshOperation (T ssh)
		{
			this.ssh = ssh;
		}
		
		protected T Ssh { get { return ssh; } }
		
		protected abstract void RunOperations ();
		public abstract void Cancel ();
		
		public void Run ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					ssh.Connect ();
					RunOperations ();
					Success = true;
				} catch (Exception ex) {
					Success = false;
					LoggingService.LogError ("Error in ssh operation", ex);
				} finally {
					try {
						if (ssh.Connected)
							ssh.Close ();
					} catch (Exception ex) {
						LoggingService.LogError ("Error disconnecting from ssh", ex);
						Success = false;
					}
				}
				IsCompleted = true;
				wait.Set ();
				if (Completed != null)
					Completed (this);
			});
		}
		
		public void WaitForCompleted ()
		{
			WaitHandle.WaitAll (new WaitHandle [] { wait });
		}
		
		public event OperationHandler Completed;
		
		public bool IsCompleted { get; private set; }
		public bool Success { get; private set; }
		public bool SuccessWithWarnings { get; private set; }
	}
	
	class SshTransferOperation<T> : SshOperation<T> where T : SshTransferProtocolBase
	{
		Action<T> action;
		
		public SshTransferOperation (T ssh, Action<T> action) : base (ssh)
		{
			this.action = action;
		}
		
		protected override void RunOperations ()
		{
			action (Ssh);
		}
		
		public override void Cancel ()
		{
			Ssh.Cancel ();
		}
	}
}

