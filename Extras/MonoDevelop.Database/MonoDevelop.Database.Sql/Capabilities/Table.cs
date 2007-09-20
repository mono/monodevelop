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
	public enum TableCapabilities
	{
		None = 0x000000,
		
		Name = 0x000001,
		TableSpaceName = 0x000002,
		Owner = 0x000004,
		Comment = 0x000008,
		Definition = 0x000010,
		Schema = 0x000020,
		IsSystem = 0x000040,
		Columns = 0x000080,

		PrimaryKeyConstraint = 0x000100,
		ForeignKeyConstraint = 0x000200,
		CheckConstraint = 0x000400,
		UniqueConstraint = 0x000800,
		
		Constraints = PrimaryKeyConstraint | ForeignKeyConstraint | CheckConstraint | UniqueConstraint,
		
		AppendConstraint = 0x001000,
		InsertConstraint = 0x002000,
		RemoveConstraint = 0x004000,

		Trigger = 0x008000,

		AppendTrigger = 0x010000,
		InsertTrigger = 0x020000,
		RemoveTrigger = 0x040000,
		
		AppendColumn = 0x080000,
		InsertColumn = 0x100000,
		RemoveColumn = 0x200000
	}
}
