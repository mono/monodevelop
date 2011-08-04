// 
// IPhoneSoftDebuggerEngine.cs
//  
// Author:
//       Rolf Bjarne Kvinge (rolf@xamarin.com)
// 
// Copyright (c) 2011 Xamarin Inc. (http://www.xamarin.com)
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
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using MonoDevelop.IPhone;

namespace MonoDevelop.Debugger.Soft.IPhone
{
	internal class UsbSocketProxy : IDisposable
	{
		IPhoneUsbConnection UsbSocket;
		Socket md_socket;
		Socket sock;
		Thread receiver_thread;
		// sock can't be accessed until md_connected is set, after that accesses must be protected by locking sync_obj
		ManualResetEvent md_connected = new ManualResetEvent (false);
		object sync_obj = new object (); // used to sync accesses to sock
		int handle = -1;
		
		public int DevicePort { get; private set; }
		public IPEndPoint EndPoint { get; private set; }
		public bool IsConnected { get { return handle != -1; } }
		
		public UsbSocketProxy (IPhoneUsbConnection usbSocket, int devicePort)
		{
			this.DevicePort = devicePort;
			this.UsbSocket = usbSocket;
			
			md_socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			md_socket.Bind (new IPEndPoint (IPAddress.Loopback, 0));
			EndPoint = (IPEndPoint) md_socket.LocalEndPoint;
		}
	
		public bool ConnectToApp (Device device, int muxConn)
		{
			uint res;
			
			if (IsConnected)
				return true;
			
			res = Device.USBMuxConnectByPort (muxConn, IPAddress.HostToNetworkOrder ((short) DevicePort), out handle);
			
			if (res != 0)
				handle = -1;
			
			if (IsConnected) {
				// Start listening for connections from MD
				md_socket.Listen (16);
				md_socket.BeginAccept (ConnectedToMD, null);
				// Start receiving data from the device
				receiver_thread = new Thread (ReceiveFromDevice);
				receiver_thread.Start ();
			}
			
			return IsConnected;
		}
		
		private void ReceiveFromDevice (object dummy)
		{
			byte[] buffer = new byte [1024];
			int res;
				
			try {
				while (true) {
					res = recv (handle, buffer, new IntPtr (buffer.Length), 0);
					if (res <= 0) {
						Dispose ();
						return;
					}
					// wait until we've connected to MD
					md_connected.WaitOne ();
					
					lock (sync_obj) {
						if (sock == null)
							return;
						res = sock.Send (buffer, res, SocketFlags.None);
					}
				}
			} catch (Exception ex) {
				Console.WriteLine ("Exception in receiver thread: {0}", ex);
				Dispose ();
			}
		}
		
		private void ConnectedToMD (IAsyncResult ar)
		{
			try {
				sock = md_socket.EndAccept (ar);
				md_socket.Dispose ();
				md_socket = null;
				
				md_connected.Set ();
				
				ReceiveFromMD ();
			} catch (Exception ex) {
				Console.WriteLine ("UsbSocketProxy.ConnectedToMD (): Exception: {0}", ex);
				Dispose ();
			}
		}
		
		private void ReceiveFromMD ()
		{
			byte[] buffer = new byte [1024];

			lock (sync_obj) {
				if (sock != null) {
					sock.BeginReceive (buffer, 0, buffer.Length, SocketFlags.None, ReceivedFromMD, buffer);
				}
			}
		}
		
		private void ReceivedFromMD (IAsyncResult ar)
		{
			int n;
			int res;
			
			// Called on threadpool (any) thread when we receive data from MD
			
			try {
				byte[] buffer = (byte []) ar.AsyncState;
				
				lock (sync_obj) {
					if (sock == null) {
						// socked closed
						return;
					}
					n = sock.EndReceive (ar);
				}
				
				if (n == 0) {
					// connection to MD closed
					Dispose ();
					return;
				}
				
				if (!IsConnected) {
					// connection to device lost
					Dispose ();
					return;
				}
				res = send (handle, buffer, new IntPtr (n), 0);

				ReceiveFromMD ();
			} catch (Exception ex) {
				Console.WriteLine ("UsbSocketProxy.ReceivedFromMD (): Exception: {0}", ex);
				Dispose ();
			}
		}
		
