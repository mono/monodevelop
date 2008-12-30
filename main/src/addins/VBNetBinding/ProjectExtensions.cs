/*
 * ProjectExtensions.cs.
 *
 * Author:
 *   Rolf Bjarne Kvinge <RKvinge@novell.com>
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */

using System;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;

namespace MonoDevelop.VBNetBinding.Extensions
{
	public static class Project
	{
		public static string GetOptionStrict (this DotNetProject project)
		{
			return GetValue (project, "OptionStrict");
		}

		public static void SetIsOptionStrict (this DotNetProject project, bool value)
		{
			SetValue (project, "OptionStrict", value ? "On" : "Off");
		}

		public static string GetOptionInfer (this DotNetProject project)
		{
			return GetValue (project, "OptionInfer");
		}

		public static void SetIsOptionInfer (this DotNetProject project, bool value)
		{
			SetValue (project, "OptionInfer", value ? "On" : "Off");
		}

		public static string GetOptionExplicit (this DotNetProject project)
		{
			return GetValue (project, "OptionExplicit");
		}

		public static void SetIsOptionExplicit (this DotNetProject project, bool value)
		{
			SetValue (project, "OptionExplicit", value ? "On" : "Off");
		}

		public static string GetOptionCompare (this DotNetProject project)
		{
			return GetValue (project, "OptionCompare");
		}

		public static void SetIsOptionCompareBinary (this DotNetProject project, bool value)
		{
			SetValue (project, "OptionCompare", value ? "Binary" : "Text");
		}

		public static string GetRootNamespace (this DotNetProject project)
		{
			return project.DefaultNamespace;
		}

		public static void SetRootNamespace (this DotNetProject project, string value)
		{
			project.DefaultNamespace = value;
		}

		public static string GetMainClass (this DotNetProject project)
		{
			return GetValue (project, "StartupObject");
		}

		public static void SetMainClass (this DotNetProject project, string value)
		{
			SetValue (project, "StartupObject", value);
		}

		public static string GetMyType (this DotNetProject project)
		{
			return GetValue (project, "MyType");
		}

		public static void SetMytype (this DotNetProject project, string value)
		{
			SetValue (project, "MyType", value);
		}

		public static string GetApplicationIconPath (this DotNetProject project)
		{
			return GetValue (project, "ApplicationIcon");
		}

		public static void SetApplicationIconPath (this DotNetProject project, string value)
		{
			SetValue (project, "ApplicationIcon", value);
		}

		// CodePage is MD-only
		public static string GetCodePage (this DotNetProject project)
		{
			return GetValue (project, "CodePage");
		}

		public static void SetCodePage (this DotNetProject project, string value)
		{
			SetValue (project, "CodePage", value);
		}
		
		private static string GetValue (this DotNetProject project, string name)
		{
			object value = null;
			
			if (project.ExtendedProperties.Contains (name))
				value = project.ExtendedProperties [name];

			if (value != null && value is string)
				return (string) value;
			
			return null;
		}

		private static void SetValue (this DotNetProject project, string name, string value)
		{
			project.ExtendedProperties [name] = value;
		}
	}
}
