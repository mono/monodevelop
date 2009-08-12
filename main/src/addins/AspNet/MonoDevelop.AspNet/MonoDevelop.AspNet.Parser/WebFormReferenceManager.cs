//
// WebFormReferenceManager.cs: Tracks references within an ASP.NET document, and 
//     resolves types from tag names
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.Globalization;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.Design;

using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AspNet.Parser
{
	public class WebFormReferenceManager : DocumentReferenceManager, IWebFormReferenceManager
	{
		int prefixIndex = 0;
		
		public WebFormReferenceManager (Document parent)
			: base (parent)
		{
		}
		
		#region IWebFormReferenceManager Members

		public Type GetObjectType (string tagPrefix, string typeName)
		{
			string fullName = GetTypeName (tagPrefix, typeName);
			if (fullName == null)
				throw new Exception (string.Format ("The tag type '{0}{1}{2}' has not been registered.", tagPrefix, string.IsNullOrEmpty(tagPrefix)? string.Empty:":", typeName));
			Type type = System.Type.GetType (fullName, false);
			if (type != null)
				return type;
			throw new NotImplementedException ("Cannot yet load custom types out-of-process.");
		}

		public string GetRegisterDirectives ()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder ();
			
			foreach (RegisterDirective directive in pageRefsList) {
				AssemblyRegisterDirective ard = directive as AssemblyRegisterDirective;
				
				if (ard != null)
					sb.AppendFormat ("<%@ Register {0}=\"{1}\" {2}=\"{3}\" {4}=\"{5}\" %>", "TagPrefix", ard.TagPrefix, "Namespace", ard.Namespace, "Assembly", ard.Assembly);
				else {
					ControlRegisterDirective crd = (ControlRegisterDirective) directive;
					sb.AppendFormat ("<%@ Register {0}=\"{1}\" {2}=\"{3}\" {4}=\"{5}\" %>", "TagPrefix", crd.TagPrefix, "TagName", crd.TagName, "Src", crd.Src);
				}
			}
			
			return sb.ToString ();
		}

		public string GetTagPrefix (Type objectType)
		{
			if (objectType.Namespace.StartsWith ("System.Web.UI"))
				return "asp";
			
			foreach (RegisterDirective directive in pageRefsList) {
				AssemblyRegisterDirective ard = directive as AssemblyRegisterDirective;
				
				if (ard != null)
					if (string.Compare (ard.Namespace, objectType.Namespace, true, CultureInfo.InvariantCulture) == 0)
						return directive.TagPrefix;
			}
			
			throw new Exception ("A tag prefix has not been registered for " + objectType.ToString ());
		}

		#endregion
		
		#region Add/Remove references

		public void AddReference (Type type)
		{
			RegisterTagPrefix (type);			
		}

		public void AddReference (Type type, string prefix)
		{
			if (type.Assembly == typeof(System.Web.UI.WebControls.WebControl).Assembly)
				return;

			//check namespace is not already registered			foreach (RegisterDirective directive in pageRefsList) {
				AssemblyRegisterDirective ard = directive as AssemblyRegisterDirective;
				if (0 == string.Compare (ard.Namespace, type.Namespace, false, CultureInfo.InvariantCulture))
					throw new Exception ("That namespace is already registered with another prefix");
				
				if (0 == string.Compare (directive.TagPrefix, prefix, true, CultureInfo.InvariantCulture)) {
					//duplicate prefix; generate a new one.
					//FIXME: possibility of stack overflow with too many default prefixes in existing document
					AddReference (type);
					return;
				}
			}
			
			doc.Project.References.Add (
			    new ProjectReference (ReferenceType.Assembly, type.Assembly.ToString ()));
		/*
		TODO: insert the reference into the document tree 
		*/
		}

		#endregion
		
		#region 2.0 WebFormsReferenceManager members
		
		public string RegisterTagPrefix (Type type)
		{
			if (type.Assembly == typeof(System.Web.UI.WebControls.WebControl).Assembly)
				return "asp";

			string prefix = null;

			//check if there's a prefix for this namespace in the assembly
			TagPrefixAttribute[] atts = (TagPrefixAttribute[]) type.Assembly.GetCustomAttributes (typeof (TagPrefixAttribute), true);
			foreach (TagPrefixAttribute tpa in atts)
					if (0 == string.Compare (tpa.NamespaceName, type.Namespace, false, CultureInfo.InvariantCulture))
						prefix = tpa.TagPrefix;
			
			//generate default prefix
			if (prefix == null) {
				prefix = "cc" + prefixIndex.ToString ();
				prefixIndex++;
			}
				
			AddReference (type, prefix);
			
			return prefix;
		}
		
		public string GetUserControlPath (string tagPrefix, string tagName)
		{
			foreach (RegisterDirective directive in pageRefsList) {
				ControlRegisterDirective crd = directive as ControlRegisterDirective;
				
				if (crd != null)
					if ((string.Compare (crd.TagPrefix, tagPrefix, true, CultureInfo.InvariantCulture) == 0)
							&& (string.Compare (crd.TagName, tagName, true, CultureInfo.InvariantCulture) == 0))
						return crd.Src;
			}
			
			throw new Exception ("That tag has not been registered");
		}
		
		public Type GetType (string tagPrefix, string tagName)
		{
			return GetObjectType (tagPrefix, tagName);
		}
		
		#endregion 2.0 WebFormsReferenceManager members
	}
}
