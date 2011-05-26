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

namespace MonoDevelop.CSharp.Analysis
{
	public class NamingRule
	{
		public string Prefix {
			get;
			set;
		}
		
		public NamingStyle NamingStyle {
			get;
			set;
		}
		
		public string Suffix {
			get;
			set;
		}
		
		public NamingRule ()
		{
		}
		
		public NamingRule (NamingStyle namingStyle)
		{
			this.NamingStyle = namingStyle;
		}
		
		public string GetPreview ()
		{
			StringBuilder result = new StringBuilder ();
			if (Prefix != null)
				result.Append (Prefix);
			switch (NamingStyle) {
			case NamingStyle.PascalCase:
				result.Append ("PascalCase");
				break;
			case NamingStyle.CamelCase:
				result.Append ("camelCase");
				break;
			case NamingStyle.AllUpper:
				result.Append ("ALL_UPPER");
				break;
			case NamingStyle.AllLower:
				result.Append ("all_lower");
				break;
			case NamingStyle.FirstUpper:
				result.Append ("First_upper");
				break;
			}
			if (Suffix != null)
				result.Append (Suffix);
			return result.ToString ();
		}
		
		public NamingRule (string prefix, NamingStyle namingStyle, string suffix)
		{
			this.Prefix = prefix;
			this.NamingStyle = namingStyle;
			this.Suffix = suffix;
		}
		
		public bool IsValid (string name)
		{
			string id = name;
			if (!string.IsNullOrEmpty (Prefix)) {
				if (!id.StartsWith (Prefix))
					return false;
				id = id.Substring (Prefix.Length);
			}
			
			if (!string.IsNullOrEmpty (Suffix)) {
				if (!id.EndsWith (Suffix))
					return false;
				id = id.Substring (0, id.Length - Suffix.Length);
			}
			switch (NamingStyle) {
			case NamingStyle.AllLower:
				return !id.Any (ch => char.IsLetter (ch) && char.IsUpper (ch));
			case NamingStyle.AllUpper:
				return !id.Any (ch => char.IsLetter (ch) && char.IsLower (ch));
			case NamingStyle.CamelCase:
				return id.Length == 0 || char.IsLower (id [0]);
			case NamingStyle.PascalCase:
				return id.Length == 0 || char.IsUpper (id [0]);
			case NamingStyle.FirstUpper:
				return id.Length == 0 && char.IsUpper (id [0]) && !id.Take (1).Any (ch => char.IsLetter (ch) && char.IsUpper (ch));
			}
			return true;
		}

		public string GetErrorMessage (string name)
		{
			string id = name;
			if (!string.IsNullOrEmpty (Prefix)) {
				if (!id.StartsWith (Prefix))
					return string.Format (GettextCatalog.GetString ("Name should start with prefix '{0}'."), Prefix);
				id = id.Substring (Prefix.Length);
			}
			
			if (!string.IsNullOrEmpty (Suffix)) {
				if (!id.EndsWith (Suffix))
					return string.Format (GettextCatalog.GetString ("Name should end with suffix '{0}'."), Suffix);
				id = id.Substring (0, id.Length - Suffix.Length);
			}
			switch (NamingStyle) {
			case NamingStyle.AllLower:
				if (id.Any (ch => char.IsLetter (ch) && char.IsUpper (ch)))
					return string.Format (GettextCatalog.GetString ("'{0}' contains upper case letters."), name);
				break;
			case NamingStyle.AllUpper:
				if (id.Any (ch => char.IsLetter (ch) && char.IsLower (ch)))
					return string.Format (GettextCatalog.GetString ("'{0}' contains lower case letters."), name);
				break;
			case NamingStyle.CamelCase:
				if (id.Length > 0 && char.IsUpper (id [0]))
					return string.Format (GettextCatalog.GetString ("'{0}' should start with a lower case letter."), name);
				break;
			case NamingStyle.PascalCase:
				if (id.Length > 0 && char.IsLower (id [0]))
					return string.Format (GettextCatalog.GetString ("'{0}' should start with an upper case letter."), name);
				break;
			case NamingStyle.FirstUpper:
				if (id.Length > 0 && char.IsLower (id [0]))
					return string.Format (GettextCatalog.GetString ("'{0}' should start with an upper case letter."), name);
				if (id.Take (1).Any (ch => char.IsLetter (ch) && char.IsUpper (ch)))
					return string.Format (GettextCatalog.GetString ("'{0}' contains an upper case letter after the first."), name);
				break;
			}
			// should never happen.
			return "no known errors.";
		}
		
		public FixableResult GetFixableResult (AstLocation location, IBaseMember node, string name)
		{
			return new FixableResult (
				new DomRegion (location.Line, location.Column, location.Line, location.Column + name.Length),
				GetErrorMessage (name),
				ResultLevel.Warning, ResultCertainty.High, ResultImportance.Medium,
				new RenameMemberFix (node, name, null));
		}
	}
}
