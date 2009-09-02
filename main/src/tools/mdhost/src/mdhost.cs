//
// mdhost.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Core.Logging;
using MonoDevelop.Core.Execution;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting.Lifetime;
using System.Reflection;
using System.Collections;
using Mono.Remoting.Channels.Unix;
using Mono.Addins;

public class MonoDevelopProcessHost
{
	public static int Main (string[] args)
	{
		string tmpFile = null;
		TextReader input = null;
		try {
			// The first parameter is the task id
			// The second parameter is the temp file that contains the data
			// If not provided, data is read from the standard input
			
			if (args.Length > 1) {
				tmpFile = args [1];
				input = new StreamReader (tmpFile);
			} else
				input = Console.In;
			
			string sref = input.ReadLine ();
			string pidToWatch = input.ReadLine ();
			
			if (tmpFile != null) {
				try {
					input.Close ();
					File.Delete (tmpFile);
				} catch {
				}
			}
			
			WatchParentProcess (int.Parse (pidToWatch));
			
			string unixPath = RegisterRemotingChannel ();
			
			byte[] data = Convert.FromBase64String (sref);
			MemoryStream ms = new MemoryStream (data);
			BinaryFormatter bf = new BinaryFormatter ();
			IProcessHostController pc = (IProcessHostController) bf.Deserialize (ms);
			
			LoggingService.AddLogger (new LocalLogger (pc.GetLogger (), args[0]));
			
			ProcessHost rp = new ProcessHost (pc);
			pc.RegisterHost (rp);
			try {
				pc.WaitForExit ();
			} catch {
			}
			
			try {
				rp.Dispose ();
			} catch {
			}
			
			if (unixPath != null)
				File.Delete (unixPath);
			
		} catch (Exception ex) {
			Console.WriteLine (ex);
		}
		
		return 0;
	}
		
	static string RegisterRemotingChannel ()
	{
		IDictionary dict = new Hashtable ();
		BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
		BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
		string unixRemotingFile = Path.GetTempFileName ();
		dict ["portName"] = Path.GetFileName (unixRemotingFile);
		serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
		ChannelServices.RegisterChannel (new IpcChannel (dict, clientProvider, serverProvider), false);
		return unixRemotingFile;
	}
	
	static void WatchParentProcess (int pid)
	{
		Thread t = new Thread (delegate () {
			while (true) {
				try {
					System.Diagnostics.Process.GetProcessById (pid);
				} catch {
					Environment.Exit (1);
				}
				Thread.Sleep (1000);
			}
		});
		t.IsBackground = true;
		t.Start ();
	}
}

class LocalLogger: ILogger
{
	ILogger wrapped;
	string id;
	
	public LocalLogger (ILogger wrapped, string id)
	{
		this.wrapped = wrapped;
		this.id = id;
	}
	
	#region ILogger implementation
	public void Log (LogLevel level, string message)
	{
		wrapped.Log (level, "[" + id + "] " + message);
	}
	
	public EnabledLoggingLevel EnabledLevel {
		get {
			return EnabledLoggingLevel.All;
		}
	}
	
	public string Name {
		get {
			return "Local Logger";
		}
	}
	#endregion
}

public class ProcessHost: MarshalByRefObject, IProcessHost, ISponsor
{
	IProcessHostController controller;
	
	public ProcessHost (IProcessHostController controller)
	{
		this.controller = controller;
		MarshalByRefObject mbr = (MarshalByRefObject) controller;
		ILease lease = mbr.GetLifetimeService () as ILease;
		lease.Register (this);
	}
	
	public IDisposable CreateInstance (Type type)
	{
		return (IDisposable) Activator.CreateInstance (type);
	}
	
	public void LoadAddins (string[] addinIds)
	{
		Runtime.Initialize (false);
		foreach (string ad in addinIds)
			AddinManager.LoadAddin (null, ad);
	}
	
	public IDisposable CreateInstance (string fullTypeName)
	{
		try {
			Type t = Type.GetType (fullTypeName);
			if (t == null) throw new InvalidOperationException ("Type not found: " + fullTypeName);
			return CreateInstance (t);
		} catch {
			throw new InvalidOperationException ("Type not found: " + fullTypeName);
		}
	}
	
	public IDisposable CreateInstance (string assemblyPath, string typeName)
	{
		Assembly asm = Assembly.LoadFrom (assemblyPath);
		Type t = asm.GetType (typeName);
		if (t == null) throw new InvalidOperationException ("Type not found: " + typeName);
		return CreateInstance (t);
	}
	
	public void DisposeObject (IDisposable obj)
	{
		obj.Dispose ();
	}

		
	public TimeSpan Renewal (ILease lease)
	{
		return TimeSpan.FromSeconds (7);
	}
	
	public void Dispose ()
	{
		MarshalByRefObject mbr = (MarshalByRefObject) controller;
		ILease lease = mbr.GetLifetimeService () as ILease;
		lease.Unregister (this);
	}
	
	public override object InitializeLifetimeService ()
	{
		return null;
	}
}
