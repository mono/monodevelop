// CompilationUnit.cs
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
using System.Threading;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST
{
	public class CompilationUnit : AbstractNode
	{
		// Childs: UsingAliasDeclaration, UsingDeclaration
		
		Stack blockStack = new Stack();
		INode lastChild = null;
		ArrayList lookUpTable = new ArrayList(); // [VariableDeclaration]
		
		public CompilationUnit()
		{
			blockStack.Push(this);
		}
		
		public void BlockStart(INode block)
		{
			blockStack.Push(block);
		}
		
		public void BlockEnd()
		{
			lastChild = (INode)blockStack.Pop();
		}
		
		public INode TakeBlock()
		{
			return (INode)blockStack.Pop();
		}
		
		public override void AddChild(INode childNode)
		{
			if (childNode != null) {
				INode parent = (INode)blockStack.Peek();
				parent.Children.Add(childNode);
				childNode.Parent = parent;
				lastChild = childNode;
				if (childNode is LocalVariableDeclaration) {
					AddToLookUpTable((LocalVariableDeclaration)childNode);
				}
			}
		}
		
		public void AddToLookUpTable(LocalVariableDeclaration v)
		{
			v.Block = (INode)blockStack.Peek();
			lookUpTable.Add(v);
		}
		
		ArrayList specials = new ArrayList();
		public void AddSpecial(string key, object val)
		{
			specials.Add(new DictionaryEntry(key, val));
		}
		
		public void CommitSpecials()
		{
			if (lastChild == null) {
				return;
			}
			foreach (DictionaryEntry entry in specials) {
				lastChild.Specials[entry.Key] = entry.Value;
			}
			specials.Clear();
		}
			
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
