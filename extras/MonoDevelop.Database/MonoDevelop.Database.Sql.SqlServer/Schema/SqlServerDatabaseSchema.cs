// 
// SqlServerDatabaseSchema.cs
//  
// Author:
//       Luciano N. Callero <lnc19@hotmail.com>
// 
// Copyright (c) 2009 
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
using MonoDevelop.Database.Sql;

namespace MonoDevelop.Database.Sql.SqlServer
{
	public enum SizeType {
		KB,
		MB,
		GB,
		TB,
		UNLIMITED,
		PERCENTAGE
	}
	
	public class FileSize
	{
		int size;
		SizeType type;
		
		public SizeType Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public int Size {
			get {
				return size;
			}
			set {
				size = value;
			}
		}
		
		
		
		public FileSize (int size, SizeType type)
		{
			this.size = size;
			this.Type = type;
		}
		
	}
		
	public class SqlServerDatabaseSchema:DatabaseSchema
	{
		SqlServerCollationSchema collation;
		FileSize size;
		FileSize fileGrowth;
		FileSize maxSize;
		string fileName;
		string logicalName;
		
		public SqlServerCollationSchema Collation {
			get {
				return collation;
			}
			set {
				collation = value;
			}
		}
		
		public FileSize Size {
			get {
				return size;
			}
			set {
				size = value;
			}
		}
		
		
		public FileSize MaxSize {
			get {
				return maxSize;
			}
			set {
				maxSize = value;
			}
		}
		
		
		public FileSize FileGrowth {
			get {
				return fileGrowth;
			}
			set {
				fileGrowth = value;
			}
		}
		
		public string LogicalName {
			get {
				return logicalName;
			}
			set {
				logicalName = value;
			}
		}
		
		
		public string FileName {
			get {
				return fileName;
			}
			set {
				fileName = value;
			}
		}
		
		
		
		public SqlServerDatabaseSchema (ISchemaProvider provider):base(provider)
		{
		}
		
		public SqlServerDatabaseSchema (DatabaseSchema schema):base(schema)
		{
			
		}
		
	}
}
