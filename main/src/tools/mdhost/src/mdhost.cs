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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
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
			
			string channel = input.ReadLine ();
			string sref = input.ReadLine ();
			
			if (tmpFile != null) {
				try {
					input.Close ();
					File.Delete (tmpFile);
				} catch {
				}
			}
			
			string unixPath = null;
			if (channel == "unix") {
				unixPath = System.IO.Path.GetTempFileName ();
				Hashtable props = new Hashtable ();
				props ["path"] = unixPath;
				props ["name"] = "__internal_unix";
				ChannelServices.RegisterChannel (new UnixChannel (props, null, null), false);
			} else {
				Hashtable props = new Hashtable ();
				props ["port"] = 0;
				props ["name"] = "__internal_tcp";
				BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
				BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();

				serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

				ChannelServices.RegisterChannel (new TcpChannel (props, clientProvider, serverProvider), false);
			}
			
			byte[] data = Convert.FromBase64String (sref);
			MemoryStream ms = new MemoryStream (data);
			BinaryFormatter bf = new BinaryFormatter ();
			IProcessHostController pc = (IProcessHostController) bf.Deserialize (ms);
			
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
	
	public RemoteProcessObject CreateInstance (Type type)
	{
		RemoteProcessObject proc = (RemoteProcessObject) Activator.CreateInstance (type);
		proc.Attach (controller);
		return proc;
	}
	
	public void LoadAddins (string[] addinIds)
	{
		Runtime.Initialize (false);
		foreach (string ad in addinIds)
			AddinManager.LoadAddin (null, ad);
	}
	
	public RemoteProcessObject CreateInstance (string fullTypeName)
	{
		try {
			Type t = Type.GetType (fullTypeName);
			if (t == null) throw new InvalidOperationException ("Type not found: " + fullTypeName);
			return CreateInstance (t);
		} catch {
			throw new InvalidOperationException ("Type not found: " + fullTypeName);
		}
	}
	
	public RemoteProcessObject CreateInstance (string assemblyPath, string typeName)
	{
		Assembly asm = Assembly.LoadFrom (assemblyPath);
		Type t = asm.GetType (typeName);
		if (t == null) throw new InvalidOperationException ("Type not found: " + typeName);
		return CreateInstance (t);
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
