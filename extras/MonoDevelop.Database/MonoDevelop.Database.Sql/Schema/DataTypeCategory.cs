//
// Authors:
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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
namespace MonoDevelop.Database.Sql
{
	public enum DataTypeCategory
	{
		/// <summary>
		/// Used to define a column with a start value and a seed
		/// </summary>
		AutoNumber,
		/// <summary>
		/// Data type allowing only 2 possibilities, like: true/false, 1/0, ...
		/// </summary>
		Boolean,
		/// <summary>
		/// Data type allowing to store individual bits, length will be shown in bits instead of bytes
		/// </summary>
		Bit,
		/// <summary>
		/// Used for all numerical values without a comma (int, long, ...)
		/// </summary>
		Integer,
		/// <summary>
		/// Used for all floating point values (float, double, ...)
		/// </summary>
		Float,
		/// <summary>
		/// Single-byte fixed length text, padding is used when the inserted text is smaller then the stored text
		/// </summary>
		Char,
		/// <summary>
		/// Single-byte text, the length is not defined and can be variable
		/// </summary>
		VarChar,
		/// <summary>
		/// Multi-byte (unicode) fixed length text, padding is used when the inserted text is smaller then the stored text
		/// </summary>
		NChar,
		/// <summary>
		/// Multi-byte (unicode text, the length is not defined and can be variable
		/// </summary>
		NVarChar,
		/// <summary>
		/// Binary objects, like blob, clob, image, ... with a fixed length
		/// </summary>
		Binary,
		/// <summary>
		/// Binary objects, like blob, clob, image, ... with variable length
		/// </summary>
		VarBinary,
		/// <summary>
		/// Globally unique identifier, can be similar like the .NET System.Guid type
		/// </summary>
		Uid,
		/// <summary>
		/// Date field
		/// </summary>
		Date,
		/// <summary>
		/// Time field
		/// </summary>
		Time,
		/// <summary>
		/// TimeStamp field
		/// </summary>
		TimeStamp,
		/// <summary>
		/// Field containing both a date and a time
		/// </summary>
		DateTime,
		/// <summary>
		/// Special text containing XML chunks
		/// </summary>
		Xml,
		/// <summary>
		/// Undefined data type
		/// </summary>
		Other,
		/// <summary>
		/// A data type defined by the user
		/// </summary>
		UserDefined,
		/// <summary>
		///A data type containing date intervals
		/// </summary>
		Interval
	}
}
