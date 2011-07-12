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
using MonoMac.Foundation;
using System.Runtime.InteropServices;
using Gtk;



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
		
		public void Remove ()
		{
			if (Parent is PDictionary) {
				((PDictionary)Parent).Value.Remove (Key);
			} else if (Parent is PArray) {
				((PArray)Parent).Value.Remove (this);
			}
		}
		
		public abstract NSObject Convert ();
		
		public abstract void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer);
		
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
		
		public override void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer)
		{
			renderer.Sensitive = true;
			renderer.Text = Value.ToString ();
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
		
		public override void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer)
		{
			renderer.Sensitive = false;
			renderer.Text = string.Format (GettextCatalog.GetPluralString ("({0} item)", "({0} items)", Value.Count), Value.Count);
		}
		
		public override NSObject Convert ()
		{
			var dict = new NSDictionary ();
//			foreach (var pair in Value) {
//				dict[new NSString (pair.Key)] = pair.Value.Convert ();
//			}
			return dict;
		}
		
		public void Save (string fileName)
		{
			var dict = (NSDictionary)Convert ();
			dict.WriteToFile (fileName, false);
		}
		
		public static PDictionary Load (string fileName)
		{
			var dict = NSDictionary.FromFile (fileName);
			return (PDictionary)Conv (dict);
		}
		
		static IntPtr selObjCType = MonoMac.ObjCRuntime.Selector.GetHandle ("objCType");
		static PObject Conv (NSObject val)
		{
			if (val is NSDictionary) {
				var result = new PDictionary ();
				foreach (var pair in (NSDictionary)val) {
					var p = Conv (pair.Value);
					string k = pair.Key.ToString ();
					p.Key = k;
					p.Parent = result;
					result.Value[k] = p;
				}
				return result;
			}
			
			if (val is NSArray) {
				var result = new PArray ();
				foreach (var f in NSArray.ArrayFromHandle<NSObject> (((NSArray)val).Handle)) {
					var p = Conv (f);
					p.Parent = result;
					result.Value.Add (p);
				}
				return result;
			}
			
			if (val is NSString)
				return ((NSString)val).ToString ();
			if (val is NSNumber) {
				var nr = (NSNumber)val;
				var str = Marshal.PtrToStringAnsi (MonoMac.ObjCRuntime.Messaging.IntPtr_objc_msgSend (val.Handle, selObjCType));
				if (str == "c" || str == "C" || str == "B")
					return nr.BoolValue;
				return nr.Int32Value;
			}
			if (val is NSDate)
				return PDate.referenceDate + TimeSpan.FromSeconds (((NSDate)val).SecondsSinceReferenceDate);
			
			if (val is NSData) {
				var data = (NSData)val;
				var bytes = new byte[data.Length];
				Marshal.Copy (data.Bytes, bytes, 0, (int)data.Length);
				return bytes;
			}
			
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
		
		public override NSObject Convert ()
		{
			return NSArray.FromNSObjects (Value.Select (x => x.Convert ()).ToArray ());
		}
		
		public override void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer)
		{
			renderer.Sensitive = false;
			renderer.Text = string.Format (GettextCatalog.GetPluralString ("({0} item)", "({0} items)", Value.Count), Value.Count);
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
		
		public override NSObject Convert ()
		{
			return NSNumber.FromBoolean (Value);
		}
		
		public override void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer)
		{
			renderer.Sensitive = true;
			renderer.Text = Value ? GettextCatalog.GetString ("Yes") : GettextCatalog.GetString ("No");
		}
	}
	
	public class PData : PValueObject<byte[]>
	{
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Data");
			}
		}
		
		public override NSObject Convert ()
		{
			return NSData.FromArray (Value);
		}
		
		public PData (byte[] value) : base(value)
		{
		}
	}
	
	public class PDate : PValueObject<DateTime>
	{
		internal static DateTime referenceDate = new DateTime (2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Date");
			}
		}
		
		public PDate (DateTime value) : base(value)
		{
		}
		
		public override NSObject Convert ()
		{
			var secs = (Value - referenceDate).TotalSeconds;
			return NSDate.FromTimeIntervalSinceReferenceDate (secs);
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
		
		public override NSObject Convert ()
		{
			return NSNumber.FromInt32 (Value);
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
		
		public override NSObject Convert ()
		{
			return new NSString (Value);
		}
		
		public override void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer)
		{
			renderer.Sensitive = true;
			var key = Parent != null ? widget.Scheme.GetKey (Parent.Key) : null;
			if (key != null) {
				var val = key.Values.FirstOrDefault (v => v.Identifier == Value);
				if (val != null) {
					renderer.Text = GettextCatalog.GetString (val.Description);
					return;
				}
			}
			base.RenderValue (widget, renderer);
		}
	}
}
