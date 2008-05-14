// Util.cs
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
using MD = Mono.Debugger;
using Mono.Debugger.Languages;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace DebuggerServer
{
	public static class Util
	{
		public static ObjectValue CreateObjectValue (MD.Thread thread, IObjectValueSource source, string path, TargetObject obj)
		{
			switch (obj.Kind) {
				case TargetObjectKind.Object:
					return new ObjectValue (source, path, (ObjectValue[]) null);
				case TargetObjectKind.Array:
					TargetArrayObject array = (TargetArrayObject) obj;
					int count = array.GetArrayBounds (thread).UpperBounds [0];
					return new ObjectValue (source, path, count, null);
				case TargetObjectKind.Fundamental:
					TargetFundamentalObject fob = (TargetFundamentalObject) obj;
					object val = fob.GetObject (thread);
					return new ObjectValue (source, path, val);
				case TargetObjectKind.Enum:
					TargetEnumObject enumobj = (TargetEnumObject) obj;
					return CreateObjectValue (thread, source, path, enumobj.GetValue (thread));
				default:
					return new ObjectValue (source, path);
			}
		}
		
		static TargetObject FindChildObject (IObjectValueSource source, TargetObject rootObj, string[] path, int pathIndex)
		{
			return null;
		}
		
		public static ObjectValue GetObjectValue (MD.Thread thread, IObjectValueSource source, TargetObject rootObj, string[] path, int pathIndex, int rootPathIndex)
		{
			string rootPath = string.Join ("/", path, rootPathIndex, path.Length - rootPathIndex);
			TargetObject obj = null;
			
			
//			- trobar l'objecte fill -
			
			return CreateObjectValue (thread, source, rootPath, obj);
		}
		
		public static ObjectValue[] GetObjectValueChildren (IObjectValueSource source, TargetObject rootObj, string[] path, int pathIndex, int rootPathIndex, int firstItemIndex, int count)
		{
  //           - trobar els fills -
			return null;
		}
		
		public static void PrintObject (MD.StackFrame frame, TargetObject obj)
		{
			try {
				Console.WriteLine ("object");
				Console.WriteLine ("  kind: " + obj.Kind);
				Console.WriteLine ("  obj-type: " + obj.GetType ());
				Console.WriteLine ("  type-name: " + obj.TypeName);
				Console.WriteLine ("  has-addr: " + obj.HasAddress);
				switch (obj.Kind) {
					case MD.Languages.TargetObjectKind.Array:
						TargetArrayObject arr = obj as TargetArrayObject;
						if (arr == null) {
							Console.WriteLine ("  (NULL)");
							return;
						}
						TargetArrayBounds ab = arr.GetArrayBounds (frame.Thread);
						Console.WriteLine ("  bounds");
						Console.WriteLine ("     multidim: " + ab.IsMultiDimensional);
						Console.WriteLine ("     unbound: " + ab.IsUnbound);
						Console.WriteLine ("     length: " + (!ab.IsMultiDimensional ? ab.Length.ToString () : "(miltidim)"));
						Console.WriteLine ("     rank: " + ab.Rank);
						Console.Write ("     lower bounds: ");
						if (ab.LowerBounds != null)
							foreach (int b in ab.LowerBounds) Console.Write (b + " ");
						else
							Console.WriteLine ("?");
						Console.WriteLine ();
						Console.Write ("     upper bounds: ");
						if (ab.UpperBounds != null)
							foreach (int b in ab.UpperBounds) Console.Write (b + " ");
						else
							Console.WriteLine ("?");
						Console.WriteLine ();
						break;
					case TargetObjectKind.Class:
						TargetClassObject co = obj as TargetClassObject;
						if (co == null) {
							Console.WriteLine ("  (NULL)");
							return;
						}
						TargetObject currob = co.GetCurrentObject (frame.Thread);
						if (currob != null && currob != co) {
							Console.WriteLine ("  >> current object");
							Console.WriteLine ("  " + currob);
							Console.WriteLine ("  << current object");
						}
											
						currob = co.GetParentObject (frame.Thread);
						if (currob != null && currob != co) {
							Console.WriteLine ("  >> parent object");
							PrintObject (frame, currob);
							Console.WriteLine ("  << parent object");
						}
						break;
					case TargetObjectKind.Enum:
						TargetEnumObject eob = (TargetEnumObject) obj;
						Console.WriteLine ("  print: " + eob.Print (frame.Thread));
						TargetObject val = eob.GetValue (frame.Thread);
						Console.WriteLine ("  >> value object");
						PrintObject (frame, val);
						Console.WriteLine ("  << value object");
						break;
					case TargetObjectKind.Fundamental:
						TargetFundamentalObject fob = obj as TargetFundamentalObject;
						if (fob == null) {
							Console.WriteLine ("  (NULL)");
							return;
						}
						object ob = fob.GetObject (frame.Thread);
	//					Console.WriteLine ("  value: " + (ob != null ? ob.ToString () : "(null)"));
	//					Console.WriteLine ("  print: " + fob.Print (frame.Thread));
						break;
					case TargetObjectKind.Object:
						TargetObjectObject oob = obj as TargetObjectObject;
						if (oob == null) {
							Console.WriteLine ("  (NULL)");
							return;
						}
						Console.WriteLine ("  >> dereferenced object");
						PrintObject (frame, oob.GetDereferencedObject (frame.Thread));
						Console.WriteLine ("  << dereferenced object");
						break;
				}
			}
			catch (Exception ex) {
				Console.WriteLine ("pp: " + ex);
			}
		}
	}
}
