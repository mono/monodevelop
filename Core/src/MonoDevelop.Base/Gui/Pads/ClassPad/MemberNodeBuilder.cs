//
// MemberNodeBuilder.cs
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

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Internal.Parser;

namespace MonoDevelop.Gui.Pads.ClassPad
{
	public abstract class MemberNodeBuilder: TypeNodeBuilder
	{
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((IMember)dataObject).Name;
		}
		
		public override Type CommandHandlerType {
			get { return typeof(MemberNodeCommandHandler); }
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (!(otherNode.DataItem is IMember)) return 1;

			if (thisNode.Options ["GroupByType"]) {
				int v1 = GetTypeSortValue (thisNode.DataItem);
				int v2 = GetTypeSortValue (otherNode.DataItem);
				if (v1 < v2) return -1;
				else if (v1 > v2) return 1;
			}
			if (thisNode.Options ["GroupByAccess"]) {
				int v1 = GetAccessSortValue (((IMember)thisNode.DataItem).Modifiers);
				int v2 = GetAccessSortValue (((IMember)otherNode.DataItem).Modifiers);
				if (v1 < v2) return -1;
				else if (v1 > v2) return 1;
			}
			return DefaultSort;
		}
		
		int GetTypeSortValue (object member)
		{
			if (member is IField) return 0;
			if (member is IEvent) return 1;
			if (member is IProperty) return 2;
			if (member is IMethod) return 3;
			return 4;
		}
		
		int GetAccessSortValue (ModifierEnum mods)
		{
			if ((mods & ModifierEnum.Private) != 0) return 0;
			if ((mods & ModifierEnum.Internal) != 0) return 1;
			if ((mods & ModifierEnum.Protected) != 0) return 2;
			if ((mods & ModifierEnum.Public) != 0) return 3;
			return 4;
		}
	}
}
