using System;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceDiagnosticsAddIn
{
	public class UIThreadMonitor
	{
		public static UIThreadMonitor Instance { get; } = new UIThreadMonitor ();

		UIThreadMonitor ()
		{
			IdeApp.Exited += IdeAppExited;
		}

		void IdeAppExited (object sender, EventArgs e)
		{
			try {
				Instance.Stop ();
			} catch (Exception ex) {
				LoggingService.LogError ("UIThreadMonitor stop error.", ex);
			}
		}

		Thread tcpLoopThread;
		Thread dumpsReaderThread;
		TcpListener listener;
		Process process;

		void TcpLoop (object param)
		{
			var connection = (ConnectionInfo)param;
			var socket = connection.Socket;
			try {
				var buffer = new byte [1];
				var waitUIThread = new ManualResetEvent (false);
				while (connection.ListenerActive) {
					var readBytes = socket.Receive (buffer, 1, SocketFlags.None);
					if (readBytes != 1)
						return;
					waitUIThread.Reset ();
					Runtime.RunInMainThread (delegate {
						waitUIThread.Set ();
					});
					waitUIThread.WaitOne ();
					socket.Send (buffer);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("UIThreadMonitor TcpLoop error.", ex);
			} finally {
				try {
					if (connection.ListenerActive)
						AcceptClientConnection (connection.Listener);
					socket.Close ();
				} catch (Exception) {
				}
			}
		}

		TimeSpan forceProfileTime = TimeSpan.Zero;

		public bool ToggleProfilingChecked => sampleProcessPid != -1;

		int sampleProcessPid = -1;
		public void ToggleProfiling ()
		{
			if (sampleProcessPid != -1) {
				Mono.Unix.Native.Syscall.kill (sampleProcessPid, Mono.Unix.Native.Signum.SIGINT);
				sampleProcessPid = -1;
				return;
			}

			var outputFilePath = Path.GetTempFileName ();
			var startInfo = new ProcessStartInfo ("sample");
			startInfo.UseShellExecute = false;
			startInfo.Arguments = $"{Process.GetCurrentProcess ().Id} 10000 -file {outputFilePath}";
			var sampleProcess = Process.Start (startInfo);
			sampleProcess.EnableRaisingEvents = true;
			sampleProcess.Exited += delegate {
				ConvertJITAddressesToMethodNames (outputFilePath, "Profile");
			};
			sampleProcessPid = sampleProcess.Id;
		}

		public void Profile (int seconds)
		{
			var outputFilePath = Path.GetTempFileName ();
			var startInfo = new ProcessStartInfo ("sample");
			startInfo.UseShellExecute = false;
			startInfo.Arguments = $"{Process.GetCurrentProcess ().Id} {seconds} -file {outputFilePath}";
			var sampleProcess = Process.Start (startInfo);
			sampleProcess.EnableRaisingEvents = true;
			sampleProcess.Exited += delegate {
				ConvertJITAddressesToMethodNames (outputFilePath, "Profile");
			};
		}

		public bool IsListening { get; private set; }
		public bool IsSampling { get; private set; }
		public string HangFileName { get; set; }

		public void Start (bool sample)
		{
			if (IsListening) {
				if (IsSampling == sample)
					return;
				Stop ();
			}
			if (sample) {
				if (!(Environment.GetEnvironmentVariable ("MONO_DEBUG")?.Contains ("disable_omit_fp") ?? false)) {
					MessageService.ShowWarning ("Set environment variable",
												$@"It is highly recommended to set environment variable ""MONO_DEBUG"" to ""disable_omit_fp"" and restart {BrandingService.ApplicationName} to have better results.");
				}
			}
			IsListening = true;
			IsSampling = sample;
			//start listening on random port
			listener = new TcpListener (IPAddress.Loopback, 0);
			listener.Start ();
			AcceptClientConnection (listener);
			//get random port provided by OS
			var port = ((IPEndPoint)listener.LocalEndpoint).Port;
			process = new Process ();
			process.StartInfo.FileName = "mono";
			process.StartInfo.Arguments = GetArguments (port, sample);
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;//Ignore it, otherwise it goes to IDE logging
			process.Start ();

			if (IsSampling) {
				dumpsReaderThread = new Thread (new ParameterizedThreadStart (DumpsReader));
				dumpsReaderThread.IsBackground = true;
				dumpsReaderThread.Start (process);
				Task.Run (() => PumpErrorStream (process)).Ignore ();
			}
		}

		void AcceptClientConnection (TcpListener tcpListener)
		{
			tcpListener.AcceptSocketAsync ().ContinueWith (t => {
				if (!t.IsFaulted && !t.IsCanceled) {
					currentConnection = new ConnectionInfo (t.Result, tcpListener);
					tcpLoopThread = new Thread (new ParameterizedThreadStart (TcpLoop));
					tcpLoopThread.IsBackground = true;
					tcpLoopThread.Start (currentConnection);
				}
			});
		}

		string GetArguments (int port, bool sample)
		{
			var arguments = new StringBuilder ();
			arguments.Append ($"\"{typeof (UIThreadMonitorDaemon.MainClass).Assembly.Location}\" {port} {Process.GetCurrentProcess ().Id}");

			if (!sample)
				arguments.Append (" --noSample");

			if (!string.IsNullOrEmpty (HangFileName))
				arguments.Append ($" --hangFile:\"{HangFileName}\"");

			return arguments.ToString ();
		}

		[DllImport ("__Internal")]
		extern static string mono_pmip (long offset);
		static Dictionary<long, string> methodsCache = new Dictionary<long, string> ();

		static async Task PumpErrorStream (Process process)
		{
			while (!process.HasExited) {
				await process.StandardError.ReadLineAsync ().ConfigureAwait (false);
			}
		}

		static void DumpsReader (object param)
		{
			var process = (Process)param;
			while (!process.HasExited) {
				var fileName = process.StandardOutput.ReadLine ();
				ConvertJITAddressesToMethodNames (fileName, "UIThreadHang");
			}
		}

		public void Stop ()
		{
			if (!IsListening)
				return;
			if (currentConnection != null)
				currentConnection.ListenerActive = false;
			process.Kill ();
			process = null;
			IsListening = false;
			IsSampling = false;
			listener.Stop ();
			listener = null;
		}

		internal static void ConvertJITAddressesToMethodNames (string fileName, string profilingType)
		{
			// sample line to replace:
			// ???  (in <unknown binary>)  [0x103648455]
			var rx = new Regex (@"\?\?\?  \(in <unknown binary>\)  \[0x([0-9a-f]+)\]", RegexOptions.Compiled);


			if (File.Exists (fileName) && new FileInfo (fileName).Length > 0) {
				Directory.CreateDirectory (Options.OutputPath);
				var outputFilename = Path.Combine (Options.OutputPath, $"{BrandingService.ApplicationName}_{profilingType}_{DateTime.Now:yyyy-MM-dd__HH-mm-ss}.txt");

				using (var sr = new StreamReader (fileName))
				using (var sw = new StreamWriter (outputFilename)) {
					string line;
					while ((line = sr.ReadLine ()) != null) {

						var match = rx.Match (line);
						if (match.Success) {
							var offset = long.Parse (match.Groups [1].Value, NumberStyles.HexNumber);

							if (!methodsCache.TryGetValue (offset, out var pmipMethodName)) {
								pmipMethodName = mono_pmip (offset)?.TrimStart ();
								if (pmipMethodName != null)
									pmipMethodName = PmipParser.ToSample (pmipMethodName.AsSpan (), offset);
								methodsCache.Add (offset, pmipMethodName);
							}

							if (pmipMethodName != null) {
								line = line.Remove (match.Index, match.Length);
								line = line.Insert (match.Index, pmipMethodName);
							}
						}
						sw.WriteLine (line);
					}
				}
			}
		}

		static class PmipParser
		{
			// pmip output:
			// (wrapper managed-to-native) Gtk.Application:gtk_main () [{0x7f968e48d1e8} + 0xdf]  (0x122577d50 0x122577f28) [0x7f9682702c90 - MonoDevelop.exe]
			// MonoDevelop.Startup.MonoDevelopMain:Main (string[]) [{0x7faef5700948} + 0x93] [/Users/therzok/Work/md/monodevelop/main/src/core/MonoDevelop.Startup/MonoDevelop.Startup/MonoDevelopMain.cs :: 39u] (0x10e7609c0 0x10e760aa8) [0x7faef7002d80 - MonoDevelop.exe]

			// sample symbolified line:
			// start  (in libdyld.dylib) + 1  [0x7fff79c7ded9]
			// mono_hit_runtime_invoke  (in mono64) + 1619  [0x102f90083]  mini-runtime.c:3148
			public static string ToSample (ReadOnlySpan<char> input, long offset)
			{
				var sb = new StringBuilder ();
				string filename = null;

				// Cut off wrapper part.
				if (input.StartsWith ("(wrapper".AsSpan ()))
					input = input.Slice (input.IndexOf (')') + 1).TrimStart ();

				if (input.StartsWith ("<Module>:".AsSpan ()))
					input = input.Slice ("<Module>:".Length);

				if (input[0] == '<')
					return input.ToString ();

				var endMethodSignature = input.IndexOf ('{');
				var methodSignature = input.Slice (0, endMethodSignature - 2); // " ["
				input = input.Slice (endMethodSignature + 1).TrimStart ();

				for (int i = 0; i < methodSignature.Length; ++i) {
					var ch = methodSignature [i];
					if (ch == ' ')
						continue;

					if (ch == ':') {
						sb.Append ("::");
						continue;
					}

					if (ch == '.') {
						sb.Append ("_");
						continue;
					}

					if (ch == '[' && methodSignature [i + 1] == ']') {
						sb.Append ("*");
						i++;
						continue;
					}

					sb.Append (ch);
				}

				sb.Append ("  (in MonoDevelop.exe) + 0  [");
				sb.AppendFormat ("0x{0:x}", offset);
				sb.Append ("]");

				// skip whole block [{0x7f968e48d1e8} + 0xdf]
				input = input.Slice (input.IndexOf ('[') + 1).TrimStart ();

				if (input[0] == '/') {
					// We have a filename
					var end = input.IndexOf (']');
					var filepath = input.Slice (0, end - 1).Trim (); // trim u
					filename = filepath.ToString ();
				}

				if (filename != null) {
					sb.Append ("  ");
					sb.Append (filename);
				}

				return sb.ToString ();
			}
		}

		ConnectionInfo currentConnection;

		class ConnectionInfo
		{
			public Socket Socket { get; }
			public TcpListener Listener { get; }
			public bool ListenerActive { get; set; }

			public ConnectionInfo (Socket socket, TcpListener listener)
			{
				Socket = socket;
				Listener = listener;
				ListenerActive = true;
			}
		}
	}
}

