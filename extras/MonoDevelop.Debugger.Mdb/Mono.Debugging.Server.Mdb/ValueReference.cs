// ValueReference.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Mono.Debugger.Languages;
using Mono.Debugger;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using MD = Mono.Debugger;
	
namespace DebuggerServer
{
	public abstract class ValueReference: RemoteFrameObject, IObjectValueSource
	{
		Thread thread;
		
		public ValueReference (Thread thread)
		{
			this.thread = thread;
		}
		
		public virtual object ObjectValue {
			get {
				TargetObject ob = Value;
				if (ob is TargetFundamentalObject) {
					TargetFundamentalObject fob = (TargetFundamentalObject) ob;
					return fob.GetObject (Thread);
				} else
					return ob;
			}
		}
		
		public abstract TargetObject Value { get; set; }
		public abstract string Name { get; }
		public abstract TargetType Type { get; }
		public abstract ObjectValueFlags Flags { get; }

		public Thread Thread {
			get {
				return thread;
			}
		}

		public virtual ObjectValue CreateObjectValue ()
		{
			Connect ();
			string name = Name;
			if (string.IsNullOrEmpty (name))
				name = "?";
			try {
				return Util.CreateObjectValue (Thread, this, new ObjectPath (Name), Value, Flags);
			} catch (Exception ex) {
				Console.WriteLine ("pp2: " + ex);
				return Mono.Debugging.Client.ObjectValue.CreateError (Name, ex.Message, Flags);
			}
		}
		
		public string SetValue (ObjectPath path, string value)
		{
			try {
				ValueReference vref = Server.Instance.Evaluator.Evaluate (Thread.CurrentFrame, value, Type);
				TargetObject newValue = vref.Value;
				newValue = TargetObjectConvert.Cast (thread.CurrentFrame, newValue, Type);
				Value = newValue;
			} catch (Exception ex) {
				Server.Instance.WriteDebuggerOutput ("Value assignment failed: " + ex.GetType () + ": " + ex.Message);
			}
			
			try {
				return Server.Instance.Evaluator.TargetObjectToString (Thread, Value);
			} catch (Exception ex) {
				Server.Instance.WriteDebuggerOutput ("Value assignment failed: " + ex.GetType () + ": " + ex.Message);
			}
			
			return value;
		}
		
		public virtual string CallToString ()
		{
			object val = ObjectValue;
			if (val is TargetStructObject)
				return Util.CallToString (Thread, (TargetStructObject) val);
			else if (val == null)
				return string.Empty;
			else
				return val.ToString ();
		}
		
		public virtual ObjectValue[] GetChildren (ObjectPath path, int index, int count)
		{
			return Util.GetObjectValueChildren (Thread, Value, index, count);
		}
		
		public virtual IEnumerable<ValueReference> GetChildReferences ()
		{
			try {
				TargetStructObject val = Value as TargetStructObject;
				if (val != null)
					return Util.GetMembers (thread, val.Type, val);
			} catch {
				// Ignore
			}
			return new ValueReference [0];
		}
		
		public virtual ValueReference GetChild (string name)
		{
			TargetObject obj = Value;
			
			if (obj == null)
				return null;
			
			obj = Util.GetRealObject (Thread, obj);
			
			if (obj == null)
				return null;
			
			switch (obj.Kind)
			{
				case TargetObjectKind.Array: {
					
					TargetArrayObject arr = obj as TargetArrayObject;
					if (arr == null)
						return null;
					
					// Parse the array indices
					string[] sinds = name.Substring (1, name.Length - 2).Split (',');
					int[] indices = new int [sinds.Length];
					for (int n=0; n<sinds.Length; n++)
						indices [n] = int.Parse (sinds [n]);
					
					return new ArrayValueReference (thread, arr, indices);
				}
					
				case TargetObjectKind.Class: {
					TargetClassObject co = obj as TargetClassObject;
					if (co == null)
						return null;
					foreach (ValueReference val in Util.GetMembers (Thread, co.Type, co)) {
						if (val.Name == name)
							return val;
					}
					return null;
				}
					
				default:
					return null;
			}
		}
	}
}
