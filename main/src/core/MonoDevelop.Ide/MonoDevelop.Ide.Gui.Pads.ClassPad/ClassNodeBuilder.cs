//
// ClassNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Text;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public class ClassNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ClassData); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ClassNodeCommandHandler); }
		}

		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/Class"; }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ClassData)dataObject).Class.FullName;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			ClassData classData = dataObject as ClassData;
			nodeInfo.Label = AmbienceService.DefaultAmbience.GetString (classData.Class.GetDefinition (), OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup);
			nodeInfo.Icon = Context.GetIcon (classData.Class.GetStockIcon ());
		}
		/*
		private string GetNameWithGenericParameters (IType c)
		{
			if (c.TypeParameters != null && c.TypeParameters.Count > 0)
			{
				StringBuilder builder = new StringBuilder (c.Name);
				builder.Append("&lt;");
				for (int i = 0; i < c.TypeParameters.Count; i++)
				{
					builder.Append(c.TypeParameters[i].Name);
					if (i + 1 < c.TypeParameters.Count) builder.Append(", ");
				}
				builder.Append("&gt;");
				return builder.ToString();
			}
			else
				return c.Name;
		}*/

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			ClassData classData = dataObject as ClassData;
			bool publicOnly = builder.Options ["PublicApiOnly"];
			bool publicProtectedOnly = builder.Options ["PublicProtectedApiOnly"];
			publicOnly |= publicProtectedOnly;
			
			// Delegates have an Invoke method, which doesn't need to be shown.
			if (classData.Class.Kind == TypeKind.Delegate)
				return;

			foreach (var innerClass in classData.Class.NestedTypes.Where (m => !m.IsSynthetic))
				if (innerClass.IsPublic || (innerClass.IsProtected && publicProtectedOnly) || !publicOnly)
					builder.AddChild (new ClassData (classData.Project, innerClass));

			foreach (var method in classData.Class.Methods.Where (m => !m.IsSynthetic)) {
				if (method.IsPublic || (method.IsProtected && publicProtectedOnly) || !publicOnly)
					builder.AddChild (method);
			}
			
			foreach (var property in classData.Class.Properties.Where (m => !m.IsSynthetic))
				if (property.IsPublic || (property.IsProtected && publicProtectedOnly) || !publicOnly)
					builder.AddChild (property);
			
			foreach (var field in classData.Class.Fields.Where (m => !m.IsSynthetic))
				if (field.IsPublic || (field.IsProtected && publicProtectedOnly) || !publicOnly)
					builder.AddChild (field);
			
			foreach (var e in classData.Class.Events.Where (m => !m.IsSynthetic))
				if (e.IsPublic || (e.IsProtected && publicProtectedOnly) || !publicOnly)
					builder.AddChild (e);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			// Checking if a class has member is expensive since it requires loading the whole
			// info from the db, so we always return true here. After all 99% of classes will have members
			return true;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (thisNode.DataItem is ClassData)
				return DefaultSort;
			else
				return 1;
		}
	}
	
	public class ClassNodeCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			ClassData cls = CurrentNode.DataItem as ClassData;
			IdeApp.ProjectOperations.JumpToDeclaration (cls.Class, true);
		}
	}	
}
