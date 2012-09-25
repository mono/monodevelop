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
using MonoDevelop.Core;
using System.Linq;
using MonoMac.Foundation;
using System.Runtime.InteropServices;
using Gtk;
using System.Text;
using MonoDevelop.Ide;
using System.IO;
using MonoMac.ObjCRuntime;

namespace MonoDevelop.MacDev.PlistEditor
{
	public abstract class PObject
	{
		static readonly IntPtr cls_NSPropertyListSerialization = Class.GetHandle ("NSPropertyListSerialization");
		static readonly IntPtr sel_dataFromPropertyList_format_options_error = Selector.GetHandle ("dataWithPropertyList:format:options:error:");
		static readonly IntPtr sel_propertyListWithData_options_format_error = Selector.GetHandle ("propertyListWithData:options:format:error:");
		
		[DllImport (MonoMac.Constants.ObjectiveCLibrary, EntryPoint="objc_msgSend")]
		static extern IntPtr IntPtr_objc_msgSend_IntPtr_Int_OutInt_OutIntPtr (
			IntPtr target,
			IntPtr selector,
			IntPtr arg0,
			int arg1,
			out int arg2,
			out IntPtr arg3);
		
		[DllImport (MonoMac.Constants.ObjectiveCLibrary, EntryPoint="objc_msgSend")]
		static extern IntPtr IntPtr_objc_msgSend_IntPtr_Int_Int_OutIntPtr (
			IntPtr target,
			IntPtr selector,
			IntPtr arg0,
			int arg1,
			int arg2,
			out IntPtr arg3);
		
		public static PObject Create (string type)
		{
			switch (type) {
			case "Array":
				return new PArray ();
			case "Dictionary":
				return new PDictionary ();
			case "Boolean":
				return new PBoolean (true);
			case "Data":
				return new PData (new byte[0]);
			case "Date":
				return new PDate (DateTime.Now);
			case "Number":
				return new PNumber (0);
			case "Real":
				return new PReal (0);
			case "String":
				return new PString ("");
			}
			LoggingService.LogError ("Unknown pobject type:" + type);
			return new PString ("<error>");
		}
		
		internal static IEnumerable<KeyValuePair<string, PObject>> ToEnumerable (PObject obj)
		{
			if (obj is PDictionary) {
				return (PDictionary) obj;
			} else if (obj is PArray) {
				return ((PArray) obj).Select (k => new KeyValuePair<string, PObject> (k is IPValueObject ? ((IPValueObject) k).Value.ToString () : null, k));
			} else {
				return Enumerable.Empty <KeyValuePair<string, PObject>> ();
			}
		}
		
		internal static NSData DataFromPropertyList (NSObject pobject, NSPropertyListFormat format, int options)
		{
			IntPtr errorPtr;
			var ptr = IntPtr_objc_msgSend_IntPtr_Int_Int_OutIntPtr (
				cls_NSPropertyListSerialization,
				sel_dataFromPropertyList_format_options_error,
				pobject.Handle, (int) format, options, out errorPtr);
			
			if (errorPtr != IntPtr.Zero) {
				var error = (NSError) MonoMac.ObjCRuntime.Runtime.GetNSObject (errorPtr);
				throw new Exception (error.LocalizedDescription);
			}
			
			if (ptr == IntPtr.Zero)
				return null;
			
			return (NSData) MonoMac.ObjCRuntime.Runtime.GetNSObject (ptr);
		}
		
		internal static NSObject PropertyListWithData (NSData data, int options, out NSPropertyListFormat format)
		{
			IntPtr errorPtr;
			int formatInt;
			var ptr = IntPtr_objc_msgSend_IntPtr_Int_OutInt_OutIntPtr (
				cls_NSPropertyListSerialization,
				sel_propertyListWithData_options_format_error,
				data.Handle, options, out formatInt, out errorPtr);
			format = (NSPropertyListFormat) formatInt;
			
			if (errorPtr != IntPtr.Zero) {
				var error = (NSError) MonoMac.ObjCRuntime.Runtime.GetNSObject (errorPtr);
				throw new Exception (error.LocalizedDescription);
			}
			
			if (ptr == IntPtr.Zero)
				return null;
			
			return MonoMac.ObjCRuntime.Runtime.GetNSObject (ptr);
		}
		
