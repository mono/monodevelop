//
// AssemblyReference.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public sealed class AssemblyReference
	{
		IReadOnlyPropertySet metadata;

		public AssemblyReference (FilePath path, string aliases = null)
		{
			FilePath = path;

			var properties = new MSBuildPropertyGroupEvaluated (null);
			if (aliases != null) {
				var property = new MSBuildPropertyEvaluated (null, nameof (Aliases), aliases, aliases);
				properties.SetProperty (nameof (Aliases), property);
			}
			metadata = properties;
		}

		public AssemblyReference (FilePath path, IReadOnlyPropertySet metadata)
		{
			FilePath = path;
			this.metadata = metadata;
		}

		public FilePath FilePath { get; private set; }
		public string Aliases => metadata.GetValue ("Aliases", "");

		/// <summary>
		/// Whether the reference is a project.
		/// </summary>
		public bool IsProjectReference => GetMetadata ("ReferenceSourceTarget") == "ProjectReference";

		/// <summary>
		/// For project references, true if the output assembly should be referenced.
		/// </summary>
		public bool ReferenceOutputAssembly => MetadataIsTrue ("ReferenceOutputAssembly");

		/// <summary>
		/// True if the assembly reference was added implicitly].
		/// </summary>
		public bool IsImplicit => MetadataIsTrue ("Implicit");

		/// <summary>
		/// True if the assembly will be copied to the output directory.
		/// </summary>
		public bool IsCopyLocal => MetadataIsTrue ("CopyLocal");

		/// <summary>
		/// True if the assembly is from the target framework.
		/// </summary>
		public bool IsFrameworkFile => MetadataIsTrue ("FrameworkFile");

		public IReadOnlyPropertySet Metadata {
			get { return metadata; }
		}

		string GetMetadata (string name)
		{
			return metadata.GetValue (name);
		}

		bool MetadataIsTrue (string name)
		{
			return metadata.GetValue (name, false);
		}

		public override bool Equals (object obj)
		{
			var ar = obj as AssemblyReference;
			return ar != null && ar.FilePath == FilePath && ar.Aliases == Aliases;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return FilePath.GetHashCode () ^ Aliases.GetHashCode ();
			}
		}

		/// <summary>
		/// Returns an enumerable collection of aliases. 
		/// </summary>
		public IEnumerable<string> EnumerateAliases ()
		{
			return Aliases.Split (new [] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
		}

		public SolutionItem GetReferencedItem (Solution parentSolution)
		{
			var projectPath = GetMetadata ("MSBuildSourceProjectFile");
			if (!string.IsNullOrEmpty (projectPath)) {
				var project = parentSolution.FindSolutionItem (projectPath);
				if (project != null) {
					return project;
				}
			}

			var projectGuid = GetMetadata ("Project");
			if (!string.IsNullOrEmpty (projectGuid)) {
				if (parentSolution.GetSolutionItem (projectGuid) is SolutionItem item) {
					return item;
				}
			}

			return null;
		}
	}
}