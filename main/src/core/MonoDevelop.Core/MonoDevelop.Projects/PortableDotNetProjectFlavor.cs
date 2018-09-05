//
// PortableDotNetProjectFlavor.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections.Generic;
using MonoDevelop.Core.Assemblies;
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	public class PortableDotNetProjectFlavor: DotNetProjectExtension
	{
		internal protected override void OnGetTypeTags (HashSet<string> types)
		{
			base.OnGetTypeTags (types);
			types.Add ("PortableDotNet");
		}

		internal protected override bool OnGetSupportsFormat (MSBuildFileFormat format)
		{
			int version;

			if (!format.Id.StartsWith ("MSBuild", StringComparison.Ordinal))
				return false;

			if (!int.TryParse (format.Id.Substring ("MSBuild".Length), out version))
				return false;

			return version >= 10;
		}

		internal protected override bool OnGetSupportsFramework (TargetFramework framework)
		{
			return framework.Id.Identifier == TargetFrameworkMoniker.ID_PORTABLE;
		}

		internal protected override TargetFrameworkMoniker OnGetDefaultTargetFrameworkForFormat (string toolsVersion)
		{
			// Note: This value is used only when serializing the TargetFramework to the .csproj file.
			// Any component of the TargetFramework that is different from this base TargetFramework
			// value will be serialized.
			//
			// Therefore, if we only specify the TargetFrameworkIdentifier, then both the
			// TargetFrameworkVersion and TargetFrameworkProfile values will be serialized.
			return new TargetFrameworkMoniker (".NETPortable", "1.0");
		}

		internal protected override TargetFrameworkMoniker OnGetDefaultTargetFrameworkId ()
		{
			// Profile78 includes .NET 4.5+, Windows Phone 8, and Xamarin.iOS/Android, so make that our default.
			// Note: see also: PortableLibrary.xpt.xml
			return new TargetFrameworkMoniker (".NETPortable", "4.5", "Profile78");
		}
	}
}