		PObject parent;
		public PObject Parent {
			get {
				return parent;
			}
			set {
				if (parent != null && value != null)
					throw new NotSupportedException ("Already parented.");
				this.parent = value;
			}
		}
		
		public abstract string TypeString {
			get;
		}

		public abstract PObject Clone ();
		
		public void Replace (PObject newObject)
		{
			var p = Parent;
			if (p is PDictionary) {
				var dict = (PDictionary)p;
				var key = dict.GetKey (this);
				if (key == null)
					return;
				Remove ();
				dict[key] = newObject;
			} else if (p is PArray) {
				var arr = (PArray)p;
				arr.Replace (this, newObject);
			}
		}
		
		public string Key {
			get {
				if (Parent is PDictionary) {
					var dict = (PDictionary)Parent;
					if (dict.Any (p => p.Value == this)) {
						var pair = dict.First (p => p.Value == this);
							return pair.Key;
					}
				}
				return null;
			}
		}
		
		public void Remove ()
		{
			if (Parent is PDictionary) {
				var dict = (PDictionary)Parent;
				dict.Remove (Key);
			} else if (Parent is PArray) {
				var arr = (PArray)Parent;
				arr.Remove (this);
			} else {
				if (Parent == null)
					throw new InvalidOperationException ("Can't remove from null parent");
				throw new InvalidOperationException ("Can't remove from parent " + Parent);
			}
		}
		
		public abstract NSObject Convert ();
		
		public abstract void SetValue (string text);
		
		public static implicit operator PObject (string value)
		{
			return new PString (value);
		}
		
		public static implicit operator PObject (int value)
		{
			return new PNumber (value);
		}
		
		public static implicit operator PObject (double value)
		{
			return new PReal (value);
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
		
		protected virtual void OnChanged (EventArgs e)
		{
			if (SuppressChangeEvents)
				return;
			
			EventHandler handler = this.Changed;
			if (handler != null)
				handler (this, e);
			
			if (Parent != null)
				Parent.OnChanged (e);
		}
		
		protected bool SuppressChangeEvents {
			get; set;
		}
		
		public event EventHandler Changed;
		
		public byte[] ToByteArray (bool binary)
		{
			using (new NSAutoreleasePool ()) {
				var pobject = Convert ();
				NSPropertyListFormat format = binary? NSPropertyListFormat.Binary : NSPropertyListFormat.Xml;
				var data = PObject.DataFromPropertyList (pobject, format, 0);
				if (data == null) {
					throw new Exception ("Could not convert the NSDictionary to the specified format");	
				}
				return data.ToArray ();
			}
		}
		
		public string ToXml ()
		{
			return Encoding.UTF8.GetString (ToByteArray (false));
		}
	}
	
	public abstract class PObjectContainer : PObject
	{
		public abstract bool Reload (string fileName);
		public abstract void Save (string fileName);
	}
	
	public interface IPValueObject
	{
		object Value { get; set; }
	}
	
	public abstract class PValueObject<T> : PObject, IPValueObject
	{
		T val;
		public T Value {
			get {
				return val;
			}
			set {
				val = value;
				OnChanged (EventArgs.Empty);
			}
		}
		
		object IPValueObject.Value {
			get { return Value; }
			set { Value = (T) value; }
		}
		
		public PValueObject (T value)
		{
			this.Value = value;
		}
		
		public PValueObject ()
		{
		}

		public static implicit operator T (PValueObject<T> pObj)
		{
			return pObj != null ? pObj.Value : default(T);
		}
	}
	
	public class PDictionary : PObjectContainer, IEnumerable<KeyValuePair<string, PObject>>
	{
		public const string Type = "Dictionary";
		Dictionary<string, PObject> dict;
		List<string> order;
		
		public override string TypeString {
			get {
				return Type;
			}
		}
		
		public PObject this[string key] {
			get {
				return dict[key];
			}
			set {
				PObject existing;
				bool exists = dict.TryGetValue (key, out existing);
				if (!exists)
					order.Add (key);
				
				dict[key] = value;
				
				if (exists)
					OnRemoved (new PObjectEventArgs (existing));
				OnAdded (new PObjectEventArgs (value));
			}
		}
		
