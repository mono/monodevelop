//
// DomPersistenceTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Serialization;

namespace MonoDevelop.Projects.DomTests
{
	[TestFixture()]
	public class DomPersistenceTests
	{
		
		[Test()]
		public void ReadWriteLocationTest ()
		{
			DomLocation input = new DomLocation (3, 9);
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, null, input);
			byte[] bytes = ms.ToArray ();
			
			DomLocation result = DomPersistence.ReadLocation (CreateReader (bytes), null);
			Assert.AreEqual (3, result.Line);
			Assert.AreEqual (9, result.Column);
		}
		
		[Test()]
		public void ReadWriteRegionTest ()
		{
			DomRegion input = new DomRegion (1, 2, 3, 4);
			
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, DefaultNameEncoder, input);
			byte[] bytes = ms.ToArray ();
			
			DomRegion result = DomPersistence.ReadRegion (CreateReader (bytes), DefaultNameDecoder);
			Assert.AreEqual (1, result.Start.Line);
			Assert.AreEqual (2, result.Start.Column);
			Assert.AreEqual (3, result.End.Line);
			Assert.AreEqual (4, result.End.Column);
		}
		
		
		[Test()]
		public void ReadWriteFieldTest ()
		{
			DomField input = new DomField ();
			input.Name = "TestField";
			input.Location = new DomLocation (5, 10);
			input.Documentation = "testDocumentation";
			input.Modifiers = Modifiers.Static;
			input.ReturnType = new DomReturnType ("System.String");
			
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, DefaultNameEncoder, input);
			byte[] bytes = ms.ToArray ();
			
			DomField result = DomPersistence.ReadField (CreateReader (bytes), DefaultNameDecoder);
			Assert.AreEqual ("TestField", result.Name);
			Assert.AreEqual ("testDocumentation", result.Documentation);
			Assert.AreEqual (new DomLocation (5, 10), result.Location);
			Assert.AreEqual (Modifiers.Static, result.Modifiers);
			Assert.AreEqual ("System.String", result.ReturnType.FullName);
		}
		
		[Test()]
		public void ReadWriteFieldTest2 ()
		{
			DomField input = new DomField ();
			input.Name = null;
			input.Location = DomLocation.Empty;
			input.Documentation = null;
			input.Modifiers = Modifiers.None;
			input.ReturnType = null;
			
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, DefaultNameEncoder, input);
			byte[] bytes = ms.ToArray ();
			
			DomField result = DomPersistence.ReadField (CreateReader (bytes), DefaultNameDecoder);
			Assert.AreEqual (null, result.Name);
			Assert.AreEqual (null, result.Documentation);
			Assert.AreEqual (DomLocation.Empty, result.Location);
			Assert.AreEqual (Modifiers.None, result.Modifiers);
			Assert.AreEqual (null, result.ReturnType);
		}
		
		[Test()]
		public void ReadWriteReturnTypeTest ()
		{
			DomReturnType input = new DomReturnType ();
			input.Name      = "Test";
			input.Namespace = "Namespace";
			input.ArrayDimensions = 5;
			input.IsByRef = true;
			input.IsNullable = true;
			input.PointerNestingLevel = 666;
			input.AddTypeParameter (new DomReturnType ("System.String"));
			input.AddTypeParameter (new DomReturnType ("System.Int32"));
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, DefaultNameEncoder, input);
			byte[] bytes = ms.ToArray ();
			
			DomReturnType result = DomPersistence.ReadReturnType (CreateReader (bytes), DefaultNameDecoder);
			Assert.AreEqual ("Test", result.Name);
			Assert.AreEqual ("Namespace", result.Namespace);
			Assert.AreEqual ("Namespace.Test", result.FullName);
			Assert.AreEqual (5, result.ArrayDimensions);
			Assert.AreEqual (true, result.IsByRef);
			Assert.AreEqual (true, result.IsNullable);
			Assert.AreEqual ("System.String", result.GenericArguments[0].FullName);
			Assert.AreEqual ("System.Int32", result.GenericArguments[1].FullName);
		}
		
		[Test()]
		public void ReadWriteMethodTest ()
		{
			DomMethod input = new DomMethod ();
			input.Name      = "Test";
			input.MethodModifier = MethodModifier.IsConstructor;
			input.Add (new DomParameter (input, "par1", DomReturnType.Void));
			
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, DefaultNameEncoder, input);
			byte[] bytes = ms.ToArray ();
			
			DomMethod result = DomPersistence.ReadMethod (CreateReader (bytes), DefaultNameDecoder);
			Assert.AreEqual ("Test", result.Name);
			Assert.AreEqual (true, result.IsConstructor);
			Assert.AreEqual ("par1", result.Parameters [0].Name);
			Assert.AreEqual ("Void", result.Parameters [0].ReturnType.Name);
		}
		
		[Test()]
		public void ReadWriteDelegateTest ()
		{
			DomType input = DomType.CreateDelegate (null, "TestDelegate", new DomLocation (10, 10), DomReturnType.Void, new List<IParameter> ());
			
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, DefaultNameEncoder, input);
			byte[] bytes = ms.ToArray ();
			
			DomType result = DomPersistence.ReadType (CreateReader (bytes), DefaultNameDecoder);
			Assert.AreEqual ("TestDelegate", result.Name);
			Assert.AreEqual (ClassType.Delegate, result.ClassType);
		}
		
		[Test()]
		public void ReadWritePropertyTest ()
		{
			DomProperty input = new DomProperty ();
			input.Name      = "Test";
			input.PropertyModifier = PropertyModifier.IsIndexer | PropertyModifier.HasGet | PropertyModifier.HasSet;
			
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, DefaultNameEncoder, input);
			byte[] bytes = ms.ToArray ();
			
			DomProperty result = DomPersistence.ReadProperty (CreateReader (bytes), DefaultNameDecoder);
			Assert.AreEqual ("Test", result.Name);
			Assert.AreEqual (true, result.IsIndexer);
			Assert.AreEqual (true, result.HasGet);
			Assert.AreEqual (true, result.HasSet);
		}
		
		[Test()]
		public void ReadWriteEventTest ()
		{
			DomEvent input     = new DomEvent ();
			input.Name         = "Test";
			input.AddMethod    = new DomMethod ("AddMethod", Modifiers.New, MethodModifier.None, DomLocation.Empty, DomRegion.Empty);
			input.RemoveMethod = new DomMethod ("RemoveMethod", Modifiers.New, MethodModifier.None, DomLocation.Empty, DomRegion.Empty);
			input.RaiseMethod  = new DomMethod ("RaiseMethod", Modifiers.New, MethodModifier.None, DomLocation.Empty, DomRegion.Empty);
			
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, DefaultNameEncoder, input);
			byte[] bytes = ms.ToArray ();
			
			DomEvent result = DomPersistence.ReadEvent (CreateReader (bytes), DefaultNameDecoder);
			Assert.AreEqual ("Test", result.Name);
			Assert.AreEqual ("AddMethod", result.AddMethod.Name);
			Assert.AreEqual ("RemoveMethod", result.RemoveMethod.Name);
			Assert.AreEqual ("RaiseMethod", result.RaiseMethod.Name);
		}
		
		[Test()]
		public void ReadWriteTypeTest ()
		{
			DomType input     = new DomType ();
			input.Name         = "Test";
			input.ClassType    = ClassType.Struct;
			input.BaseType     = new DomReturnType ("BaseClass");
			
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, DefaultNameEncoder, input);
			byte[] bytes = ms.ToArray ();
			
			DomType result = DomPersistence.ReadType (CreateReader (bytes), DefaultNameDecoder);
			Assert.AreEqual ("Test", result.Name);
			Assert.AreEqual (ClassType.Struct, result.ClassType);
			Assert.AreEqual ("BaseClass", result.BaseType.Name);
		}
		
		[Test()]
		public void ReadWriteTypeTestComplex ()
		{
			DomType input   = new DomType ();
			
			input.Name      = "Test";
			input.ClassType = ClassType.Struct;
			input.BaseType  = new DomReturnType ("BaseClass");
			input.AddInterfaceImplementation (new DomReturnType ("Interface1"));
			input.AddInterfaceImplementation (new DomReturnType ("Interface2"));
			
			input.Add (new DomMethod ("TestMethod", Modifiers.None, MethodModifier.None, DomLocation.Empty, DomRegion.Empty));
			input.Add (new DomMethod (".ctor", Modifiers.None, MethodModifier.IsConstructor, DomLocation.Empty, DomRegion.Empty));
			
			input.Add (new DomField ("TestField", Modifiers.None, DomLocation.Empty, DomReturnType.Void));
			input.Add (new DomProperty ("TestProperty", Modifiers.None, DomLocation.Empty, DomRegion.Empty, DomReturnType.Void));
			input.Add (new DomEvent ("TestEvent", Modifiers.None, DomLocation.Empty, DomReturnType.Void));
			MemoryStream ms = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter (ms);
			DomPersistence.Write (writer, DefaultNameEncoder, input);
			byte[] bytes = ms.ToArray ();
			
			DomType result = DomPersistence.ReadType (CreateReader (bytes), DefaultNameDecoder);
			Assert.AreEqual ("Test", result.Name);
			Assert.AreEqual (ClassType.Struct, result.ClassType);
			Assert.AreEqual ("BaseClass", result.BaseType.Name);
			Assert.AreEqual (1, result.MethodCount);
			Assert.AreEqual (1, result.ConstructorCount);
			Assert.AreEqual (1, result.FieldCount);
			Assert.AreEqual (1, result.PropertyCount);
			Assert.AreEqual (1, result.EventCount);
			
		}
		
		static BinaryReader CreateReader (byte[] bytes)
		{
			return new BinaryReader (new MemoryStream (bytes));
		}

