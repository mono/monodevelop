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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;

using System.Xml;

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

		[Serializable]
		public struct MemoryStats {
			public long PrivateMemory;
			public long VirtualMemory;
			public long WorkingSet;
			public long PeakVirtualMemory;
			public long PagedSystemMemory;
			public long PagedMemory;
			public long NonPagedSystemMemory;
		};

		public MemoryStats GetMemoryStats ()
		{
			MemoryStats stats;
			using (Process proc = Process.GetCurrentProcess ()) {
				stats = new MemoryStats {
					PrivateMemory = proc.PrivateMemorySize64,
					VirtualMemorySize = proc.VirtualMemorySize64,
					WorkingSet = proc.WorkingSet64,
					PeakVirtualMemory = proc.PeakVirtualMemorySize64,
					PagedSystemMemory = proc.PagedSystemMemorySize64,
					PagedMemory = proc.PagedMemorySize64,
					NonPagedSystemMemory = proc.NonpagedSystemMemorySize64
				};
				return stats;
			}
		}

		public string[] GetCounterStats ()
		{
			return Counters.CounterReport ();
		}

		public void ExecuteCommand (object cmd, object dataItem = null, CommandSource source = CommandSource.Unknown)
		{
			Gtk.Application.Invoke ((o, args) => {
				AutoTestService.CommandManager.DispatchCommand (cmd, dataItem, null, source);
			});
		}
		
		object Sync (Func<object> del, bool safe)
		{
			object res = null;
			Exception error = null;

			if (Runtime.IsMainThread) {
				res = del ();
				return safe ? SafeObject (res) : res;
			}

			syncEvent.Reset ();
			Gtk.Application.Invoke ((o, args) => {
				try {
					res = del ();
				} catch (Exception ex) {
					error = ex;
				} finally {
					syncEvent.Set ();
				}
			});
			if (!syncEvent.WaitOne (20000))
				throw new TimeoutException ("Timeout while executing synchronized call");
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
				IdeApp.Exit ().ContinueWith ((arg) => {
					if (arg.IsFaulted)
						Console.WriteLine (arg.Exception);
				});
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
			Runtime.RunInMainThread (delegate {
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
			#else
			Sync (delegate {
				try {
					using (var bmp = new System.Drawing.Bitmap (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
						System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height)) {
						using (var g = System.Drawing.Graphics.FromImage(bmp))
						{
							g.CopyFromScreen(System.Windows.Forms.Screen.PrimaryScreen.Bounds.X,
								System.Windows.Forms.Screen.PrimaryScreen.Bounds.Y,
								0, 0,
								bmp.Size,
								System.Drawing.CopyPixelOperation.SourceCopy);
						}
						bmp.Save(screenshotPath);
					}
					return null;
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
		public int ErrorCount (TaskSeverity severity)
		{
			return TaskService.Errors.Count (x => x.Severity == severity);
		}

		public List<TaskListEntryDTO> GetErrors (TaskSeverity severity)
		{
			return TaskService.Errors.Where (x => x.Severity == severity).Select (x => new TaskListEntryDTO () {
				Line = x.Line,
				Description = x.Description,
				File = x.FileName.FileName,
				Path = x.FileName.FullPath,
				Project = x.WorkspaceObject?.Name
			}).ToList ();
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
			else if (res == null)
				return null;
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

		public void ExecuteOnIdle (Action idleFunc, bool wait = true, int timeout = 20000)
		{
			if (Runtime.IsMainThread) {
				idleFunc ();
				return;
			}

			if (wait == false) {
				GLib.Idle.Add (() => {
					idleFunc ();
					return false;
				});

				return;
			}
				
			syncEvent.Reset ();
			GLib.Idle.Add (() => {
				idleFunc ();
				syncEvent.Set ();
				return false;
			});

			if (!syncEvent.WaitOne (timeout)) {
				throw new TimeoutException ("Timeout while executing ExecuteOnIdleAndWait");
			}
		}

		// Executes the query outside of a syncEvent wait so it is safe to call from
		// inside an ExecuteOnIdleAndWait
		internal AppResult[] ExecuteQueryNoWait (AppQuery query)
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

			try {
				ExecuteOnIdle (() => {
					resultSet = ExecuteQueryNoWait (query);
				});
			} catch (TimeoutException e) {
				throw new TimeoutException (string.Format ("Timeout while executing ExecuteQuery: {0}", query), e);
			}
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
				throw new TimeoutException (String.Format ("Timeout while executing WaitForElement: {0}", query));
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
				throw new TimeoutException (String.Format ("Timeout while executing WaitForNoElement: {0}", query));
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

			throw new TimeoutException ("Timed out waiting for event");
		}

		public bool Select (AppResult result)
		{
			bool success = false;

			try {
				ExecuteOnIdle (() => {
					success = result.Select ();
				});
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("Select", result.SourceQuery, result, e);
			}

			return success;
		}

		public bool Click (AppResult result, bool wait = true)
		{
			bool success = false;

			try {
				ExecuteOnIdle (() => {
					success = result.Click ();
				}, wait);
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("Click", result.SourceQuery, result, e);
			}

			return success;
		}

		public bool Click (AppResult result, double x, double y, bool wait = true)
		{
			bool success = false;

			try {
				ExecuteOnIdle (() => {
					success = result.Click (x, y);
				}, wait);
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("Click", result.SourceQuery, result, e);
			}

			return success;
		}

		public bool EnterText (AppResult result, string text)
		{
			try {
				ExecuteOnIdle (() => result.EnterText (text));
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("EnterText", result.SourceQuery, result, e);
			}

			return true;
		}

		public bool TypeKey (AppResult result, char key, string modifiers)
		{
			try {
				ExecuteOnIdle (() => result.TypeKey (key, modifiers));
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("TypeKey", result.SourceQuery, result, e);
			}

			return true;
		}

		public bool TypeKey (AppResult result, string keyString, string modifiers)
		{
			try {
				ExecuteOnIdle (() => result.TypeKey (keyString, modifiers));
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("TypeKey", result.SourceQuery, result, e);
			}

			return true;
		}

		public bool Toggle (AppResult result, bool active)
		{
			bool success = false;

			try {
				ExecuteOnIdle (() => {
					success = result.Toggle (active);
				});
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("Toggle", result.SourceQuery, result, e);
			}

			return success;
		}

		public void Flash (AppResult result)
		{
			try {
				ExecuteOnIdle (() => result.Flash ());
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("Flash", result.SourceQuery, result, e);
			}
		}

		public void SetProperty (AppResult result, string name, object o)
		{
			try {
				ExecuteOnIdle (() => result.SetProperty (name, o));
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("SetProperty", result.SourceQuery, result, e);
			}
		}

		public bool SetActiveConfiguration (AppResult result, string configuration)
		{
			bool success = false;

			try {
				ExecuteOnIdle (() => {
					success = result.SetActiveConfiguration (configuration);
				});
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("SetActiveConfiguration", result.SourceQuery, result, e);
			}

			return success;
		}

		public bool SetActiveRuntime (AppResult result, string runtime)
		{
			bool success = false;

			try {
				ExecuteOnIdle (() => {
					success = result.SetActiveRuntime (runtime);
				});
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("SetActiveRuntime", result.SourceQuery, result, e);
			}

			return success;
		}

		void ThrowOperationTimeoutException (string operation, string query, AppResult result, Exception innerException)
		{
			throw new TimeoutException (string.Format ("Timeout while executing {0}: {1}\n\ton Element: {2}", operation, query, result), innerException);
		}

		void AddChildrenToDocument (XmlDocument document, XmlElement parentElement, AppResult children, bool withSiblings = true)
		{
			while (children != null) {
				XmlElement childElement = document.CreateElement ("result");
				children.ToXml (childElement);
				parentElement.AppendChild (childElement);

				if (children.FirstChild != null) {
					AddChildrenToDocument (document, childElement, children.FirstChild);
				}

				children = withSiblings ? children.NextSibling : null;
			}
		}

		class UTF8StringWriter : StringWriter
		{
			public override Encoding Encoding {
				get {
					return Encoding.UTF8;
				}
			}
		}

		//
		// The XML result structure
		// <AutoTest>
		//   <query>c =&gt; c.Window()</query>
		//   <results>
		//     <result type="Gtk.Window" fulltype="Gtk.Window" name="Main Window" visible="true" sensitive="true" allocation="1,1 1024x1024">
		//       ... contains result elements for all children widgets ...
		//     </result>
		//     ... and more result element trees for each of the AppResult in results ...
		//   </results>
		// </AutoTest>
		//
		public string ResultsAsXml (AppResult[] results)
		{
			XmlDocument document = new XmlDocument ();
			XmlElement rootElement = document.CreateElement ("AutoTest");
			document.AppendChild (rootElement);

			if (results [0].SourceQuery != null) {
				XmlElement queryElement = document.CreateElement ("query");
				queryElement.AppendChild (document.CreateTextNode (results [0].SourceQuery));
				rootElement.AppendChild (queryElement);
			}

			XmlElement resultsElement = document.CreateElement ("results");
			rootElement.AppendChild (resultsElement);

			try {
				ExecuteOnIdle (() => {
					foreach (var result in results) {
						AddChildrenToDocument (document, resultsElement, result, false);
					}
				});
			} catch (TimeoutException e) {
				ThrowOperationTimeoutException ("ResultsAsXml", null, null, e);
			}

			string output;

			using (var sw = new UTF8StringWriter ()) {
				using (var xw = XmlWriter.Create (sw, new XmlWriterSettings { Indent = true })) {
					document.WriteTo (xw);
				}

				output = sw.ToString ();
			}

			return output;
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

