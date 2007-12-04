//
// Authors:
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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

using System;

namespace MonoDevelop.Database.Sql
{
	[Flags]
	public enum ColumnCapabilities
	{
		None = 0x00000,
		
		Name = 0x00001,
		Owner = 0x00002,
		Comment = 0x00004,
		Definition = 0x00008,
		Schema = 0x00010,
		DataType = 0x00020,
		DefaultValue = 0x00040,
		Nullable = 0x00080,
		Length = 0x00100,
		Precision = 0x00200,
		Scale = 0x00400,
		Position = 0x00800,

		PrimaryKeyConstraint = 0x01000,
		ForeignKeyConstraint = 0x02000,
		CheckConstraint = 0x04000,
		UniqueConstraint = 0x08000,
		
		Constraints = PrimaryKeyConstraint | ForeignKeyConstraint | CheckConstraint | UniqueConstraint,
		
		AppendConstraint = 0x10000,
		InsertConstraint = 0x20000,
		RemoveConstraint = 0x40000
	}
}
