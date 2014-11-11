// 
// ProjectItem.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects
{
	public class ProjectItem: IExtendedDataItem
	{
		Hashtable extendedProperties;

		public IDictionary ExtendedProperties {
			get {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
			}
		}

		public void SetMetadata (string name, string value, bool isXml = false)
		{
		}

		public string GetMetadata (string name, string defaultValue = null)
		{
			return null;
		}
		
		internal string Condition { get; set; }

		public string ItemName { get; protected set; }

		public virtual string Include { get; protected set; }

		public string UnevaluatedInclude { get; protected set; }

		public ProjectItemFlags Flags { get; set; }

		public bool IsHidden {
			get { return (Flags & ProjectItemFlags.Hidden) == ProjectItemFlags.Hidden; }
		}

		internal protected virtual void Read (Project project, IMSBuildItemEvaluated buildItem)
		{
			ItemName = buildItem.Name;
			Include = buildItem.Include;
			UnevaluatedInclude = buildItem.UnevaluatedInclude;
			Condition = buildItem.Condition;
		}

		internal protected virtual void Write (MSBuildFileFormat fmt, MSBuildItem buildItem)
		{
			buildItem.Condition = Condition;
		}
	}
	
	public class UnknownProjectItem: ProjectItem
	{
		public UnknownProjectItem (string name, string include)
		{
			this.ItemName = name;
			this.Include = include;
		}
	}

	[Flags]
	public enum ProjectItemFlags
	{
		None = 0,

		/// <summary>
		/// The item is for internal use and will not be shown to the user
		/// </summary>
		Hidden = 1,

		/// <summary>
		/// The item will not be saved to the project file
		/// </summary>
		DontPersist = 2
	}
}
