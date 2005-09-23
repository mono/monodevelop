// INode.cs
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
using System.Drawing;
using System.Collections;
using ICSharpCode.SharpRefactory.Parser.AST.VB;
using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public interface INode
	{
		INode Parent { 
			get;
			set;
		}
		
		Hashtable Specials {
			get;
		}
		
		ArrayList Children {
			get;
		}
		
		Point StartLocation {
			get;
			set;
		}
		
		Point EndLocation {
			get;
			set;
		}
		
		/// <summary>
		/// Visits all children
		/// </summary>
		/// <param name="visitor">The visitor to accept</param>
		/// <param name="data">Additional data for the visitor</param>
		/// <returns>The paremeter <paramref name="data"/></returns>
		object AcceptChildren(IASTVisitor visitor, object data);
		
		/// <summary>
		/// Accept the visitor
		/// </summary>
		/// <param name="visitor">The visitor to accept</param>
		/// <param name="data">Additional data for the visitor</param>
		/// <returns>The value the visitor returns after the visit</returns>
		object AcceptVisitor(IASTVisitor visitor, object data);
	}
}
