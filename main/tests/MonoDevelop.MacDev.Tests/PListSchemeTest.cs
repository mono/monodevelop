// 
// PListSchemeTest.cs
//  
// Author:
//       Alan McGovenrn <alan@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;

using MonoDevelop.MacDev;
using MonoDevelop.MacDev.PlistEditor;
using System.Collections.Generic;

namespace MonoDevelop.MacDev.Tests
{
	[TestFixture]
	public class PListSchemeTest
	{
		[Test]
		public void ArrayKey_WithArrayType ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType = ""Dictionary"" />
</PListScheme>");
			
			var key = scheme.GetKey ("keyname");
			Assert.AreEqual ("Array", key.Type, "#1");
			Assert.AreEqual ("Dictionary", key.ArrayType, "#2");
		}

		[Test]
		public void ArrayKey_WithoutArrayType ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" />
</PListScheme>");
			
			var key = scheme.GetKey ("keyname");
			Assert.AreEqual ("Array", key.Type, "#1");
			Assert.IsNull (key.ArrayType, "#2");
		}
				
		[Test]
		public void AvailableKeys_NumberArray()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType = ""Number"" >
		<Value name = ""1"" />
		<Value name = ""2"" />
		<Value name = ""3"" />
	</Key>
</PListScheme>");
			
			var key = scheme.GetKey ("keyname");
			var value = new PArray ();
			
			var root = new PDictionary ();
			root.Add (key.Identifier, value);
			
			var available = PListScheme.AvailableKeys (value, PListScheme.Match (root, scheme));
			Assert.AreEqual (3, available.Count, "#1");
			
			// We can create '2' or '3', but not '1'
			value.Add (new PNumber (1));
			available = PListScheme.AvailableKeys (value, PListScheme.Match (root, scheme));
			Assert.AreEqual (2, available.Count, "#2");
			Assert.IsFalse (available.Any (v => v.Identifier == "1"), "#3");
			
			// We can only create '3'
			value.Add (new PNumber (2));
			available = PListScheme.AvailableKeys (value, PListScheme.Match (root, scheme));
			Assert.AreEqual (1, available.Count, "#4");
			Assert.IsTrue (available.Any (v => v.Identifier == "3"), "#5");
			
			value.Add (new PNumber (3));
			available = PListScheme.AvailableKeys (value, PListScheme.Match (root, scheme));
			Assert.AreEqual (0, available.Count, "#4");
		}

		[Test]
		public void AvailableKeys_Array_NotInScheme ()
		{
			var root = new PDictionary ();
			root.Add ("foo", new PArray ());
			
			var available = PListScheme.AvailableKeys (root["foo"], PListScheme.Match (root, PListScheme.Empty));
			Assert.IsNull (available, "#1");
		}

		[Test]
		public void AvailableKeys_Array_NumberType ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType = ""Number"" />
</PListScheme>");

			var value = new PArray ();
			var root = new PDictionary ();
			root.Add ("keyname", value);
			
			var available = PListScheme.AvailableKeys (value, PListScheme.Match (root, scheme));
			Assert.AreEqual (1, available.Count, "#1");
			Assert.AreEqual (PNumber.Type, available [0].Type, "#2");
		}

		[Test]
		public void AvailableKeys_Array_StringType ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType = ""String"" />
</PListScheme>");

			var value = new PArray ();
			var root = new PDictionary ();
			root.Add ("keyname", value);
			
			var available = PListScheme.AvailableKeys (value, PListScheme.Match (root, scheme));
			Assert.AreEqual (1, available.Count, "#1");
			Assert.AreEqual (PString.Type, available [0].Type, "#2");
		}

		[Test]
		public void AvailableKeys_Dictionary ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"" />
