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
using MonoDevelop.Services;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting.Lifetime;
using System.Reflection;
using System.Collections;

public class MonoDevelopProcessHost
{
	public static int Main (string[] args)
	{
		try {
			Hashtable props = new Hashtable ();
			props ["port"] = 0;
			props ["name"] = "__internal_tcp";
			ChannelServices.RegisterChannel (new TcpChannel (props, null, null));
			
			string sref = Console.In.ReadLine ();
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
			rp.Dispose ();
			
		} catch (Exception ex) {
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
	
	public RemoteProcessObject CreateInstance (string fullTypeName)
	{
		try {
			Type t = Type.GetType (fullTypeName);
			if (t == null) throw new InvalidOperationException ("Type not found: " + fullTypeName);
			return CreateInstance (t);
		} catch (Exception ex) {
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
