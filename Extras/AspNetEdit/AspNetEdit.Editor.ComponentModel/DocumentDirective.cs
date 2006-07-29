/* 
* Directive.cs - base class for tracking ASP.NET directives and their properties
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
/* 
* DocumentDirective.cs - Represents an ASP.NET directive in the Document.
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
using System.Text;
using System.Collections;
using System.Globalization;

namespace AspNetEdit.Editor.ComponentModel
{
	public class DocumentDirective
	{
		private Hashtable properties;
		private string name;
		private int key;

		public DocumentDirective (string name, IDictionary properties, int key)
		{
			this.name = name;
			this.key = key;

			CaseInsensitiveHashCodeProvider provider = new CaseInsensitiveHashCodeProvider(CultureInfo.InvariantCulture);
			CaseInsensitiveComparer comparer = new CaseInsensitiveComparer(CultureInfo.InvariantCulture);
			this.properties = new Hashtable (provider, comparer);
			
			if (properties != null)
				foreach (DictionaryEntry de in properties) {
					CheckValidPropertyName (name, (string) de.Key);
					CheckValidPropertyValue (name, (string)de.Key, (string)de.Value);
					this.properties.Add (de.Key, de.Value);
				}
		}

		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Used to reference the placeholder in the design document
		/// </summary>
		public int Key {
			get { return key; }
		}

		public string this[string name]
		{
			get {
				if (properties.ContainsKey(name))
					return (string) properties[name];
				else
					return string.Empty;
			}
			set {
				if (!properties.ContainsKey(name))
					CheckValidPropertyName (this.name, name);

				CheckValidPropertyValue (this.name, name, value);
				properties[name] = (value ==  string.Empty)? null : value;
			}

		}

		private static void CheckValidPropertyName (string directiveName, string propertyName)
		{
			//TODO:Check valid names. For now, anything loaded will be okay already.
			//throw new InvalidOperationException(propertyName + " is not a property of directive " + directiveName);
		}

		private static void CheckValidPropertyValue (string directiveName, string propertyName, string value)
		{
			//TODO:Check valid values
			//throw new NotImplementedException();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("<%@ ");
			sb.Append (Name);
			foreach (DictionaryEntry de in properties)
				sb.AppendFormat (" {0}=\"{1}\"", (string) de.Key, (string) de.Value);
			sb.Append (" %>");
			return sb.ToString ();
		}
	}
}