</PListScheme>");

			var value = new PDictionary ();
			var root = new PDictionary ();
			root.Add ("keyname", value);
			
			var available = PListScheme.AvailableKeys (value, PListScheme.Match (root, scheme));
			Assert.AreEqual (1, available.Count, "#1");
			Assert.AreEqual (PString.Type, available [0].Type, "#2");
		}

		[Test]
		public void AvailableValues_String_NotInScheme ()
		{
			var root = new PDictionary ();
			root.Add ("foo", new PString ("bar"));
			
			var available = PListScheme.AvailableValues (root["foo"], PListScheme.Match (root, PListScheme.Empty));
			Assert.IsNull (available, "#1");
		}
	
		[Test]
		public void AvailableValues_Array_NoValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType = ""Number"" />
</PListScheme>");
			
			var root = new PDictionary ();
			var key = scheme.GetKey ("keyname");
			root.Add (key.Identifier, key.Create ());
			
			var tree = PListScheme.Match (root, scheme);
			var available = PListScheme.AvailableValues (tree.First ().Key, tree);
			Assert.IsNull (available, "#1");
		}

		[Test]
		public void AvailableValues_Boolean ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Boolean"" />
</PListScheme>");
			
			var root = new PDictionary ();
			var key = scheme.GetKey ("keyname");
			root.Add (key.Identifier, key.Create ());
			
			var tree = PListScheme.Match (root, scheme);
			var available = PListScheme.AvailableValues (tree.First ().Key, tree);
			Assert.AreEqual (2, available.Count, "#1");
			Assert.AreEqual ("Yes", available [0].Identifier, "#1");
			Assert.AreEqual ("Yes", available [0].Description, "#2");
			Assert.AreEqual ("No", available [1].Identifier, "#3");
			Assert.AreEqual ("No", available [1].Description, "#4");
		}
		
		[Test]
		public void AvailableValues_Boolean_InArray ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType = ""Boolean"" />
</PListScheme>");
			
			var value = new PBoolean (true);
			var root = new PDictionary ();
			root.Add (scheme.Keys[0].Identifier, value);

			var available = PListScheme.AvailableValues (value, PListScheme.Match (root, scheme));
			Assert.AreEqual (2, available.Count, "#1");
		}
	
		[Test]
		public void AvailableValues_Boolean_InDictionary ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"">
		<Value name = ""key1"" type = ""Boolean"" required = ""True"" />
	</Key>
</PListScheme>");
			
			var root = new PDictionary ();
			var key = scheme.GetKey ("keyname");
			root.Add (key.Identifier, key.Create ());
			
			var tree = PListScheme.Match (root, scheme);
			var key1 = ((PDictionary) root ["keyname"]) ["key1"];
			var available = PListScheme.AvailableValues (key1, tree);
			Assert.AreEqual (2, available.Count, "#1");
		}
		
		[Test]
		public void AvailableValues_Boolean_NotInScheme ()
		{
			var value = new PBoolean (true);
			var root = new PDictionary ();
			root.Add ("foo", value);
			
			var available = PListScheme.AvailableValues (value, PListScheme.Match (root, PListScheme.Empty));
			Assert.AreEqual (2, available.Count, "#1");
		}
		
		[Test]
		public void AvailableValues_Number_InArray ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType = ""Number"" >
		<Value name = ""1"" />
		<Value name = ""2"" />
		<Value name = ""3"" />
	</Key>
