// ObjectValue.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
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
//
//

using System;
using System.Text;
using Mono.Debugging.Backend;
using System.Collections.Generic;
using System.Threading;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class ObjectValue
	{
		ObjectPath path;
		int arrayCount = -1;
		bool isNull;
		string name;
		string value;
		string typeName;
		string displayValue;
		string childSelector;
		ObjectValueFlags flags;
		IObjectValueSource source;
		IObjectValueUpdater updater;
		List<ObjectValue> children;
		ManualResetEvent evaluatedEvent;

		[NonSerialized]
		UpdateCallback updateCallback;
		
		[NonSerialized]
		EventHandler valueChanged;
		
		[NonSerialized]
		StackFrame parentFrame;
		
		static ObjectValue Create (IObjectValueSource source, ObjectPath path, string typeName)
		{
			ObjectValue ob = new ObjectValue ();
			ob.source = source;
			ob.path = path;
			ob.typeName = typeName;
			return ob;
		}
		
		public static ObjectValue CreateObject (IObjectValueSource source, ObjectPath path, string typeName, string value, ObjectValueFlags flags, ObjectValue[] children)
		{
			return CreateObject (source, path, typeName, new EvaluationResult (value), flags, children);
		}
		
		public static ObjectValue CreateObject (IObjectValueSource source, ObjectPath path, string typeName, EvaluationResult value, ObjectValueFlags flags, ObjectValue[] children)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.path = path;
			ob.flags = flags | ObjectValueFlags.Object;
			ob.value = value.Value;
			ob.displayValue = value.DisplayValue;
			if (children != null) {
				ob.children = new List<ObjectValue> ();
				ob.children.AddRange (children);
			}
			return ob;
		}
		
		public static ObjectValue CreateNullObject (IObjectValueSource source, string name, string typeName, ObjectValueFlags flags)
		{
			return CreateNullObject (source, new ObjectPath (name), typeName, flags);
		}
		
		public static ObjectValue CreateNullObject (IObjectValueSource source, ObjectPath path, string typeName, ObjectValueFlags flags)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.flags = flags | ObjectValueFlags.Object;
			ob.value = "(null)";
			ob.isNull = true;
			return ob;
		}
		
		public static ObjectValue CreatePrimitive (IObjectValueSource source, ObjectPath path, string typeName, EvaluationResult value, ObjectValueFlags flags)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.flags = flags | ObjectValueFlags.Primitive;
			ob.value = value.Value;
			ob.displayValue = value.DisplayValue;
			return ob;
		}
		
		public static ObjectValue CreateArray (IObjectValueSource source, ObjectPath path, string typeName, int arrayCount, ObjectValueFlags flags, ObjectValue[] children)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.arrayCount = arrayCount;
			ob.flags = flags | ObjectValueFlags.Array;
			ob.value = "[" + arrayCount + "]";
			if (children != null && children.Length > 0) {
				ob.children = new List<ObjectValue> ();
				ob.children.AddRange (children);
			}
			return ob;
		}
		
		public static ObjectValue CreateUnknown (IObjectValueSource source, ObjectPath path, string typeName)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.flags = ObjectValueFlags.Unknown | ObjectValueFlags.ReadOnly;
			return ob;
		}
		
		public static ObjectValue CreateUnknown (string name)
		{
			return CreateUnknown (null, new ObjectPath (name), "");
		}
		
		public static ObjectValue CreateError (IObjectValueSource source, ObjectPath path, string typeName, string value, ObjectValueFlags flags)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.flags = flags | ObjectValueFlags.Error;
			ob.value = value;
			return ob;
		}
		
		public static ObjectValue CreateImplicitNotSupported (IObjectValueSource source, ObjectPath path, string typeName, ObjectValueFlags flags)
		{
			return CreateNotSupported (source, path, typeName, "Implicit evaluation is disabled", flags);
		}
		
		public static ObjectValue CreateNotSupported (IObjectValueSource source, ObjectPath path, string typeName, string message, ObjectValueFlags flags)
		{
			ObjectValue ob = Create (source, path, typeName);
			ob.flags = flags | ObjectValueFlags.NotSupported;
			ob.value = message;
			return ob;
		}
		
		public static ObjectValue CreateFatalError (string name, string message, ObjectValueFlags flags)
		{
			ObjectValue ob = new ObjectValue ();
			ob.flags = flags | ObjectValueFlags.Error;
			ob.value = message;
			ob.name = name;
			return ob;
		}
		
		public static ObjectValue CreateEvaluating (IObjectValueUpdater updater, ObjectPath path, ObjectValueFlags flags)
		{
			ObjectValue ob = Create (null, path, null);
			ob.updater = updater;
			ob.path = path;
			ob.flags = flags | ObjectValueFlags.Evaluating;
			return ob;
		}
		
		/// <summary>
		/// Gets the flags of the value
		/// </summary>
		public ObjectValueFlags Flags {
			get { return flags; }
		}

		/// <summary>
		/// Name of the value (for example, the property name)
		/// </summary>
		public string Name {
			get {
				if (name == null)
					return path [path.Length - 1];
				else
					return name;
			}
			set {
				name = value;
			}
		}

		/// <summary>
		/// Gets or sets the value of the object
		/// </summary>
		/// <value>
		/// The value.
		/// </value>
		/// <exception cref='InvalidOperationException'>
		/// Is thrown when trying to set a value on a read-only ObjectValue
		/// </exception>
		/// <remarks>
		/// This value is a string representation of the ObjectValue. The content depends on several evaluation
		/// options. For example, if ToString calls are enabled, this value will be the result of calling
		/// ToString.
		/// If the object is a primitive type, in general the Value will be an expression that represents the
		/// value in the target language. For example, when debugging C#, if the property is an string, the value
		/// will include the quotation marks and chars like '\' will be properly escaped.
		/// If you need to get the real CLR value of the object, use GetRawValue.
		/// </remarks>
		public virtual string Value {
			get {
				return value;
			}
			set {
				if (IsReadOnly || source == null)
					throw new InvalidOperationException ("Value is not editable");
				EvaluationResult res = source.SetValue (path, value, null);
				if (res != null) {
					this.value = res.Value;
					displayValue = res.DisplayValue;
					isNull = value == null;
				}
			}
		}

		/// <summary>
		/// Gets or sets the display value of this object
		/// </summary>
		/// <remarks>
		/// This method returns a string to be used when showing the value of this object.
		/// In most cases, the Value and DisplayValue properties return the same text, but there are some cases
		/// in which DisplayValue may return a more convenient textual representation of the value, which
		/// may not be a valid target language expression.
		/// For example in C#, an enum Value includes the full enum type name (e.g. "Gtk.ResponseType.OK"),
		/// while DisplayValue only has the enum value name ("OK").
		/// </remarks>
		public string DisplayValue {
			get { return displayValue ?? Value; }
			set { displayValue = value; }
		}
		
		/// <summary>
		/// Sets the value of this object, using the default evaluation options
		/// </summary>
		public void SetValue (string value)
		{
			SetValue (value, parentFrame.DebuggerSession.EvaluationOptions);
		}
		
		/// <summary>
		/// Sets the value of this object, using the specified evaluation options
		/// </summary>
		/// <param name='value'>
		/// The value
		/// </param>
		/// <param name='options'>
		/// The options
		/// </param>
		/// <exception cref='InvalidOperationException'>
		/// Is thrown if the value is read-only
		/// </exception>
		public void SetValue (string value, EvaluationOptions options)
		{
			if (IsReadOnly || source == null)
				throw new InvalidOperationException ("Value is not editable");
			EvaluationResult res = source.SetValue (path, value, options);
			if (res != null) {
				this.value = res.Value;
				displayValue = res.DisplayValue;
			}
		}
		
		/// <summary>
		/// Gets the raw value of this object
		/// </summary>
		/// <returns>
		/// The raw value.
		/// </returns>
		/// <remarks>
		/// This method can be used to get the CLR value of the object. For example, if this ObjectValue is
		/// a property of type String, this method will return the System.String value of the property.
		/// If this ObjectValue refers to an object instead of a primitive value, then a RawValue object
		/// will be returned. RawValue can be used to get and set members of an object, and to call methods.
		/// If this ObjectValue refers to an array, then a RawValueArray object will be returned.
		/// </remarks>
		public object GetRawValue ()
		{
			EvaluationOptions ops = parentFrame.DebuggerSession.EvaluationOptions.Clone ();
			ops.EllipsizeStrings = false;
			
			return GetRawValue (ops);
		}
		
		/// <summary>
		/// Gets the raw value of this object
		/// </summary>
		/// <param name='options'>
		/// The evaluation options
		/// </param>
		/// <returns>
		/// The raw value.
		/// </returns>
		/// <remarks>
		/// This method can be used to get the CLR value of the object. For example, if this ObjectValue is
		/// a property of type String, this method will return the System.String value of the property.
		/// If this ObjectValue refers to an object instead of a primitive value, then a RawValue object
		/// will be returned. RawValue can be used to get and set members of an object, and to call methods.
		/// If this ObjectValue refers to an array, then a RawValueArray object will be returned.
		/// </remarks>
		public object GetRawValue (EvaluationOptions options)
		{
			object res = source.GetRawValue (path, options);
			RawValue val = res as RawValue;
			if (val != null)
				val.options = options;
			return res;
		}
		
		/// <summary>
		/// Sets the raw value of this object
		/// </summary>
		/// <param name='value'>
		/// The value
		/// </param>
		/// <remarks>
		/// The provided value can be a primitive type, a RawValue object or a RawValueArray object.
		/// </remarks>
		public void SetRawValue (object value)
		{
			SetRawValue (value, parentFrame.DebuggerSession.EvaluationOptions);
		}
		
		/// <summary>
		/// Sets the raw value of this object
		/// </summary>
		/// <param name='value'>
		/// The value
		/// </param>
		/// <param name='options'>
		/// The evaluation options
		/// </param>
		/// <remarks>
		/// The provided value can be a primitive type, a RawValue object or a RawValueArray object.
		/// </remarks>
		public void SetRawValue (object value, EvaluationOptions options)
		{
			source.SetRawValue (path, value, options);
		}
		
		/// <summary>
		/// Full name of the type of the object
		/// </summary>
		public string TypeName {
			get { return typeName; }
			set { typeName = value; }
		}
		
		/// <summary>
		/// Gets or sets the child selector.
		/// </summary>
		/// <remarks>
		/// The child selector is an expression which can be concatenated to a parent expression to get this child.
		/// For example, if this object is a reference to a field named 'foo' of an object, the child
		/// selector is '.foo'.
		/// </remarks>
		public string ChildSelector {
			get {
				if (childSelector != null)
					return childSelector;
				if ((flags & ObjectValueFlags.ArrayElement) != 0)
					return Name;
				else
					return "." + Name;
			}
			set { childSelector = value; }
		}
		
		/// <summary>
		/// Gets a value indicating whether this object has children.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance has children; otherwise, <c>false</c>.
		/// </value>
		public bool HasChildren {
			get {
				if (isNull)
					return false;
				if (IsEvaluating)
					return false;
				if (children != null)
					return children.Count > 0;
				else if (source == null)
					return false;
				else if (IsArray)
					return arrayCount > 0;
				else if (IsObject)
					return true;
				else
					return false;
			}
		}

		/// <summary>
		/// Gets a child value
		/// </summary>
		/// <returns>
		/// The child.
		/// </returns>
		/// <param name='name'>
		/// Name of the member
		/// </param>
		/// <remarks>
		/// This method can be used to get a member of an object (such as a field or property)
		/// </remarks>
		public ObjectValue GetChild (string name)
		{
			return GetChild (name, parentFrame.DebuggerSession.EvaluationOptions);
		}
		
		/// <summary>
		/// Gets a child value
		/// </summary>
		/// <returns>
		/// The child.
		/// </returns>
		/// <param name='name'>
		/// Name of the member
		/// </param>
		/// <param name='options'>
		/// Options to be used to evaluate the child
		/// </param>
		/// <remarks>
		/// This method can be used to get a member of an object (such as a field or property)
		/// </remarks>
		public ObjectValue GetChild (string name, EvaluationOptions options)
		{
			if (IsArray)
				throw new InvalidOperationException ("Object is an array.");
			if (IsEvaluating)
				return null;
			
			if (children == null) {
				children = new List<ObjectValue> ();
				if (source != null) {
					try {
						ObjectValue[] cs = source.GetChildren (path, -1, -1, options);
						ConnectCallbacks (parentFrame, cs);
						children.AddRange (cs);
					} catch (Exception ex) {
						children = null;
						return CreateFatalError ("", ex.Message, ObjectValueFlags.ReadOnly);
					}
				}
			}
			
			foreach (ObjectValue ob in children) {
				if (ob.Name == name)
					return ob;
			}
			
			return null;
		}
		
		/// <summary>
		/// Gets all children of the object
		/// </summary>
		/// <returns>
		/// An array of all child values
		/// </returns>
		public ObjectValue[] GetAllChildren ()
		{
			return GetAllChildren (parentFrame.DebuggerSession.EvaluationOptions);
		}
		
		/// <summary>
		/// Gets all children of the object
		/// </summary>
		/// <returns>
		/// An array of all child values
		/// </returns>
		/// <param name='options'>
		/// Options to be used to evaluate the children
		/// </param>
		public ObjectValue[] GetAllChildren (EvaluationOptions options)
		{
			if (IsEvaluating)
				return new ObjectValue[0];
			
			if (IsArray) {
				GetArrayItem (arrayCount - 1);
				return children.ToArray ();
			} else {
				if (children == null) {
					children = new List<ObjectValue> ();
					if (source != null) {
						try {
							ObjectValue[] cs = source.GetChildren (path, -1, -1, options);
							ConnectCallbacks (parentFrame, cs);
							children.AddRange (cs);
						} catch (Exception ex) {
							if (parentFrame != null)
								parentFrame.DebuggerSession.OnDebuggerOutput (true, ex.ToString ());
							children.Add (CreateFatalError ("", ex.Message, ObjectValueFlags.ReadOnly));
						}
					}
				}
				return children.ToArray ();
			}
		}
		
		/// <summary>
		/// Gets an item of an array
		/// </summary>
		/// <returns>
		/// The array item.
		/// </returns>
		/// <param name='index'>
		/// Item index
		/// </param>
		/// <exception cref='InvalidOperationException'>
		/// Is thrown if this object is not an array (IsArray returns false)
		/// </exception>
		public ObjectValue GetArrayItem (int index)
		{
			return GetArrayItem (index, parentFrame.DebuggerSession.EvaluationOptions);
		}
		
		/// <summary>
		/// Gets an item of an array
		/// </summary>
		/// <returns>
		/// The array item.
		/// </returns>
		/// <param name='index'>
		/// Item index
		/// </param>
		/// <param name='options'>
		/// Options to be used to evaluate the item
		/// </param>
		/// <exception cref='InvalidOperationException'>
		/// Is thrown if this object is not an array (IsArray returns false)
		/// </exception>
		public ObjectValue GetArrayItem (int index, EvaluationOptions options)
		{
			if (!IsArray)
				throw new InvalidOperationException ("Object is not an array.");
			if (index >= arrayCount || index < 0 || IsEvaluating)
				throw new IndexOutOfRangeException ();
			
			if (children == null)
				children = new List<ObjectValue> ();
			if (index >= children.Count) {
				int nc = (index + 50);
				if (nc > arrayCount) nc = arrayCount;
				nc = nc - children.Count;
				try {
					ObjectValue[] items = source.GetChildren (path, children.Count, nc, options);
					ConnectCallbacks (parentFrame, items);
					children.AddRange (items);
				} catch (Exception ex) {
					return CreateFatalError ("", ex.Message, ObjectValueFlags.ArrayElement | ObjectValueFlags.ReadOnly);
				}
			}
			return children [index];
		}
		
		/// <summary>
		/// Gets the number of items of an array
		/// </summary>
		/// <exception cref='InvalidOperationException'>
		/// Is thrown if this object is not an array (IsArray returns false)
		/// </exception>
		public int ArrayCount {
			get {
				if (!IsArray)
					throw new InvalidOperationException ("Object is not an array.");
				if (IsEvaluating)
					return 0;
				return arrayCount; 
			}
		}

		public bool IsReadOnly {
			get { return HasFlag (ObjectValueFlags.ReadOnly); }
		}
		
		public bool IsArray {
			get { return HasFlag (ObjectValueFlags.Array); }
		}
		
		public bool IsObject {
			get { return HasFlag (ObjectValueFlags.Object); }
		}
		
		public bool IsPrimitive {
			get { return HasFlag (ObjectValueFlags.Primitive); }
		}
		
		public bool IsUnknown {
			get { return HasFlag (ObjectValueFlags.Unknown); }
		}
		
		public bool IsNotSupported {
			get { return HasFlag (ObjectValueFlags.NotSupported); }
		}
		
		public bool IsError {
			get { return HasFlag (ObjectValueFlags.Error); }
		}
		
		public bool IsEvaluating {
			get { return HasFlag (ObjectValueFlags.Evaluating); }
		}
		
		public bool IsEvaluatingGroup {
			get { return HasFlag (ObjectValueFlags.EvaluatingGroup); }
		}
		
		public bool CanRefresh {
			get { return source != null && !HasFlag (ObjectValueFlags.NoRefresh); }
		}
		
		public bool HasFlag (ObjectValueFlags flag)
		{
			return (flags & flag) != 0;
		}

		public event EventHandler ValueChanged {
			add {
				lock (this) {
					if (IsEvaluating)
						valueChanged += value;
					else
						value (this, EventArgs.Empty);
				}
			}
			remove {
				lock (this) {
					valueChanged -= value;
				}
			}
		}
		
		/// <summary>
		/// Refreshes the value of this object
		/// </summary>
		/// <remarks>
		/// This method can be called to get a more up-to-date value for this object.
		/// </remarks>
		public void Refresh ()
		{
			Refresh (parentFrame.DebuggerSession.EvaluationOptions);
		}
		
		/// <summary>
		/// Refreshes the value of this object
		/// </summary>
		/// <remarks>
		/// This method can be called to get a more up-to-date value for this object.
		/// </remarks>
		public void Refresh (EvaluationOptions options)
		{
			if (!CanRefresh)
				return;
			ObjectValue val = source.GetValue (path, options);
			UpdateFrom (val, false);
		}

		/// <summary>
		/// Gets a wait handle which can be used to wait for the evaluation of this object to end
		/// </summary>
		/// <value>
		/// The wait handle.
		/// </value>
		public WaitHandle WaitHandle {
			get {
				lock (this) {
					if (evaluatedEvent == null)
						evaluatedEvent = new ManualResetEvent (!IsEvaluating);
					return evaluatedEvent;
				}
			}
		}

		internal IObjectValueUpdater Updater {
			get { return updater; }
		}

		internal void UpdateFrom (ObjectValue val, bool notify)
		{
			lock (this) {
				arrayCount = val.arrayCount;
				if (val.name != null)
					name = val.name;
				value = val.value;
				displayValue = val.displayValue;
				typeName = val.typeName;
				flags = val.flags;
				source = val.source;
				children = val.children;
				path = val.path;
				updater = val.updater;
				ConnectCallbacks (parentFrame, this);
				if (evaluatedEvent != null)
					evaluatedEvent.Set ();
				if (notify && valueChanged != null)
					valueChanged (this, EventArgs.Empty);
			}
		}

		internal UpdateCallback GetUpdateCallback ()
		{
			if (IsEvaluating) {
				if (updateCallback == null)
					updateCallback = new UpdateCallback (new UpdateCallbackProxy (this), path);
				return updateCallback;
			} else
				return null;
		}

		~ObjectValue ()
		{
			if (updateCallback != null)
				System.Runtime.Remoting.RemotingServices.Disconnect ((UpdateCallbackProxy)updateCallback.Callback);
		}
		
		internal static void ConnectCallbacks (StackFrame parentFrame, params ObjectValue[] values)
		{
			Dictionary<IObjectValueUpdater, List<UpdateCallback>> callbacks = null;
			List<ObjectValue> valueList = new List<ObjectValue> (values);
			for (int n=0; n<valueList.Count; n++) {
				ObjectValue val = valueList [n];
				val.parentFrame = parentFrame;
				UpdateCallback cb = val.GetUpdateCallback ();
				if (cb != null) {
					if (callbacks == null)
						callbacks = new Dictionary<IObjectValueUpdater, List<UpdateCallback>> ();
					List<UpdateCallback> list;
					if (!callbacks.TryGetValue (val.Updater, out list)) {
						list = new List<UpdateCallback> ();
						callbacks [val.Updater] = list;
					}
					list.Add (cb);
				}
				if (val.children != null)
					valueList.AddRange (val.children);
			}
			if (callbacks != null) {
				// Do the callback connection in a background thread
				System.Threading.ThreadPool.QueueUserWorkItem (delegate {
					foreach (KeyValuePair<IObjectValueUpdater, List<UpdateCallback>> cbs in callbacks) {
						cbs.Key.RegisterUpdateCallbacks (cbs.Value.ToArray ());
					}
				});
			}
		}
	}

	class UpdateCallbackProxy: MarshalByRefObject, IObjectValueUpdateCallback
	{
		WeakReference valRef;
		
		public void UpdateValue (ObjectValue newValue)
		{
			ObjectValue val = valRef.Target as ObjectValue;
			if (val != null)
				val.UpdateFrom (newValue, true);
		}
		
		public UpdateCallbackProxy (ObjectValue val)
		{
			valRef = new WeakReference (val);
		}
	}
}
