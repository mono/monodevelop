// 
// PObject.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using System.Collections.Generic;
using MonoDevelop.MacDev.Plist;
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.MacDev.PlistEditor
{
	public abstract class PObject
	{
		public PObject Parent {
			get;
			set;
		}
		
		public string Key {
			get;
			set;
		}
		
		public abstract string TypeString {
			get;
		}
		
		public void Replace (PObject newObject)
		{
			// TODO
		}
		
		public abstract void RenderValue (CustomPropertiesWidget widget, CustomPropertiesWidget.CellRendererProperty renderer);
		
		public static implicit operator PObject (string value)
		{
			return new PString (value);
		}
		
		public static implicit operator PObject (int value)
		{
			return new PNumber (value);
		}
		
		public static implicit operator PObject (bool value)
		{
			return new PBoolean (value);
		}
		
		public static implicit operator PObject (DateTime value)
		{
			return new PDate (value);
		}
		
		public static implicit operator PObject (byte[] value)
		{
			return new PData (value);
		}
	}
	
	public abstract class PValueObject<T> : PObject
	{
		public T Value {
			get;
			set;
		}
		
		public PValueObject (T value)
		{
			this.Value = value;
		}
		
		public PValueObject ()
		{
		}
		
		public override void RenderValue (CustomPropertiesWidget widget, CustomPropertiesWidget.CellRendererProperty renderer)
		{
			renderer.Sensitive = true;
			renderer.RenderValue = Value.ToString ();
		}
		
		public static implicit operator T (PValueObject<T> pObj)
		{
			return pObj.Value;
		}
		
	}
	
	public class PDictionary : PValueObject<Dictionary<string, PObject>>
	{
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Dictionary");
			}
		}
		
		public PDictionary ()
		{
			Value = new Dictionary<string, PObject> ();
		}
		
		public T Get<T> (string key) where T : PObject
		{
			PObject obj = null;
			if (!Value.TryGetValue (key, out obj))
				return default(T);
			return (T)obj;
		}
		
		public override void RenderValue (CustomPropertiesWidget widget, CustomPropertiesWidget.CellRendererProperty renderer)
		{
			renderer.Sensitive = false;
			renderer.RenderValue = string.Format (GettextCatalog.GetPluralString ("({0} item)", "({0} items)", Value.Count), Value.Count);
		}
		
		public void Save (string fileName)
		{
			throw new NotImplementedException ();
		}
		
		public static PDictionary Load (string fileName)
		{
			// todo: NSDictionary loading.
			var doc = new PlistDocument ();
			doc.LoadFromXmlFile (fileName);
			return (PDictionary)Conv (doc.Root);
		}
		
		static PObject Conv (PlistObjectBase val)
		{
			if (val is PlistDictionary) {
				var result = new PDictionary ();
				foreach (var pair in ((PlistDictionary)val)) {
					var p = Conv (pair.Value);
					p.Key = pair.Key;
					p.Parent = result;
					result.Value[pair.Key] = p;
				}
				return result;
			}
			
			if (val is PlistArray) {
				var result = new PArray ();
				foreach (var f in ((PlistArray)val)) {
					var p = Conv (f);
					p.Parent = result;
					result.Value.Add (p);
				}
				return result;
			}
			
			if (val is PlistObject<string>)
				return ((PlistObject<string>)val).Value;
			if (val is PlistObject<int>)
				return ((PlistObject<int>)val).Value;
			if (val is PlistObject<bool>)
				return ((PlistObject<bool>)val).Value;
			if (val is PlistObject<DateTime>)
				return ((PlistObject<DateTime>)val).Value;
			if (val is PlistObject<byte[]>)
				return ((PlistObject<byte[]>)val).Value;
			
			throw new NotSupportedException (val.ToString ());
		}
		
	}
	
	public class PArray : PValueObject<List<PObject>>
	{
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Array");
			}
		}
		
		public PArray ()
		{
			Value = new List<PObject> ();
		}
		
		public override void RenderValue (CustomPropertiesWidget widget, CustomPropertiesWidget.CellRendererProperty renderer)
		{
			renderer.Sensitive = false;
			renderer.RenderValue = string.Format (GettextCatalog.GetPluralString ("({0} item)", "({0} items)", Value.Count), Value.Count);
		}
	}
	
	public class PBoolean : PValueObject<bool>
	{
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Boolean");
			}
		}
		
		public PBoolean (bool value) : base(value)
		{
		}
		
		public override void RenderValue (CustomPropertiesWidget widget, CustomPropertiesWidget.CellRendererProperty renderer)
		{
			renderer.Sensitive = true;
			renderer.RenderValue = Value ? GettextCatalog.GetString ("Yes") : GettextCatalog.GetString ("No");
		}
	}
	
	public class PData : PValueObject<byte[]>
	{
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Data");
			}
		}
		
		public PData (byte[] value) : base(value)
		{
		}
	}
	
	public class PDate : PValueObject<DateTime>
	{
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Date");
			}
		}
		
		public PDate (DateTime value) : base(value)
		{
		}
	}
	
	public class PNumber : PValueObject<int>
	{
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Number");
			}
		}
		
		public PNumber (int value) : base(value)
		{
		}
	}
	
	public class PString : PValueObject<string>
	{
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("String");
			}
		}
		
		public PString (string value) : base(value)
		{
		}
		
		public override void RenderValue (CustomPropertiesWidget widget, CustomPropertiesWidget.CellRendererProperty renderer)
		{
			renderer.Sensitive = true;
			var key = Parent != null ? widget.Sheme.GetKey (Parent.Key) : null;
			if (key != null) {
				var val = key.Values.FirstOrDefault (v => v.Identifier == Value);
				if (val != null) {
					renderer.RenderValue = GettextCatalog.GetString (val.Description);
					return;
				}
			}
			base.RenderValue (widget, renderer);
		}
		
	}
}