		bool disposing;
		public void Dispose ()
		{
			int res;
			
			try {
				if (disposing)
					return;
				disposing = true;
				lock (sync_obj) {
					if (sock != null) {
						sock.Dispose ();
						sock = null;
						// MD connection closed
					}
				
					if (handle != -1) {
						res = close (handle);
						handle = -1;
					}
				}
				
				// Dispose all resources
				UsbSocket.Dispose ();
				
				// unblock anybody waiting for things to get connected
				md_connected.Set ();
			} catch (Exception ex) {
				Console.WriteLine ("UsbSocketProxy.Dispose (): Exception: {0}", ex);
			} finally {
				disposing = false;
			}
		}

		[DllImport ("libc")]
		static extern int send (int handle, byte[] buffer, IntPtr length, int flags);
			
		[DllImport ("libc")]
		static extern int recv (int handle, byte[] buffer, IntPtr length, int flags);
		
		[DllImport ("libc")]
		static extern int close (int handle);
	}
	
	internal class IPhoneUsbConnection : IDisposable
	{
		Device device;
		UsbSocketProxy Debug;
		UsbSocketProxy Output;
		bool disposed;
		
		public IPAddress IPAddress { get { return IPAddress.Loopback; } }
		public int DebugPort { get { return Debug.EndPoint.Port; } }
		public int OutputPort { get { return Output.EndPoint.Port; } }
		
		public IPhoneUsbConnection (int debug_port, int output_port)
			: base ()
		{	
			device = new Device ();
			device.Connected += ConnectedToDevice;
			device.Disconnected += DisconnectedFromDevice;
			ThreadPool.QueueUserWorkItem (ConnectToDevice);

			Debug = new UsbSocketProxy (this, debug_port);
			Output = new UsbSocketProxy (this, output_port);
		}
		
		private void ConnectToDevice (object dummy)
		{
			try {
				device.SubscribeToNotifications ();
				device.RunLoop ();
			} catch (Exception ex) {
				Console.WriteLine ("IPhoneUsbConnection.ConnectToDevice (): Exception: {0}", ex);
				Dispose ();
			}
		}
		
		private void ConnectedToDevice (object sender, EventArgs ea)
		{
			ThreadPool.QueueUserWorkItem (ConnectToApp);
		}
		
		private void ConnectToApp (object dummy)
		{
			int muxConn;
			bool connected;
			
			try {
				device.Connect ();
				
				muxConn = device.GetConnectionID ();
				
				do {
					connected = true;
					connected &= Debug.ConnectToApp (device, muxConn);
					connected &= Output.ConnectToApp (device, muxConn);
					if (!connected)
						Thread.Sleep (250);
				} while (!connected && !disposed);
			} catch (Exception ex) {
				Console.WriteLine ("IPhoneUsbConnection.ConnectToApp (): Exception: {0}", ex);
				Dispose ();
			}
		}
		
		private void DisconnectFromDevice ()
		{
			try {
				Device device = this.device;
				this.device = null;
				
				if (device != null) {
					device.UnsubscribeFromNotifications ();
					// Calling Disconnect always throws an exception (error 0xe8000007)
					// device.Disconnect ();
					device.StopLoop ();
				}
			} catch (Exception ex) {
				Console.WriteLine ("IPhoneUsbConnection.DisconnectFromDevice (): Exception: {0}", ex);
			}
		}
		
		private void DisconnectedFromDevice (object sender, EventArgs ea)
		{
			try {
				Dispose ();
			} catch (Exception ex) {
				Console.WriteLine ("IPhoneUsbConnection.DisconnectedFromDevice: Exception: {0}", ex);
			}
		}
		
		bool disposing;
		public void Dispose ()
		{
			// Any thread.
			try {
				if (disposing)
					return;
				disposing = true;
				DisconnectFromDevice ();
				Debug.Dispose ();
				Output.Dispose ();
			} catch (Exception ex) {
				Console.WriteLine ("IPhoneUsbConnection.Dispose (): Exception: {0}", ex);
			} finally {
				disposed = true;
				disposing = false;
			}
		}
	}
}

