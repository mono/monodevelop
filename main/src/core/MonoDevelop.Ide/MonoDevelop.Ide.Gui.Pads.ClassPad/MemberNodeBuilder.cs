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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public abstract class MemberNodeBuilder : TypeNodeBuilder
	{
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ISymbol)dataObject).ToDisplayString (Ambience.NameFormat);
		}

		public override Type CommandHandlerType {
			get { return typeof (MemberNodeCommandHandler); }
		}

		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (!(otherNode.DataItem is ISymbol)) return 1;

			if (thisNode.Options ["GroupByType"]) {
				int v1 = GetTypeSortValue (thisNode.DataItem);
				int v2 = GetTypeSortValue (otherNode.DataItem);
				if (v1 < v2) return -1;
				else if (v1 > v2) return 1;
			}
			if (thisNode.Options ["GroupByAccess"]) {
				int v1 = GetAccessSortValue (((ISymbol)thisNode.DataItem).DeclaredAccessibility);
				int v2 = GetAccessSortValue (((ISymbol)otherNode.DataItem).DeclaredAccessibility);
				if (v1 < v2) return -1;
				else if (v1 > v2) return 1;
			}
			return DefaultSort;
		}

		int GetTypeSortValue (object member)
		{
			if (member is IFieldSymbol) return 0;
			if (member is IEventSymbol) return 1;
			if (member is IPropertySymbol) return 2;
			if (member is IMethodSymbol) return 3;
			return 4;
		}

		int GetAccessSortValue (Accessibility mods)
		{
			if ((mods & Accessibility.Private) != 0) return 0;
			if ((mods & Accessibility.Internal) != 0) return 1;
			if ((mods & Accessibility.Protected) != 0) return 2;
			if ((mods & Accessibility.Public) != 0) return 3;
			return 4;
		}
	}
}
