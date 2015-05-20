// 
// AutoTestServer.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Components.AutoTest
{
	public class AutoTestSession: MarshalByRefObject
	{		
		[System.Runtime.InteropServices.DllImport ("/System/Library/Frameworks/QuartzCore.framework/QuartzCore")]
		static extern IntPtr CGDisplayCreateImage (int displayID);

		[System.Runtime.InteropServices.DllImport("/System/Library/Frameworks/ApplicationServices.framework/Versions/Current/ApplicationServices", EntryPoint="CGMainDisplayID")]
		internal static extern int MainDisplayID();

		readonly ManualResetEvent syncEvent = new ManualResetEvent (false);
		public readonly AutoTestSessionDebug SessionDebug = new AutoTestSessionDebug ();
		public IAutoTestSessionDebug<MarshalByRefObject> DebugObject { 
			get { return SessionDebug.DebugObject; }
			set { SessionDebug.DebugObject = value; }
		}

		public AutoTestSession ()
		{
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}

		public void ExecuteCommand (object cmd, object dataItem = null, CommandSource source = CommandSource.Unknown)
		{
			Gtk.Application.Invoke (delegate {
				AutoTestService.CommandManager.DispatchCommand (cmd, dataItem, null, source);
			});
		}
		
		object Sync (Func<object> del, bool safe)
		{
			object res = null;
			Exception error = null;

			if (DispatchService.IsGuiThread) {
				res = del ();
				return safe ? SafeObject (res) : res;
			}

			syncEvent.Reset ();
			Gtk.Application.Invoke (delegate {
				try {
					res = del ();
				} catch (Exception ex) {
					error = ex;
				} finally {
					syncEvent.Set ();
				}
			});
			if (!syncEvent.WaitOne (20000))
				throw new Exception ("Timeout while executing synchronized call");
			if (error != null)
				throw error;
			return safe ? SafeObject (res) : res;
		}

		public object Sync (Func<object> del)
		{
			return Sync (del, true);
		}

		// The difference between Sync and UnsafeSync is that UnsafeSync will return objects
		// that may not be safe to be serialized across the remoting channel.
		// But that is ok, so long as we know the places where it is being called from will
		// not be trying to send the object over the remoting channel.
		public object UnsafeSync (Func<object> del)
		{
			return Sync (del, false);
		}

		public void ExitApp ()
		{
			Sync (delegate {
				try {
					IdeApp.Exit ();
				} catch (Exception e) {
					Console.WriteLine (e);
				}
				return true;
			});
		}

		public object GlobalInvoke (string name, object[] args)
		{
			return Sync (delegate {
				int i = name.LastIndexOf ('.');
				if (i == -1)
					throw new Exception ("Member name not specified");
				object ob = GetGlobalObject (name.Substring (0, i));
				if (ob == null)
					throw new Exception ("Object not found");
				var type = ob as Type;
				if (type != null)
					return Invoke (null, type, name.Substring (i + 1), args);
				return Invoke (ob, ob.GetType (), name.Substring (i + 1), args);
			});
		}
		
		public object GetGlobalValue (string name)
		{
			return Sync (delegate {
				return GetGlobalObject (name);
			});
		}

		public void TakeScreenshot (string screenshotPath)
		{
			#if MAC
			DispatchService.GuiDispatch (delegate {
				try {
					IntPtr handle = CGDisplayCreateImage (MainDisplayID ());
					CoreGraphics.CGImage screenshot = ObjCRuntime.Runtime.GetINativeObject <CoreGraphics.CGImage> (handle, true);
					AppKit.NSBitmapImageRep imgRep =  new AppKit.NSBitmapImageRep (screenshot);
					var imageData = imgRep.RepresentationUsingTypeProperties (AppKit.NSBitmapImageFileType.Png);
					imageData.Save (screenshotPath, true);
				} catch (Exception e) {
					Console.WriteLine (e);
					throw;
				}
			});
			#endif
		}
		
		public void SetGlobalValue (string name, object value)
		{
			Sync (delegate {
				int i = name.LastIndexOf ('.');
				if (i == -1)
					throw new Exception ("Member name not specified");
				object ob = GetGlobalObject (name.Substring (0, i));
				if (ob == null)
					throw new Exception ("Object not found");
				var type = ob as Type;
				if (type != null)
					SetValue (null, type, name.Substring (i + 1), value);
				else
					SetValue (ob, ob.GetType (), name.Substring (i+1), value);
				return null;
			});
		}
			
		// FIXME: This shouldn't be here.
		public bool IsBuildSuccessful ()
		{
			return TaskService.Errors.Count (x => x.Severity == TaskSeverity.Error) == 0;
		}

		object SafeObject (object ob)
		{
			if (ob == null)
				return null;
			if (ob.GetType ().IsPrimitive || ob.GetType ().IsSerializable)
				return ob;
			if (ob is string)
				return ob;
			return null;
		}
		
		object Invoke (object target, Type type, string methodName, object[] args)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			flags |= BindingFlags.InvokeMethod;
			return type.InvokeMember (methodName, flags, null, target, args);
		}
		
		object GetValue (object target, Type type, string name)
		{
			int i = name.IndexOf ('.');
			string remaining = null;
			if (i != -1) {
				remaining = name.Substring (i+1);
				name = name.Substring (0, i);
			}
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			flags |= BindingFlags.GetField | BindingFlags.GetProperty;
			object res = type.InvokeMember (name, flags, null, target, null);
			
			if (remaining == null)
				return res;
			else
				return GetValue (res, res.GetType (), remaining);
		}
		
		void SetValue (object target, Type type, string name, object value)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			flags |= BindingFlags.SetField | BindingFlags.SetProperty;
			type.InvokeMember (name, flags, null, target, new object[] { value });
		}
			
		object GetGlobalObject (string name)
		{
			int i = 0;
			Type type = null;
			var asms = AppDomain.CurrentDomain.GetAssemblies ();
			do {
				i = name.IndexOf ('.', i);
				if (i == -1)
					i = name.Length;
				string cname = name.Substring (0, i);
				foreach (var a in asms) {
					type = a.GetType (cname);
					if (type != null)
						break;
				}
				i++;
			}
			while (type == null && i < name.Length);
			
			if (type == null)
				throw new Exception ("Object '" + name + "' not found");				
			
			if (i >= name.Length)
				return type;
			
			return GetValue (null, type, name.Substring (i));
		}

		public AppQuery CreateNewQuery ()
		{
			AppQuery query = new AppQuery ();
			query.SessionDebug = SessionDebug;

			return query;
		}

		public void ExecuteOnIdleAndWait (Action idleFunc, int timeout = 20000)
		{
			syncEvent.Reset ();
			GLib.Idle.Add (() => {
				idleFunc ();
				syncEvent.Set ();
				return false;
			});

			if (!syncEvent.WaitOne (timeout)) {
				throw new Exception ("Timeout while executing ExecuteOnIdleAndWait");
			}
		}

		// Executes the query outside of a syncEvent wait so it is safe to call from
		// inside an ExecuteOnIdleAndWait
		AppResult[] ExecuteQueryNoWait (AppQuery query)
		{
			AppResult[] resultSet = query.Execute ();
			Sync (() => {
				DispatchService.RunPendingEvents ();
				return true;
			});

			return resultSet;
		}

		public AppResult[] ExecuteQuery (AppQuery query, int timeout = 20000)
		{
			AppResult[] resultSet = null;

			ExecuteOnIdleAndWait (() => {
				resultSet = ExecuteQueryNoWait (query);
			});

			return resultSet;
		}

		public AppResult[] WaitForElement (AppQuery query, int timeout)
		{
			const int pollTime = 200;
			syncEvent.Reset ();
			AppResult[] resultSet = null;

			GLib.Timeout.Add ((uint)pollTime, () => {
				resultSet = ExecuteQueryNoWait (query);

				if (resultSet.Length > 0) {
					syncEvent.Set ();
					return false;
				}

				timeout -= pollTime;
				return timeout > 0;
			});

			if (!syncEvent.WaitOne (timeout)) {
				throw new Exception (String.Format ("Timeout while executing WaitForElement: {0}", query));
			}

			return resultSet;
		}

		public void WaitForNoElement (AppQuery query, int timeout)
		{
			const int pollTime = 100;
			syncEvent.Reset ();
			AppResult[] resultSet = null;

			GLib.Timeout.Add ((uint)pollTime, () => {
				resultSet = ExecuteQueryNoWait (query);
				if (resultSet.Length == 0) {
					syncEvent.Set ();
					return false;
				}

				timeout -= pollTime;
				return timeout > 0;
			});

			if (!syncEvent.WaitOne (timeout)) {
				throw new Exception (String.Format ("Timeout while executing WaitForNoElement: {0}", query));
			}
		}

		[Serializable]
		public struct TimerCounterContext {
			public string CounterName;
			public TimeSpan TotalTime;
		};

		Counter GetCounterByIDOrName (string idOrName)
		{
			Counter c = InstrumentationService.GetCounterByID (idOrName);
			return c ?? InstrumentationService.GetCounter (idOrName);
		}

		public TimerCounterContext CreateNewTimerContext (string counterName)
		{
			TimerCounter tc = GetCounterByIDOrName (counterName) as TimerCounter;
			if (tc == null) {
				throw new Exception ("Unknown timer counter " + counterName);
			}

			TimerCounterContext context = new TimerCounterContext {
				CounterName = counterName,
				TotalTime = tc.TotalTime
			};

			return context;
		}

		public void WaitForTimerContext (TimerCounterContext context, int timeout = 20000, int pollStep = 200)
		{
			TimerCounter tc = GetCounterByIDOrName (context.CounterName) as TimerCounter;
			if (tc == null) {
				throw new Exception ("Unknown timer counter " + context.CounterName);
			}

			do {
				if (tc.TotalTime > context.TotalTime) {
					return;
				}

				timeout -= pollStep;
				Thread.Sleep (pollStep);
			} while (timeout > 0);

			throw new Exception ("Timed out waiting for event");
		}

		public bool Select (AppResult result)
		{
			bool success = false;

			ExecuteOnIdleAndWait (() => {
				success = result.Select ();
			});

			return success;
		}

		public bool Click (AppResult result)
		{
			bool success = false;

			ExecuteOnIdleAndWait (() => {
				success = result.Click ();
			});

			return success;
		}

		public bool EnterText (AppResult result, string text)
		{
			ExecuteOnIdleAndWait (() => result.EnterText (text));

			return true;
		}

		public bool Toggle (AppResult result, bool active)
		{
			bool success = false;

			ExecuteOnIdleAndWait (() => {
				success = result.Toggle (active);
			});

			return success;
		}
	}

	[StructLayout (LayoutKind.Sequential)] 
	struct NativeEventButtonStruct { 
		public Gdk.EventType type; 
		public IntPtr window; 
		public sbyte send_event; 
		public uint time; 
		public double x; 
		public double y; 
		public IntPtr axes; 
		public uint state; 
		public uint button; 
		public IntPtr device; 
		public double x_root; 
		public double y_root; 
	} 
	
	[StructLayout (LayoutKind.Sequential)] 
	struct NativeEventScrollStruct { 
		public Gdk.EventType type; 
		public IntPtr window; 
		public sbyte send_event; 
		public uint time; 
		public double x; 
		public double y; 
		public uint state; 
		public Gdk.ScrollDirection direction;
		public IntPtr device; 
		public double x_root; 
		public double y_root; 
	}
	// Analysis restore InconsistentNaming
}

