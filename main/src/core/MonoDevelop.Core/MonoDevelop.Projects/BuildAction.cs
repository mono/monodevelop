// 
// BuildAction.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Projects
{
	
	public static class BuildAction
	{
		public const string None = "None"; //Nothing
		public const string Compile = "Compile";
		public const string EmbeddedResource = "EmbeddedResource"; //EmbedAsResource, "Embed as resource"
		public const string Content = "Content"; //Exclude
		public const string ApplicationDefinition = "ApplicationDefinition";
		public const string Page = "Page";
		public const string InterfaceDefinition = "InterfaceDefinition";
		public const string BundleResource = "BundleResource";
		public const string AtlasResource = "AtlasResource";
		public const string Resource = "Resource";
		public const string SplashScreen = "SplashScreen";
		public const string EntityDeploy = "EntityDeploy";
		
		public static string[] StandardActions {
			get {
				return new string[] {
					None,
					Compile,
				};
			}
		}
		
		public static string[] DotNetCommonActions {
			get {
				return new string[] {
					None,
					Compile,
					EmbeddedResource,
				};
			}
		}
		
		public static string[] DotNetActions {
			get {
				return new string[] {
					None,
					Compile,
					Content,
					EmbeddedResource,
				};
			}
		}
	}
}
