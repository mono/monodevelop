// 
// ExtensionModelTypeNodeBuilder.cs
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
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring.NodeBuilders
{
	public abstract class ExtensionModelTypeNodeBuilder: TypeNodeBuilder
	{
		public AddinRegistry GetRegistry (ITreeNavigator nav)
		{
			Solution sol = (Solution) nav.GetParentDataItem (typeof(Solution), true);
			if (sol != null)
				return sol.GetAddinData ().Registry;
			RegistryInfo reg = (RegistryInfo) nav.GetParentDataItem (typeof(RegistryInfo), true);
			if (reg != null)
				return reg.CachedRegistry;
			return null;
		}
		
		public AddinData GetAddinData (ITreeNavigator nav)
		{
			DotNetProject p = (DotNetProject) nav.GetParentDataItem (typeof(DotNetProject), true);
			if (p == null)
				return null;
			return p.GetAddinData ();
		}
		
		public CachedModelData GetCachedModelData (ITreeNavigator nav)
		{
			Project p = (Project) nav.GetParentDataItem (typeof(Project), true);
			if (p == null)
				return new CachedModelData ();
			CachedModelData data = (CachedModelData) p.ExtendedProperties [typeof(CachedModelData)];
			if (data == null) {
				data = new CachedModelData ();
				p.ExtendedProperties [typeof(CachedModelData)] = data;
			}
			return data;
		}
	}
	
	public class CachedModelData
	{
	}
}

