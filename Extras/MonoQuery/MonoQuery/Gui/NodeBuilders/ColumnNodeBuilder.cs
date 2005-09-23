//
// ColumnNodeBuilder.cs
//
// Authors:
//   Christian Hergert <chris@mosaix.net>
//
// Copyright (c) 2005 Christian Hergert
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text.RegularExpressions;

using Mono.Data.Sql;
using MonoDevelop.Services;
using MonoDevelop.Gui.Pads;

namespace MonoQuery
{
	public class ColumnNodeBuilder : TypeNodeBuilder
	{
		public ColumnNodeBuilder()
		{
		}
		
		public override Type NodeDataType {
			get {
				return typeof(ColumnSchema);
			}
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return GettextCatalog.GetString ("Column");
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			ColumnSchema schema = (ColumnSchema) dataObject;
			label = schema.Name;
			string iconName = "md-mono-query-column";
			
			string typeName = schema.DataTypeName.ToLower ();
			
			if (Regex.IsMatch (typeName, "(timestamp|datetime)"))
				iconName = "md-mono-query-date-time";
			else if (Regex.IsMatch (typeName, "(bytea|blob)"))
				iconName = "md-mono-query-blob";
			else if (Regex.IsMatch (typeName, "numeric"))
				iconName = "md-mono-query-numeric";
			else if (Regex.IsMatch (typeName, "(varchar|character var|char)"))
				iconName = "md-mono-query-string";
			
			icon = Context.GetIcon (iconName);
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
		}
		
		public static void BuildChildNodes (ITreeBuilder builder, ColumnSchema node)
		{
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}
	}
}