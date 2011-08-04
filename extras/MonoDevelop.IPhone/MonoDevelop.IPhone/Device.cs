// 
// Device.cs
//  
// Author:
//       Geoff Norton <gnorton@novell.vom>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Threading;
using System.Runtime.InteropServices;


namespace MonoDevelop.IPhone
{
	public class Device
	{
		DeviceNotificationDelegate del;
		am_device_notification_callback_info info;
		IntPtr context;
		IntPtr loop;

		public event EventHandler Connected;
		public event EventHandler Disconnected;
		public event EventHandler Unknown;

		/*
		static void Main (string [] pad) {
			IntPtr loop = IntPtr.Zero;
			Device d = new Device ();
			d.Connected += delegate (object sender, EventArgs a) {
				DeviceNotificationEventArgs args = (DeviceNotificationEventArgs) a;
				am_device device = (am_device) Marshal.PtrToStructure (args.Info.am_device, typeof (am_device));
				Console.WriteLine ("Device Connected: {0}", Marshal.PtrToStringAuto (device.uuid));
				d.UnsubscribeFromNotifications ();
				Console.WriteLine ("Shutting down runloop");
				CFRunLoopStop (loop);
			};

			ThreadPool.QueueUserWorkItem (delegate {
				d.SubscribeToNotifications ();
				loop = CFRunLoopGetCurrent ();
				CFRunLoopRun ();
				Console.WriteLine ("Shut down runloop");
			});

			Console.WriteLine ("Press enter to force exit...");
			Console.ReadLine ();
		}
		*/

		public Device () {
			del = new DeviceNotificationDelegate (NotificationCallback);
		}

		public void SubscribeToNotifications () {
			uint ret = AMDeviceNotificationSubscribe (del, 0, 0, 0, out context);

			if (ret != 0)
				throw new Exception ("AMDeviceNotificationSubscribe returned: " + ret);
		}

		public void UnsubscribeFromNotifications () {
			uint ret = AMDeviceNotificationUnsubscribe (context);

			if (ret != 0)
				throw new InvalidOperationException ("AMDeviceNotificationUnsubscribe returned: " + ret);
		}

		public void Connect ()
		{
			uint ret = AMDeviceConnect (info.am_device);

			if (ret != 0)
				throw new InvalidOperationException (String.Format ("AMDeviceConnect returned: 0x{0:x}", ret));
		}
		
		public void Disconnect ()
		{
			uint ret = AMDeviceDisconnect (info.am_device);
			
			if (ret != 0)
				throw new InvalidOperationException (string.Format ("AMDeviceDisconnect returned: 0x{0:x}", ret));
		}
		
		public int GetConnectionID ()
		{
			return AMDeviceGetConnectionID (info.am_device);
		}
		
		public void RunLoop ()
		{
			loop = CFRunLoopGetCurrent ();
			CFRunLoopRun ();
		}
		
		public void StopLoop ()
		{
			if (loop == IntPtr.Zero)
				return;

			CFRunLoopStop (loop);
			loop = IntPtr.Zero;
		}
		
		private void NotificationCallback (ref am_device_notification_callback_info info) {
			var args = new DeviceNotificationEventArgs (info);
			this.info = info;

			switch (this.info.message) {
				case 1:
					if (Connected != null)
						Connected (this, args);
					break;
				case 2:
					if (Disconnected != null)
						Disconnected (this, args);
					break;
				case 3:
					if (Unknown != null)
						Unknown (this, args);
					break;
				default:
					throw new Exception ("NotificationCallback with unknown message: " + this.info.message);
			}
		}

		public IntPtr AMDevice {
			get { return info.am_device; }
		}
		
		internal struct am_device_notification_callback_info {
			internal IntPtr am_device;
			internal uint message;
		}

		internal struct am_device {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			internal byte[] pad0;
			internal IntPtr uuid;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 76)]
			internal byte[] pad1;
		}
	
		internal class DeviceNotificationEventArgs : EventArgs {

			internal DeviceNotificationEventArgs (am_device_notification_callback_info info) {
				this.Info = info;
			}

			internal am_device_notification_callback_info Info { get; private set; }
		}

		internal delegate void DeviceNotificationDelegate (ref am_device_notification_callback_info info);

		const string MOBILEDEVICE_FRAMEWORK = "/System/Library/PrivateFrameworks/MobileDevice.framework/MobileDevice";
		const string CF_FRAMEWORK = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
		
		[DllImport (MOBILEDEVICE_FRAMEWORK)]
		static extern uint AMDeviceNotificationUnsubscribe (IntPtr context);
		
		[DllImport (MOBILEDEVICE_FRAMEWORK)]
		static extern uint AMDeviceNotificationSubscribe (DeviceNotificationDelegate callback, uint unused0, uint unused1, uint dn_unknown3, out IntPtr context);
		
		[DllImport (MOBILEDEVICE_FRAMEWORK)]
		public static extern uint USBMuxConnectByPort (int connection, short port, out int socketHandle);
		
		[DllImport (MOBILEDEVICE_FRAMEWORK)]
		static extern uint AMDeviceConnect (IntPtr device);
		
		[DllImport (MOBILEDEVICE_FRAMEWORK)]
		static extern uint AMDeviceDisconnect (IntPtr device);
		
		[DllImport (MOBILEDEVICE_FRAMEWORK)]
		static extern int AMDeviceGetConnectionID (IntPtr device);
		
		[DllImport (CF_FRAMEWORK)]
		static extern IntPtr CFRunLoopGetCurrent ();
		
		[DllImport (CF_FRAMEWORK)]
		static extern void CFRunLoopRun ();
		
		[DllImport (CF_FRAMEWORK)]
		static extern void CFRunLoopStop (IntPtr loop);
	}
}
