// 
// PortableDotNetProject.cs
//  
// Author: Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	public class PortableDotNetProject : DotNetProject
	{
		public PortableDotNetProject ()
		{
		}
		
		public PortableDotNetProject (string languageName) : base (languageName)
		{
		}
		
		public PortableDotNetProject (string languageName, ProjectCreateInformation projectCreateInfo, XmlElement projectOptions)
			: base (languageName, projectCreateInfo, projectOptions)
		{
		}

		public override IEnumerable<string> GetProjectTypes ()
		{
			yield return "PortableDotNet";
			foreach (var t in base.GetProjectTypes ())
				yield return t;
		}
		
		public override bool SupportsFormat (FileFormat format)
		{
			int version;
			
			if (!format.Id.StartsWith ("MSBuild", StringComparison.Ordinal))
				return false;
			
			if (!int.TryParse (format.Id.Substring ("MSBuild".Length), out version))
				return false;
			
			return version >= 10;
		}
		
		public override bool SupportsFramework (TargetFramework framework)
		{
			return framework.Id.Identifier == TargetFrameworkMoniker.ID_PORTABLE;
		}
		
		public override TargetFrameworkMoniker GetDefaultTargetFrameworkForFormat (FileFormat format)
		{
			// Note: This value is used only when serializing the TargetFramework to the .csproj file.
			// Any component of the TargetFramework that is different from this base TargetFramework
			// value will be serialized.
			//
			// Therefore, if we only specify the TargetFrameworkIdentifier, then both the
			// TargetFrameworkVersion and TargetFrameworkProfile values will be serialized.
			return new TargetFrameworkMoniker (".NETPortable", "1.0");
		}
		
		public override TargetFrameworkMoniker GetDefaultTargetFrameworkId ()
		{
			// Profile78 includes .NET 4.5+, Windows Phone 8, and Xamarin.iOS/Android, so make that our default.
			// Note: see also: PortableLibrary.xpt.xml
			return new TargetFrameworkMoniker (".NETPortable", "4.5", "Profile78");
		}

		protected internal override IEnumerable<string> OnGetReferencedAssemblies (ConfigurationSelector configuration, bool includeProjectReferences)
		{
			var res = base.OnGetReferencedAssemblies (configuration, includeProjectReferences);
			var asms = TargetRuntime.AssemblyContext.GetAssemblies (TargetFramework).Where (a => a.Package.IsFrameworkPackage).Select (a => a.Location);
			return res.Concat (asms).Distinct ();
		}

		protected internal override bool OnGetSupportsExecute ()
		{
			return false;
		}
	}
}