		public EventHandler<PObjectEventArgs> Added;
		
		protected virtual void OnAdded (PObjectEventArgs e)
		{
			e.PObject.Parent = this;
			var handler = this.Added;
			if (handler != null)
				handler (this, e);
			OnChanged (EventArgs.Empty);
		}
		
		public void Add (string key, PObject value)
		{
			dict.Add (key, value);
			order.Add (key);
			OnAdded (new PObjectEventArgs (value));
		}
		
		public void InsertAfter (string keyBefore, string key, PObject value)
		{
			dict.Add (key, value);
			order.Insert (order.IndexOf (keyBefore) + 1, key);
			OnAdded (new PObjectEventArgs (value));
		}
		
		public int Count {
			get {
				return dict.Count;
			}
		}
		
		#region IEnumerable[KeyValuePair[System.String,PObject]] implementation
		public IEnumerator<KeyValuePair<string, PObject>> GetEnumerator ()
		{
			foreach (var key in order)
				yield return new KeyValuePair<string, PObject> (key, dict[key]);
		}
		#endregion

		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		#endregion
		
		public PDictionary ()
		{
			dict = new Dictionary<string, PObject> ();
			order = new List<string> ();
		}

		public override PObject Clone ()
		{
			var dict = new PDictionary ();
			foreach (var kv in this)
				dict.Add (kv.Key, kv.Value.Clone ());
			return dict;
		}

		public bool ContainsKey (string name)
		{
			return dict.ContainsKey (name);
		}
		
		public EventHandler<PObjectEventArgs> Removed;
		
		protected virtual void OnRemoved (PObjectEventArgs e)
		{
			e.PObject.Parent = null;
			var handler = this.Removed;
			if (handler != null)
				handler (this, e);
			OnChanged (EventArgs.Empty);
		}

		public bool Remove (string key)
		{
			PObject obj;
			if (dict.TryGetValue (key, out obj)) {
				dict.Remove (key);
				order.Remove (key);
				OnRemoved (new PObjectEventArgs (obj));
				return true;
			}
			return false;
		}

		public bool ChangeKey (PObject obj, string newKey)
		{
			return ChangeKey (obj, newKey, null);
		}
		
		public bool ChangeKey (PObject obj, string newKey, PObject newValue)
		{
			var oldkey = GetKey (obj);
			if (oldkey == null || dict.ContainsKey (newKey))
				return false;
			
			dict.Remove (oldkey);
			dict.Add (newKey, newValue ?? obj);
			order[order.IndexOf (oldkey)] = newKey;
			if (newValue != null) {
				OnRemoved (new PObjectEventArgs (obj));
				OnAdded (new PObjectEventArgs (newValue));
			} else {
				OnChanged (EventArgs.Empty);
			}
			return true;
		}

		public string GetKey (PObject obj)
		{
			foreach (var pair in dict) {
				if (pair.Value == obj)
					return pair.Key;
			}
			return null;
		}
		
		public T Get<T> (string key) where T : PObject
		{
			PObject obj = null;
			if (!dict.TryGetValue (key, out obj))
				return default(T);
			return (T)obj;
		}
		
		
		public bool TryGetValue<T> (string key, out T value) where T : PObject
		{
			PObject obj = null;
			if (!dict.TryGetValue (key, out obj)) {
				value = default(T);
				return false;
			}
			
			if (!(obj is T)) {
				value = default(T);
				return false;
			}
			
			value = (T)obj;
			return true;
		}

		public override void SetValue (string text)
		{
			throw new NotSupportedException ();
		}
		
		public override NSObject Convert ()
		{
			List<NSObject> objs = new List<NSObject> ();
			List<NSObject> keys = new List<NSObject> ();
			
			foreach (var key in order) {
				var val = dict[key].Convert ();
				objs.Add (val);
				keys.Add (new NSString (key));
			}
			return NSDictionary.FromObjectsAndKeys (objs.ToArray (), keys.ToArray ());
		}
		
