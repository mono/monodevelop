// GenericProject.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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


using System.Xml;
using System.Collections.Generic;
using System.Collections.Immutable;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	[ProjectModelDataItem]
	public class GenericProject: Project
	{
		public GenericProject ()
		{
		}
		
		public GenericProject (ProjectCreateInformation info, XmlElement projectOptions)
		{
			Configurations.Add (CreateConfiguration ("Default"));
		}

		protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
		{
			Configurations.Add (CreateConfiguration ("Default"));
		}

		protected override SolutionItemConfiguration OnCreateConfiguration (string name, ConfigurationKind kind)
		{
			GenericProjectConfiguration conf = new GenericProjectConfiguration (name);
			return conf;
		}

		protected override void OnGetTypeTags (HashSet<string> types)
		{
			base.OnGetTypeTags (types);
			types.Add ("GenericProject");
		}

		protected override void OnWriteConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IPropertySet pset)
		{
			base.OnWriteConfiguration (monitor, config, pset);
			pset.SetValue ("OutputPath", config.OutputDirectory);
		}

		protected override ProjectFeatures OnGetSupportedFeatures ()
		{
			return ProjectFeatures.Build | ProjectFeatures.Configurations | ProjectFeatures.Execute;
		}
	}
	
	[ProjectModelDataItem]
	public class GenericProjectConfiguration: ProjectConfiguration
	{
		public GenericProjectConfiguration (string id): base (id)
		{
		}
	}
}