</PListScheme>");
			
			var array = new PArray ();
			var key = scheme.GetKey ("keyname");
			
			var root = new PDictionary ();
			root.Add (key.Identifier, array);
			
			// The array cannot have a different value (it's a container!)
			var available = PListScheme.AvailableValues (array, PListScheme.Match (root, scheme));
			Assert.IsNull (available, "#1");
			
			// The only element in the array can be of value '1', '2' or '3'
			array.Add (new PNumber (1));
			available = PListScheme.AvailableValues (array [0], PListScheme.Match (root, scheme));
			Assert.AreEqual (3, available.Count, "#2");
			
			// Now the first element can be either a '1' or a '3'
			array.Add (new PNumber (2));
			available = PListScheme.AvailableValues (array [0], PListScheme.Match (root, scheme));
			Assert.AreEqual (2, available.Count, "#3");
			Assert.IsTrue (available.Any (f => f.Identifier == "1"), "#4");
			Assert.IsTrue (available.Any (f => f.Identifier == "3"), "#5");
			
			// The second element can be a '2' or a '3'
			available = PListScheme.AvailableValues (array [1], PListScheme.Match (root, scheme));
			Assert.AreEqual (2, available.Count, "#6");
			Assert.IsTrue (available.Any (f => f.Identifier == "2"), "#7");
			Assert.IsTrue (available.Any (f => f.Identifier == "3"), "#8");
			
			// Now it can only be '1'
			array.Add (new PNumber (3));
			available = PListScheme.AvailableValues (array [0], PListScheme.Match (root, scheme));
			Assert.AreEqual (1, available.Count, "#9");
			Assert.IsTrue (available.Any (f => f.Identifier == "1"), "#10");
		}
		
		[Test]
		public void AvailableValues_Number_InDictionary ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"">
		<Value name = ""num"" type = ""Number"" required = ""True"">
			<Value name = ""1"" />
			<Value name = ""2"" />
			<Value name = ""3"" />
		</Value>
	</Key>
</PListScheme>");
			
			var value = new PDictionary ();
			var num = new PNumber (1);
			
			var root = new PDictionary ();
			root.Add ("keyname", value);
			value.Add ("num", num);

			var available = PListScheme.AvailableValues (num, PListScheme.Match (root, scheme));
			Assert.AreEqual (3, available.Count, "#1");
		}

		[Test]
		public void AvailableValues_Number_NoValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Number"" />
</PListScheme>");
			
			var root = new PDictionary ();
			var key = scheme.GetKey ("keyname");
			root.Add (key.Identifier, key.Create ());
			
			var tree = PListScheme.Match (root, scheme);
			var available = PListScheme.AvailableValues (tree.First ().Key, tree);
			Assert.IsNull (available);
		}

		[Test]
		public void AvailableValues_Number_WithValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Number"">
		<Value name = ""1"" />
		<Value name = ""2"" />
		<Value name = ""3"" />
	</Key>
</PListScheme>");
			
			var key = scheme.GetKey ("keyname");
			var value = (PNumber) key.Create ();
			
			var root = new PDictionary ();
			root.Add (key.Identifier, value);
			
			var tree = PListScheme.Match (root, scheme);
			var available = PListScheme.AvailableValues (value, tree);
			Assert.AreEqual (3, available.Count, "#1");
			Assert.AreEqual (1, value.Value, "#2");
		}

		[Test]
		public void ArrayKey_ExplicitDictionaryValue ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType = ""Dictionary"">
		<Value />
	</Key>
</PListScheme>");
			
			var key = scheme.GetKey ("keyname");
			Assert.AreEqual (1, key.Values.Count, "#1");
			Assert.AreEqual (PDictionary.Type, key.Values [0].Type, "#2");
		}
		
		[Test]
		public void BooleanKey_ImplicitValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Boolean"" />
</PListScheme>");
			
			var key = scheme.GetKey ("keyname");
			Assert.AreEqual ("Boolean", key.Type, "#1");
			Assert.AreEqual (0, key.Values.Count, "#2");
		}
		
		[Test]
		public void CreateKey_Boolean ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Boolean"" />
</PListScheme>");
			
			var key = scheme.GetKey ("keyname").Create ();
			IsInstanceOf <PBoolean> (key, "#1");
			Assert.IsTrue (((PBoolean) key).Value, "#2");
		}
		
		[Test]
		public void CreateKey_Dictionary_NoValue ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"" />
</PListScheme>");
			
			var obj = (PDictionary) scheme.Keys [0].Create ();
			Assert.AreEqual (1, obj.Count, "#1");
			Assert.IsTrue (obj.ContainsKey ("newNode"));
			IsInstanceOf<PString> (obj ["newNode"], "#2");
		}
		
		[Test]
		public void CreateKey_Dictionary_WithValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"">
		<Value name = ""key1"" type = ""Number"" />
		<Value name = ""key2"" type = ""Array"" />
	</Key>