		public override  void Save (string fileName)
		{
			using (new NSAutoreleasePool ()) {
				var dict = (NSDictionary)Convert ();
				dict.WriteToFile (fileName, false);
			}
		}
		
		public static PDictionary FromByteArray (byte[] array, out bool isBinary)
		{
			using (new NSAutoreleasePool ()) {
				NSPropertyListFormat format;
				var data = NSData.FromArray (array);
				var dict = PObject.PropertyListWithData (data, 0, out format);
				isBinary = format != NSPropertyListFormat.OpenStep;
				return (PDictionary) FromNSObject (dict);
			}
		}
		
		[Obsolete ("Use FromFile")]
		public static PDictionary Load (string fileName)
		{
			bool isBinary;
			return FromFile (fileName, out isBinary);
		}
		
		public static PDictionary FromFile (string fileName)
		{
			bool isBinary;
			return FromFile (fileName, out isBinary);
		}
		
		public static PDictionary FromFile (string fileName, out bool isBinary)
		{
			using (new NSAutoreleasePool ()) {
				NSError error;
				var data = NSData.FromFile (fileName, 0, out error);
				if (error == null) {
					NSPropertyListFormat format;
					var dict = PObject.PropertyListWithData (data, 0, out format);
					if (error == null) {
						isBinary = format != NSPropertyListFormat.OpenStep;
						return (PDictionary) FromNSObject (dict);
					}
				}
				throw new Exception (error.LocalizedDescription);
			}
		}
		
		public override bool Reload (string fileName)
		{
			if (string.IsNullOrEmpty (fileName))
				throw new ArgumentNullException ("fileName");
			var pool = new NSAutoreleasePool ();
			SuppressChangeEvents = true;
			try {
				dict.Clear ();
				order.Clear ();
				var nsd = NSDictionary.FromFile (fileName);
				if (nsd != null) {
					foreach (var pair in nsd) {
						string k = pair.Key.ToString ();
						this [k] = FromNSObject (pair.Value);
					}
				} else {
					return false;
				}
			} finally {
				SuppressChangeEvents = false;
				pool.Dispose ();
			}
			OnChanged (EventArgs.Empty);
			return true;
		}
		
		public override string ToString ()
		{
			return string.Format ("[PDictionary: Items={0}]", dict.Count);
		}

		public void SetString (string key, string value)
		{
			var result = Get<PString> (key);
			if (result == null) {
				this[key] = result = new PString (value);
				return;
			}
			result.Value = value;
			OnChanged (EventArgs.Empty);
		}
		
		public PString GetString (string key)
		{
			var result = Get<PString> (key);
			if (result == null) {
				this[key] = result = new PString ("");
			}
			return result;
		}
		
		public PArray GetArray (string key)
		{
			var result = Get<PArray> (key);
			if (result == null) {
				this[key] = result = new PArray ();
			}
			return result;
		}
		
		static IntPtr selObjCType = Selector.GetHandle ("objCType");
		
		public static PObject FromNSObject (NSObject val)
		{
			if (val == null)
				return null;
			
			var dict = val as NSDictionary;
			if (dict != null) {
				var result = new PDictionary ();
				foreach (var pair in dict) {
					string k = pair.Key.ToString ();
					result[k] = FromNSObject (pair.Value);
				}
				return result;
			}
			
			var arr = val as NSArray;
			if (arr != null) {
				var result = new PArray ();
				uint count = arr.Count;
				for (uint i = 0; i < count; i++) {
					var obj = MonoMac.ObjCRuntime.Runtime.GetNSObject (arr.ValueAt (i));
					if (obj != null)
						result.Add (FromNSObject (obj));
				}
				return result;
			}
			
			var str = val as NSString;
			if (str != null)
				return str.ToString ();
			
			var nr = val as NSNumber;
			if (nr != null) {
				char t;
				unsafe {
					t = (char) *((byte*) MonoMac.ObjCRuntime.Messaging.IntPtr_objc_msgSend (val.Handle, selObjCType));
				}
				if (t == 'c' || t == 'C' || t == 'B')
					return nr.BoolValue;
				return nr.Int32Value;
			}
			
			var date = val as NSDate;
			if (date != null)
				return (DateTime) date;
			
			var data = val as NSData;
			if (data != null) {
				var bytes = new byte[data.Length];
				Marshal.Copy (data.Bytes, bytes, 0, (int)data.Length);
				return bytes;
			}
			
			throw new NotSupportedException (val.ToString ());
		}
	}
	
