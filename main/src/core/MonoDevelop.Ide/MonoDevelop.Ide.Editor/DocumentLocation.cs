//
// DocumentLocation.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.ComponentModel;
using System.Globalization;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// A line/column position.
	/// Text editor lines/columns are counted started from one.
	/// </summary>
	[Serializable]
	[TypeConverter(typeof(DocumentLocationConverter))]
	public readonly struct DocumentLocation : IComparable<DocumentLocation>, IEquatable<DocumentLocation>
	{
		/// <summary>
		/// Represents no text location (0, 0).
		/// </summary>
		public static readonly DocumentLocation Empty = new DocumentLocation(0, 0);

		/// <summary>
		/// Constant of the minimum line.
		/// </summary>
		public const int MinLine   = 1;

		/// <summary>
		/// Constant of the minimum column.
		/// </summary>
		public const int MinColumn = 1;

		/// <summary>
		/// Creates a TextLocation instance.
		/// </summary>
		public DocumentLocation(int line, int column)
		{
			this.Line = line;
			this.Column = column;
		}

		/// <summary>
		/// Gets the line number.
		/// </summary>
		public int Line { get; }

		/// <summary>
		/// Gets the column number.
		/// </summary>
		public int Column { get; }

		/// <summary>
		/// Gets whether the TextLocation instance is empty.
		/// </summary>
		public bool IsEmpty {
			get {
				return Column < MinLine && Line < MinColumn;
			}
		}

		/// <summary>
		/// Gets a string representation for debugging purposes.
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "(Line {1}, Col {0})", this.Column, this.Line);
		}

		/// <summary>
		/// Gets a hash code.
		/// </summary>
		public override int GetHashCode()
		{
			return unchecked (Column << 20 ^ Line);
		}

		/// <summary>
		/// Equality test.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (!(obj is DocumentLocation)) return false;
			return (DocumentLocation)obj == this;
		}

		/// <summary>
		/// Equality test.
		/// </summary>
		public bool Equals(DocumentLocation other)
		{
			return this == other;
		}

		/// <summary>
		/// Equality test.
		/// </summary>
		public static bool operator ==(DocumentLocation left, DocumentLocation right)
		{
			return left.Column == right.Column && left.Line == right.Line;
		}

		/// <summary>
		/// Inequality test.
		/// </summary>
		public static bool operator !=(DocumentLocation left, DocumentLocation right)
		{
			return left.Column != right.Column || left.Line != right.Line;
		}

		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public static bool operator <(DocumentLocation left, DocumentLocation right)
		{
			if (left.Line < right.Line)
				return true;
			else if (left.Line == right.Line)
				return left.Column < right.Column;
			else
				return false;
		}

		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public static bool operator >(DocumentLocation left, DocumentLocation right)
		{
			if (left.Line > right.Line)
				return true;
			else if (left.Line == right.Line)
				return left.Column > right.Column;
			else
				return false;
		}

		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public static bool operator <=(DocumentLocation left, DocumentLocation right)
		{
			return !(left > right);
		}

		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public static bool operator >=(DocumentLocation left, DocumentLocation right)
		{
			return !(left < right);
		}

		public static implicit operator Microsoft.CodeAnalysis.Text.LinePosition (DocumentLocation location)
		{
			return new Microsoft.CodeAnalysis.Text.LinePosition (location.Line - 1, location.Column - 1);
		}

		public static implicit operator DocumentLocation(Microsoft.CodeAnalysis.Text.LinePosition location)
		{
			return new DocumentLocation (location.Line + 1, location.Character + 1);
		}

		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public int CompareTo(DocumentLocation other)
		{
			if (this == other)
				return 0;
			if (this < other)
				return -1;
			else
				return 1;
		}
	}

	public class DocumentLocationConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(DocumentLocation) || base.CanConvertTo(context, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is string) {
				string[] parts = ((string)value).Split(';', ',');
				if (parts.Length == 2) {
					return new DocumentLocation(int.Parse(parts[0]), int.Parse(parts[1]));
				}
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (value is DocumentLocation) {
				var loc = (DocumentLocation)value;
				return loc.Line + ";" + loc.Column;
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	public class DocumentLocationEventArgs : System.EventArgs
	{
		readonly DocumentLocation location;

		public DocumentLocation Location {
			get {
				return location;
			}
		}

		public DocumentLocationEventArgs (DocumentLocation location)
		{
			this.location = location;
		}
	}
}