</PListScheme>");
			
			var obj = (PDictionary) scheme.Keys [0].Create ();
			Assert.AreEqual (1, obj.Count, "1");
			IsInstanceOf <PNumber> (obj ["key1"], "#2");
			Assert.AreEqual (0, ((PNumber) obj ["key1"]).Value, "#3");
		}
		
		[Test]
		public void CreateKey_Dictionary_WithRequiredValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"">
		<Value name = ""key1"" type = ""Number"" required = ""True"" />
		<Value name = ""key2"" type = ""Array"" required = ""True"" />
	</Key>
</PListScheme>");
			
			var obj = scheme.Keys [0].Create ();
			IsInstanceOf<PDictionary> (obj, "#1");
			Assert.AreEqual (2, ((PDictionary)obj).Count, "#2");
		}
		
		[Test]
		public void CreateKey_Dictionary_WithRequiredValuesAndSubvalues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"">
		<Value name = ""key1"" type = ""Number"" required = ""True"">
			<Value name = ""5"" />
			<Value name = ""7"" />
		</Value>
		<Value name = ""key2"" type = ""Array"" arrayType = ""String"" required = ""True"">
			<Value name = ""str1"" required = ""True"" />
			<Value name = ""str2"" />
			<Value name = ""str3"" required = ""True"" />
		</Value>
	</Key>
</PListScheme>");
			
			var obj = scheme.Keys [0].Create ();
			IsInstanceOf<PDictionary> (obj, "#1");
			
			var dict = (PDictionary) obj;
			Assert.AreEqual (2, dict.Count, "#2");
			IsInstanceOf<PNumber> (dict ["key1"], "#3");
			IsInstanceOf<PArray> (dict ["key2"], "#4");
			
			var val1 = dict.Get<PNumber> ("key1");
			Assert.AreEqual (5, val1.Value, "#5");
			
			var val2 = dict.Get<PArray> ("key2");
			Assert.AreEqual (2, val2.Count, "#6");
			Assert.AreEqual ("str1", ((PString) val2[0]).Value, "#7");
			Assert.AreEqual ("str3", ((PString) val2[1]).Value, "#8");
		}
		
		[Test]
		public void CreateKey_NumberArray_NoValue ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType = ""Number"" />
</PListScheme>");
			
			var obj = (PArray) scheme.Keys [0].Create ();
			Assert.AreEqual (1, obj.Count, "#1");
			IsInstanceOf <PNumber> (obj [0], "#2");
			Assert.AreEqual (0, ((PNumber) obj [0]).Value, "#3");
			
		}
		
		[Test]
		public void CreateKey_NumberArray_WithValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType= ""Number"" >
		<Value name = ""6"" description = ""bar"" />
		<Value name = ""8"" description = ""bar"" />
	</Key>
</PListScheme>");
			
			var obj = (PArray) scheme.Keys [0].Create ();
			Assert.AreEqual (1, obj.Count, "#1");
			Assert.AreEqual (6, ((PNumber)obj[0]).Value, "#2");
		}
		
		[Test]
		public void CreateKey_NumberArray_WithRequiredValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Array"" arrayType= ""Number"" >
		<Value name = ""6"" description = ""bar"" required = ""True"" />
		<Value name = ""8"" description = ""bar"" />
		<Value name = ""12"" description = ""bar"" required = ""True"" />
	</Key>
</PListScheme>");
			
			var obj = scheme.Keys [0].Create ();
			IsInstanceOf<PArray> (obj, "#1");
			
			var array = (PArray)obj;
			Assert.AreEqual (2, array.Count, "#2");
			
			IsInstanceOf<PNumber> (array[0], "#3");
			IsInstanceOf<PNumber> (array[1], "#4");
			
			Assert.AreEqual (6, ((PNumber)array[0]).Value, "#5");
			Assert.AreEqual (12, ((PNumber)array[1]).Value, "#6");
		}

		[Test]
		public void CreateKey_Number_NoValue ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Number"" />
