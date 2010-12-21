// 
// MD24Workarounds.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core;
using System.Collections.Generic;
using System.IO;

namespace MonoDevelop.MonoDroid
{
	class MD24Workarounds
	{
		static bool fixedBuilders;

		// MD 2.4.1 MSI on windows did not include the MSBuild config file so can't create MSBuild builders
		// so we duplicate the builder creation code and the config and create the builder before we need it
		public static void FixBuilders ()
		{
			if (fixedBuilders)
				return;
			fixedBuilders = true;
			if (!PropertyService.IsWindows)
				return ;
			foreach (var version in new string[] { "2.0", "3.5", "4.0" } )
				GetExeLocation (version);
		}

		static string GetExeLocation (string toolsVersion)
		{
			FilePath sourceExe = typeof(MonoDevelop.Ide.IdeApp).Assembly.Location;
			sourceExe = sourceExe.ParentDirectory.Combine ("MonoDevelop.Projects.Formats.MSBuild.exe");
			
			var newVersions = new Dictionary<string, string[]> ();
			string version;
			Mono.Cecil.TargetRuntime runtime;
			
			switch (toolsVersion) {
			case "2.0":
				version = "2.0.0.0";
				newVersions.Add ("Microsoft.Build.Engine", new string[] {"Microsoft.Build.Engine", version});
				newVersions.Add ("Microsoft.Build.Framework", new string[] {"Microsoft.Build.Framework", version});
				newVersions.Add ("Microsoft.Build.Utilities", new string[] {"Microsoft.Build.Utilities", version});
				runtime = Mono.Cecil.TargetRuntime.NET_2_0;
				break;
			case "3.5":
				version = "3.5.0.0";
				newVersions.Add ("Microsoft.Build.Engine", new string[] {"Microsoft.Build.Engine", version});
				newVersions.Add ("Microsoft.Build.Framework", new string[] {"Microsoft.Build.Framework", version});
				newVersions.Add ("Microsoft.Build.Utilities", new string[] {"Microsoft.Build.Utilities.v3.5", version});
				runtime = Mono.Cecil.TargetRuntime.NET_2_0;
				break;
			case "4.0":
				version = "4.0.0.0";
				newVersions.Add ("Microsoft.Build.Engine", new string[] {"Microsoft.Build.Engine", version});
				newVersions.Add ("Microsoft.Build.Framework", new string[] {"Microsoft.Build.Framework", version});
				newVersions.Add ("Microsoft.Build.Utilities", new string[] {"Microsoft.Build.Utilities.v4.0", version});
				runtime = Mono.Cecil.TargetRuntime.NET_4_0;
				break;
			default:
				throw new InvalidOperationException ("Unknown MSBuild ToolsVersion '" + toolsVersion + "'");
			}
			
			FilePath p = FilePath.Build (PropertyService.ConfigPath, "xbuild", toolsVersion, "MonoDevelop.Projects.Formats.MSBuild.exe");
			if (!File.Exists (p) || File.GetLastWriteTime (p) < File.GetLastWriteTime (sourceExe)) {
				if (!Directory.Exists (p.ParentDirectory))
					Directory.CreateDirectory (p.ParentDirectory);
				
				// Update the references to msbuild
				Mono.Cecil.AssemblyDefinition asm = Mono.Cecil.AssemblyFactory.GetAssembly (sourceExe);
				foreach (Mono.Cecil.AssemblyNameReference ar in asm.MainModule.AssemblyReferences) {
					string[] replacement;
					if (newVersions.TryGetValue (ar.Name, out replacement)) {
						ar.Name = replacement[0];
						ar.Version = new Version (replacement[1]);
					}
				}
				asm.Runtime = runtime;
				
				//run in 32-bit mode because usually msbuild targets are installed for 32-bit only
				asm.MainModule.Image.CLIHeader.Flags |= Mono.Cecil.Binary.RuntimeImage.F32BitsRequired;
				
				// Workaround to a bug in mcs. The ILOnly flag is not emitted when using /platform:x86
				asm.MainModule.Image.CLIHeader.Flags |= Mono.Cecil.Binary.RuntimeImage.ILOnly;
				
				Mono.Cecil.AssemblyFactory.SaveAssembly (asm, p);
			}
			
			FilePath configFile = p + ".config";
			if (!File.Exists (configFile)) {
				var config = configSrcText.Replace ("@@VERSION@@", version);
				File.WriteAllText (p + ".config", config);
			}
			return p;
		}
		
		const string configSrcText =
@"<configuration>
        <runtime>
                <generatePublisherEvidence enabled=""false"" />
                <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
                        <dependentAssembly>
                                <assemblyIdentity name=""Microsoft.Build.Framework"" publicKeyToken=""b03f5f7f11d50a3a"" culture=""neutral"" />
                                <bindingRedirect oldVersion=""0.0.0.0-100.0.0.0"" newVersion=""@@VERSION@@"" />
                        </dependentAssembly>
                        <dependentAssembly>
                                <assemblyIdentity name=""Microsoft.Build.Engine"" publicKeyToken=""b03f5f7f11d50a3a"" culture=""neutral"" />
                                <bindingRedirect oldVersion=""0.0.0.0-100.0.0.0"" newVersion=""@@VERSION@@"" />
                        </dependentAssembly>
                </assemblyBinding>
        </runtime>
</configuration>";
	}
}

