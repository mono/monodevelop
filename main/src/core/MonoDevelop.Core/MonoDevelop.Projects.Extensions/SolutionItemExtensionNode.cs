//
// SolutionItemExtensionNode.cs
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
using Mono.Addins;
using System.Linq;

namespace MonoDevelop.Projects.Extensions
{
	[ExtensionNode (ExtensionAttributeType = typeof(ExportProjectFlavorAttribute))]
	public class SolutionItemExtensionNode: ProjectModelExtensionNode
	{
		[NodeAttribute (Description = "GUID of the extension. The extension will be loaded if the project has this GUID in the project type GUID list. " +
			"If not specified, the extension will be applied to all projects.")]
		string guid;

		public string Guid {
			get { return guid; }
			set { guid = value; }
		}

		[NodeAttribute]
		bool migrationRequired = true;

		[NodeAttribute]
		string migrationHandler;

		[NodeAttribute ("msbuildSupport")]
		public MSBuildSupport MSBuildSupport { get; set; }

		ProjectMigrationHandler handler;

		[NodeAttribute ("alias", Description = "Friendly id of the extension")]
		public string TypeAlias { get; set; }

		public SolutionItemExtensionNode ()
		{
			MSBuildSupport = MSBuildSupport.Supported;
		}

		public bool IsMigrationRequired {
			get { return migrationRequired; }
			set { migrationRequired = value; }
		}

		public bool SupportsMigration {
			get { return !string.IsNullOrEmpty (migrationHandler) || handler != null; }
		}

		public ProjectMigrationHandler MigrationHandler {
			get {
				if (handler == null)
					handler = (ProjectMigrationHandler) Addin.CreateInstance (migrationHandler); 
				return handler;
			}
			set {
				handler = value;
			}
		}

		public override bool CanHandleObject (object ob)
		{
			SolutionItem p = ob as SolutionItem;
			if (p == null)
				return false;

			if (guid == null)
				return true;

			var typeGuids = p.GetItemTypeGuids ();
			return typeGuids.Any (g => string.Equals (g, guid, StringComparison.InvariantCultureIgnoreCase));
		}

		public override WorkspaceObjectExtension CreateExtension ()
		{
			var ext = (SolutionItemExtension) CreateInstance (typeof(SolutionItemExtension));
			ext.FlavorGuid = Guid;
			ext.TypeAlias = TypeAlias;
			return ext;
		}
	}
}

