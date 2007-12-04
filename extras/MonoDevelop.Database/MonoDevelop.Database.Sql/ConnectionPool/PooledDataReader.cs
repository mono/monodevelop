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
using System.Data;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public sealed class PooledDataReader : IDataReader
	{
		private IPooledDbConnection connection;
		private IDataReader reader;

		public PooledDataReader (IPooledDbConnection connection, IDataReader reader)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");
			if (reader == null)
				throw new ArgumentNullException ("reader");
			
			this.connection = connection;
			this.reader = reader;
		}

		public IPooledDbConnection Connection
		{
			get { return connection; }
		}

		public IDataReader DataReader
		{
			get { return reader; }
		}

		public void Dispose ()
		{
			reader.Dispose ();
			connection.Release ();
		}

		public void Close ()
		{
			connection.Release ();
			reader.Close ();
		}

		public int Depth
		{
			get { return reader.Depth; }
		}

		public DataTable GetSchemaTable ()
		{
			return reader.GetSchemaTable ();
		}

		public bool IsClosed
		{
			get { return reader.IsClosed; }
		}

		public bool NextResult ()
		{
			return reader.NextResult ();
		}

		public bool Read ()
		{
			return reader.Read ();
		}

		public int RecordsAffected
		{
			get { return reader.RecordsAffected; }
		}

		public int FieldCount
		{
			get { return reader.FieldCount; }
		}

		public bool GetBoolean (int i)
		{
			return reader.GetBoolean (i);
		}

		public byte GetByte (int i)
		{
			return reader.GetByte (i);
		}

		public long GetBytes (int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			return reader.GetBytes (i, fieldOffset, buffer, bufferoffset, length);
		}

		public char GetChar (int i)
		{
			return reader.GetChar (i);
		}

		public long GetChars (int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			return reader.GetChars (i, fieldoffset, buffer, bufferoffset, length);
		}

		public IDataReader GetData (int i)
		{
			return reader.GetData (i);
		}

		public string GetDataTypeName (int i)
		{
			return reader.GetDataTypeName (i);
		}

		public DateTime GetDateTime (int i)
		{
			return reader.GetDateTime (i);
		}

		public decimal GetDecimal (int i)
		{
			return reader.GetDecimal (i);
		}

		public double GetDouble (int i)
		{
			return reader.GetDouble (i);
		}

		public Type GetFieldType (int i)
		{
			return reader.GetFieldType (i);
		}

		public float GetFloat (int i)
		{
			return reader.GetFloat (i);
		}

		public Guid GetGuid (int i)
		{
			return reader.GetGuid (i);
		}

		public short GetInt16 (int i)
		{
			return reader.GetInt16 (i);
		}

		public int GetInt32 (int i)
		{
			return reader.GetInt32 (i);
		}

		public long GetInt64 (int i)
		{
			return reader.GetInt64 (i);
		}

		public string GetName (int i)
		{
			return reader.GetName (i);
		}

		public int GetOrdinal (string name)
		{
			return reader.GetOrdinal (name);
		}

		public string GetString (int i)
		{
			return reader.GetString (i);
		}

		public object GetValue (int i)
		{
			return reader.GetValue (i);
		}

		public int GetValues (object[] values)
		{
			return reader.GetValues (values);
		}

		public bool IsDBNull (int i)
		{
			return reader.IsDBNull (i);
		}

		public object this[string name]
		{
			get { return reader[name]; }
		}

		public object this[int i]
		{
			get { return reader[i]; }
		}
	}
}