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
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.Core.Execution;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MonoDevelop.Projects.MSBuild
{
	class MainClass
	{
		[STAThread]
		public static void Main (string[] args)
		{
			// This is required for MSBuild to properly load the .exe.config configuration file for this executable.
			Environment.SetEnvironmentVariable ("MSBUILD_EXE_PATH", typeof(MainClass).Assembly.Location);

			RemoteProcessServer server = new RemoteProcessServer ();
			server.Connect (args, new AssemblyResolver (server));
		}

		/// <summary>
		/// Since BuildEngine class directly access MSBuild types it tries to load Microsoft.Build.dll assembly when
		/// constructor is called so before Initialize is called which specifies msbuildBinDir and installs MSBuildAssemblyResolver.
		/// To solve this problem we use AssemblyResolver class which we install as listener to RemoteProcessServer(implicitly in Connect call).
		/// and after installing MSBuildAssemblyResolver we create BuildEngine and add it as Listener and remove AssemblyResolver
		/// as if it was never there. 
		/// </summary>
		class AssemblyResolver
		{
			string msbuildBinDir;
			RemoteProcessServer server;

			public AssemblyResolver (RemoteProcessServer server)
			{
				this.server = server;
			}

			Assembly MSBuildAssemblyResolver (object sender, ResolveEventArgs args)
			{
				var msbuildAssemblies = new string [] {
							"Microsoft.Build",
							"Microsoft.Build.Engine",
							"Microsoft.Build.Framework",
							"Microsoft.Build.Tasks.Core",
							"Microsoft.Build.Utilities.Core",
							"System.Reflection.Metadata"};

				var asmName = new AssemblyName (args.Name);
				if (!msbuildAssemblies.Any (n => string.Compare (n, asmName.Name, StringComparison.OrdinalIgnoreCase) == 0))
					return null;

				// Temporary workaround: System.Reflection.Metadata.dll is required in msbuildBinDir, but it is present only
				// in $msbuildBinDir/Roslyn .
				//
				// https://github.com/xamarin/bockbuild/commit/3609dac69598f10fbfc33281289c34772eef4350
				//
				// Adding this till we have a release out with the above fix!
				if (String.Compare (asmName.Name, "System.Reflection.Metadata") == 0) {
					string fixedPath = Path.Combine (msbuildBinDir, "Roslyn", "System.Reflection.Metadata.dll");
					if (File.Exists (fixedPath))
						return Assembly.LoadFrom (fixedPath);
					return null;
				}

				string fullPath = Path.Combine (msbuildBinDir, asmName.Name + ".dll");
				if (File.Exists (fullPath)) {
					// If the file exists under the msbuild bin dir, then we need
					// to load it only from there. If that fails, then let that exception
					// escape
					return Assembly.LoadFrom (fullPath);
				} else
					return null;
			}

			[MessageHandler]
			public BinaryMessage Initialize (InitializeRequest msg)
			{
				msbuildBinDir = msg.BinDir;
				AppDomain.CurrentDomain.AssemblyResolve += MSBuildAssemblyResolver;
				return CreateBuildEngineAndRespondToInitialize(msg);
			}

			//Keep in seperate method so MSBuildAssemblyResolver is installed before BuildEngine is loaded
			[MethodImpl (MethodImplOptions.NoInlining)]
			BinaryMessage CreateBuildEngineAndRespondToInitialize (InitializeRequest msg)
			{
				var buildEngine = new BuildEngine (server);
				server.AddListener (buildEngine);
				server.RemoveListener (this);
				return buildEngine.Initialize (msg);
			}
		}
	}
}
