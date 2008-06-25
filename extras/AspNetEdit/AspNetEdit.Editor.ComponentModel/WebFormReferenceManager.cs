 /* 
 * WebFormReferenceManager.cs - tracks references in a WebForm page
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using System.Web.UI.Design;
using System.ComponentModel.Design;
using System.Reflection;
using System.Web.UI;
using System.Globalization;

namespace AspNetEdit.Editor.ComponentModel
{
	public class WebFormReferenceManager : IWebFormReferenceManager
	{
		private DesignerHost host;
		private ITypeResolutionService typeRes;
		private int prefixIndex = 1;

		//TODO: Some way to reset this when host is reset 
		public WebFormReferenceManager (DesignerHost host)
		{
			if (host == null)
				throw new ArgumentNullException ("host");
			this.host = host;
			this.typeRes = host.GetService (typeof (ITypeResolutionService)) as ITypeResolutionService;
			if (typeRes == null)
				throw new Exception ("Could not obtain ITypesResolutionService from host");
		}


		#region IWebFormReferenceManager Members

		public Type GetObjectType(string tagPrefix, string typeName)
		{
			if (0 == string.Compare (tagPrefix, "asp", true, CultureInfo.InvariantCulture))
				return typeof (System.Web.UI.WebControls.WebControl).Assembly.GetType ("System.Web.UI.WebControls."+typeName, true, true);

			//look it up in reference directives
			DocumentDirective[] directives = host.RootDocument.GetDirectives("Register");
			foreach (DocumentDirective dd in directives)
				if (0 == string.Compare (tagPrefix, dd["Tagprefix"], true, CultureInfo.InvariantCulture)) {

					if (dd["Assembly"] == string.Empty || dd["Assembly"] == null)
						//TODO: Usercontrols
						throw new NotImplementedException ();
					return typeRes.GetType (dd["Namespace"] + "." + typeName, true, true);
				}

			throw new Exception ("The tag prefix \"" + tagPrefix + "\" has not been registered");
		}

		public string GetRegisterDirectives ()
		{
			DocumentDirective[] directives = host.RootDocument.GetDirectives("Register");
			string retVal = string.Empty;
			foreach (DocumentDirective dd in directives)
				retVal += dd.ToString();
			return retVal;
		}

		public string GetTagPrefix (Type objectType)
		{
			if (objectType.Namespace.StartsWith ("System.Web.UI"))
				return "asp";

			DocumentDirective[] directives = host.RootDocument.GetDirectives ("Reference");
			foreach (DocumentDirective dd in directives)
				if (0 == string.Compare (dd["Namespace"], objectType.Namespace, true, CultureInfo.InvariantCulture))
					return dd["Tagprefix"];

			throw new Exception ("A tag prefix has not been registered for " + objectType.ToString ());
		}

		#endregion

		#region Add/Remove references

		public void AddReference (Type type)
		{
			if (type.Assembly == typeof(System.Web.UI.WebControls.WebControl).Assembly)
				return;
			if (type.Assembly == typeof(WebFormReferenceManager).Assembly)
				return;

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
			return;
			
		}

		public void AddReference (Type type, string prefix)
		{
			if (type.Assembly == typeof(System.Web.UI.WebControls.WebControl).Assembly)
				return;

			//check namespace is not already registered
			bool prefixExists = false;
			bool namespaceExists = false;
			DocumentDirective[] directives = host.RootDocument.GetDirectives("Register");
			foreach (DocumentDirective dd in directives) {
				if (0 == string.Compare (dd["Namespace"], type.Namespace, false, CultureInfo.InvariantCulture))
					namespaceExists = true;
				if (0 == string.Compare (dd["Tagprefix"], prefix, true, CultureInfo.InvariantCulture))
					prefixExists = true;
			}
			if (namespaceExists)
				throw new Exception ("That namespace is already registered with another prefix");
			if (prefixExists) {
				//duplicate prefix; generate a new one.
				//FIXME: possibility of stack overflow with too many default prefixes in existing document
				AddReference (type);
				return;
			}

			//build and register the assembly
			Hashtable atts = new Hashtable ();
			atts["Namespace"] = type.Namespace;
			atts["Tagprefix"] = prefix;
			atts["Assembly"] = type.Assembly.GetName ().Name;

			host.RootDocument.AddDirective ("Register", atts);
		}

		#endregion


		//ASPNET2 WebFormReferenceManager members
		/* 
		public string RegisterTagPrefix (Type objectType)
		{
		}
		
		public string GetUserControlPath (string tagPrefix, string tagName)
		{
		}
		
		public Type GetType(string tagPrefix, string tagName)
		{
		}
		*/
	}
}
