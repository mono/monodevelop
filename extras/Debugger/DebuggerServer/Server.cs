using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using DebuggerLibrary;

namespace DebuggerServer
{
	public class Server
	{
		static void Main(string[] args)
		{
			Console.WriteLine("## DebuggerServer started");
			try
			{
				string channel = Console.In.ReadLine();

				string unixPath = null;
				if (channel == "unix")
				{
					/*unixPath = System.IO.Path.GetTempFileName();
					  Hashtable props = new Hashtable();
					  props["path"] = unixPath;
					  props["name"] = "__internal_unix";
					  ChannelServices.RegisterChannel(new UnixChannel(props, null, null), false);*/
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

				DebuggerServer server = new DebuggerServer(dc);
				dc.RegisterDebugger(server);
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
					server.Dispose();
				}
				catch
				{
				}

				//if (unixPath != null)
				// File.Delete(unixPath);
			} catch (Exception e)
			{
				Console.WriteLine ("DS: {0}", e.ToString());
			}

			Console.WriteLine ("DebuggerServer exiting.");
		}
	}
}