//	Doesn't work: ?
//		byte[] Write<T> (T input)
//		{
//			MemoryStream ms = new MemoryStream ();
//			BinaryWriter writer = new BinaryWriter (ms);
//			DomPersistence.Write (writer, null, input);
//			return ms.ToArray ();
//		}
		
		
		static StringNameTable DefaultNameEncoder;
		static StringNameTable DefaultNameDecoder;
		
		static DomPersistenceTests ()
		{
			DefaultNameEncoder = new StringNameTable (sharedNameTable);
			DefaultNameDecoder = new StringNameTable (sharedNameTable);
		}
		
		static readonly string[] sharedNameTable = new string[] {
			"", // 505195
			"System.Void", // 116020
			"To be added", // 78598
			"System.Int32", // 72669
			"System.String", // 72097
			"System.Object", // 48530
			"System.Boolean", // 46200
			".ctor", // 39938
			"System.IntPtr", // 35184
			"To be added.", // 19082
			"value", // 11906
			"System.Byte", // 8524
			"To be added: an object of type 'string'", // 7928
			"e", // 7858
			"raw", // 7830
			"System.IAsyncResult", // 7760
			"System.Type", // 7518
			"name", // 7188
			"object", // 6982
			"System.UInt32", // 6966
			"index", // 6038
			"To be added: an object of type 'int'", // 5196
			"System.Int64", // 4166
			"callback", // 4158
			"System.EventArgs", // 4140
			"method", // 4030
			"System.Enum", // 3980
			"value__", // 3954
			"Invoke", // 3906
			"result", // 3856
			"System.AsyncCallback", // 3850
			"System.MulticastDelegate", // 3698
			"BeginInvoke", // 3650
			"EndInvoke", // 3562
			"node", // 3416
			"sender", // 3398
			"context", // 3310
			"System.EventHandler", // 3218
			"System.Double", // 3206
			"type", // 3094
			"x", // 3056
			"System.Single", // 2940
			"data", // 2930
			"args", // 2926
			"System.Char", // 2813
			"Gdk.Key", // 2684
			"ToString", // 2634
			"'a", // 2594
			"System.Drawing.Color", // 2550
			"y", // 2458
			"To be added: an object of type 'object'", // 2430
			"System.DateTime", // 2420
			"message", // 2352
			"GLib.GType", // 2292
			"o", // 2280
			"a <see cref=\"T:System.Int32\" />", // 2176
			"path", // 2062
			"obj", // 2018
			"Nemerle.Core.list`1", // 1950
			"System.Windows.Forms", // 1942
			"System.Collections.ArrayList", // 1918
			"a <see cref=\"T:System.String\" />", // 1894
			"key", // 1868
			"Add", // 1864
			"arg0", // 1796
			"System.IO.Stream", // 1794
			"s", // 1784
			"arg1", // 1742
			"provider", // 1704
			"System.UInt64", // 1700
			"System.Drawing.Rectangle", // 1684
			"System.IFormatProvider", // 1684
			"gch", // 1680
			"System.Exception", // 1652
			"Equals", // 1590
			"System.Drawing.Pen", // 1584
			"count", // 1548
			"System.Collections.IEnumerator", // 1546
			"info", // 1526
			"Name", // 1512
			"System.Attribute", // 1494
			"gtype", // 1470
			"To be added: an object of type 'Type'", // 1444
			"System.Collections.Hashtable", // 1416
			"array", // 1380
			"System.Int16", // 1374
			"Gtk", // 1350
			"System.ComponentModel.ITypeDescriptorContext", // 1344
			"System.Collections.ICollection", // 1330
			"Dispose", // 1330
			"Gtk.Widget", // 1326
			"System.Runtime.Serialization.StreamingContext", // 1318
			"Nemerle.Compiler.Parsetree.PExpr", // 1312
			"System.Guid", // 1310
			"i", // 1302
			"Gtk.TreeIter", // 1300
			"text", // 1290
			"System.Runtime.Serialization.SerializationInfo", // 1272
			"state", // 1264
			"Remove" // 1256
		};		
	}
}