</PListScheme>");
			
			var obj = scheme.Keys [0].Create ();
			IsInstanceOf<PNumber> (obj, "#1");
			Assert.AreEqual (0, ((PNumber)obj).Value, "#2");
		}
		
		[Test]
		public void CreateKey_Number_WithValue ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Number"">
		<Value name = ""6"" description = ""bar"" />
		<Value name = ""8"" description = ""bar"" />
	</Key>
</PListScheme>");
			
			var obj = scheme.Keys [0].Create ();
			IsInstanceOf<PNumber> (obj, "#1");
			Assert.AreEqual (6, ((PNumber)obj).Value, "#2");
		}

		[Test]
		public void CreateKey_String_NoValue ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""String"" />
</PListScheme>");
			
			var obj = scheme.Keys [0].Create ();
			IsInstanceOf<PString> (obj, "#1");
			Assert.AreEqual ("", ((PString)obj).Value, "#2");
		}
		
		[Test]
		public void CreateKey_String_WithValue ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""String"">
		<Value name = ""foo"" description = ""bar"" />
		<Value name = ""baz"" description = ""bip"" />
	</Key>
</PListScheme>");
			
			var obj = scheme.Keys [0].Create ();
			IsInstanceOf<PString> (obj, "#1");
			Assert.AreEqual ("foo", ((PString)obj).Value, "#2");
		}
		
		[Test]
		public void DictionaryKey_ValueDescriptions ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"" >
		<Value _description = ""Foo""  />
		<Value _description = ""Bar""  />
	</Key>
</PListScheme>
");
			var key = scheme.GetKey ("keyname");
			Assert.AreEqual ("Foo", key.Values [0].Description, "#1");
			Assert.AreEqual ("Bar", key.Values [1].Description, "#2");
		}

		[Test]
		public void DictionaryKey_ValueRequired ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"" >
		<Value name = ""dict1"" type = ""String"" required=""True""  />
	</Key>
</PListScheme>
");
			var key = scheme.GetKey ("keyname");
			Assert.IsTrue (key.Values [0].Required, "#1");
		}

		[Test]
		public void DictionaryKey_WithValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"" >
		<Value name = ""dict1"" type = ""String""  />
		<Value name = ""dict2"" type = ""Dictionary"" />
	</Key>
</PListScheme>
");
			Assert.AreEqual (1, scheme.Keys.Count, "#1");
			
			var key = scheme.GetKey ("keyname");
			Assert.AreEqual (2, key.Values.Count, "#2");
			IsInstanceOf<PListScheme.Value> (key.Values [0], "#3");
			IsInstanceOf<PListScheme.Value> (key.Values [1], "#4");
			
			var first = (PListScheme.Value) key.Values [0];
			Assert.AreEqual ("dict1", first.Identifier, "#5");
			Assert.AreEqual ("String", first.Type, "#6");
			
			var second = (PListScheme.Value) key.Values [1];
			Assert.AreEqual ("dict2", second.Identifier, "#7");
			Assert.AreEqual ("Dictionary", second.Type, "#8");
		}
		
		[Test]
		public void DictionaryKey_WithSubValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Dictionary"" >
		<Value name = ""dict1"" type = ""Dictionary"" >
			<Value name = ""inner"" type = ""String"" >
				<Value name  = ""final"" />
			</Value>
		</Value>
	</Key>
</PListScheme>
");
			Assert.AreEqual (1, scheme.Keys.Count, "#1");
			
			var key = scheme.GetKey ("keyname");
			Assert.AreEqual (1, key.Values.Count, "#2");
			
			var dict1 = key.Values [0];
			Assert.AreEqual ("dict1", dict1.Identifier, "#3");
			Assert.AreEqual (1, dict1.Values.Count, "#4");
			
			var inner = dict1.Values [0];
			Assert.AreEqual (1, inner.Values.Count, "#5");
			
			var final = inner.Values [0];
			Assert.AreEqual ("final", final.Identifier, "#6");
		}
		
		[Test]
		public void NumberKey_WithFullValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""Number"" _description = ""text"" >
		<Value name = ""1"" /> 
		<Value name = ""2"" /> 
	</Key>