	public class PArray : PObjectContainer, IEnumerable<PObject>
	{
		public const string Type = "Array";
		List<PObject> list;
		
		public override string TypeString {
			get {
				return Type;
			}
		}
		
		public int Count {
			get {
				return list.Count;
			}
		}
		
		public PObject this[int i] {
			get {
				return list[i];
			}
		}
		
		public PArray ()
		{
			list = new List<PObject> ();
		}

		public override PObject Clone ()
		{
			var array = new PArray ();
			foreach (var item in this)
				array.Add (item.Clone ());
			return array;
		}
		
		public EventHandler<PObjectEventArgs> Added;
		
		protected virtual void OnAdded (PObjectEventArgs e)
		{
			e.PObject.Parent = this;
			if (SuppressChangeEvents)
				return;
			
			var handler = this.Added;
			if (handler != null)
				handler (this, e);
			OnChanged (EventArgs.Empty);
		}
		
		public override bool Reload (string fileName)
		{
			if (string.IsNullOrEmpty (fileName))
				throw new ArgumentNullException ("fileName");
			var pool = new NSAutoreleasePool ();
			SuppressChangeEvents = true;
			try {
				list.Clear ();
				var nsa = NSArray.FromFile (fileName);
				if (nsa != null) {
					var arr = NSArray.ArrayFromHandle<NSObject> (nsa.Handle);
					foreach (var f in arr) {
						Add (PDictionary.FromNSObject (f));
					}
				} else {
					return false;
				}
			} finally {
				SuppressChangeEvents = false;
				pool.Dispose ();
			}
			OnChanged (EventArgs.Empty);
			return true;
		}
		
		public override void Save (string fileName)
		{
			using (new NSAutoreleasePool ()) {
				var arr = (NSArray)Convert ();
				arr.WriteToFile (fileName, false);
			}
		}

		public void Add (PObject obj)
		{
			list.Add (obj);
			OnAdded (new PObjectEventArgs (obj));
		}

		public void Replace (PObject oldObj, PObject newObject)
		{
			for (int i = 0; i < Count; i++) {
				if (list[i] == oldObj) {
					list[i] = newObject;
					OnRemoved (new PObjectEventArgs (oldObj));
					OnAdded (new PObjectEventArgs (newObject));
					break;
				}
			}
		}
		
		public EventHandler<PObjectEventArgs> Removed;
		
		protected virtual void OnRemoved (PObjectEventArgs e)
		{
			e.PObject.Parent = null;
			if (SuppressChangeEvents)
				return;
			
			var handler = this.Removed;
			if (handler != null)
				handler (this, e);
			OnChanged (EventArgs.Empty);
		}
		
		public void Remove (PObject obj)
		{
			if (list.Remove (obj))
				OnRemoved (new PObjectEventArgs (obj));
		}

		public void Clear ()
		{
			list.Clear ();
			OnChanged (EventArgs.Empty);
		}
		
		public override void SetValue (string text)
		{
			throw new NotSupportedException ();
		}
		
		public override NSObject Convert ()
		{
			return NSArray.FromNSObjects (list.Select (x => x.Convert ()).ToArray ());
		}
		
		public override string ToString ()
		{
			return string.Format ("[PArray: Items={0}]", Count);
		}
		
		public void AssignStringList (string strList)
		{
			SuppressChangeEvents = true;
			try {
				Clear ();
				foreach (var item in strList.Split (',', ' ')) {
					if (string.IsNullOrEmpty (item))
						continue;
					Add (new PString (item));
				}
			} finally {
				SuppressChangeEvents = false;
				OnChanged (EventArgs.Empty);
			}
		}
		
		public string ToStringList ()
		{
			var sb = new StringBuilder ();
			foreach (PString str in list.Where (o => o is PString)) {
				if (sb.Length > 0)
					sb.Append (", ");
				sb.Append (str);
			}
			return sb.ToString ();
		}
		
