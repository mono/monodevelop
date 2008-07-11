//
// GtkCoreService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.GtkCore
{
	public class GtkCoreService
	{
		static string[] supportedGtkVersions;
		static string defaultGtkVersion;
		
		internal static void Initialize ()
		{
			Runtime.SystemAssemblyService.PackagesChanged += delegate {
				supportedGtkVersions = null;
			};
		}
		
		public static string[] SupportedGtkVersions {
			get {
				FindSupportedGtkVersions ();
				return supportedGtkVersions;
			}
		}
		
		public static string DefaultGtkVersion {
			get {
				FindSupportedGtkVersions ();
				return defaultGtkVersion; 
			}
		}
		
		static void FindSupportedGtkVersions ()
		{
			if (supportedGtkVersions == null) {
				List<string> versions = new List<string> ();
				foreach (SystemPackage p in Runtime.SystemAssemblyService.GetPackages ()) {
					if (p.Name == "gtk-sharp-2.0") {
						versions.Add (p.Version);
						if (p.Version.StartsWith ("2.8"))
							defaultGtkVersion = p.Version;
					}
				}
				versions.Sort ();
				supportedGtkVersions = versions.ToArray ();
				if (defaultGtkVersion == null && supportedGtkVersions.Length > 0)
					defaultGtkVersion = supportedGtkVersions [0];
			}
		}
	}
	
	class GtkCoreStartupCommand: CommandHandler
	{
		protected override void Run()
		{
			GtkCoreService.Initialize ();
		}
	}
}
