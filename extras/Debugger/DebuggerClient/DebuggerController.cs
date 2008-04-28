using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using DebuggerLibrary;

namespace DebuggerClient
{
	class DebuggerController : MarshalByRefObject, IDebuggerController
	{
		private IDebugger debugger;
		private DebuggerSession session;
		private Process process;
		private ManualResetEvent exitRequestEvent = new ManualResetEvent(false);
		private string [] args;

		public event TargetEventHandler TargetEvent;
		public event ProcessEventHandler MainProcessCreatedEvent;

		public IDebugger DebuggerServer {
			get { return debugger; }
		}

		#region IDebuggerController Members

		public void RegisterDebugger(IDebugger debugger)
		{
			this.debugger = debugger;
			debugger.Run (args);
		}

		public void WaitForExit()
		{
			exitRequestEvent.WaitOne();
		}

		public void OnMainProcessCreated(int process_id)
		{
			if (MainProcessCreatedEvent != null)
				MainProcessCreatedEvent(process_id);
		}

		public void OnTargetEvent (TargetEventArgs args)
		{
			if (TargetEvent != null)
				TargetEvent (args);
		}

		#endregion

		public DebuggerSession StartSession (string [] args)
		{
			//FIXME: This should be allowed only once, no reuse
			session = new DebuggerSession(this);
			StartDebugger(session);
			//FIXME: check whether we got debugger instance or not.. wait on something?
			this.args = args;
			return session;
		}

		private void StartDebugger(DebuggerSession session)
		{
			lock (this)
			{
				//if (starting) return;
				//starting = true;
				exitRequestEvent.Reset ();

				//string chId = Runtime.ProcessService.RegisterRemotingChannel ();
				string chId = RegisterRemotingChannel();

				BinaryFormatter bf = new BinaryFormatter();
				ObjRef oref = RemotingServices.Marshal(this);
				MemoryStream ms = new MemoryStream();
				bf.Serialize(ms, oref);
				string sref = Convert.ToBase64String(ms.ToArray());
				try
				{
					Process process = new Process();
					//process.Exited += new EventHandler (ProcessExited);
					string location = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
					string argv = string.Empty;
					//if (isDebugMode) argv += " --debug";
					argv += " --debug '" + Path.Combine(location, "DebuggerServer.exe") + "' ";

					process.StartInfo = new ProcessStartInfo("mono", argv);
					process.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.RedirectStandardInput = true;
					process.EnableRaisingEvents = true;
					process.Start();
					process.StandardInput.WriteLine(chId);
					process.StandardInput.WriteLine(sref);
					process.StandardInput.Flush();
					this.process = process;
				}
				catch (Exception ex)
				{
					//LoggingService.LogError (ex.ToString ());
					Console.WriteLine(ex.ToString());
					throw;
				}

			}
		}


		//Frmo ProcessService, temporarily moving here..
		string remotingChannel = "tcp";
		string RegisterRemotingChannel()
		{
			if (remotingChannel == "tcp")
			{
				IChannel ch = ChannelServices.GetChannel("tcp");
				if (ch == null)
				{
					IDictionary dict = new Hashtable();
					BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
					BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();

					dict["port"] = 0;
					serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

					ChannelServices.RegisterChannel(new TcpChannel(dict, clientProvider, serverProvider), false);
				}
			}
			else
			{
				/*IChannel ch = ChannelServices.GetChannel ("unix");
				  if (ch == null) {
				  unixRemotingFile = Path.GetTempFileName (); 
				  ChannelServices.RegisterChannel (new UnixChannel (unixRemotingFile), false);
				  }*/
			}
			return remotingChannel;
		}
	}
}
