// 
// CompiledAssemblyProjectMSBuildHandler.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class CompiledAssemblyProjectMSBuildHandler: MSBuildProjectHandler
	{
		public override bool HasSlnData {
			get {
				return true;
			}
		}

		public override DataItem WriteSlnData ()
		{
			DataSerializer ser = new DataSerializer (MSBuildProjectService.DataContext);
			ser.SerializationContext.BaseFile = EntityItem.FileName;
			ser.SerializationContext.DirectorySeparatorChar = '\\';
			DataItem data = (DataItem) ser.Serialize (EntityItem, typeof(CompiledAssemblyProject));
			return data;
		}

		public override void ReadSlnData (DataItem item)
		{
			// Remove the default configuration, since new ones will be loaded
			CompiledAssemblyProject project = (CompiledAssemblyProject) EntityItem;
			project.Configurations.Clear ();
			
			DataSerializer ser = new DataSerializer (MSBuildProjectService.DataContext);
			ser.SerializationContext.BaseFile = EntityItem.FileName;
			ser.SerializationContext.DirectorySeparatorChar = '\\';
			ser.Deserialize (project, item);
		}

		protected override void SaveItem (IProgressMonitor monitor)
		{
			// Do nothing, since info is saved to the .sln
		}
	}
}

