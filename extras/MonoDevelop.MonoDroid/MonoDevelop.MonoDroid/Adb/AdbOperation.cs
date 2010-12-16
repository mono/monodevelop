// 
// AdbOperation.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core;
using System.Threading;

namespace MonoDevelop.MonoDroid
{
	public abstract class AdbOperation : IAsyncOperation, IDisposable
	{
		AdbClient client;
		object lockObj = new object ();
		bool cancel = false;
		ManualResetEvent mre;
		string command;
		
		public AdbOperation (string command)
		{
			this.command = command;
			BeginConnect ();
		}
		
		protected abstract void OnGotResponse (string response, ref bool readAgain);
		
		void BeginConnect ()
		{
			client = new AdbClient ();
			client.BeginConnect (OnConnected, null);
			
		}
		
		void OnConnected (IAsyncResult ar)
		{
			if (cancel) {
				MarkCompleted ();
				return;
			}
			try {
				client.EndConnect (ar);
				client.BeginWriteCommand (command, OnWroteCommand, null);
			} catch (Exception ex) {
				SetError (ex);
			}
		}
		
		void OnWroteCommand (IAsyncResult ar)
		{
			if (cancel) {
				MarkCompleted ();
				return;
			}
			try {
				client.EndWriteCommand (ar);
				client.BeginReadStatus (OnGotStatus, null);
			} catch (Exception ex) {
				SetError (ex);
			}
		}
		
		void OnGotStatus (IAsyncResult ar)
		{
			if (cancel) {
				MarkCompleted ();
				return;
			}
			try {
				if (!client.EndGetStatus (ar)) {
					client.BeginReadResponseString (OnGotErrorResponse, null);
				} else {
					client.BeginReadResponseString (OnGotResponse, null);
				}
			} catch (Exception ex) {
				SetError (ex);
			}
		}
		
		void OnGotErrorResponse (IAsyncResult ar)
		{
			if (cancel) {
				MarkCompleted ();
				return;
			}	
			try {
				var error = client.EndReadResponseString (ar);
				SetError (new Exception (error));
			} catch (Exception ex) {
				SetError (ex);
			}
		}
		
		void OnGotResponse (IAsyncResult ar)
		{
			if (cancel) {
				MarkCompleted ();
				return;
			}
			try {
				var response = client.EndReadResponseString (ar);
				bool readAgain = false;
				OnGotResponse (response, ref readAgain);
				if (readAgain) {
					client.BeginReadResponseString (OnGotResponse, null);
				} else {
					Success = true;
					MarkCompleted ();
				}
			} catch (Exception ex) {
				SetError (ex);
			}
		}
		
		void SetError (Exception ex)
		{
			Error = ex;
			MarkCompleted ();
		}
		
		void MarkCompleted ()
		{
			OperationHandler completedEv;
			lock (lockObj) {
				completedEv = completed;
				IsCompleted = true;
				if (mre != null)
					mre.Set ();
			}
			try {
				if (completedEv != null)
					completedEv (this);
				Dispose ();
			} catch (Exception ex) {
				//FIXME: better way to deal with this? letting it throw from an async callback is not an option
				System.Console.WriteLine ("Unhandled error completing AdbOperation: " + ex.ToString ());
			}
		}
		
		OperationHandler completed;
		
		public event OperationHandler Completed {
			add {
				lock (lockObj) {
					completed += value;
				}
			}
			remove {
				lock (lockObj) {
					completed -= value;
				}
			}
		}

		public void Cancel ()
		{
			//FIXME: can we cancel pending requests aside from disposing the client?
			cancel = true;
			Dispose ();
		}

		public void WaitForCompleted ()
		{
			lock (lockObj) {
				if (IsCompleted)
					return;
				if (mre == null)
					mre = new ManualResetEvent (false);
			}
			mre.WaitOne ();
		}

		public bool IsCompleted { get; private set; }

		public bool Success { get; private set; }
		public bool SuccessWithWarnings { get { return Success; } }
		
		public Exception Error { get; private set; }
		
		public void Dispose ()
		{
			var c = client;
			if (c != null) {
				c.Dispose ();
				client = null;
			}
		}
	}
}