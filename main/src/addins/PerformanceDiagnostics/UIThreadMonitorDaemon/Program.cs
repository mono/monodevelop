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

		public static int Main (string [] args)
		{
			if (args.Length != 2)
				return 1;
			if (!int.TryParse (args [0], out tcpPort))
				return 2;
			if (!int.TryParse (args [1], out processId))
				return 3;
			var thread = new Thread (new ParameterizedThreadStart (Loop));
			thread.Start (tcpPort);
			var sw = Stopwatch.StartNew ();
			while (!disonnected) {
				sentEvent.WaitOne ();
				sw.Restart ();
				if (!responseEvent.WaitOne (100)) {
					Console.Error.WriteLine ($"Timeout({seq}):" + sw.Elapsed);
					StartCollectingStacks ();
					if (!responseEvent.WaitOne (10000))
						Console.Error.WriteLine ($"No response({seq}) in 10sec");
					else
						Console.Error.WriteLine ($"Response({seq}) in {sw.Elapsed}");
					StopCollectingStacks ();
				} else {
					if (sw.ElapsedMilliseconds > 20)
						Console.Error.WriteLine ($"In time({seq}):" + sw.Elapsed);
				}
			}
			return 0;
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
		static bool disonnected;
		static byte seq;
		static void Loop (object portObj)
		{
			var ipe = new IPEndPoint (IPAddress.Loopback, (int)portObj);
			var socket = new Socket (ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect (ipe);
			var response = new byte [1];
			while (true) {
				Thread.Sleep (100);
				socket.Send (new byte [1] { ++seq });
				responseEvent.Reset ();
				sentEvent.Set ();
				var readBytes = socket.Receive (response, 1, SocketFlags.None);
				if (readBytes != 1) {
					disonnected = true;
					disconnectEvent.Set ();
					return;
				}
				if (response [0] != seq)
					throw new InvalidOperationException ($"Expected {seq}, got {response [0]}.");
				responseEvent.Set ();
			}
		}
	}
}
