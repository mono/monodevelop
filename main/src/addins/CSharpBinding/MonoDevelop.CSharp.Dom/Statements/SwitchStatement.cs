// 
// SwitchStatement.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using System.Linq;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Dom
{
	public class SwitchStatement : AbstractCSharpNode
	{
		public const int SwitchSectionRole = 100;
		
		public INode Expression {
			get { return GetChildByRole (Roles.Expression); }
		}
		
		public IEnumerable<SwitchSection> SwitchSections {
			get { return GetChildrenByRole (SwitchSectionRole).Cast<SwitchSection> (); }
		}
		
		public CSharpTokenNode LPar {
			get { return (CSharpTokenNode)GetChildByRole (Roles.LPar); }
		}
		
		public CSharpTokenNode RPar {
			get { return (CSharpTokenNode)GetChildByRole (Roles.RPar); }
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSwitchStatement (this, data);
		}
	}
	
	public class SwitchSection : AbstractCSharpNode
	{
		public const int CaseLabelRole = 100;
		
		public IEnumerable<CaseLabel> CaseLabels {
			get { return GetChildrenByRole (CaseLabelRole).Cast<CaseLabel> (); }
		}
		
		public IEnumerable<INode> Statements {
			get { return GetChildrenByRole (Roles.Statement); }
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSwitchSection (this, data);
		}
	}
	
	public class CaseLabel : AbstractCSharpNode
	{
		public INode Expression {
			get { return GetChildByRole (Roles.Expression); }
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCaseLabel (this, data);
		}
	}
}
