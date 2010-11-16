// 
// InstrumentationService.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core.ProgressMonitoring;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Threading;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace MonoDevelop.Core.Instrumentation
{
	public static class InstrumentationService
	{
		static Dictionary <string, Counter> counters;
		static List<CounterCategory> categories;
		static bool enabled = true;
		static DateTime startTime;
		static int publicPort = -1;
		static Thread autoSaveThread;
		static bool stopping;
		static int autoSaveInterval;
		
		static InstrumentationService ()
		{
			counters = new Dictionary <string, Counter> ();
			categories = new List<CounterCategory> ();
			startTime = DateTime.Now;
		}
		
		public static int PublishService (int port)
		{
			// Get a free port
			TcpListener listener = new TcpListener (IPAddress.Loopback, port);
			listener.Start ();
			if (port == 0)
				port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.Stop ();
			
			Hashtable dict = new Hashtable ();
			BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
			BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
			dict ["port"] = port;
			serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
			ChannelServices.RegisterChannel (new TcpChannel (dict, clientProvider, serverProvider), false);
			publicPort = port;
			
			InstrumentationServiceBackend backend = new InstrumentationServiceBackend ();
			System.Runtime.Remoting.RemotingServices.Marshal (backend, "InstrumentationService");
			
			return port;
		}
		
		public static void StartMonitor ()
		{
			if (publicPort == -1)
				throw new InvalidOperationException ("Service not published");
			
			if (PropertyService.IsMac) {
				var macOSDir = PropertyService.EntryAssemblyPath.ParentDirectory.ParentDirectory.ParentDirectory;
				var app = macOSDir.Combine ("MDMonitor.app");
				if (Directory.Exists (app)) {
					var psi = new ProcessStartInfo ("open", string.Format ("-n '{0}' --args -c localhost:{1} ", app, publicPort)) {
						UseShellExecute = false,
					};
					Process.Start (psi);
					return;
				}
			}	
			
			string exe = Path.Combine (Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location), "mdmonitor.exe");
			string args = "-c localhost:" + publicPort;
			Runtime.SystemAssemblyService.CurrentRuntime.ExecuteAssembly (exe, args);
		}
		
		public static void StartAutoSave (string file, int interval)
		{
			autoSaveInterval = interval;
			autoSaveThread = new Thread (delegate () {
				AutoSave (file, interval);
			});
			autoSaveThread.IsBackground = true;
			autoSaveThread.Start ();
		}
		
		public static void Stop ()
		{
			stopping = true;
			if (autoSaveThread != null)
				autoSaveThread.Join (autoSaveInterval*3);
		}
		
		static void AutoSave (string file, int interval)
		{
			while (!stopping) {
				Thread.Sleep (interval);
				try {
					lock (counters) {
						InstrumentationServiceData data = new InstrumentationServiceData ();
						data.EndTime = DateTime.Now;
						data.StartTime = StartTime;
						data.Counters = counters;
						data.Categories = categories;
						FilePath path = file + ".tmp";
						using (Stream fs = File.OpenWrite (path)) {
							BinaryFormatter f = new BinaryFormatter ();
							f.Serialize (fs, data);
						}
						FileService.SystemRename (path, file);
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Instrumentation service data could not be saved", ex);
				}
			}
			autoSaveThread = null;
		}
		
		public static IInstrumentationService GetRemoteService (string hostAndPort)
		{
			return (IInstrumentationService) Activator.GetObject (typeof(IInstrumentationService), "tcp://" + hostAndPort + "/InstrumentationService");
		}
		
		public static IInstrumentationService LoadServiceDataFromFile (string file)
		{
			using (Stream s = File.OpenRead (file)) {
				BinaryFormatter f = new BinaryFormatter ();
				IInstrumentationService data = f.Deserialize (s) as IInstrumentationService;
				if (data == null)
					throw new Exception ("Invalid instrumentation service data file");
				return data;
			}
		}
		
		public static bool Enabled {
			get { return enabled; }
			set { enabled = value; }
		}
		
		public static DateTime StartTime {
			get { return startTime; }
		}
		
		public static Counter CreateCounter (string name)
		{
			return CreateCounter (name, null);
		}
		
		public static Counter CreateCounter (string name, string category)
		{
			return CreateCounter (name, category, false);
		}
		
		public static Counter CreateCounter (string name, string category, bool logMessages)
		{
			return CreateCounter (name, category, logMessages, false);
		}
		
		static Counter CreateCounter (string name, string category, bool logMessages, bool isTimer)
		{
			if (category == null)
				category = "Global";
				
			lock (counters) {
				CounterCategory cat = GetCategory (category);
				if (cat == null) {
					cat = new CounterCategory (category);
					categories.Add (cat);
				}
				
				Counter c = isTimer ? new TimerCounter (name, cat) : new Counter (name, cat);
				c.LogMessages = logMessages;
				cat.AddCounter (c);
				
				Counter old;
				if (counters.TryGetValue (name, out old))
					old.Disposed = true;
				counters [name] = c;
				return c;
			}
		}
		
		public static MemoryProbe CreateMemoryProbe (string name)
		{
			return CreateMemoryProbe (name, null);
		}
		
		public static MemoryProbe CreateMemoryProbe (string name, string category)
		{
			if (!enabled)
				return null;
			
			Counter c;
			lock (counters) {
				if (!counters.TryGetValue (name, out c))
					c = CreateCounter (name, category);
			}
			return new MemoryProbe (c);
		}
		
		public static TimerCounter CreateTimerCounter (string name)
		{
			return CreateTimerCounter (name, null);
		}
		
		public static TimerCounter CreateTimerCounter (string name, string category)
		{
			return CreateTimerCounter (name, category, 0, false);
		}
		
		public static TimerCounter CreateTimerCounter (string name, string category, double minSeconds, bool logMessages)
		{
			TimerCounter c = (TimerCounter) CreateCounter (name, category, logMessages, true);
			c.DisplayMode = CounterDisplayMode.Line;
			c.LogMessages = logMessages;
			c.MinSeconds = minSeconds;
			return c;
		}
		
		public static IEnumerable<Counter> GetCounters ()
		{
			lock (counters) {
				return new List<Counter> (counters.Values);
			}
		}
		
		public static Counter GetCounter (string name)
		{
			lock (counters) {
				Counter c;
				if (counters.TryGetValue (name, out c))
					return c;
				c = new Counter (name, null);
				counters [name] = c;
				return c;
			}
		}
		
		public static CounterCategory GetCategory (string name)
		{
			lock (counters) {
				foreach (CounterCategory cat in categories)
					if (cat.Name == name)
						return cat;
				return null;
			}
		}
		
		public static IEnumerable<CounterCategory> GetCategories ()
		{
			lock (counters) {
				return new List<CounterCategory> (categories);
			}
		}

		[ThreadStatic]
		internal static bool IsLoggingMessage;
		
		internal static void LogMessage (string message)
		{
			IsLoggingMessage = true;
			try {
				LoggingService.LogInfo (message);
			} finally {
				IsLoggingMessage = false;
			}
		}
		
		public static void Dump ()
		{
			foreach (CounterCategory cat in categories) {
				Console.WriteLine (cat.Name);
				Console.WriteLine (new string ('-', cat.Name.Length));
				Console.WriteLine ();
				foreach (Counter c in cat.Counters)
					Console.WriteLine ("{0,-6} {1,-6} : {2}", c.Count, c.TotalCount, c.Name);
				Console.WriteLine ();
			}
		}
		
		public static IProgressMonitor GetInstrumentedMonitor (IProgressMonitor monitor, TimerCounter counter)
		{
			if (enabled) {
				AggregatedProgressMonitor mon = new AggregatedProgressMonitor (monitor);
				mon.AddSlaveMonitor (new IntrumentationMonitor (counter), MonitorAction.Tasks | MonitorAction.WriteLog);
				return mon;
			} else
				return monitor;
		}
	}
	
	class IntrumentationMonitor: NullProgressMonitor
	{
		TimerCounter counter;
		Stack<ITimeTracker> timers = new Stack<ITimeTracker> ();
		LogTextWriter logger = new LogTextWriter ();
		
		public IntrumentationMonitor (TimerCounter counter)
		{
			this.counter = counter;
			logger.TextWritten += HandleLoggerTextWritten;
		}

		void HandleLoggerTextWritten (string writtenText)
		{
			if (timers.Count > 0)
				timers.Peek ().Trace (writtenText);
		}
		
		public override void BeginTask (string name, int totalWork)
		{
			if (!string.IsNullOrEmpty (name)) {
				ITimeTracker c = counter.BeginTiming (name);
				c.Trace (name);
				timers.Push (c);
			} else {
				timers.Push (null);
			}
			base.BeginTask (name, totalWork);
		}
		
		public override void BeginStepTask (string name, int totalWork, int stepSize)
		{
			if (!string.IsNullOrEmpty (name)) {
				ITimeTracker c = counter.BeginTiming (name);
				c.Trace (name);
				timers.Push (c);
			} else {
				timers.Push (null);
			}
			base.BeginStepTask (name, totalWork, stepSize);
		}

		public override void EndTask ()
		{
			if (timers.Count > 0) {
				ITimeTracker c = timers.Pop ();
				if (c != null)
					c.End ();
			}
			base.EndTask ();
		}
		
		public override System.IO.TextWriter Log {
			get {
				return logger;
			}
		}
	}
	
	public interface IInstrumentationService
	{
		DateTime StartTime { get; }
		DateTime EndTime { get; }
		IEnumerable<Counter> GetCounters ();
		Counter GetCounter (string name);
		CounterCategory GetCategory (string name);
		IEnumerable<CounterCategory> GetCategories ();
	}
	
	class InstrumentationServiceBackend: MarshalByRefObject, IInstrumentationService
	{
		public DateTime StartTime {
			get {
				return InstrumentationService.StartTime;
			}
		}
		
		public DateTime EndTime {
			get {
				return DateTime.Now;
			}
		}

		public IEnumerable<Counter> GetCounters ()
		{
			return InstrumentationService.GetCounters ();
		}
		
		public Counter GetCounter (string name)
		{
			return InstrumentationService.GetCounter (name);
		}
		
		public CounterCategory GetCategory (string name)
		{
			return InstrumentationService.GetCategory (name);
		}
		
		public IEnumerable<CounterCategory> GetCategories ()
		{
			return InstrumentationService.GetCategories ();
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
	
	[Serializable]
	class InstrumentationServiceData: IInstrumentationService
	{
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public Dictionary <string, Counter> Counters { get; set; }
		public List<CounterCategory> Categories { get; set; }
		
		public IEnumerable<Counter> GetCounters ()
		{
			return Counters.Values;
		}
		
		public Counter GetCounter (string name)
		{
			Counter c;
			if (Counters.TryGetValue (name, out c))
				return c;
			c = new Counter (name, null);
			Counters [name] = c;
			return c;
		}
		
		public CounterCategory GetCategory (string name)
		{
			return Categories.FirstOrDefault (c => c.Name == name);
		}
		
		public IEnumerable<CounterCategory> GetCategories ()
		{
			return Categories;
		}
	}
}
