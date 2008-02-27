/* 
* ServerControlParsingObject.cs - A ParsingObject for server controls
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
using System.Text;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.Design;
using System.ComponentModel.Design;
using System.Globalization;

using AspNetEdit.Editor.ComponentModel;
using MonoDevelop.AspNet.Parser.Internal;
using MonoDevelop.AspNet.Parser.Dom;

namespace AspNetEdit.Editor.Persistence
{
	internal class ServerObjectParsingObject : ParsingObject
	{
		private object obj;
		ParseChildrenAttribute parseAtt;
		PropertyDescriptorCollection pdc;
		private ParseChildrenMode mode;
		private string innerText = String.Empty;

		public ServerObjectParsingObject(Type type, Hashtable attributes, string tagid, ParsingObject parent)
			: base (tagid, parent)
		{
			//create the object
			if (type.GetInterface ("System.ComponentModel.IComponent") != null)
				//note: this automatically adds to parent's container, as some controls
				//need to be sited e.g. if they use site dictionaries
				//TODO: should this action be passed up the tree so controls can intercept?
				obj = ((AspNetEdit.Editor.ComponentModel.DesignerHost) base.DesignerHost).CreateComponent (type, attributes["ID"] as string, false);
			else
				obj = Activator.CreateInstance (type);
				
			//and populate it from the attributes
			pdc = TypeDescriptor.GetProperties (obj);
			foreach (DictionaryEntry de in attributes) {
				if (0 == string.Compare((string)de.Key, "runat"))
					continue;
				if (0 == string.Compare((string)de.Key, "ID"))
					continue;
				//use the dash subproperty syntax
				string[] str = ((string)de.Key).Split ('-');
				PropertyDescriptor pd = pdc.Find (str[0], true);

				//if property not found, try events
				if (str.Length == 1 && pd == null && CultureInfo.InvariantCulture.CompareInfo.IsPrefix (str[0].ToLower(), "on")) {
					IEventBindingService iebs = (IEventBindingService) DesignerHost.GetService (typeof (IEventBindingService));
					if (iebs == null)
						throw new Exception ("Could not obtain IEventBindingService from host");

					EventDescriptorCollection edc = TypeDescriptor.GetEvents (obj);
					EventDescriptor e = edc.Find (str[0].Remove(0,2), true);
					if (e != null)
						pd = iebs.GetEventProperty(e);
					else
						throw new Exception ("Could not find event " + str[0].Remove(0,2));
				}
				
				object loopObj = obj;
				
				for (int i = 0; i < str.Length; i++ )
				{
					if (pd == null)
						throw new Exception ("Could not find property " + (string)de.Key);
					
					if (i == str.Length - 1) {
						pd.SetValue (obj, pd.Converter.ConvertFromString ((string) de.Value));
						break;
					}
					
					loopObj = pd.GetValue (loopObj);
					pd = TypeDescriptor.GetProperties (loopObj).Find (str[0], true);
					
				}
			}

			parseAtt = TypeDescriptor.GetAttributes (obj)[typeof(ParseChildrenAttribute )] as ParseChildrenAttribute;
			//FIXME: fix this in MCS classlib
			if (parseAtt.DefaultProperty.Length == 0)
				parseAtt = null;
			
			//work out how we're trying to parse the children
			if (parseAtt != null) {
				if (parseAtt.DefaultProperty != null) {
					PropertyDescriptor pd = pdc[parseAtt.DefaultProperty];
					if (pd == null)
						throw new Exception ("Default property does not exist");
					if (pd.PropertyType.GetInterface("System.Collections.IList") == (typeof(IList)))
						mode = ParseChildrenMode.DefaultCollectionProperty;
					else
						mode = ParseChildrenMode.DefaultProperty;
				}
				else if (parseAtt.ChildrenAsProperties)
					mode = ParseChildrenMode.Properties;
				else
					mode = ParseChildrenMode.Controls;
			}
			else {
				//FIXME: these are actually persistence hints, but ParseChildrenAttribute doesn't always exist.
				//FIXME: logic would be dodgy with bad input
				parseAtt = ParseChildrenAttribute.Default;
				mode = ParseChildrenMode.Controls;
				foreach (PropertyDescriptor pd in pdc) {
					PersistenceModeAttribute modeAttrib = pd.Attributes[typeof(PersistenceModeAttribute)] as PersistenceModeAttribute;
					if (modeAttrib == null) return;
					
					switch (modeAttrib.Mode) {
						case PersistenceMode.Attribute:
							continue;
						case PersistenceMode.EncodedInnerDefaultProperty:
							parseAtt.DefaultProperty = pd.Name;
							mode = ParseChildrenMode.DefaultEncodedProperty;
							break;				
						case PersistenceMode.InnerDefaultProperty:
							parseAtt.DefaultProperty = pd.Name;
							if (pd.PropertyType.GetInterface("System.Collections.IList") == (typeof(IList)))
								mode = ParseChildrenMode.DefaultCollectionProperty;
							else
								mode = ParseChildrenMode.DefaultProperty;
							break;
						case PersistenceMode.InnerProperty:
							mode = ParseChildrenMode.Properties;
							break;
					}
				}
			}
		
		}

		public override void AddText (string text)
		{
			switch (mode) {
				case ParseChildrenMode.Controls:
					this.AddControl (new LiteralControl (text));
					return;
				case ParseChildrenMode.DefaultCollectionProperty:
				case ParseChildrenMode.Properties:
					if (IsWhiteSpace(text))
						return;
					else
						throw new Exception ("Unexpected text found in child properties");
				case ParseChildrenMode.DefaultProperty:
					innerText += text;
					return;
				case ParseChildrenMode.DefaultEncodedProperty:
					innerText += System.Web.HttpUtility.HtmlDecode (text);
					return;
			}
		}

		private bool IsWhiteSpace(string s)
		{
			bool onlyWhitespace = true;
			foreach (char c in s)
				if (!Char.IsWhiteSpace (c)) {
					onlyWhitespace = false;
					break;
				}
			return onlyWhitespace;
		}

		public override ParsingObject CloseObject (string closingTagText)
		{
			//we do this here in case we have tags inside
			if (mode == ParseChildrenMode.DefaultProperty && !string.IsNullOrEmpty(innerText)) {
				PropertyDescriptor pd = pdc[parseAtt.DefaultProperty];
				pd.SetValue(obj, pd.Converter.ConvertFromString(innerText));
			}
			//FIME: what if it isn't?
			if (obj is Control) {
				Control c = (Control) obj;
				Document.InitialiseControl (c);
				base.AddText (Document.RenderDesignerControl (c)); // add initial rendered text representation 
			}
			base.AddControl (obj);
			return base.CloseObject (closingTagText);
		}

		public override ParsingObject CreateChildParsingObject (ILocation location, string tagid, TagAttributes attributes)
		{
			switch (mode) {
				case ParseChildrenMode.DefaultProperty:
					//oops, we didn't need to tokenise this.
					innerText += location.PlainText;
					//how do we get end tag?
					throw new NotImplementedException ("Inner default properties that look like tags have not been implemented yet.");
				case ParseChildrenMode.DefaultEncodedProperty:
					innerText += System.Web.HttpUtility.HtmlDecode (location.PlainText);
					//how do we get end tag?
					throw new NotImplementedException ("Inner default properties that look like tags have not been implemented yet.");
				case ParseChildrenMode.Controls:
					//html tags
					if (tagid.IndexOf(':') == -1)
						return new HtmlParsingObject (location.PlainText, tagid, this);
					goto case ParseChildrenMode.DefaultCollectionProperty;
				case ParseChildrenMode.DefaultCollectionProperty:
					string[] str = tagid.Split(':');
					if (str.Length != 2)
						throw new ParseException (location, "Server tag name is not of form prefix:name");

					Type tagType = WebFormReferenceManager.GetObjectType(str[0], str[1]);
					if (tagType == null)
						throw new ParseException(location, "The tag " + tagid + "has not been registered");

					return new ServerObjectParsingObject (tagType, attributes.GetDictionary(null), tagid, this);
				case ParseChildrenMode.Properties:
					throw new NotImplementedException ("Multiple child properties have not yet been implemented.");
			}
			throw new ParseException (location, "Unexpected state encountered: ");
		}

		protected override void AddControl(object control)
		{
			switch (mode) {
				case ParseChildrenMode.DefaultProperty:
				case ParseChildrenMode.Properties:
					throw new Exception ("Cannot add a control to default property");
				case ParseChildrenMode.DefaultCollectionProperty:
					PropertyDescriptor pd = pdc[parseAtt.DefaultProperty];
					((IList)pd.GetValue(obj)).Add(control);
					return;
				case ParseChildrenMode.Controls:
					throw new NotImplementedException("Child controls have not yet been implemented.");					
			}
		}
	}

	public enum ParseChildrenMode
	{
		DefaultProperty,
		DefaultEncodedProperty,
		DefaultCollectionProperty,
		Properties,
		Controls
	}
}