		#region IEnumerable[PObject] implementation
		public IEnumerator<PObject> GetEnumerator ()
		{
			return list.GetEnumerator ();
		}
		#endregion

		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return ((System.Collections.IEnumerable)list).GetEnumerator ();
		}
		#endregion
	}
	
	public class PBoolean : PValueObject<bool>
	{
		public const string Yes = "Yes";
		public const string No = "No";
		
		public const string Type = "Boolean";
		
		public override string TypeString {
			get {
				return Type;
			}
		}
		
		public PBoolean (bool value) : base(value)
		{
		}

		public override PObject Clone ()
		{
			return new PBoolean (Value);
		}
		
		public override void SetValue (string text)
		{
			if (text == Yes || text == GettextCatalog.GetString ("Yes"))
				Value = true;
			else if (text == No || text == GettextCatalog.GetString ("No"))
				Value = false;
		}
		
		public override NSObject Convert ()
		{
			return NSNumber.FromBoolean (Value);
		}
	}
	
	public class PData : PValueObject<byte[]>
	{
		public const string Type = "Data";
		static readonly byte[] Empty = new byte [0];
		
		public override string TypeString {
			get {
				return Type;
			}
		}
		
		public override NSObject Convert ()
		{
			// Work around a bug in NSData.FromArray as it cannot (currently) handle
			// zero length arrays
			if (Value.Length == 0)
				return new NSData ();
			else
				return NSData.FromArray (Value);
		}
		
		public PData (byte[] value) : base(value ?? Empty)
		{
		}

		public override PObject Clone ()
		{
			return new PData (Value);
		}
		
		public override void SetValue (string text)
		{
			throw new NotSupportedException ();
		}
	}
	
	public class PDate : PValueObject<DateTime>
	{
		public const string Type = "Date";
		internal static DateTime referenceDate = new DateTime (2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		
		public override string TypeString {
			get {
				return Type;
			}
		}
		
		public PDate (DateTime value) : base(value)
		{
		}

		public override PObject Clone ()
		{
			return new PDate (Value);
		}
		
		public override void SetValue (string text)
		{
			throw new NotImplementedException ();
		}
		
		public override NSObject Convert ()
		{
			var secs = (Value - referenceDate).TotalSeconds;
			return NSDate.FromTimeIntervalSinceReferenceDate (secs);
		}
	}
	
	public class PNumber : PValueObject<int>
	{
		public const string Type = "Number";
		
		public override string TypeString {
			get {
				return Type;
			}
		}
		
		public PNumber (int value) : base(value)
		{
		}

		public override PObject Clone ()
		{
			return new PNumber (Value);
		}
		
		public override void SetValue (string text)
		{
			int newValue;
			if (int.TryParse (text, out newValue))
				Value = newValue;
		}

		public override NSObject Convert ()
		{
			return NSNumber.FromInt32 (Value);
		}
	}

	public class PReal : PValueObject<double>
	{
		public const string Type = "Real";
		
		public override string TypeString {
			get {
				return Type;
			}
		}
		
		public PReal (double value) : base(value)
		{
		}

		public override PObject Clone ()
		{
			return new PReal (Value);
		}
		
		public override void SetValue (string text)
		{
			double newValue;
			if (double.TryParse (text, out newValue))
				Value = newValue;
		}

		public override NSObject Convert ()
		{
			return NSNumber.FromDouble (Value);
		}
	}
	
	public class PString : PValueObject<string>
	{
		public const string Type = "String";
		
		public override string TypeString {
			get {
				return Type;
			}
		}
		
		public PString (string value) : base(value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
		}

		public override PObject Clone ()
		{
			return new PString (Value);
		}
		
		public override void SetValue (string text)
		{
			if (text == null)
				throw new ArgumentNullException ("text");
			Value = text;
		}
		
		public override NSObject Convert ()
		{
			return new NSString (Value);
		}
	}
	
	[Serializable]
	public sealed class PObjectEventArgs : EventArgs
	{
		public PObject PObject {
			get;
			private set;
		}
		
		public PObjectEventArgs (PObject pObject)
		{
			this.PObject = pObject;
		}
	}
}
