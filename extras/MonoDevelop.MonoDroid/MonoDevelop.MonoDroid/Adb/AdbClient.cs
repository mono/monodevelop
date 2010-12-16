// 
// AdbClient.cs
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
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Globalization;
using System.Threading;

namespace MonoDevelop.MonoDroid
{
	class AdbClient : IDisposable
	{
		const int ADB_PORT = 5037;
		
		bool disposed;
		NetworkStream stream;
		TcpClient client;
		
		public IAsyncResult BeginConnect (AsyncCallback callback, object state)
		{
			CreateClient ();
			return client.BeginConnect (IPAddress.Loopback, ADB_PORT, callback, state);
		}
		
		public void EndConnect (IAsyncResult asyncResult)
		{
			client.EndConnect (asyncResult);
			stream = client.GetStream ();
		}
				
		public void Connect ()
		{
			CreateClient ();
			client.Connect (IPAddress.Loopback, ADB_PORT);
			stream = client.GetStream ();
		}
		
		void CreateClient ()
		{
			CheckDisposed ();
			if (client != null)
				throw new InvalidOperationException ("Already connected");
			client = new TcpClient ();
		}
		
		void CheckDisposed ()
		{
			if (disposed)
				throw new ObjectDisposedException ("AdbClient");
		}
		
		void CheckConnected ()
		{
			CheckDisposed ();
			if (stream == null)
				throw new InvalidOperationException ("Not connected");
		}
		
		public IAsyncResult BeginWriteCommand (string command, AsyncCallback callback, object state)
		{
			CheckConnected ();
			var buf = GetCommandBuffer (command);
			return stream.BeginWrite (buf, 0, buf.Length, callback, state);
		}
		
		public void EndWriteCommand (IAsyncResult asyncResult)
		{
			stream.EndWrite (asyncResult);
		}
		
		public void WriteCommand (string command)
		{
			CheckConnected ();
			var buf = GetCommandBuffer (command);
			stream.Write (buf, 0, buf.Length);
		}
		
		static byte[] GetCommandBuffer (string command)
		{
			if (string.IsNullOrEmpty (command))
				throw new ArgumentException ("command");
			
			var bytes = Encoding.ASCII.GetBytes (command);
			var len = Encoding.ASCII.GetBytes (string.Format ("{0:x4}", bytes.Length));
			var all = new byte[bytes.Length + 4];
			len.CopyTo (all, 0);
			bytes.CopyTo (all, 4);
			return all;
		}
		
		public IAsyncResult BeginReadStatus (AsyncCallback callback, object state)
		{
			CheckConnected ();
			var buf = new byte[4];
			var wrapper = new WrapperResult<byte[]> (callback, buf);
			wrapper.InnerResult = stream.BeginRead (buf, 0, 4, wrapper.Callback, wrapper);
			return wrapper;
		}
		
		public bool EndGetStatus (IAsyncResult ar)
		{
			var war = (WrapperResult<byte[]>) ar.AsyncState;
			return InterpretStatus (stream.EndRead (war.InnerResult), war.WrapperState);
		}
		
		bool InterpretStatus (int len, byte[] buf)
		{
			if (len != 4)
				throw new Exception ("Did not get status");
			var status = Encoding.ASCII.GetString (buf);
			if (status == "FAIL")
				return false;
			if (status == "OKAY")
				return true;
			throw new Exception ("Unknown ADB status: " + status);
		}
		
		public bool ReadStatus ()
		{
			CheckConnected ();
			var buf = new byte[4];
			return InterpretStatus (stream.Read (buf, 0, 4), buf);
		}
		
		public IAsyncResult BeginReadResponseString (AsyncCallback callback, object state)
		{
			CheckConnected ();
			var ar = new ResponseAsyncResult (callback, state);
			ar.Buffer = new byte [4];
			stream.BeginRead (ar.Buffer, 0, 4, ResponseLengthCallback, ar);
			return ar;
		}
		
