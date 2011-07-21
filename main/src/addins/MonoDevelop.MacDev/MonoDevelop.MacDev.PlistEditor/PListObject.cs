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
using System.Text;

namespace MonoDevelop.MacDev.PlistEditor
{
	public abstract class PObject
	{
		PObject parent;
		public PObject Parent {
			get {
				return parent;
			}
			set {
				if (parent != null)
					throw new NotSupportedException ("Already parented.");
				this.parent = value;
			}
		}
		
		public abstract string TypeString {
			get;
		}
		
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
				dict.QueueRebuild ();
			} else if (Parent is PArray) {
				var arr = (PArray)Parent;
				arr.Remove (this);
				arr.QueueRebuild ();
			} else {
				if (Parent == null)
					throw new InvalidOperationException ("Can't remove from null parent");
				throw new InvalidOperationException ("Can't remove from parent " + Parent);
			}
		}
		
		public abstract NSObject Convert ();
		
		public abstract void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer);
		
		public abstract void SetValue (string text);
		
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
		
		internal bool SuppressChangeEvents;
		
		public event EventHandler Changed;
	}
	
	public abstract class PValueObject<T> : PObject
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
			return pObj != null ? pObj.Value : default(T);
		}
	}
	
	public class PDictionary : PObject, IEnumerable<KeyValuePair<string, PObject>>
	{
		Dictionary<string, PObject> dict;
		List<string> order;
		
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Dictionary");
			}
		}
		
		public PObject this[string key] {
			get {
				return dict[key];
			}
			set {
				value.Parent = this;
				if (!dict.ContainsKey (key))
					order.Add (key);
				dict[key] = value;
				QueueRebuild ();
			}
		}
		
		public void Add (string key, PObject value)
		{
			dict.Add (key, value);
			order.Add (key);
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

		public bool ContainsKey (string name)
		{
			return dict.ContainsKey (name);
		}

		public bool Remove (string key)
		{
			if (dict.Remove (key)) {
				order.Remove (key);
				return true;
			}
			return false;
		}

		public bool ChangeKey (PObject obj, string newKey)
		{
			var oldkey = GetKey (obj);
			if (oldkey == null || dict.ContainsKey (newKey))
				return false;
			dict.Remove (oldkey);
			dict.Add (newKey, obj);
			order[order.IndexOf (oldkey)] = newKey;
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
		
		
		public override void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer)
		{
			renderer.Sensitive = false;
			renderer.Text = string.Format (GettextCatalog.GetPluralString ("({0} item)", "({0} items)", dict.Count), dict.Count);
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
		
		public void Save (string fileName)
		{
			using (new NSAutoreleasePool ()) {
				var dict = (NSDictionary)Convert ();
				dict.WriteToFile (fileName, false);
			}
		}
		
		public static PDictionary Load (string fileName)
		{
			using (new NSAutoreleasePool ()) {
				var dict = NSDictionary.FromFile (fileName);
				return (PDictionary)Conv (dict);
			}
		}
		
		public void Reload (string fileName)
		{
			var pool = new NSAutoreleasePool ();
			SuppressChangeEvents = true;
			try {
				dict.Clear ();
				order.Clear ();
				var nsd = NSDictionary.FromFile (fileName);
				foreach (var pair in nsd) {
					string k = pair.Key.ToString ();
					this[k] = Conv (pair.Value);
				}
			} finally {
				SuppressChangeEvents = false;
				pool.Dispose ();
			}
			OnChanged (EventArgs.Empty);
		}
		
		static IntPtr selObjCType = MonoMac.ObjCRuntime.Selector.GetHandle ("objCType");
		static PObject Conv (NSObject val)
		{
			if (val == null)
				return null;
			if (val is NSDictionary) {
				var result = new PDictionary ();
				foreach (var pair in (NSDictionary)val) {
					string k = pair.Key.ToString ();
					result[k] = Conv (pair.Value);
				}
				return result;
			}
			
			if (val is NSArray) {
				var result = new PArray ();
				foreach (var f in NSArray.ArrayFromHandle<NSObject> (((NSArray)val).Handle)) {
					result.Add (Conv (f));
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
		
		public override string ToString ()
		{
			return string.Format ("[PDictionary: Items={0}]", dict.Count);
		}

		public void SetString (string key, string value)
		{
			var result = Get<PString> (key);
			if (result == null) {
				this[key] = result = new PString (value);
				QueueRebuild ();
				return;
			}
			result.Value = value;
		}
		
		public PString GetString (string key)
		{
			var result = Get<PString> (key);
			if (result == null) {
				this[key] = result = new PString ("");
				QueueRebuild ();
			}
			return result;
		}
		
		public PArray GetArray (string key)
		{
			var result = Get<PArray> (key);
			if (result == null) {
				this[key] = result = new PArray ();
				QueueRebuild ();
			}
			return result;
		}
		
		public void QueueRebuild ()
		{
			if (Rebuild != null)
				Rebuild (this, EventArgs.Empty);
			OnChanged (EventArgs.Empty);
		}
		
		public event EventHandler Rebuild;
	}
	
	public class PArray : PObject, IEnumerable<PObject>
	{
		List<PObject> list;
		
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Array");
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
		
		
		public void Add (PObject obj)
		{
			obj.Parent = this;
			list.Add (obj);
		}

		public void Replace (PObject oldObj, PObject newObject)
		{
			for (int i = 0; i < Count; i++) {
				if (list[i] == oldObj) {
					newObject.Parent = this;
					list[i] = newObject;
					QueueRebuild ();
					break;
				}
			}
		}
		
		public void Remove (PObject obj)
		{
			list.Remove (obj);
		}

		public void Clear ()
		{
			list.Clear ();
		}
		
		public override void SetValue (string text)
		{
			throw new NotSupportedException ();
		}
		
		public override NSObject Convert ()
		{
			return NSArray.FromNSObjects (list.Select (x => x.Convert ()).ToArray ());
		}
		
		public override void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer)
		{
			renderer.Sensitive = false;
			renderer.Text = string.Format (GettextCatalog.GetPluralString ("({0} item)", "({0} items)", Count), Count);
		}
		
		public override string ToString ()
		{
			return string.Format ("[PArray: Items={0}]", Count);
		}
		
		public void AssignStringList (string strList)
		{
			Clear ();
			foreach (var item in strList.Split (',', ' ')) {
				if (string.IsNullOrEmpty (item))
					continue;
				Add (new PString (item));
			}
			
			QueueRebuild ();
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
		
		public void QueueRebuild ()
		{
			if (Rebuild != null)
				Rebuild (this, EventArgs.Empty);
			OnChanged (EventArgs.Empty);
		}
		
		public event EventHandler Rebuild;

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
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Boolean");
			}
		}
		
		public PBoolean (bool value) : base(value)
		{
		}
		
		public override void SetValue (string text)
		{
			Value = text == GettextCatalog.GetString ("Yes");
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
		
		public override void SetValue (string text)
		{
			throw new NotSupportedException ();
		}
		
		public override void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer)
		{
			renderer.Sensitive = false;
			renderer.Text = string.Format ("byte[{0}]", Value != null ? Value.Length : 0);
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
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("Number");
			}
		}
		
		public PNumber (int value) : base(value)
		{
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
	
	public class PString : PValueObject<string>
	{
		public override string TypeString {
			get {
				return GettextCatalog.GetString ("String");
			}
		}
		
		public PString (string value) : base(value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
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
		
		public override void RenderValue (CustomPropertiesWidget widget, CellRendererCombo renderer)
		{
			renderer.Sensitive = true;
			var key = Parent != null? widget.Scheme.GetKey (Parent.Key) : null;
			if (key != null) {
				var val = key.Values.FirstOrDefault (v => v.Identifier == Value);
				if (val != null && widget.ShowDescriptions) {
					renderer.Text = GettextCatalog.GetString (val.Description);
					return;
				}
			}
			base.RenderValue (widget, renderer);
		}
	}
}