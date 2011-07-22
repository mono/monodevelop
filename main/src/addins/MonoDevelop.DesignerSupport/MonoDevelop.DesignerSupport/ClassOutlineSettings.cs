//
// ClassOutlineSortingProperties.cs
//
// Authors:
//  Helmut Duregger <helmutduregger@gmx.at>
//
// Copyright (c) 2010 Helmut Duregger
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Linq;

using MonoDevelop.Core;

namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Stores sorting status and is serialized to configuration properties.
	/// </summary>
	/// <remarks>
	/// Stores the sorting configuration, e.g. if the class outline is currently sorted
	/// or what primary sort key values the individual node groups have. This class is
	/// serialized to the configuration file MonoDevelopProperties.xml.
	/// </remarks>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtension"/>
	class ClassOutlineSettings
	{
		const string KEY_GROUP_ORDER = "MonoDevelop.DesignerSupport.ClassOutline.GroupOrder";
		const string KEY_IS_GROUPED = "MonoDevelop.DesignerSupport.ClassOutline.IsGrouped";
		const string KEY_IS_SORTED  = "MonoDevelop.DesignerSupport.ClassOutline.IsSorted";
		
		public const string GroupRegions = "Regions";
		public const string GroupNamespaces = "Namespaces";
		public const string GroupTypes = "Types";
		public const string GroupFields = "Fields";
		public const string GroupProperties = "Properties";
		public const string GroupEvents = "Events";
		public const string GroupMethods = "Methods";
		
		static Dictionary<string,string> groupNames = new Dictionary<string, string> {
			{ GroupRegions,    GettextCatalog.GetString ("Regions") },
			{ GroupNamespaces, GettextCatalog.GetString ("Namespaces") },
			{ GroupTypes,      GettextCatalog.GetString ("Types") },
			{ GroupProperties, GettextCatalog.GetString ("Properties") },
			{ GroupFields,     GettextCatalog.GetString ("Fields") },
			{ GroupEvents,     GettextCatalog.GetString ("Events") },
			{ GroupMethods,    GettextCatalog.GetString ("Methods") },
		};
		
		ClassOutlineSettings ()
		{
		}
		
		public static ClassOutlineSettings Load ()
		{
			var cs = new ClassOutlineSettings ();
			cs.IsGrouped = PropertyService.Get (KEY_IS_GROUPED, false);
			cs.IsSorted = PropertyService.Get (KEY_IS_SORTED, false);
			
			string s = PropertyService.Get (KEY_GROUP_ORDER, "");
			if (s.Length == 0) {
				cs.GroupOrder = groupNames.Keys.ToArray ();
			} else {
				cs.GroupOrder = s.Split (new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
			}
			return cs;
		}
		
		public void Save ()
		{
			PropertyService.Set (KEY_IS_GROUPED, IsGrouped);
			PropertyService.Set (KEY_IS_SORTED, IsSorted);
			PropertyService.Set (KEY_GROUP_ORDER, string.Join (",", GroupOrder));
		}
		
		public static string GetGroupName (string group)
		{
			return groupNames [group];
		}
		
		public IList<string> GroupOrder { get; set; }
		public bool IsGrouped { get; set; }
		public bool IsSorted { get; set; }
	}
}