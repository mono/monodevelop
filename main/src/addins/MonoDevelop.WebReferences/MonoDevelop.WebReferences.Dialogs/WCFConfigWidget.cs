//
// WCFConfigWidget.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.WebReferences.WCF;
using System.ServiceModel.Description;
using Gtk;

namespace MonoDevelop.WebReferences.Dialogs
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class WCFConfigWidget : Gtk.Bin
	{
		public WCFConfigWidget (ClientOptions options)
		{
			this.Options = options;
			this.Build ();
			
			listTypes = new List<Type> (DefaultListTypes);
			PopulateBox (listCollection, "List", listTypes);
			
			dictTypes = new List<Type> (DefaultDictionaryTypes);
			PopulateBox (dictionaryCollection, "Dictionary", dictTypes);
		}
		
		public ClientOptions Options {
			get; private set;
		}
		
		static readonly Type[] DefaultListTypes = {
			typeof (Array),
			typeof (System.Collections.Generic.LinkedList<>),
			typeof (System.Collections.Generic.List<>),
			typeof (System.Collections.ObjectModel.Collection<>),
			typeof (System.Collections.ObjectModel.ObservableCollection<>),
			typeof (System.ComponentModel.BindingList<>)
		};
		
		static readonly Type[] DefaultDictionaryTypes = {
			typeof (System.Collections.Generic.Dictionary<,>),
			typeof (System.Collections.Generic.SortedList<,>),
			typeof (System.Collections.Generic.SortedDictionary<,>)
		};
		
		public bool Modified {
			get;
			private set;
		}
		
		List<Type> listTypes;
		List<Type> dictTypes;
		
		static bool? runtimeSupport;
		
		internal static bool IsSupported ()
		{
			if (runtimeSupport != null)
				return runtimeSupport.Value;
			
			try {
				// Test runtime support.
				var ms = new MetadataSet ();
				var importer = new WsdlImporter (ms);
				importer.State.GetType ();
				runtimeSupport = true;
				return true;
			} catch {
				runtimeSupport = false;
				return false;
			}
		}
		
		internal static bool IsSupported (WebReferenceItem item)
		{
			if (!IsSupported ())
				return false;
			return item.MapFile.FilePath.Extension == ".svcmap";
		}
		
		internal static Type GetType (string name)
		{
			var type = typeof (char).Assembly.GetType (name);
			if (type != null)
				return type;
			type = typeof (System.Collections.Generic.LinkedList<>).Assembly.GetType (name);
			if (type != null)
				return type;
			return null;
		}
		
		internal static string GetTypeName (Type type)
		{
			var name = type.FullName;
			var pos = name.IndexOf ("`");
			if (pos < 0)
				return name;
			return name.Substring (0, pos);
		}
		
		void PopulateBox (ComboBox box, string category, List<Type> types)
		{
			var mapping = Options.CollectionMappings.FirstOrDefault (m => m.Category == category);
			
			Type current = null;
			if (mapping != null)
				current = GetType (mapping.TypeName);
			if (current == null)
				current = types [0];
			
			if (!types.Contains (current))
				types.Add (current);
			
			foreach (var type in types)
				box.AppendText (GetTypeName (type));
			
			box.Active = types.IndexOf (current);
		}
		
		void UpdateBox (ComboBox box, string category, List<Type> types)
		{
			var mapping = Options.CollectionMappings.FirstOrDefault (m => m.Category == category);
			if (mapping == null) {
				mapping = new CollectionMapping { Category = category };
				Options.CollectionMappings.Add (mapping);
				Modified = true;
			}
			
			var current = types [box.Active];
			if (mapping.TypeName != current.FullName) {
				mapping.TypeName = current.FullName;
				Modified = true;
			}
		}
		
		internal void Update ()
		{
			UpdateBox (listCollection, "List", listTypes);
			UpdateBox (dictionaryCollection, "Dictionary", dictTypes);
		}
	}
}

