//
// FlagsSelectorDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.ComponentModel;

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors
{
	[PropertyEditorType (typeof (byte))]
	[PropertyEditorType (typeof (sbyte))]
	[PropertyEditorType (typeof (Int16))]
	[PropertyEditorType (typeof (UInt16))]
	[PropertyEditorType (typeof (Int32))]
	[PropertyEditorType (typeof (UInt32))]
	[PropertyEditorType (typeof (Int64))]
	[PropertyEditorType (typeof (UInt64))]
	[PropertyEditorType (typeof (Decimal))]
	public class IntRangeEditor : Gtk.SpinButton, IPropertyEditor
	{
		Type propType;
		
		public IntRangeEditor () : base (0, 0, 1.0)
		{
			this.HasFrame = false;
		}
		
		public void Initialize (EditSession session)
		{
			propType = session.Property.PropertyType;
			
			double min, max;
			
			switch (Type.GetTypeCode (propType)) {
				case TypeCode.Int16:
					min = (double) Int16.MinValue;
					max = (double) Int16.MaxValue;
					break;
				case TypeCode.UInt16:
					min = (double) UInt16.MinValue;
					max = (double) UInt16.MaxValue;
					break;
				case TypeCode.Int32:
					min = (double) Int32.MinValue;
					max = (double) Int32.MaxValue;
					break;
				case TypeCode.UInt32:
					min = (double) UInt32.MinValue;
					max = (double) UInt32.MaxValue;
					break;
				case TypeCode.Int64:
					min = (double) Int64.MinValue;
					max = (double) Int64.MaxValue;
					break;
				case TypeCode.UInt64:
					min = (double) UInt64.MinValue;
					max = (double) UInt64.MaxValue;
					break;
				case TypeCode.Byte:
					min = (double) Byte.MinValue;
					max = (double) Byte.MaxValue;
					break;
				case TypeCode.SByte:
					min = (double) SByte.MinValue;
					max = (double) SByte.MaxValue;
					break;
				default:
					throw new ApplicationException ("IntRange editor does not support editing values of type " + session.Property.PropertyType);
			}
			
			SetRange (min, max);
		}
		
		object IPropertyEditor.Value {
			get { return Convert.ChangeType (base.Value, propType); }
			set { base.Value = (double) Convert.ChangeType (value, typeof(double)); }
		}
	}
}
