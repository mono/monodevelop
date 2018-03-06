// 
// PObject.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//       Alex Corrado <corrado@xamarin.com>
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

// Now a purely managed implementation for plist reading & writing.
// Define POBJECT_MONOMAC to enable the conversions to/from NSObject and friends.

// Binary format reference: http://opensource.apple.com/source/CF/CF-635.21/CFBinaryPList.c

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	abstract class PObject
	{
		public static PObject Create (PObjectType type)
		{
			switch (type) {
			case PObjectType.Dictionary:
				return new PDictionary ();
			case PObjectType.Array:
				return new PArray ();
			case PObjectType.Number:
				return new PNumber (0);
			case PObjectType.Real:
				return new PReal (0);
			case PObjectType.Boolean:
				return new PBoolean (true);
			case PObjectType.Data:
				return new PData (new byte [0]);
			case PObjectType.String:
				return new PString ("");
			case PObjectType.Date:
				return new PDate (DateTime.Now);
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		public static IEnumerable<KeyValuePair<string, PObject>> ToEnumerable (PObject obj)
		{
			if (obj is PDictionary)
				return (PDictionary)obj;

			if (obj is PArray)
				return ((PArray)obj).Select (k => new KeyValuePair<string, PObject> (k is IPValueObject ? ((IPValueObject)k).Value.ToString () : null, k));

			return Enumerable.Empty<KeyValuePair<string, PObject>> ();
		}

		PObjectContainer parent;
		public PObjectContainer Parent {
			get { return parent; }
			set {
				if (parent != null && value != null)
					throw new NotSupportedException ("Already parented.");

				parent = value;
			}
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
				dict [key] = newObject;
			} else if (p is PArray) {
				var arr = (PArray)p;
				arr.Replace (this, newObject);
			}
		}

		public string Key {
			get {
				if (Parent is PDictionary) {
					var dict = (PDictionary)Parent;
					return dict.GetKey (this);
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

#if POBJECT_MONOMAC
		public abstract NSObject Convert ();
#endif

		public abstract PObjectType Type { get; }

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

		public static implicit operator PObject (byte [] value)
		{
			return new PData (value);
		}

		protected virtual void OnChanged (EventArgs e)
		{
			if (SuppressChangeEvents)
				return;

			var handler = Changed;
			if (handler != null)
				handler (this, e);

			if (Parent != null)
				Parent.OnCollectionChanged (Key, this);
		}

		protected bool SuppressChangeEvents {
			get; set;
		}

		public event EventHandler Changed;

		public byte [] ToByteArray (bool binary)
		{
			var format = binary ? PropertyListFormat.Binary : PropertyListFormat.Xml;

			using (var stream = new MemoryStream ()) {
				using (var context = format.StartWriting (stream))
					context.WriteObject (this);
				return stream.ToArray ();
			}
		}

		public string ToXml ()
		{
			return Encoding.UTF8.GetString (ToByteArray (false));
		}

#if POBJECT_MONOMAC
		static readonly IntPtr selObjCType = Selector.GetHandle ("objCType");

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
					var obj = Runtime.GetNSObject (arr.ValueAt (i));
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
				System.Runtime.InteropServices.Marshal.Copy (data.Bytes, bytes, 0, (int)data.Length);
				return bytes;
			}
			
			throw new NotSupportedException (val.ToString ());
		}
#endif

		public static PObject FromByteArray (byte [] array, int startIndex, int length, out bool isBinary)
		{
			var ctx = PropertyListFormat.Binary.StartReading (array, startIndex, length);

			isBinary = true;

			try {
				if (ctx == null) {
					isBinary = false;
					ctx = PropertyListFormat.CreateReadContext (array, startIndex, length);
					if (ctx == null)
						return null;
				}

				return ctx.ReadObject ();
			} finally {
				if (ctx != null)
					ctx.Dispose ();
			}
		}

		public static PObject FromByteArray (byte [] array, out bool isBinary)
		{
			return FromByteArray (array, 0, array.Length, out isBinary);
		}

		public static PObject FromString (string str)
		{
			var ctx = PropertyListFormat.CreateReadContext (Encoding.UTF8.GetBytes (str));
			if (ctx == null)
				return null;
			return ctx.ReadObject ();
		}

		public static PObject FromStream (Stream stream)
		{
			var ctx = PropertyListFormat.CreateReadContext (stream);
			if (ctx == null)
				return null;
			return ctx.ReadObject ();
		}
	}


	abstract class PObjectContainer : PObject
	{
		public abstract int Count { get; }

		public bool Reload (string fileName)
		{
			using (var stream = new FileStream (fileName, FileMode.Open, FileAccess.Read)) {
				using (var ctx = PropertyListFormat.CreateReadContext (stream)) {
					if (ctx == null)
						return false;

					return Reload (ctx);
				}
			}
		}

		protected abstract bool Reload (PropertyListFormat.ReadWriteContext ctx);

		public Task SaveAsync (string filename, bool atomic = false, bool binary = false)
		{
			return Task.Factory.StartNew (() => Save (filename, atomic, binary));
		}

		public void Save (string filename, bool atomic = false, bool binary = false)
		{
			var tempFile = atomic ? GetTempFileName (filename) : filename;
			try {
				if (!Directory.Exists (Path.GetDirectoryName (tempFile)))
					Directory.CreateDirectory (Path.GetDirectoryName (tempFile));

				using (var stream = new FileStream (tempFile, FileMode.Create, FileAccess.Write)) {
					using (var ctx = binary ? PropertyListFormat.Binary.StartWriting (stream) : PropertyListFormat.Xml.StartWriting (stream))
						ctx.WriteObject (this);
				}
				if (atomic) {
					if (File.Exists (filename))
						File.Replace (tempFile, filename, null, true);
					else
						File.Move (tempFile, filename);
				}
			} finally {
				if (atomic)
					File.Delete (tempFile); // just in case- no exception is raised if file is not found
			}
		}

		static string GetTempFileName (string filename)
		{
			var i = 1;
			var tempfile = filename + ".tmp";
			while (File.Exists (tempfile))
				tempfile = filename + ".tmp." + (i++).ToString ();
			return tempfile;
		}

		protected void OnChildAdded (string key, PObject child)
		{
			child.Parent = this;

			OnCollectionChanged (PObjectContainerAction.Added, key, null, child);
		}

		internal void OnCollectionChanged (string key, PObject child)
		{
			OnCollectionChanged (PObjectContainerAction.Changed, key, null, child);
		}

		protected void OnChildRemoved (string key, PObject child)
		{
			child.Parent = null;

			OnCollectionChanged (PObjectContainerAction.Removed, key, child, null);
		}

		protected void OnChildReplaced (string key, PObject oldChild, PObject newChild)
		{
			oldChild.Parent = null;
			newChild.Parent = this;

			OnCollectionChanged (PObjectContainerAction.Replaced, key, oldChild, newChild);
		}

		protected void OnCleared ()
		{
			OnCollectionChanged (PObjectContainerAction.Cleared, null, null, null);
		}

		protected void OnCollectionChanged (PObjectContainerAction action, string key, PObject oldChild, PObject newChild)
		{
			if (SuppressChangeEvents)
				return;

			var handler = CollectionChanged;
			if (handler != null)
				handler (this, new PObjectContainerEventArgs (action, key, oldChild, newChild));

			OnChanged (EventArgs.Empty);

			if (Parent != null)
				Parent.OnCollectionChanged (Key, this);
		}

		public event EventHandler<PObjectContainerEventArgs> CollectionChanged;
	}


	interface IPValueObject
	{
		object Value { get; set; }
		bool TrySetValueFromString (string text, IFormatProvider formatProvider);
	}


	abstract class PValueObject<T> : PObject, IPValueObject
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
			set { Value = (T)value; }
		}

		protected PValueObject (T value)
		{
			Value = value;
		}

		protected PValueObject ()
		{
		}

		public static implicit operator T (PValueObject<T> pObj)
		{
			return pObj != null ? pObj.Value : default (T);
		}

		public abstract bool TrySetValueFromString (string text, IFormatProvider formatProvider);
	}


	class PDictionary : PObjectContainer, IEnumerable<KeyValuePair<string, PObject>>
	{
		static readonly byte [] BeginMarkerBytes = Encoding.ASCII.GetBytes ("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		static readonly byte [] EndMarkerBytes = Encoding.ASCII.GetBytes ("</plist>");

		readonly Dictionary<string, PObject> dict;
		readonly List<string> order;

		public PObject this [string key] {
			get {
				PObject value;
				if (dict.TryGetValue (key, out value))
					return value;
				return null;
			}
			set {
				PObject existing;
				bool exists = dict.TryGetValue (key, out existing);
				if (!exists)
					order.Add (key);

				dict [key] = value;

				if (exists)
					OnChildReplaced (key, existing, value);
				else
					OnChildAdded (key, value);
			}
		}

		public void Add (string key, PObject value)
		{
			try {
				dict.Add (key, value);
			} catch (Exception e) {
				LoggingService.LogError ("error while adding " + key);
				throw e;
			}
			order.Add (key);

			OnChildAdded (key, value);
		}

		public void InsertAfter (string keyBefore, string key, PObject value)
		{
			dict.Add (key, value);
			order.Insert (order.IndexOf (keyBefore) + 1, key);

			OnChildAdded (key, value);
		}

		public override int Count {
			get { return dict.Count; }
		}

		#region IEnumerable[KeyValuePair[System.String,PObject]] implementation
		public IEnumerator<KeyValuePair<string, PObject>> GetEnumerator ()
		{
			foreach (var key in order)
				yield return new KeyValuePair<string, PObject> (key, dict [key]);
		}
		#endregion

		#region IEnumerable implementation
		IEnumerator IEnumerable.GetEnumerator ()
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

		public bool Remove (string key)
		{
			PObject obj;
			if (dict.TryGetValue (key, out obj)) {
				dict.Remove (key);
				order.Remove (key);
				OnChildRemoved (key, obj);
				return true;
			}
			return false;
		}

		public void Clear ()
		{
			dict.Clear ();
			order.Clear ();
			OnCleared ();
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
			order [order.IndexOf (oldkey)] = newKey;
			if (newValue != null) {
				OnChildRemoved (oldkey, obj);
				OnChildAdded (newKey, newValue);
			} else {
				OnChildRemoved (oldkey, obj);
				OnChildAdded (newKey, obj);
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
			PObject obj;

			if (!dict.TryGetValue (key, out obj))
				return null;

			return obj as T;
		}

		public bool TryGetValue<T> (string key, out T value) where T : PObject
		{
			PObject obj;

			if (!dict.TryGetValue (key, out obj)) {
				value = default (T);
				return false;
			}

			value = obj as T;

			return value != null;
		}

		static int IndexOf (byte [] haystack, int startIndex, byte [] needle)
		{
			int maxLength = haystack.Length - needle.Length;
			int n;

			for (int i = startIndex; i < maxLength; i++) {
				for (n = 0; n < needle.Length; n++) {
					if (haystack [i + n] != needle [n])
						break;
				}

				if (n == needle.Length)
					return i;
			}

			return -1;
		}

		public static new PDictionary FromByteArray (byte [] array, int startIndex, int length, out bool isBinary)
		{
			return (PDictionary)PObject.FromByteArray (array, startIndex, length, out isBinary);
		}

		public static new PDictionary FromByteArray (byte [] array, out bool isBinary)
		{
			return (PDictionary)PObject.FromByteArray (array, out isBinary);
		}

		public static PDictionary FromBinaryXml (byte [] array)
		{
			//find the raw plist within the .mobileprovision file
			int start = IndexOf (array, 0, BeginMarkerBytes);
			bool binary;
			int length;

			if (start < 0 || (length = (IndexOf (array, start, EndMarkerBytes) - start)) < 1)
				throw new Exception ("Did not find XML plist in buffer.");

			length += EndMarkerBytes.Length;

			return PDictionary.FromByteArray (array, start, length, out binary);
		}

		public static PDictionary FromFile (string fileName)
		{
			bool isBinary;
			return FromFile (fileName, out isBinary);
		}

		public static Task<PDictionary> FromFileAsync (string fileName)
		{
			return Task<PDictionary>.Factory.StartNew (() => {
				bool isBinary;
				return FromFile (fileName, out isBinary);
			});
		}

		public static PDictionary FromFile (string fileName, out bool isBinary)
		{
			using (var stream = new FileStream (fileName, FileMode.Open, FileAccess.Read)) {
				return FromStream(stream, out isBinary);
			}
		}

		new public static PDictionary FromStream (Stream stream)
		{
			bool isBinary;
			return FromStream (stream, out isBinary);
		}

		public static PDictionary FromStream (Stream stream, out bool isBinary)
		{
			isBinary = true;
			var ctx = PropertyListFormat.Binary.StartReading (stream);
			try {
				if (ctx == null) {
					isBinary = false;
					ctx = PropertyListFormat.CreateReadContext (stream);
					if (ctx == null)
						throw new FormatException ("Unrecognized property list format.");
				}
				return (PDictionary)ctx.ReadObject ();
			} finally {
				if (ctx != null)
					ctx.Dispose ();
			}
		}


		public static PDictionary FromBinaryXml (string fileName)
		{
			return FromBinaryXml (File.ReadAllBytes (fileName));
		}

		protected override bool Reload (PropertyListFormat.ReadWriteContext ctx)
		{
			SuppressChangeEvents = true;
			var result = ctx.ReadDict (this);
			SuppressChangeEvents = false;
			if (result)
				OnChanged (EventArgs.Empty);
			return result;
		}

		public override string ToString ()
		{
			return string.Format ("[PDictionary: Items={0}]", dict.Count);
		}

		public void SetString (string key, string value)
		{
			var result = Get<PString> (key);

			if (result == null)
				this [key] = new PString (value);
			else
				result.Value = value;
		}

		public PString GetString (string key)
		{
			var result = Get<PString> (key);

			if (result == null)
				this [key] = result = new PString ("");

			return result;
		}

		public PArray GetArray (string key)
		{
			var result = Get<PArray> (key);

			if (result == null)
				this [key] = result = new PArray ();

			return result;
		}

		public override PObjectType Type {
			get { return PObjectType.Dictionary; }
		}
	}


	class PArray : PObjectContainer, IEnumerable<PObject>
	{
		List<PObject> list;

		public override int Count {
			get { return list.Count; }
		}

		public PObject this [int i] {
			get {
				return list [i];
			}
			set {
				if (i < 0 || i >= Count)
					throw new ArgumentOutOfRangeException ();
				var existing = list [i];
				list [i] = value;

				OnChildReplaced (null, existing, value);
			}
		}

		public PArray ()
		{
			list = new List<PObject> ();
		}

		public PArray (List<PObject> list)
		{
			this.list = list;
		}

		public override PObject Clone ()
		{
			var array = new PArray ();
			foreach (var item in this)
				array.Add (item.Clone ());
			return array;
		}

		protected override bool Reload (PropertyListFormat.ReadWriteContext ctx)
		{
			SuppressChangeEvents = true;
			var result = ctx.ReadArray (this);
			SuppressChangeEvents = false;
			if (result)
				OnChanged (EventArgs.Empty);
			return result;
		}

		public void Add (PObject obj)
		{
			list.Add (obj);
			OnChildAdded (null, obj);
		}

		public void Insert (int index, PObject obj)
		{
			list.Insert (index, obj);
			OnChildAdded (null, obj);
		}

		public void Replace (PObject oldObj, PObject newObject)
		{
			for (int i = 0; i < Count; i++) {
				if (list [i] == oldObj) {
					list [i] = newObject;
					OnChildReplaced (null, oldObj, newObject);
					break;
				}
			}
		}

		public void Remove (PObject obj)
		{
			if (list.Remove (obj))
				OnChildRemoved (null, obj);
		}

		public void Clear ()
		{
			list.Clear ();
			OnCleared ();
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

		public string [] ToStringArray ()
		{
			var strlist = new List<string> ();

			foreach (PString str in list.OfType<PString> ())
				strlist.Add (str.Value);

			return strlist.ToArray ();
		}

		public string ToStringList ()
		{
			var sb = StringBuilderCache.Allocate ();
			foreach (PString str in list.OfType<PString> ()) {
				if (sb.Length > 0)
					sb.Append (", ");
				sb.Append (str);
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}

		public IEnumerator<PObject> GetEnumerator ()
		{
			return list.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return list.GetEnumerator ();
		}

		public override PObjectType Type {
			get { return PObjectType.Array; }
		}
	}


	class PBoolean : PValueObject<bool>
	{
		public PBoolean (bool value) : base (value)
		{
		}

		public override PObject Clone ()
		{
			return new PBoolean (Value);
		}

		public override PObjectType Type {
			get { return PObjectType.Boolean; }
		}

		public override bool TrySetValueFromString (string text, IFormatProvider formatProvider)
		{
			const StringComparison ic = StringComparison.OrdinalIgnoreCase;

			if ("true".Equals (text, ic) || "yes".Equals (text, ic)) {
				Value = true;
				return true;
			}

			if ("false".Equals (text, ic) || "no".Equals (text, ic)) {
				Value = false;
				return true;
			}

			return false;
		}
	}


	class PData : PValueObject<byte []>
	{
		static readonly byte [] Empty = new byte [0];

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			// Work around a bug in NSData.FromArray as it cannot (currently) handle
			// zero length arrays
			if (Value.Length == 0)
				return new NSData ();
			else
				return NSData.FromArray (Value);
		}
#endif

		public PData (byte [] value) : base (value ?? Empty)
		{
		}

		public override PObject Clone ()
		{
			return new PData (Value);
		}

		public override PObjectType Type {
			get { return PObjectType.Data; }
		}

		public override bool TrySetValueFromString (string text, IFormatProvider formatProvider)
		{
			return false;
		}
	}


	class PDate : PValueObject<DateTime>
	{
		public PDate (DateTime value) : base (value)
		{
		}

		public override PObject Clone ()
		{
			return new PDate (Value);
		}

		public override PObjectType Type {
			get { return PObjectType.Date; }
		}

		public override bool TrySetValueFromString (string text, IFormatProvider formatProvider)
		{
			DateTime result;
			if (DateTime.TryParse (text, formatProvider, DateTimeStyles.None, out result)) {
				Value = result;
				return true;
			}
			return false;
		}
	}


	class PNumber : PValueObject<int>
	{
		public PNumber (int value) : base (value)
		{
		}

		public override PObject Clone ()
		{
			return new PNumber (Value);
		}

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			return NSNumber.FromInt32 (Value);
		}
#endif

		public override PObjectType Type {
			get { return PObjectType.Number; }
		}

		public override bool TrySetValueFromString (string text, IFormatProvider formatProvider)
		{
			int result;
			if (int.TryParse (text, NumberStyles.Integer, formatProvider, out result)) {
				Value = result;
				return true;
			}
			return false;
		}
	}


	class PReal : PValueObject<double>
	{
		public PReal (double value) : base (value)
		{
		}

		public override PObject Clone ()
		{
			return new PReal (Value);
		}

		public override PObjectType Type {
			get { return PObjectType.Real; }
		}

		public override bool TrySetValueFromString (string text, IFormatProvider formatProvider)
		{
			double result;
			if (double.TryParse (text, NumberStyles.AllowDecimalPoint, formatProvider, out result)) {
				Value = result;
				return true;
			}
			return false;
		}
	}


	class PString : PValueObject<string>
	{
		public PString (string value) : base (value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
		}

		public override PObject Clone ()
		{
			return new PString (Value);
		}

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			return new NSString (Value);
		}
#endif

		public override PObjectType Type {
			get { return PObjectType.String; }
		}

		public override bool TrySetValueFromString (string text, IFormatProvider formatProvider)
		{
			Value = text;
			return true;
		}
	}


	abstract class PropertyListFormat
	{
		public static readonly PropertyListFormat Xml = new XmlFormat ();
		public static readonly PropertyListFormat Binary = new BinaryFormat ();

		// Stream must be seekable
		public static ReadWriteContext CreateReadContext (Stream input)
		{
			return Binary.StartReading (input) ?? Xml.StartReading (input);
		}

		public static ReadWriteContext CreateReadContext (byte [] array, int startIndex, int length)
		{
			return CreateReadContext (new MemoryStream (array, startIndex, length));
		}

		public static ReadWriteContext CreateReadContext (byte [] array)
		{
			return CreateReadContext (new MemoryStream (array, 0, array.Length));
		}

		// returns null if the input is not of the correct format. Stream must be seekable
		public abstract ReadWriteContext StartReading (Stream input);
		public abstract ReadWriteContext StartWriting (Stream output);

		public ReadWriteContext StartReading (byte [] array, int startIndex, int length)
		{
			return StartReading (new MemoryStream (array, startIndex, length));
		}

		public ReadWriteContext StartReading (byte [] array)
		{
			return StartReading (new MemoryStream (array, 0, array.Length));
		}

		class BinaryFormat : PropertyListFormat
		{
			// magic is bplist + 2 byte version id
			static readonly byte [] BPLIST_MAGIC = { 0x62, 0x70, 0x6C, 0x69, 0x73, 0x74 };  // "bplist"
			static readonly byte [] BPLIST_VERSION = { 0x30, 0x30 }; // "00"

			public override ReadWriteContext StartReading (Stream input)
			{
				if (input.Length < BPLIST_MAGIC.Length + 2)
					return null;

				input.Seek (0, SeekOrigin.Begin);
				for (var i = 0; i < BPLIST_MAGIC.Length; i++) {
					if ((byte)input.ReadByte () != BPLIST_MAGIC [i])
						return null;
				}

				// skip past the 2 byte version id for now
				//  we currently don't bother checking it because it seems different versions of OSX might write different values here?
				input.Seek (2, SeekOrigin.Current);
				return new Context (input, true);
			}

			public override ReadWriteContext StartWriting (Stream output)
			{
				output.Write (BPLIST_MAGIC, 0, BPLIST_MAGIC.Length);
				output.Write (BPLIST_VERSION, 0, BPLIST_VERSION.Length);

				return new Context (output, false);
			}

			class Context : ReadWriteContext
			{

				static readonly DateTime AppleEpoch = new DateTime (2001, 1, 1, 0, 0, 0, DateTimeKind.Utc); //see CFDateGetAbsoluteTime

				//https://github.com/mono/referencesource/blob/mono/mscorlib/system/datetime.cs
				const long TicksPerMillisecond = 10000;
				const long TicksPerSecond = TicksPerMillisecond * 1000;

				Stream stream;
				int currentLength;

				CFBinaryPlistTrailer trailer;

				//for writing
				List<object> objectRefs;
				int currentRef;
				long [] offsets;

				public Context (Stream stream, bool reading)
				{
					this.stream = stream;
					if (reading) {
						trailer = CFBinaryPlistTrailer.Read (this);
						ReadObjectHead ();
					}
				}

				#region Binary reading members
				protected override bool ReadBool ()
				{
					return CurrentType == PlistType.@true;
				}

				protected override void ReadObjectHead ()
				{
					var b = stream.ReadByte ();
					var len = 0L;
					var type = (PlistType)(b & 0xF0);
					if (type == PlistType.@null) {
						type = (PlistType)b;
					} else {
						len = b & 0x0F;
						if (len == 0xF) {
							ReadObjectHead ();
							len = ReadInteger ();
						}
					}
					CurrentType = type;
					currentLength = (int)len;
				}

				protected override long ReadInteger ()
				{
					switch (CurrentType) {
					case PlistType.integer:
						return ReadBigEndianInteger ((int)Math.Pow (2, currentLength));
					}

					throw new NotSupportedException ("Integer of type: " + CurrentType);
				}

				protected override double ReadReal ()
				{
					var bytes = ReadBigEndianBytes ((int)Math.Pow (2, currentLength));
					switch (CurrentType) {
					case PlistType.real:
						switch (bytes.Length) {
						case 4:
							return (double)BitConverter.ToSingle (bytes, 0);
						case 8:
							return BitConverter.ToDouble (bytes, 0);
						}
						throw new NotSupportedException (bytes.Length + "-byte real");
					}

					throw new NotSupportedException ("Real of type: " + CurrentType);
				}

				protected override DateTime ReadDate ()
				{
					var bytes = ReadBigEndianBytes (8);
					var seconds = BitConverter.ToDouble (bytes, 0);
					// We need to manually convert the seconds to ticks because
					//  .NET DateTime/TimeSpan methods dealing with (milli)seconds
					//  round to the nearest millisecond (bxc #29079)
					return AppleEpoch.AddTicks ((long)(seconds * TicksPerSecond));
				}

				protected override byte [] ReadData ()
				{
					var bytes = new byte [currentLength];
					stream.Read (bytes, 0, currentLength);
					return bytes;
				}

				protected override string ReadString ()
				{
					byte [] bytes;
					switch (CurrentType) {
					case PlistType.@string: // ASCII
						bytes = new byte [currentLength];
						stream.Read (bytes, 0, bytes.Length);
						return Encoding.ASCII.GetString (bytes);
					case PlistType.wideString: //CFBinaryPList.c: Unicode string...big-endian 2-byte uint16_t
						bytes = new byte [currentLength * 2];
						stream.Read (bytes, 0, bytes.Length);
						return Encoding.BigEndianUnicode.GetString (bytes);
					}

					throw new NotSupportedException ("String of type: " + CurrentType);
				}

				public override bool ReadArray (PArray array)
				{
					if (CurrentType != PlistType.array)
						return false;

					array.Clear ();

					// save currentLength as it will be overwritten by next ReadObjectHead call
					var len = currentLength;
					for (var i = 0; i < len; i++) {
						var obj = ReadObjectByRef ();
						if (obj != null)
							array.Add (obj);
					}

					return true;
				}

				public override bool ReadDict (PDictionary dict)
				{
					if (CurrentType != PlistType.dict)
						return false;

					dict.Clear ();

					// save currentLength as it will be overwritten by next ReadObjectHead call
					var len = currentLength;
					var keys = new string [len];
					for (var i = 0; i < len; i++)
						keys [i] = ((PString)ReadObjectByRef ()).Value;
					for (var i = 0; i < len; i++)
						dict.Add (keys [i], ReadObjectByRef ());

					return true;
				}

				PObject ReadObjectByRef ()
				{
					// read index into offset table
					var objRef = (long)ReadBigEndianUInteger (trailer.ObjectRefSize);

					// read offset in file from table
					var lastPos = stream.Position;
					stream.Seek (trailer.OffsetTableOffset + objRef * trailer.OffsetEntrySize, SeekOrigin.Begin);
					stream.Seek ((long)ReadBigEndianUInteger (trailer.OffsetEntrySize), SeekOrigin.Begin);

					ReadObjectHead ();
					var obj = ReadObject ();

					// restore original position
					stream.Seek (lastPos, SeekOrigin.Begin);
					return obj;
				}

				byte [] ReadBigEndianBytes (int count)
				{
					var bytes = new byte [count];
					stream.Read (bytes, 0, count);
					if (BitConverter.IsLittleEndian)
						Array.Reverse (bytes);
					return bytes;
				}

				long ReadBigEndianInteger (int numBytes)
				{
					var bytes = ReadBigEndianBytes (numBytes);
					switch (numBytes) {
					case 1:
						return (long)bytes [0];
					case 2:
						return (long)BitConverter.ToInt16 (bytes, 0);
					case 4:
						return (long)BitConverter.ToInt32 (bytes, 0);
					case 8:
						return BitConverter.ToInt64 (bytes, 0);
					}
					throw new NotSupportedException (bytes.Length + "-byte integer");
				}

				ulong ReadBigEndianUInteger (int numBytes)
				{
					var bytes = ReadBigEndianBytes (numBytes);
					switch (numBytes) {
					case 1:
						return (ulong)bytes [0];
					case 2:
						return (ulong)BitConverter.ToUInt16 (bytes, 0);
					case 4:
						return (ulong)BitConverter.ToUInt32 (bytes, 0);
					case 8:
						return BitConverter.ToUInt64 (bytes, 0);
					}
					throw new NotSupportedException (bytes.Length + "-byte integer");
				}

				ulong ReadBigEndianUInt64 ()
				{
					var bytes = ReadBigEndianBytes (8);
					return BitConverter.ToUInt64 (bytes, 0);
				}
				#endregion

				#region Binary writing members
				public override void WriteObject (PObject value)
				{
					if (offsets == null)
						InitOffsetTable (value);
					base.WriteObject (value);
				}

				protected override void Write (PBoolean boolean)
				{
					WriteObjectHead (boolean, boolean ? PlistType.@true : PlistType.@false);
				}

				protected override void Write (PNumber number)
				{
					if (WriteObjectHead (number, PlistType.integer))
						Write (number.Value);
				}

				protected override void Write (PReal real)
				{
					if (WriteObjectHead (real, PlistType.real))
						Write (real.Value);
				}

				protected override void Write (PDate date)
				{
					if (WriteObjectHead (date, PlistType.date)) {
						var bytes = MakeBigEndian (BitConverter.GetBytes (date.Value.Subtract (AppleEpoch).TotalSeconds));
						stream.Write (bytes, 0, bytes.Length);
					}
				}

				protected override void Write (PData data)
				{
					var bytes = data.Value;
					if (WriteObjectHead (data, PlistType.data, bytes.Length))
						stream.Write (bytes, 0, bytes.Length);
				}

				protected override void Write (PString str)
				{
					var type = PlistType.@string;
					byte [] bytes;

					if (str.Value.Any (c => c > 127)) {
						type = PlistType.wideString;
						bytes = Encoding.BigEndianUnicode.GetBytes (str.Value);
					} else {
						bytes = Encoding.ASCII.GetBytes (str.Value);
					}

					if (WriteObjectHead (str, type, str.Value.Length))
						stream.Write (bytes, 0, bytes.Length);
				}

				protected override void Write (PArray array)
				{
					if (!WriteObjectHead (array, PlistType.array, array.Count))
						return;

					var curRef = currentRef;

					foreach (var item in array)
						Write (GetObjRef (item), trailer.ObjectRefSize);

					currentRef = curRef;

					foreach (var item in array)
						WriteObject (item);
				}

				protected override void Write (PDictionary dict)
				{
					if (!WriteObjectHead (dict, PlistType.dict, dict.Count))
						return;

					// it sucks we have to loop so many times, but we gotta do it
					//  if we want to lay things out the same way apple does

					var curRef = currentRef;

					//write key refs
					foreach (var item in dict)
						Write (GetObjRef (item.Key), trailer.ObjectRefSize);

					//write value refs
					foreach (var item in dict)
						Write (GetObjRef (item.Value), trailer.ObjectRefSize);

					currentRef = curRef;

					//write keys and values
					foreach (var item in dict)
						WriteObject (item.Key);
					foreach (var item in dict)
						WriteObject (item.Value);
				}

				bool WriteObjectHead (PObject obj, PlistType type, int size = 0)
				{
					var id = GetObjRef (obj);
					if (offsets [id] != 0) // if we've already been written, don't write us again
						return false;
					offsets [id] = stream.Position;
					switch (type) {
					case PlistType.@null:
					case PlistType.@false:
					case PlistType.@true:
					case PlistType.fill:
						stream.WriteByte ((byte)type);
						break;
					case PlistType.date:
						stream.WriteByte (0x33);
						break;
					case PlistType.integer:
					case PlistType.real:
						break;
					default:
						if (size < 15) {
							stream.WriteByte ((byte)((byte)type | size));
						} else {
							stream.WriteByte ((byte)((byte)type | 0xF));
							Write (size);
						}
						break;
					}
					return true;
				}

				void Write (double value)
				{
					if (value >= float.MinValue && value <= float.MaxValue) {
						stream.WriteByte ((byte)PlistType.real | 0x2);
						var bytes = MakeBigEndian (BitConverter.GetBytes ((float)value));
						stream.Write (bytes, 0, bytes.Length);
					} else {
						stream.WriteByte ((byte)PlistType.real | 0x3);
						var bytes = MakeBigEndian (BitConverter.GetBytes (value));
						stream.Write (bytes, 0, bytes.Length);
					}
				}

				void Write (int value)
				{
					if (value < 0) { //they always write negative numbers with 8 bytes
						stream.WriteByte ((byte)PlistType.integer | 0x3);
						var bytes = MakeBigEndian (BitConverter.GetBytes ((long)value));
						stream.Write (bytes, 0, bytes.Length);
					} else if (value >= 0 && value < byte.MaxValue) {
						stream.WriteByte ((byte)PlistType.integer);
						stream.WriteByte ((byte)value);
					} else if (value >= short.MinValue && value < short.MaxValue) {
						stream.WriteByte ((byte)PlistType.integer | 0x1);
						var bytes = MakeBigEndian (BitConverter.GetBytes ((short)value));
						stream.Write (bytes, 0, bytes.Length);
					} else {
						stream.WriteByte ((byte)PlistType.integer | 0x2);
						var bytes = MakeBigEndian (BitConverter.GetBytes (value));
						stream.Write (bytes, 0, bytes.Length);
					}
				}

				void Write (long value, int byteCount)
				{
					byte [] bytes;
					switch (byteCount) {
					case 1:
						stream.WriteByte ((byte)value);
						break;
					case 2:
						bytes = MakeBigEndian (BitConverter.GetBytes ((short)value));
						stream.Write (bytes, 0, bytes.Length);
						break;
					case 4:
						bytes = MakeBigEndian (BitConverter.GetBytes ((int)value));
						stream.Write (bytes, 0, bytes.Length);
						break;
					case 8:
						bytes = MakeBigEndian (BitConverter.GetBytes (value));
						stream.Write (bytes, 0, bytes.Length);
						break;
					default:
						throw new NotSupportedException (byteCount.ToString () + "-byte integer");
					}
				}

				void InitOffsetTable (PObject topLevel)
				{
					objectRefs = new List<object> ();

					var count = 0;
					MakeObjectRefs (topLevel, ref count);
					trailer.ObjectRefSize = GetMinByteLength (count);
					offsets = new long [count];
				}

				void MakeObjectRefs (object obj, ref int count)
				{
					if (obj == null)
						return;

					if (ShouldDuplicate (obj) || !objectRefs.Any (val => PObjectEqualityComparer.Instance.Equals (val, obj))) {
						objectRefs.Add (obj);
						count++;
					}

					// for containers, also count their contents
					var pobj = obj as PObject;
					if (pobj != null) {
						switch (pobj.Type) {

						case PObjectType.Array:
							foreach (var child in (PArray)obj)
								MakeObjectRefs (child, ref count);
							break;
						case PObjectType.Dictionary:
							foreach (var child in (PDictionary)obj)
								MakeObjectRefs (child.Key, ref count);
							foreach (var child in (PDictionary)obj)
								MakeObjectRefs (child.Value, ref count);
							break;
						}
					}
				}

				static bool ShouldDuplicate (object obj)
				{
					var pobj = obj as PObject;
					if (pobj == null)
						return false;

					return pobj.Type == PObjectType.Boolean || pobj.Type == PObjectType.Array || pobj.Type == PObjectType.Dictionary ||
						(pobj.Type == PObjectType.String && ((PString)pobj).Value.Any (c => c > 255)); //LAMESPEC: this is weird. Some things are duplicated
				}

				int GetObjRef (object obj)
				{
					if (currentRef < objectRefs.Count && PObjectEqualityComparer.Instance.Equals (objectRefs [currentRef], obj))
						return currentRef++;

					return objectRefs.FindIndex (val => PObjectEqualityComparer.Instance.Equals (val, obj));
				}

				static int GetMinByteLength (long value)
				{
					if (value >= 0 && value < byte.MaxValue)
						return 1;
					if (value >= short.MinValue && value < short.MaxValue)
						return 2;
					if (value >= int.MinValue && value < int.MaxValue)
						return 4;
					return 8;
				}

				static byte [] MakeBigEndian (byte [] bytes)
				{
					if (BitConverter.IsLittleEndian)
						Array.Reverse (bytes);
					return bytes;
				}
				#endregion

				public override void Dispose ()
				{
					if (offsets != null) {
						trailer.OffsetTableOffset = stream.Position;
						trailer.OffsetEntrySize = GetMinByteLength (trailer.OffsetTableOffset);
						foreach (var offset in offsets)
							Write (offset, trailer.OffsetEntrySize);

						//LAMESPEC: seems like they always add 6 extra bytes here. not sure why
						for (var i = 0; i < 6; i++)
							stream.WriteByte ((byte)0);

						trailer.Write (this);
					}
				}

				class PObjectEqualityComparer : IEqualityComparer<object>
				{
					public static readonly PObjectEqualityComparer Instance = new PObjectEqualityComparer ();

					PObjectEqualityComparer ()
					{
					}

					public new bool Equals (object x, object y)
					{
						var vx = x as IPValueObject;
						var vy = y as IPValueObject;

						if (vx == null && vy == null)
							return EqualityComparer<object>.Default.Equals (x, y);

						if (vx == null && x != null && vy.Value != null)
							return vy.Value.Equals (x);

						if (vy == null && y != null && vx.Value != null)
							return vx.Value.Equals (y);

						if (vx == null || vy == null)
							return false;

						return vx.Value.Equals (vy.Value);
					}

					public int GetHashCode (object obj)
					{
						var valueObj = obj as IPValueObject;
						if (valueObj != null)
							return valueObj.Value.GetHashCode ();
						return obj.GetHashCode ();
					}
				}

				struct CFBinaryPlistTrailer
				{
					const int TRAILER_SIZE = 26;

					public int OffsetEntrySize;
					public int ObjectRefSize;
					public long ObjectCount;
					public long TopLevelRef;
					public long OffsetTableOffset;

					public static CFBinaryPlistTrailer Read (Context ctx)
					{
						var pos = ctx.stream.Position;
						ctx.stream.Seek (-TRAILER_SIZE, SeekOrigin.End);
						var result = new CFBinaryPlistTrailer {
							OffsetEntrySize = ctx.stream.ReadByte (),
							ObjectRefSize = ctx.stream.ReadByte (),
							ObjectCount = (long)ctx.ReadBigEndianUInt64 (),
							TopLevelRef = (long)ctx.ReadBigEndianUInt64 (),
							OffsetTableOffset = (long)ctx.ReadBigEndianUInt64 ()
						};
						ctx.stream.Seek (pos, SeekOrigin.Begin);
						return result;
					}

					public void Write (Context ctx)
					{
						byte [] bytes;
						ctx.stream.WriteByte ((byte)OffsetEntrySize);
						ctx.stream.WriteByte ((byte)ObjectRefSize);
						//LAMESPEC: apple's comments say this is the number of entries in the offset table, but this really *is* number of objects??!?!
						bytes = MakeBigEndian (BitConverter.GetBytes ((long)ctx.objectRefs.Count));
						ctx.stream.Write (bytes, 0, bytes.Length);
						bytes = new byte [8]; //top level always at offset 0
						ctx.stream.Write (bytes, 0, bytes.Length);
						bytes = MakeBigEndian (BitConverter.GetBytes (OffsetTableOffset));
						ctx.stream.Write (bytes, 0, bytes.Length);
					}
				}
			}
		}

		// Adapted from:
		//https://github.com/mono/monodevelop/blob/07d9e6c07e5be8fe1d8d6f4272d3969bb087a287/main/src/addins/MonoDevelop.MacDev/MonoDevelop.MacDev.Plist/PlistDocument.cs
		class XmlFormat : PropertyListFormat
		{
			const string PLIST_HEADER = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
";
			static readonly Encoding outputEncoding = new UTF8Encoding (false, false);

			public override ReadWriteContext StartReading (Stream input)
			{
				//allow DTD but not try to resolve it from web
				var settings = new XmlReaderSettings () {
					CloseInput = true,
					DtdProcessing = DtdProcessing.Ignore,
					XmlResolver = null,
				};

				XmlReader reader = null;
				input.Seek (0, SeekOrigin.Begin);
				try {
					reader = XmlReader.Create (input, settings);
					reader.ReadToDescendant ("plist");
					while (reader.Read () && reader.NodeType != XmlNodeType.Element)
						;
				} catch (Exception ex) {
					Console.WriteLine ("Exception: {0}", ex);
				}

				if (reader == null || reader.EOF)
					return null;

				return new Context (reader);
			}

			public override ReadWriteContext StartWriting (Stream output)
			{
				var writer = new StreamWriter (output, outputEncoding);
				writer.Write (PLIST_HEADER);

				return new Context (writer);
			}

			class Context : ReadWriteContext
			{
				const string DATETIME_FORMAT = "yyyy-MM-dd'T'HH:mm:ssK";

				XmlReader reader;
				TextWriter writer;

				int indentLevel;
				string indentString;

				public Context (XmlReader reader)
				{
					this.reader = reader;
					ReadObjectHead ();
				}
				public Context (TextWriter writer)
				{
					this.writer = writer;
					indentString = "";
				}

				#region XML reading members
				protected override void ReadObjectHead ()
				{
					try {
						CurrentType = (PlistType)Enum.Parse (typeof (PlistType), reader.LocalName);
					} catch (Exception ex) {
						throw new ArgumentException (string.Format ("Failed to parse PList data type: {0}", reader.LocalName), ex);
					}
				}

				protected override bool ReadBool ()
				{
					// Create the PBoolean object, then move to the xml reader to next node
					// so we are ready to parse the next object. 'bool' types don't have
					// content so we have to move the reader manually, unlike integers which
					// implicitly move to the next node because we parse the content.
					var result = CurrentType == PlistType.@true;
					reader.Read ();
					return result;
				}

				protected override long ReadInteger ()
				{
					return reader.ReadElementContentAsLong ();
				}

				protected override double ReadReal ()
				{
					return reader.ReadElementContentAsDouble ();
				}

				protected override DateTime ReadDate ()
				{
					return DateTime.ParseExact (reader.ReadElementContentAsString (), DATETIME_FORMAT, CultureInfo.InvariantCulture).ToUniversalTime ();
				}

				protected override byte [] ReadData ()
				{
					return Convert.FromBase64String (reader.ReadElementContentAsString ());
				}

				protected override string ReadString ()
				{
					return reader.ReadElementContentAsString ();
				}

				public override bool ReadArray (PArray array)
				{
					if (CurrentType != PlistType.array)
						return false;

					array.Clear ();

					if (reader.IsEmptyElement) {
						reader.Read ();
						return true;
					}

					// advance to first node
					reader.ReadStartElement ();
					while (!reader.EOF && reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement) {
						if (!reader.Read ())
							break;
					}

					while (!reader.EOF && reader.NodeType != XmlNodeType.EndElement) {
						if (reader.NodeType == XmlNodeType.Element) {
							ReadObjectHead ();

							var val = ReadObject ();
							if (val != null)
								array.Add (val);
						} else if (!reader.Read ()) {
							break;
						}
					}

					if (!reader.EOF && reader.NodeType == XmlNodeType.EndElement && reader.Name == "array") {
						reader.ReadEndElement ();
						return true;
					}

					return false;
				}

				public override bool ReadDict (PDictionary dict)
				{
					if (CurrentType != PlistType.dict)
						return false;

					dict.Clear ();

					if (reader.IsEmptyElement) {
						reader.Read ();
						return true;
					}

					reader.ReadToDescendant ("key");

					while (!reader.EOF && reader.NodeType == XmlNodeType.Element) {
						var key = reader.ReadElementString ();

						while (!reader.EOF && reader.NodeType != XmlNodeType.Element && reader.Read ()) {
							if (reader.NodeType == XmlNodeType.EndElement)
								throw new FormatException (string.Format ("No value found for key {0}", key));
						}

						ReadObjectHead ();
						var result = ReadObject ();
						if (result != null)
							dict.Add (key, result);

						do {
							if (reader.NodeType == XmlNodeType.Element && reader.Name == "key")
								break;

							if (reader.NodeType == XmlNodeType.EndElement)
								break;
						} while (reader.Read ());
					}

					if (!reader.EOF && reader.NodeType == XmlNodeType.EndElement && reader.Name == "dict") {
						reader.ReadEndElement ();
						return true;
					}

					return false;
				}
				#endregion

				#region XML writing members
				protected override void Write (PBoolean boolean)
				{
					WriteLine (boolean.Value ? "<true/>" : "<false/>");
				}

				protected override void Write (PNumber number)
				{
					WriteLine ("<integer>" + SecurityElement.Escape (number.Value.ToString (CultureInfo.InvariantCulture)) + "</integer>");
				}

				protected override void Write (PReal real)
				{
					WriteLine ("<real>" + SecurityElement.Escape (real.Value.ToString (CultureInfo.InvariantCulture)) + "</real>");
				}

				protected override void Write (PDate date)
				{
					WriteLine ("<date>" + SecurityElement.Escape (date.Value.ToString (DATETIME_FORMAT, CultureInfo.InvariantCulture)) + "</date>");
				}

				protected override void Write (PData data)
				{
					WriteLine ("<data>" + SecurityElement.Escape (Convert.ToBase64String (data.Value)) + "</data>");
				}

				protected override void Write (PString str)
				{
					WriteLine ("<string>" + SecurityElement.Escape (str.Value) + "</string>");
				}

				protected override void Write (PArray array)
				{
					if (array.Count == 0) {
						WriteLine ("<array/>");
						return;
					}

					WriteLine ("<array>");
					IncreaseIndent ();

					foreach (var item in array)
						WriteObject (item);

					DecreaseIndent ();
					WriteLine ("</array>");
				}

				protected override void Write (PDictionary dict)
				{
					if (dict.Count == 0) {
						WriteLine ("<dict/>");
						return;
					}

					WriteLine ("<dict>");
					IncreaseIndent ();

					foreach (var kv in dict) {
						WriteLine ("<key>" + SecurityElement.Escape (kv.Key) + "</key>");
						WriteObject (kv.Value);
					}

					DecreaseIndent ();
					WriteLine ("</dict>");
				}

				void WriteLine (string value)
				{
					writer.Write (indentString);
					writer.Write (value);
					writer.Write ('\n');
				}

				void IncreaseIndent ()
				{
					indentString = new string ('\t', ++indentLevel);
				}

				void DecreaseIndent ()
				{
					indentString = new string ('\t', --indentLevel);
				}
				#endregion

				public override void Dispose ()
				{
					if (writer != null) {
						writer.Write ("</plist>\n");
						writer.Flush ();
						writer.Dispose ();
					}
				}
			}
		}

		public abstract class ReadWriteContext : IDisposable
		{
			// Binary: The type is encoded in the 4 high bits; the low bits are data (except: null, true, false)
			// Xml: The enum value name == element tag name (this actually reads a superset of the format, since null, fill and wideString are not plist xml elements afaik)
			protected enum PlistType : byte
			{
				@null = 0x00,
				@false = 0x08,
				@true = 0x09,
				fill = 0x0F,
				integer = 0x10,
				real = 0x20,
				date = 0x30,
				data = 0x40,
				@string = 0x50,
				wideString = 0x60,
				array = 0xA0,
				dict = 0xD0,
			}

			#region Reading members
			public PObject ReadObject ()
			{
				switch (CurrentType) {
				case PlistType.@true:
				case PlistType.@false:
					return new PBoolean (ReadBool ());
				case PlistType.fill:
					ReadObjectHead ();
					return ReadObject ();

				case PlistType.integer:
					return new PNumber ((int)ReadInteger ()); //FIXME: should PNumber handle 64-bit values? ReadInteger can if necessary
				case PlistType.real:
					return new PReal (ReadReal ());    //FIXME: we should probably make PNumber take floating point as well as ints

				case PlistType.date:
					return new PDate (ReadDate ());
				case PlistType.data:
					return new PData (ReadData ());

				case PlistType.@string:
				case PlistType.wideString:
					return new PString (ReadString ());

				case PlistType.array:
					var array = new PArray ();
					ReadArray (array);
					return array;

				case PlistType.dict:
					var dict = new PDictionary ();
					ReadDict (dict);
					return dict;
				}
				return null;
			}

			protected abstract void ReadObjectHead ();
			protected PlistType CurrentType { get; set; }

			protected abstract bool ReadBool ();
			protected abstract long ReadInteger ();
			protected abstract double ReadReal ();
			protected abstract DateTime ReadDate ();
			protected abstract byte [] ReadData ();
			protected abstract string ReadString ();

			public abstract bool ReadArray (PArray array);
			public abstract bool ReadDict (PDictionary dict);
			#endregion

			#region Writing members
			public virtual void WriteObject (PObject value)
			{
				switch (value.Type) {
				case PObjectType.Boolean:
					Write ((PBoolean)value);
					return;
				case PObjectType.Number:
					Write ((PNumber)value);
					return;
				case PObjectType.Real:
					Write ((PReal)value);
					return;
				case PObjectType.Date:
					Write ((PDate)value);
					return;
				case PObjectType.Data:
					Write ((PData)value);
					return;
				case PObjectType.String:
					Write ((PString)value);
					return;
				case PObjectType.Array:
					Write ((PArray)value);
					return;
				case PObjectType.Dictionary:
					Write ((PDictionary)value);
					return;
				}
				throw new NotSupportedException (value.Type.ToString ());
			}

			protected abstract void Write (PBoolean boolean);
			protected abstract void Write (PNumber number);
			protected abstract void Write (PReal real);
			protected abstract void Write (PDate date);
			protected abstract void Write (PData data);
			protected abstract void Write (PString str);
			protected abstract void Write (PArray array);
			protected abstract void Write (PDictionary dict);
			#endregion

			public abstract void Dispose ();
		}
	}


	enum PObjectContainerAction
	{
		Added,
		Changed,
		Removed,
		Replaced,
		Cleared
	}


	sealed class PObjectContainerEventArgs : EventArgs
	{
		internal PObjectContainerEventArgs (PObjectContainerAction action, string key, PObject oldItem, PObject newItem)
		{
			Action = action;
			Key = key;
			OldItem = oldItem;
			NewItem = newItem;
		}

		public PObjectContainerAction Action {
			get; private set;
		}

		public string Key {
			get; private set;
		}

		public PObject OldItem {
			get; private set;
		}

		public PObject NewItem {
			get; private set;
		}
	}


	enum PObjectType
	{
		Dictionary,
		Array,
		Real,
		Number,
		Boolean,
		Data,
		String,
		Date
	}
}