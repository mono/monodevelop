using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Mono.Debugging.Backend.Mdb;
using System.Reflection;
using Mono.Remoting.Channels.Unix;

namespace DebuggerServer
{
	public class ServerApp
	{
		static void Main(string[] args)
		{
			Console.WriteLine("## DebuggerServer started");
			
			// The first line of the input is the location of the Mono.Debugging assembly
			string debuggingAsmLocation = Console.In.ReadLine();
			Assembly.LoadFrom (debuggingAsmLocation);
			
			// Mono can't jit a direct call to Server.Run because it fails trying to load Mono.Debugging.
			// The reflection call delays the loading of Mono.Debugging.
			typeof(Server).GetMethod ("Run", BindingFlags.Public | BindingFlags.Static).Invoke (null, new object[] { args });
		}
	}
	
	class Server
	{
		public static DebuggerServer Instance;
		
		public static void Run (string[] args)
		{
			try
			{
				// Load n-refactory from MD's dir
				string path = typeof(Mono.Debugging.Client.DebuggerSession).Assembly.Location;
				path = System.IO.Path.GetDirectoryName (path);
				path = System.IO.Path.Combine (path, "NRefactory.dll");
				Assembly.LoadFrom (path);
				
				string channel = Console.In.ReadLine();

				string unixPath = null;
				if (channel == "unix")
				{
					unixPath = System.IO.Path.GetTempFileName();
					Hashtable props = new Hashtable();
					props["path"] = unixPath;
					props["name"] = "__internal_unix";
					ChannelServices.RegisterChannel(new UnixChannel(props, null, null), false);
				}
				else
				{
					Hashtable props = new Hashtable();
					props["port"] = 0;
					props["name"] = "__internal_tcp";
					BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
					BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();

					serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
					ChannelServices.RegisterChannel(new TcpChannel(props, clientProvider, serverProvider), false);
				}

				string sref = Console.In.ReadLine();
				byte[] data = Convert.FromBase64String(sref);
				
				MemoryStream ms = new MemoryStream(data);
				BinaryFormatter bf = new BinaryFormatter();
				IDebuggerController dc = (IDebuggerController) bf.Deserialize(ms);

				Instance = new DebuggerServer(dc);
				dc.RegisterDebugger (Instance);
				try
				{
					dc.WaitForExit();
				}
				catch (Exception e)
				{
					Console.WriteLine ("DS: Exception while waiting for WaitForExit: {0}", e.ToString ());
				}

				try
				{
					Instance.Dispose();
				}
				catch
				{
				}

				if (unixPath != null)
					File.Delete(unixPath);
				
			} catch (Exception e)
			{
				Console.WriteLine ("DS: {0}", e.ToString());
			}
			
			// Delay the exit a few seconds, to make sure all remoting calls
			// from the client have been completed
			System.Threading.Thread.Sleep (3000);

			Console.WriteLine ("DebuggerServer exiting.");
		}
	}
}
