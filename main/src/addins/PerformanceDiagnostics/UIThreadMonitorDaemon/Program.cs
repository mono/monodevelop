//
// Program.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UIThreadMonitorDaemon
{
	/// <summary>
	/// This program is started by AddIn and gets parameters how to connect back to AddIn and is pinging it.
	/// If AddIn fails to responds in time it starts profiling until AddIn responds or max 10 seconds.
	/// Profiling output path is written to OutputStream.
	/// </summary>
	public class MainClass
	{
		static int tcpPort;
		static int processId;
		static bool sample = true;
		static string hangFile;
		static bool hangFileCreated;
		static int sendInterval;
		static Process parentProcess;

		public static int Main (string [] args)
		{
			int result = ParseArguments (args);
			if (result != 0)
				return result;

			parentProcess = Process.GetProcessById (processId);

			var sendEvents = new WaitHandle[] { sentEvent, parentProcessExitedEvent };
			var events = new WaitHandle[] { responseEvent, disconnectEvent, parentProcessExitedEvent };
			var thread = new Thread (new ParameterizedThreadStart (Loop));
			thread.Start (tcpPort);
			var sw = Stopwatch.StartNew ();
			while (!parentProcess.HasExited) {
				int waitResult = WaitHandle.WaitAny (sendEvents);
				if (waitResult == 1)
					return 0; // Parent process exited.

				sw.Restart ();
				if (!responseEvent.WaitOne (100)) {
					if (sample) {
						Console.Error.WriteLine ($"Timeout({seq}):" + sw.Elapsed);
						StartCollectingStacks ();
					}

					waitResult = WaitHandle.WaitAny (events, 10000);
					if (waitResult == 0) {// Got a response.
						if (sample)
							Console.Error.WriteLine ($"Response({seq}) in {sw.Elapsed}");
					} else if (waitResult == 1) { // Disconnected
						// Do nothing. The while loop will wait for the next send event.
					} else if (waitResult == 2) { // Parent process exited
						// Do nothing. The while loop checks for an exit.
					} else { // Timeout
						if (sample)
							Console.Error.WriteLine ($"No response({seq}) in 10sec");
						CreateHangFile ();
					}

					if (sample)
						StopCollectingStacks ();
				} else {
					if (sample && sw.ElapsedMilliseconds > 20)
						Console.Error.WriteLine ($"In time({seq}):" + sw.Elapsed);
					RemoveHangFile ();
				}
			}
			return 0;
		}

		static int ParseArguments (string[] args)
		{
			if (args.Length < 2)
				return 1;
			if (!int.TryParse (args [0], out tcpPort))
				return 2;
			if (!int.TryParse (args [1], out processId))
				return 3;

			sendInterval = 100; // Default time (ms) to wait before sending message to IDE.

			if (args.Length > 2) {
				const string hangFileOption = "--hangFile:";
				foreach (string arg in args.Skip (2)) {
					if (arg == "--noSample") {
						sample = false;
						// Increase the send interval if no sampling is being done.
						sendInterval = 1000; // 1 second.
					}
					else if (arg.StartsWith (hangFileOption, StringComparison.OrdinalIgnoreCase))
						hangFile = arg.Substring (hangFileOption.Length);
				}
			}

			return 0;
		}

		static void CreateHangFile ()
		{
			if (hangFileCreated)
				return;

			File.WriteAllText (hangFile, string.Empty);
			hangFileCreated = true;
		}

		static void RemoveHangFile ()
		{
			if (!hangFileCreated)
				return;

			File.Delete (hangFile);
			hangFileCreated = false;
		}

		static Process sampleProcess;
		static string outputFilePath;
		static void StartCollectingStacks ()
		{
			var startInfo = new ProcessStartInfo ("sample");
			startInfo.UseShellExecute = false;
			outputFilePath = Path.GetTempFileName ();
			Console.Error.WriteLine ("Storing in:" + outputFilePath);
			startInfo.Arguments = $"{processId} -file {outputFilePath}";
			sampleProcess = Process.Start (startInfo);
		}

		static void StopCollectingStacks ()
		{
			if (!sampleProcess.HasExited)
				Mono.Unix.Native.Syscall.kill (sampleProcess.Id, Mono.Unix.Native.Signum.SIGINT);
			Console.Error.WriteLine ("Waiting for sample close.");
			sampleProcess.WaitForExit ();
			Console.Error.WriteLine ("Sample closed.");
			if (File.Exists (outputFilePath) && new FileInfo (outputFilePath).Length > 0) {
				Console.WriteLine (outputFilePath);
			}
		}

		static AutoResetEvent sentEvent = new AutoResetEvent (false);
		static ManualResetEvent responseEvent = new ManualResetEvent (false);
		static ManualResetEvent disconnectEvent = new ManualResetEvent (false);
		static ManualResetEvent parentProcessExitedEvent = new ManualResetEvent (false);
		static byte seq;
		static void Loop (object portObj)
		{
			int port = (int)portObj;
			Socket socket = Connect (port);
			var response = new byte [1];
			while (!parentProcess.HasExited) {
				try {
					Thread.Sleep (sendInterval);
					socket.Send (new byte [1] { ++seq });
					responseEvent.Reset ();
					sentEvent.Set ();
					var readBytes = socket.Receive (response, 1, SocketFlags.None);
					if (readBytes != 1) {
						disconnectEvent.Set ();
						throw new ApplicationException ("Disconnected from parent.");
					}
					if (response [0] != seq)
						throw new InvalidOperationException ($"Expected {seq}, got {response [0]}.");
					responseEvent.Set ();
				} catch (Exception ex) {
					if (sample)
						Console.Error.WriteLine ($"Error communicating with parent. {ex.Message}");
					try {
						socket.Close ();
					} catch (Exception) {
						// Ignore.
					}
					socket = Connect (port);
				}
			}
			// Ensure main loop exits if it is waiting for a send event.
			parentProcessExitedEvent.Set ();
		}

		static int connectRetryInterval = 500; // ms

		static Socket Connect (int port)
		{
			while (!parentProcess.HasExited) {
				try {
					var ipe = new IPEndPoint (IPAddress.Loopback, port);
					var socket = new Socket (ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					socket.Connect (ipe);

					disconnectEvent.Reset ();

					return socket;
				} catch (Exception ex) {
					if (sample)
						Console.Error.WriteLine ($"Could not connect. {ex.Message}");
					if (!parentProcess.HasExited) {
						Thread.Sleep (connectRetryInterval);
					}
				}
			}
			return null;
		}
	}
}