</PListScheme>");

			var key = scheme.GetKey ("keyname");
			Assert.AreEqual (2, key.Values.Count, "#1");
			Assert.AreEqual ("1", key.Values [0].Identifier, "#2");
			Assert.AreEqual ("2", key.Values [1].Identifier, "#3");
		}

		[Test]
		public void StringKey ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""String"" _description = ""text"" />
</PListScheme>");
			Assert.AreEqual (1, scheme.Keys.Count, "#1");
			
			var key = scheme.GetKey ("keyname");
			Assert.IsNull (key.ArrayType, "#2");
			Assert.AreEqual ("text", key.Description, "#3");
			Assert.AreEqual ("keyname", key.Identifier, "#4");
			Assert.AreEqual ("String", key.Type, "#5");
			Assert.AreEqual (0, key.Values.Count, "#6");
		}

		[Test]
		public void StringKey_Descriptions ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname1"" type = ""String"" _description = ""text1"" />
	<Key name = ""keyname2"" type = ""String"" _description = ""text2"" />
</PListScheme>");
			Assert.AreEqual (2, scheme.Keys.Count, "#1");
			
			var key = scheme.GetKey ("keyname1");
			Assert.AreEqual ("text1", key.Description, "#2");
			
			key = scheme.GetKey ("keyname2");
			Assert.AreEqual ("text2", key.Description, "#3");
		}

		[Test]
		public void StringKey_WithFullValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""String"" _description = ""text"" >
		<Value name = ""ValidValue1"" _description = ""desc1"" /> 
		<Value name = ""ValidValue2"" _description = ""desc2"" required=""True"" /> 
	</Key>
</PListScheme>");

			var key = scheme.GetKey ("keyname");
			Assert.AreEqual (2, key.Values.Count, "#1");
			Assert.AreEqual ("ValidValue1", key.Values [0].Identifier, "#2");
			Assert.AreEqual ("desc1", key.Values [0].Description, "#3");
			Assert.AreEqual ("ValidValue2", key.Values [1].Identifier, "#4");
			Assert.AreEqual ("desc2", key.Values [1].Description, "#5");
			Assert.IsTrue (key.Values [1].Required, "#6");
		}

		[Test]
		public void StringKey_WithPartialValue ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""keyname"" type = ""String"" _description = ""text"" >
		<Value name = ""ValidValue1"" /> 
	</Key>
</PListScheme>");

			var key = scheme.GetKey ("keyname");
			Assert.AreEqual (1, key.Values.Count, "#1");
			IsInstanceOf<PListScheme.Value> (key.Values [0], "#2");
			Assert.AreEqual ("ValidValue1", key.Values [0].Identifier, "#3");
			Assert.IsNull (key.Values [0].Description, "#4");
		}
		
		[Test]
		public void WalkScheme_Array_NotPartOfScheme ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""key1"" type = ""Array"" arrayType = ""Dictionary"" />
</PListScheme>");
			
			var tree = new PArray ();
			tree.Add (new PNumber (0));
			tree.Add (new PNumber (1));
			
			var root = new PDictionary ();
			root.Add ("foo", tree);
			
			var result = PListScheme.Match (root, scheme);
			Assert.AreEqual (3, result.Count, "#1");
			Assert.IsNull (result [tree], "#2");
			Assert.IsNull (result [tree [0]], "#3");
			Assert.IsNull (result [tree [1]], "#4");
		}
		
		[Test]
		public void WalkScheme_Dictionary_NotPartOfScheme ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""key1"" type = ""Array"" arrayType = ""Dictionary"" />
