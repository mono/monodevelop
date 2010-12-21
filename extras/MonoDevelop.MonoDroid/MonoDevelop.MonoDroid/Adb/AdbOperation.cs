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
using System.IO;

namespace MonoDevelop.MonoDroid
{
	public abstract class AdbOperation : IAsyncOperation, IDisposable
	{
		AdbClient client;
		object lockObj = new object ();
		bool cancel = false;
		ManualResetEvent mre;
		bool disposed;
		
		public AdbOperation ()
		{
			BeginConnect ();
		}
		
		void BeginConnect ()
		{
			client = new AdbClient ();
			client.BeginConnect (EndConnect, null);
		}
		
		void EndConnect (IAsyncResult ar)
		{
			if (cancel) {
				SetCompleted (false);
				return;
			}
			try {
				client.EndConnect (ar);
				OnConnected ();
			} catch (Exception ex) {
				if (client != null)
					SetError (ex);
			}
		}
		
		protected abstract void OnConnected ();
		
		protected void WriteCommand (string command, Action callback)
		{
			client.BeginWriteCommand (command, OnWroteCommand, callback);
		}
		
		void OnWroteCommand (IAsyncResult ar)
		{
			if (cancel) {
				SetCompleted (false);
				return;
			}
			try {
				client.EndWriteCommand (ar);
				((Action)ar.AsyncState) ();
			} catch (Exception ex) {
				if (client != null)
					SetError (ex);
			}
		}
		
		protected void GetStatus (Action callback)
		{
			client.BeginReadStatus (OnGotStatus, callback);
		}
		
		void OnGotStatus (IAsyncResult ar)
		{
			if (cancel) {
				SetCompleted (false);
				return;
			}
			try {
				if (!client.EndGetStatus (ar)) {
					client.BeginReadResponseWithLength (OnGotErrorResponse, null);
				} else {
					((Action)ar.AsyncState) ();
				}
			} catch (Exception ex) {
				if (client != null)
					SetError (ex);
			}
		}
		
		void OnGotErrorResponse (IAsyncResult ar)
		{
			if (cancel) {
				SetCompleted (false);
				return;
			}	
			try {
				var error = client.EndReadResponseWithLength (ar);
				SetError (new Exception (error));
			} catch (Exception ex) {
				if (client != null)
					SetError (ex);
			}
		}
		
		protected void ReadResponseWithLength (Action<string> callback)
		{
			client.BeginReadResponseWithLength (OnGotResponseWithLength, callback);
		}
		
		void OnGotResponseWithLength (IAsyncResult ar)
		{
			if (cancel) {
				SetCompleted (false);
				return;
			}
			try {
				var response = client.EndReadResponseWithLength (ar);
				((Action<string>)ar.AsyncState) (response);
			} catch (Exception ex) {
				if (client != null)
					SetError (ex);
			}
		}
		
		protected void ReadResponse (TextWriter writer, Action<TextWriter> callback)
		{
			client.BeginReadResponse (writer, OnGotResponse, callback);
		}
		
		void OnGotResponse (IAsyncResult ar)
		{
			if (cancel) {
				SetCompleted (false);
				return;
			}
			try {
				var writer = client.EndReadResponse (ar);
				((Action<TextWriter>)ar.AsyncState) (writer);
			} catch (Exception ex) {
				if (client != null)
					SetError (ex);
			}
		}
		
		protected void SetError (Exception ex)
		{
			Error = ex;
			SetCompleted (false);
		}
		
		protected void SetCompleted (bool success)
		{
			OperationHandler completedEv;
			lock (lockObj) {
				if (IsCompleted)
					throw new InvalidOperationException ("Already completed");
				completedEv = completed;
				if (success)
					this.Success = true;
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
				LoggingService.LogError ("Unhandled error completing AdbOperation", ex);
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
			lock (lockObj) {
				if (disposed)
					return;
				disposed = true;
			}
			GC.SuppressFinalize (this);
			Dispose (true);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				client.Dispose ();
				client = null;
			}
		}
		
		~AdbOperation ()
		{
			Dispose (false);
		}
	}
	
	public abstract class AdbTransportOperation : AdbOperation
	{
		AndroidDevice device;
		
		public AdbTransportOperation (AndroidDevice device)
		{
			if (device == null)
				throw new ArgumentNullException ("device");
			this.device = device;
		}
		
		protected sealed override void OnConnected ()
		{
			WriteCommand ("host:transport:" + device.ID, () => GetStatus (() => OnGotTransport ()));
		}
		
		protected abstract void OnGotTransport ();
	}
}