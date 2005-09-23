// VBNetVisitor.cs
// Copyright (C) 2003 Mike Krueger (mike@icsharpcode.net)
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.Reflection;
using System.CodeDom;
using System.Text;
using System.Collections;


using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.PrettyPrinter
{
	public class VBNetRefactory
	{
		Hashtable namespaces;
		
		public void Refactor(CompilationUnit compilationUnit)
		{
			namespaces = new Hashtable();
			for (int i = 0; i < compilationUnit.Children.Count; ++i) {
				object o = compilationUnit.Children[i];
				if (o is UsingDeclaration ) {
					namespaces[((UsingDeclaration )o).Namespace] = "";
				} else if (o is NamespaceDeclaration) {
					Refactor(compilationUnit, (NamespaceDeclaration)o);
				}
			}
		}
		
		void Refactor(CompilationUnit compilationUnit, NamespaceDeclaration nsd)
		{
			for (int i = 0; i < nsd.Children.Count; ++i) {
				object o = nsd.Children[i];
				if (o is UsingDeclaration) {
					UsingDeclaration  ud = (UsingDeclaration) o;
					if (namespaces[ud.Namespace] == null) {
						namespaces[ud.Namespace] = "";
						compilationUnit.Children.Insert(0, ud);
					}
					nsd.Children.RemoveAt(i);
					i = -1;
				} else if (o is UsingAliasDeclaration) {
					compilationUnit.Children.Insert(0, o);
					nsd.Children.RemoveAt(i);
					i = -1;
				}
			}
		}
	}
}