</PListScheme>");
			
			var dict = new PDictionary ();
			dict.Add ("foo", new PNumber (1));
			
			var tree = new PArray ();
			tree.Add (dict);
			tree.Add (new PNumber (1));
			
			var root = new PDictionary ();
			root.Add ("foo", tree);
			
			var result = PListScheme.Match (root, scheme);
			var keys = result.Keys.ToArray ();
			for (int i = 0; i < keys.Length; i++) {
				Assert.IsNull (result [keys [i]], "#1." + i); 
			}
		}
		
		[Test]
		public void WalkScheme_PArrayKey_RequiredValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""key1"" type = ""Array"" arrayType = ""Dictionary"" >
		<Value required = ""True"" >
			<Value name = ""val1"" type = ""Number"" required = ""True"" />
			<Value name = ""val2"" type = ""Array"" arrayType = ""String"" required = ""True"" />
		</Value>
	</Key>
</PListScheme>");
			
			var key = scheme.Keys [0];
			var root = new PDictionary ();
			var tree = (PArray) key.Create ();
			root.Add ("key1", tree);
			
			var result = PListScheme.Match (root, scheme);
			Assert.AreEqual (5, result.Count);
			Assert.AreSame (result [tree], key, "#2");
			
			var dict = (PDictionary) tree[0];
			Assert.AreEqual (result [dict], key.Values [0], "#3");
			
			var val1 = dict ["val1"];
			Assert.AreSame (result [val1], key.Values [0].Values [0], "#4");
			
			var val2 = dict ["val2"];
			Assert.AreSame (result [val2], key.Values [0].Values [1], "#5");
		}
		
		[Test]
		public void WalkScheme_PDictionaryKey_RequiredValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""key1"" type = ""Dictionary"">
		<Value name = ""val1"" type = ""Number"" required = ""True"" />
		<Value name = ""val2"" type = ""Array"" arrayType = ""Number"" required = ""True"" />
	</Key>
</PListScheme>");
			
			var key = scheme.Keys [0];
			var root = new PDictionary ();
			var tree = (PDictionary) key.Create ();
			root.Add ("key1", tree);
			
			var result = PListScheme.Match (root, scheme);
			Assert.AreEqual (4, result.Count);
			Assert.AreSame (result [tree], key, "#2");
			
			var val1 = tree ["val1"];
			Assert.AreSame (result [val1], key.Values [0], "#3");
			
			var val2 = tree ["val2"];
			Assert.AreSame (result [val2], key.Values [1], "#4");
			IsInstanceOf<PNumber> (((PArray) val2) [0], "#5");
			
		}
		
		[Test]
		public void WalkScheme_PNumberKey_WithValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""key1"" type = ""Number"">
		<Value name = ""0"" />
		<Value name = ""1"" />
		<Value name = ""2"" />
	</Key>
</PListScheme>");
			
			var key = scheme.Keys [0];
			var root = new PDictionary ();
			var tree = key.Create ();
			root.Add ("key1", tree);
			
			var result = PListScheme.Match (root, scheme);
			Assert.AreEqual (1, result.Count);
			Assert.AreSame (result [tree], key, "#2");
		}
		
		[Test]
		public void WalkScheme_PStringKey_WithValues ()
		{
			var scheme = Load (@"
<PListScheme>
	<Key name = ""key1"" type = ""String"">
		<Value name = ""A"" />
		<Value name = ""B"" />
		<Value name = ""C"" />
	</Key>
</PListScheme>");
			
			var key = scheme.Keys [0];
			var root = new PDictionary ();
			var tree = key.Create ();
			root.Add ("key1", tree);
			
			var result = PListScheme.Match (root, scheme);
			Assert.AreEqual (1, result.Count);
			Assert.AreSame (result [tree], key, "#2");
		}

		PListScheme Load (string value)
		{
			using (var reader = XmlReader.Create (new StringReader (value)))
				return PListScheme.Load (reader);
		}
		
		void IsInstanceOf<T> (object o, string message)
		{
			if (o == null || o.GetType () != typeof (T)) {
				var error = string.Format ("Expected an object of type {0} but was {1}. {2}", typeof (T), o == null ? "<NULL>" : o.GetType ().FullName, message);
				throw new NUnit.Framework.AssertionException (error);
			}
		}
	}
}

