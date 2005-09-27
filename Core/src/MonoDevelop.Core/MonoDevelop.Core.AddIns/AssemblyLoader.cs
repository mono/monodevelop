//
// AssemblyLoader.cs
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
using System.Collections;
using System.Reflection;
using System.IO;
using Mono.Cecil;

namespace MonoDevelop.Core.AddIns
{	
	class AssemblyLoader: MarshalByRefObject
	{
		Hashtable assemblies = new Hashtable ();
		
		public Assembly LoadAssembly (string fileName)
		{
			Assembly asm = null;
			if (File.Exists (fileName)) {
				CheckAssemblyFile (fileName);
				asm = Assembly.LoadFrom (fileName);
			}
			CheckAssembly (fileName);
			if (asm == null) {
				asm = Assembly.Load(fileName);
			}
			if (asm == null) {
				asm = Assembly.LoadWithPartialName(fileName);
			}
			return asm;
		}
		
		public void CheckAssembly (Assembly asm)
		{
			CheckAssemblyFile (asm.Location);
		}
		
		public void CheckAssembly (string aname)
		{
			CheckAssemblyVersion (aname, null, Environment.CurrentDirectory);
		}
		
		public void CheckAssemblyFile (string assemblyFile)
		{
			IAssemblyDefinition asm = AssemblyFactory.GetAssembly (assemblyFile);
			CheckAssemblyVersion (asm.Name.FullName, asm, Path.GetDirectoryName (assemblyFile));
		}
		
		void CheckAssemblyVersion (string aname, IAssemblyDefinition asm, string baseDirectory)
		{
			int i = aname.IndexOf (",");
			if (i == -1) return;
			
			string name = aname.Substring (0, i).Trim ();
			if (IsSystemAssembly (name))
				return;
				
			string loadedVersion = (string) assemblies [name];

			if (loadedVersion != null) {
				if (loadedVersion != aname)
					throw new InvalidAssemblyVersionException (loadedVersion, aname);
				return;
			}
			
			assemblies [name] = aname;
			
			if (asm == null) {
				string file = FindAssembly (aname, baseDirectory);
				
				if (file == null && baseDirectory != AppDomain.CurrentDomain.BaseDirectory)
					file = FindAssembly (aname, AppDomain.CurrentDomain.BaseDirectory);
					
				if (file == null)
					return;
					
				asm = AssemblyFactory.GetAssembly (file);
				baseDirectory = Path.GetDirectoryName (file);
			}
			
			try {
				foreach (IAssemblyNameReference ar in asm.MainModule.AssemblyReferences) {
					CheckAssemblyVersion (ar.FullName, null, baseDirectory);
				}
			} catch {
				assemblies.Remove (name);
				throw;
			}
		}
		
		string FindAssembly (string aname, string baseDirectory)
		{
			string name = aname.Substring (0, aname.IndexOf (",")).Trim ();
			string file = Path.Combine (baseDirectory, name + ".dll");
			
			if (File.Exists (file))
				return file;
				
			file = Path.Combine (baseDirectory, name + ".exe");
			if (File.Exists (file))
				return file;
			
			// Look for the assembly in the GAC.
			// WARNING: this is a hack, but there isn't right now a better
			// way of doing it
			
			string gacDir = typeof(Uri).Assembly.Location;
			gacDir = Path.GetDirectoryName (gacDir);
			gacDir = Path.GetDirectoryName (gacDir);
			gacDir = Path.GetDirectoryName (gacDir);
			
			string[] parts = aname.Split (',');
			if (parts.Length != 4) return null;
			name = parts[0].Trim ();
			
			int i = parts[1].IndexOf ('=');
			string version = i != -1 ? parts[1].Substring (i+1).Trim () : parts[1].Trim ();
			
			i = parts[2].IndexOf ('=');
			string culture = i != -1 ? parts[2].Substring (i+1).Trim () : parts[2].Trim ();
			if (culture == "neutral") culture = "";
			
			i = parts[3].IndexOf ('=');
			string token = i != -1 ? parts[3].Substring (i+1).Trim () : parts[3].Trim ();
			
			file = Path.Combine (gacDir, name);
			file = Path.Combine (file, version + "_" + culture + "_" + token);
			file = Path.Combine (file, name + ".dll");
			
			if (File.Exists (file))
				return file;
			else
				return null;
		}
		
		bool IsSystemAssembly (string aname)
		{
			return Array.IndexOf (systemAssemblies, aname) != -1;
		}
		
		// Those assemblies are automatically remapped by the runtime
		
		string[] systemAssemblies = new string[] {
			"Accessibility",
			"Commons.Xml.Relaxng",
			"I18N",
			"I18N.CJK",
			"I18N.MidEast",
			"I18N.Other",
			"I18N.Rare",
			"I18N.West",
			"Microsoft.VisualBasic",
			"Microsoft.VisualC",
			"Mono.Cairo",
			"Mono.CompilerServices.SymbolWriter",
			"Mono.Data",
			"Mono.Data.SqliteClient",
			"Mono.Data.SybaseClient",
			"Mono.Data.Tds",
			"Mono.Data.TdsClient",
			"Mono.GetOptions",
			"Mono.Http",
			"Mono.Posix",
			"Mono.Security",
			"Mono.Security.Win32",
			"Mono.Xml.Ext",
			"Novell.Directory.Ldap",
			"Npgsql",
			"PEAPI",
			"System",
			"System.Configuration.Install",
			"System.Data",
			"System.Data.OracleClient",
			"System.Data.SqlXml",
			"System.Design",
			"System.DirectoryServices",
			"System.Drawing",
			"System.Drawing.Design",
			"System.EnterpriseServices",
			"System.Management",
			"System.Messaging",
			"System.Runtime.Remoting",
			"System.Runtime.Serialization.Formatters.Soap",
			"System.Security",
			"System.ServiceProcess",
			"System.Web",
			"System.Web.Mobile",
			"System.Web.Services",
			"System.Windows.Forms",
			"System.Xml",
			"mscorlib"
		};
	}
	
	[Serializable]
	public class InvalidAssemblyVersionException: Exception
	{
		string msg;
		
		public InvalidAssemblyVersionException (string old, string anew)
		{
			msg = "An assembly version conflict has been detected. ";
			msg += "The assembly '" + anew + "' has been requested, but ";
			msg += "a different version of this assembly is already loaded: '" + old + "'.";
		}
		
		public override string Message {
			get { return msg; }
		}
	}	
}
