// 
// NamingConventions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.AnalysisCore;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.AnalysisCore.Fixes;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Text;

namespace MonoDevelop.CSharp.Inspection
{
	[PolicyType ("C# naming")]
	public class CSharpNamingPolicy // : IEquatable<CSharpNamingPolicy>
	{
		[ItemProperty]
		public NamingRule Namespace {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Type {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Interface {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule TypeParameter {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Method {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Property{
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Event {
			get;
			set;
		}
		
		
		[ItemProperty]
		public NamingRule LocalVariable {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule LocalConstant {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Parameter {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Field {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule InstanceField {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule InstanceStaticField {
			get;
			set;
		}
		 
		[ItemProperty]
		public NamingRule Constant {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule InstanceConstant {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule EnumMember {
			get;
			set;
		}
		
		public CSharpNamingPolicy ()
		{
			Namespace = new NamingRule (NamingStyle.PascalCase);
			Type = new NamingRule (NamingStyle.PascalCase);
			Interface = new NamingRule ("I", NamingStyle.PascalCase, null);
			TypeParameter = new NamingRule (NamingStyle.PascalCase);
			Method = new NamingRule (NamingStyle.PascalCase);
			Property = new NamingRule (NamingStyle.PascalCase);
			Event = new NamingRule (NamingStyle.PascalCase);
			LocalVariable = new NamingRule (NamingStyle.CamelCase);
			LocalConstant = new NamingRule (NamingStyle.CamelCase);
			Parameter = new NamingRule (NamingStyle.CamelCase);
			Field = new NamingRule (NamingStyle.PascalCase);
			InstanceField = new NamingRule (NamingStyle.CamelCase);
			InstanceStaticField = new NamingRule (NamingStyle.PascalCase);
			Constant = new NamingRule (NamingStyle.PascalCase);
			InstanceConstant = new NamingRule (NamingStyle.PascalCase);
			EnumMember = new NamingRule (NamingStyle.PascalCase);
		}
	}
}
