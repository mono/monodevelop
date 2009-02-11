// MD1CustomDataItem.cs
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
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects.Formats.MD1
{
	public class MD1CustomDataItem: ICustomDataItemHandler
	{
		public virtual DataCollection Serialize (object obj, ITypeSerializer handler)
		{
			if (obj is ProjectFile) {
				ProjectFile pf = (ProjectFile) obj;
				DataCollection data = handler.Serialize (obj);
				
				//Map the Content build action to the old FileCopy action if CopyToOutputDirectory is set
				if (pf.BuildAction == BuildAction.Content && pf.CopyToOutputDirectory != FileCopyMode.None) {
					DataValue value = data ["buildaction"] as DataValue;
					if (value != null) {
						data.Remove (value);
						data.Add (new DataValue ("buildaction", "FileCopy"));
						data.Extract ("copyToOutputDirectory");
					}
				}
				// Don't store the resource id if it matches the default.
				if (pf.BuildAction == BuildAction.EmbeddedResource && pf.ResourceId != Path.GetFileName (pf.FilePath))
					data.Add (new DataValue ("resource_id", pf.ResourceId));
				return data;
			}
			else if (obj is SolutionEntityItem) {
				DotNetProject project = obj as DotNetProject;
				if (project != null) {
					foreach (DotNetProjectConfiguration config in project.Configurations)
						config.ExtendedProperties ["Build/target"] = project.CompileTarget.ToString ();
				}
				DataCollection data = handler.Serialize (obj);
				SolutionEntityItem item = (SolutionEntityItem) obj;
				if (item.DefaultConfiguration != null) {
					DataItem confItem = data ["Configurations"] as DataItem;
					if (confItem != null) {
						confItem.UniqueNames = true;
						if (item.ParentSolution != null)
							confItem.ItemData.Add (new DataValue ("active", item.ParentSolution.DefaultConfigurationId));
					}
				}
				if (project != null) {
					data.Extract ("targetFramework");
					data.Add (new DataValue ("targetFramework", project.TargetFramework.Id));
				}
				WriteItems (handler, (SolutionEntityItem) obj, data);
				return data;
			}
			else if (obj is ProjectReference) {
				ProjectReference pref = (ProjectReference) obj;
				DataCollection data = handler.Serialize (obj);
				string refto = pref.Reference;
				if (pref.ReferenceType == ReferenceType.Assembly) {
					string basePath = Path.GetDirectoryName (handler.SerializationContext.BaseFile);
					refto = FileService.AbsoluteToRelativePath (basePath, refto);
				} else if (pref.ReferenceType == ReferenceType.Gac && pref.LoadedReference != null)
					refto = pref.LoadedReference;
	
				data.Add (new DataValue ("refto", refto));
				return data;
			}
			return handler.Serialize (obj);
		}
		
		void WriteItems (ITypeSerializer handler, SolutionEntityItem item, DataCollection data)
		{
			DataItem items = new DataItem ();
			items.Name = "Items";
			foreach (object it in item.Items) {
				if (it is ProjectFile || it is ProjectReference)
					continue; // Already handled by serializer
				DataNode node = handler.SerializationContext.Serializer.Serialize (it, it.GetType ());
				items.ItemData.Add (node);
			}
			if (items.HasItemData)
				data.Add (items);
		}
		
		public virtual void Deserialize (object obj, ITypeSerializer handler, DataCollection data)
		{
			if (obj is ProjectFile) {
				ProjectFile pf = (ProjectFile) obj;
				
				//Map the old FileCopy action to the Content BuildAction with CopyToOutputDirectory set to "always"
				//BuildActionDataType handles mapping the BuildAction to "Content"
				DataValue value = data ["buildaction"] as DataValue;
				bool isFileCopy = value != null && value.Value == "FileCopy";
				
				DataValue resourceId = data.Extract ("resource_id") as DataValue;
				
				handler.Deserialize (obj, data);
				if (isFileCopy)
					pf.CopyToOutputDirectory = FileCopyMode.Always;
				
				if (resourceId != null)
					pf.ResourceId = resourceId.Value;
			}
			else if (obj is SolutionEntityItem) {
				DataValue ac = null;
				DataItem confItem = data ["Configurations"] as DataItem;
				if (confItem != null)
					ac = (DataValue) confItem.ItemData.Extract ("active");
					
				DataItem items = data.Extract ("Items") as DataItem;
				if (items != null)
					ReadItems (handler, (SolutionEntityItem)obj, items);
				
				handler.Deserialize (obj, data);
				if (ac != null) {
					SolutionEntityItem item = (SolutionEntityItem) obj;
					item.DefaultConfigurationId = ac.Value;
				}
				DotNetProject np = obj as DotNetProject;
				if (np != null) {
					// Import the framework version
					string fx = null;
					DataValue vfx = data["targetFramework"] as DataValue;
					if (vfx != null)
						fx = vfx.Value;
					else {
						vfx = data ["clr-version"] as DataValue;
						if (vfx != null && vfx.Value == "Net_2_0")
							fx = "2.0";
						else if (vfx != null && vfx.Value == "Net_1_1")
							fx = "1.1";
					}
					if (!string.IsNullOrEmpty (fx))
						np.TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (fx);

					// Get the compile target from one of the configurations
					if (np.Configurations.Count > 0)
						np.CompileTarget = (CompileTarget) Enum.Parse (typeof(CompileTarget), (string) np.Configurations [0].ExtendedProperties ["Build/target"]);
				}
			}
			else if (obj is ProjectReference) {
				ProjectReference pref = (ProjectReference) obj;
				DataValue refto = data.Extract ("refto") as DataValue;
				handler.Deserialize (obj, data);
				if (refto != null) {
					pref.Reference = refto.Value;
					if (pref.ReferenceType == ReferenceType.Assembly) {
						string basePath = Path.GetDirectoryName (handler.SerializationContext.BaseFile);
						pref.Reference = FileService.RelativeToAbsolutePath (basePath, pref.Reference);
					}
				}
			} else
				handler.Deserialize (obj, data);
		}
		
		void ReadItems (ITypeSerializer handler, SolutionEntityItem item, DataItem items)
		{
			foreach (DataNode node in items.ItemData) {
				DataType dtype = handler.SerializationContext.Serializer.DataContext.GetConfigurationDataType (node.Name);
				if (dtype != null && typeof(ProjectItem).IsAssignableFrom (dtype.ValueType)) {
					ProjectItem it = (ProjectItem) handler.SerializationContext.Serializer.Deserialize (dtype.ValueType, node);
					item.Items.Add (it);
				}
			}
		}
	}
}
