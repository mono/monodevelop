// 
// Main.cs
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
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Diagnostics;

namespace MonoDevelop.Projects.MSBuild
{
	class MainClass
	{
		static readonly ManualResetEvent exitEvent = new ManualResetEvent (false);
		static string msbuildBinDir = null;

		[STAThread]
		public static void Main (string [] args)
		{
			try {
				msbuildBinDir = Console.ReadLine ().Trim ();
				AppDomain.CurrentDomain.AssemblyResolve += MSBuildAssemblyResolver;

				Start ();
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		static void Start()
		{
			RegisterRemotingChannel ();
			WatchProcess (Console.ReadLine ());

			var builderEngine = new BuildEngine ();
			var bf = new BinaryFormatter ();
			ObjRef oref = RemotingServices.Marshal (builderEngine);
			var ms = new MemoryStream ();
			bf.Serialize (ms, oref);
			Console.Error.WriteLine ("[MonoDevelop]" + Convert.ToBase64String (ms.ToArray ()));

			if (WaitHandle.WaitAny (new WaitHandle[] { builderEngine.WaitHandle, exitEvent }) == 0) {
				// Wait before exiting, so that the remote call that disposed the builder can be completed
				Thread.Sleep (400);
			}
		}
		
		public static void RegisterRemotingChannel ()
		{
			IDictionary dict = new Hashtable ();
			var clientProvider = new BinaryClientFormatterSinkProvider();
			var serverProvider = new BinaryServerFormatterSinkProvider();
			dict ["port"] = 0;
			dict ["rejectRemoteRequests"] = true;
			serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
			ChannelServices.RegisterChannel (new TcpChannel (dict, clientProvider, serverProvider), false);
		}
		
		public static void WatchProcess (string procId)
		{
			int id = int.Parse (procId);
			var t = new Thread (delegate () {
				while (true) {
					Thread.Sleep (1000);
					try {
						// Throws exception if process is not running.
						// When watching a .NET process from Mono, GetProcessById may
						// return the process with HasExited=true
						Process p = Process.GetProcessById (id);
						if (p.HasExited)
							break;
					}
					catch {
						break;
					}
				}
				exitEvent.Set ();
			});
			t.IsBackground = true;
			t.Start ();
		}

		static Assembly MSBuildAssemblyResolver (object sender, ResolveEventArgs args)
		{
			var msbuildAssemblies = new string[] {
							"Microsoft.Build",
							"Microsoft.Build.Engine",
							"Microsoft.Build.Framework",
							"Microsoft.Build.Tasks.Core",
							"Microsoft.Build.Utilities.Core" };

			var asmName = new AssemblyName (args.Name);
			if (!msbuildAssemblies.Any (n => string.Compare (n, asmName.Name, StringComparison.OrdinalIgnoreCase) == 0))
				return null;

			string fullPath = Path.Combine (msbuildBinDir, asmName.Name + ".dll");
			if (File.Exists (fullPath)) {
				// If the file exists under the msbuild bin dir, then we need
				// to load it only from there. If that fails, then let that exception
				// escape
				return Assembly.LoadFrom (fullPath);
			} else
				return null;
		}
	}
}
