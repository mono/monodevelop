// MSBuildProjectHandler.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects.Formats.MD1;
using MonoDevelop.Projects.Extensions;
using Mono.Addins;
using System.Linq;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.ProgressMonitoring;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class MSBuildSerializer: DataSerializer
	{
		public DataItem InternalItemProperties = new DataItem ();
		public DataItem ExternalItemProperties = new DataItem ();
		
		public MSBuildSerializer (string baseFile): base (MSBuildProjectService.DataContext)
		{
			// Use windows separators
			SerializationContext.BaseFile = baseFile;
			SerializationContext.DirectorySeparatorChar = '\\';
		}
		
		internal protected override bool CanHandleProperty (ItemProperty prop, SerializationContext serCtx, object instance)
		{
			if (instance is Project) {
				if (prop.Name == "Contents")
					return false;
			}
			if (instance is DotNetProject) {
				if (prop.Name == "References" || prop.Name == "LanguageParameters")
					return false;
			}
			if (instance is SolutionItem) {
				if (prop.IsExtendedProperty (typeof(SolutionItem)))
					return true;
				return prop.Name != "name" && prop.Name != "Configurations";
			}
			if (instance is SolutionFolder) {
				if (prop.Name == "Files")
					return false;
			}
			if (instance is ProjectFile)
				return prop.IsExtendedProperty (typeof(ProjectFile));
			if (instance is ProjectReference)
				return prop.IsExtendedProperty (typeof(ProjectReference)) || prop.Name == "Package" || prop.Name == "Aliases";
			if (instance is DotNetProjectConfiguration)
				if (prop.Name == "CodeGeneration")
					return false;
			if (instance is ItemConfiguration)
				if (prop.Name == "name")
					return false;
			return true;
		}
		
		internal protected override DataNode OnSerializeProperty (ItemProperty prop, SerializationContext serCtx, object instance, object value)
		{
			DataNode data = base.OnSerializeProperty (prop, serCtx, instance, value);
			if (instance is SolutionItem && data != null) {
				if (prop.IsExternal)
					ExternalItemProperties.ItemData.Add (data);
				else
					InternalItemProperties.ItemData.Add (data);
			}
			return data;
		}
	}
	
	class MSBuildElementOrder: Dictionary<string, int>
	{
		public MSBuildElementOrder (params string[] elements)
		{
			for (int n=0; n<elements.Length; n++)
				this [elements [n]] = n;
		}
	}
}
