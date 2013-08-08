// 
// NamingRule.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.CSharp.Refactoring.CodeIssues
{
	[DataItem ("NamingRule")]
	public class NameConventionRule
	{
		ICSharpCode.NRefactory.CSharp.Refactoring.NamingRule wrappedRule = new ICSharpCode.NRefactory.CSharp.Refactoring.NamingRule (ICSharpCode.NRefactory.CSharp.Refactoring.AffectedEntity.None);

		[ItemProperty]
		public string Name {
			get { return wrappedRule.Name; } 
			set { wrappedRule.Name = value;} 
		}
		
		[ItemProperty]
		public string[] RequiredPrefixes {
			get { return wrappedRule.RequiredPrefixes; } 
			set { wrappedRule.RequiredPrefixes = value;} 
		}
		
		[ItemProperty]
		public string[] AllowedPrefixes {
			get { return wrappedRule.AllowedPrefixes; } 
			set { wrappedRule.AllowedPrefixes = value;} 
		}
		
		[ItemProperty]
		public string[] RequiredSuffixes {
			get { return wrappedRule.RequiredSuffixes; } 
			set { wrappedRule.RequiredSuffixes = value;} 
		}

		[ItemProperty]
		public string[] ForbiddenPrefixes {
			get { return wrappedRule.ForbiddenPrefixes; } 
			set { wrappedRule.ForbiddenPrefixes = value;} 
		}

		[ItemProperty]
		public string[] ForbiddenSuffixes {
			get { return wrappedRule.ForbiddenSuffixes; } 
			set { wrappedRule.ForbiddenSuffixes = value;} 
		}

		[ItemProperty]
		public ICSharpCode.NRefactory.CSharp.Refactoring.AffectedEntity AffectedEntity {
			get { return wrappedRule.AffectedEntity; } 
			set { wrappedRule.AffectedEntity = value;} 
		}

		[ItemProperty]
		public ICSharpCode.NRefactory.CSharp.Modifiers VisibilityMask {
			get { return wrappedRule.VisibilityMask; } 
			set { wrappedRule.VisibilityMask = value;} 
		}

		[ItemProperty]
		public ICSharpCode.NRefactory.CSharp.Refactoring.NamingStyle NamingStyle {
			get { return wrappedRule.NamingStyle; } 
			set { wrappedRule.NamingStyle = value;} 
		}

		[ItemProperty]
		public bool IncludeInstanceMembers {
			get { return wrappedRule.IncludeInstanceMembers; } 
			set { wrappedRule.IncludeInstanceMembers = value;} 
		}

		[ItemProperty]
		public bool IncludeStaticEntities {
			get { return wrappedRule.IncludeStaticEntities; } 
			set { wrappedRule.IncludeStaticEntities = value;} 
		}

		internal NameConventionRule (ICSharpCode.NRefactory.CSharp.Refactoring.NamingRule wrappedRule)
		{
			this.wrappedRule = wrappedRule;
		}
		
		public NameConventionRule ()
		{
		}

		public NameConventionRule Clone ()
		{
			return new NameConventionRule () {
				wrappedRule = this.wrappedRule.Clone ()
			};
		}

		public string GetPreview ()
		{
			return wrappedRule.GetPreview ();
		}

		internal ICSharpCode.NRefactory.CSharp.Refactoring.NamingRule GetNRefactoryRule ()
		{
			return wrappedRule;
		}
	}
}