		void ResponseLengthCallback (IAsyncResult ar)
		{
			var r = (ResponseAsyncResult) ar.AsyncState;
			try {
				var respLen = stream.EndRead (ar);
				if (respLen != 4)
					throw new Exception ("Unexpected response length " + respLen);
				
				var len = int.Parse (Encoding.ASCII.GetString (r.Buffer), NumberStyles.HexNumber);
				if (len == 0) {
					r.Buffer = null;
					r.FinalCallback (r);
					return;
				}
				r.Buffer = new byte [len];
				stream.BeginRead (r.Buffer, 0, len, r.FinalCallback, null);
			} catch (Exception ex) {
				r.SetError (ex);
				r.FinalCallback (r);
			}
		}
		
		public string EndReadResponseString (IAsyncResult ar)
		{
			var r = (ResponseAsyncResult) ar;
			if (r.Error != null)
				throw r.Error;
			if (r.Buffer == null)
				return "";
			return Encoding.ASCII.GetString (r.Buffer);
		}
		
		public string ReadResponseString ()
		{
			CheckConnected ();
			var buf = new byte[4];
			var respLen = stream.Read (buf, 0, 4);
			if (respLen != 4)
				throw new Exception ("Unexpected response length " + respLen);
			var len = int.Parse (Encoding.ASCII.GetString (buf), NumberStyles.HexNumber);
			buf = new byte[len];
			var read = stream.Read (buf, 0, len);
			if (read != len)
				throw new Exception ("Response too short");
			return Encoding.ASCII.GetString (buf);
		}
		
		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			
			if (stream != null) {
				stream.Dispose ();
				stream = null;
			}
			
			if (client != null) {
				((IDisposable)client).Dispose ();
				client = null;
			}
		}
		
		class WrapperResult<T> : IAsyncResult
		{
			public WrapperResult (AsyncCallback callback, T wrapperState)
			{
				this.Callback = callback;
				this.WrapperState = wrapperState;
			}
			
			public void WrapperCallback ()
			{
				Callback (InnerResult);
			}
			
			public AsyncCallback Callback { get; private set; }
			public IAsyncResult InnerResult { get; set; }
			public T WrapperState { get; private set; }
			
			public object AsyncState {
				get { return InnerResult.AsyncState; }
			}

			public WaitHandle AsyncWaitHandle {
				get { return InnerResult.AsyncWaitHandle; }
			}

			public bool CompletedSynchronously {
				get { return InnerResult.CompletedSynchronously; }
			}

			public bool IsCompleted {
				get { return InnerResult.IsCompleted; }
			}
		}
		
		private class ResponseAsyncResult : IAsyncResult
		{
			public ResponseAsyncResult (AsyncCallback callback, object state)
			{
				this.State = state;
				this.Callback = callback;
			}
			
			public byte[] Buffer { get; set; }
			
			public void FinalCallback (IAsyncResult ar)
			{
				MarkCompleted ();
				Callback (this);
			}
			
			public void SetError (Exception error)
			{
				Error = error;
				MarkCompleted ();
			}
			
			void MarkCompleted ()
			{
				lock (this) {
					IsCompleted = true;
					if (waitHandle != null)
						waitHandle.Set ();
				}
			}

			public Exception Error { get; private set; }
			public AsyncCallback Callback { get; private set; }
			public object State { get; private set; }			
			public bool IsCompleted { get; private set; }	
			
			object IAsyncResult.AsyncState { get { return State; } }	
			
			ManualResetEvent waitHandle;
			
			WaitHandle IAsyncResult.AsyncWaitHandle {
				get {
					lock (this) {
						if (waitHandle == null)
							waitHandle = new ManualResetEvent (IsCompleted);
					}
					return waitHandle;
				}
			}

			bool IAsyncResult.CompletedSynchronously {
				get { return false; }
			}
		}
	}
}

