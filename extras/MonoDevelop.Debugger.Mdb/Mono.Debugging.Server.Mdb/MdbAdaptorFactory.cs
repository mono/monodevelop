// 
// MdbAdaptorFactory.cs
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
using System.Reflection;
using System.Diagnostics;
using System.IO;

namespace DebuggerServer
{
	public static class MdbAdaptorFactory
	{
		// Bump this version number if any change is in MdbAdaptor or subclases
		const int ApiVersion = 2;
		
		static readonly string[] supportedVersions = new string[] {"2-6|2-4-2", "2-4-2", "2-0"};
		
		public static MdbAdaptor CreateAdaptor (string mdbVersion)
		{
			ProcessStartInfo pinfo = new ProcessStartInfo ();
			pinfo.FileName = "gmcs";
			
			if (mdbVersion != null) {
				MdbAdaptor mdb = TryCreateAdaptor (pinfo, mdbVersion);
				if (mdb == null)
					throw new InvalidOperationException ("Unsupported MDB version");
				return mdb;
			}
			
			foreach (string v in supportedVersions) {
				MdbAdaptor mdb = TryCreateAdaptor (pinfo, v);
				if (mdb != null)
					return mdb;
			}
			throw new InvalidOperationException ("Unsupported MDB version");
		}
		
		static MdbAdaptor TryCreateAdaptor (ProcessStartInfo pinfo, string versions)
		{
			string[] versionsArray = versions.Split ('|');
			string version = versionsArray [0];
			
			string tmpPath = Path.GetTempPath ();
			tmpPath = Path.Combine (tmpPath, "monodevelop-debugger-mdb");
			if (!Directory.Exists (tmpPath))
				Directory.CreateDirectory (tmpPath);
			
			string outFile = Path.Combine (tmpPath, "adaptor-" + ApiVersion + "--" + version + ".dll");
			DateTime thisTime = File.GetLastWriteTime (typeof(MdbAdaptorFactory).Assembly.Location);
			
			if (!File.Exists (outFile) || File.GetLastWriteTime (outFile) < thisTime) {
				string args = "/t:library ";
				args += "\"/out:" + outFile + "\" ";
				args += "\"/r:" + typeof(MdbAdaptorFactory).Assembly.Location + "\" ";
				args += "\"/r:" + typeof(Mono.Debugger.Debugger).Assembly.Location + "\" ";
				args += "\"/r:" + typeof(Mono.Debugging.Client.DebuggerSession).Assembly.Location + "\" ";
				args += "\"/r:" + typeof(Mono.Debugging.Backend.Mdb.IDebuggerServer).Assembly.Location + "\" ";
				
				// Write the source code for all required classes
				foreach (string ver in versionsArray) {
					Stream s = typeof(MdbAdaptorFactory).Assembly.GetManifestResourceStream ("MdbAdaptor-" + ver + ".cs");
					StreamReader sr = new StreamReader (s);
					string txt = sr.ReadToEnd ();
					sr.Close ();
					s.Close ();
					
					string csfile = Path.Combine (tmpPath, "adaptor-" + ver + ".cs");
					File.WriteAllText (csfile, txt);
					args += "\"" + csfile + "\" ";
				}
				
				pinfo.Arguments = args;
				Process proc = Process.Start (pinfo);
				proc.WaitForExit ();
				if (proc.ExitCode != 0)
					return null;
				Console.WriteLine ("Generated: " + outFile);
			}
			
			Assembly asm = Assembly.LoadFrom (outFile);
			Type at = asm.GetType ("DebuggerServer.MdbAdaptor_" + version.Replace ('-','_'));
			if (at != null) {
				MdbAdaptor a = (MdbAdaptor) Activator.CreateInstance (at);
				a.MdbVersion = version;
				return a;
			}
			return null;
		}
	}
}
