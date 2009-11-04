// 
// NpgsqlEncoding.cs
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

namespace MonoDevelop.Database.Sql.Npgsql
{

	public class NpgsqlEncoding:AbstractSchema
	{
		string language;
		string description;
		bool server;
		string bytes_char;
		string aliases;
		
		public bool Server {
			get {
				return server;
			}
			set {
				server = value;
			}
		}
		
		public string Language {
			get {
				return language;
			}
			set {
				language = value;
			}
		}
		
		public string BytesChar {
			get {
				return bytes_char;
			}
			set {
				bytes_char = value;
			}
		}
		
		public string Aliases {
			get {
				return aliases;
			}
			set {
				aliases = value;
			}
		}
		
		public string Description {
			get {
				return description;
			}
			set {
				description = value;
			}
		}
		
		
		public NpgsqlEncoding (ISchemaProvider provider ):base (provider)
		{
		}
		
		public NpgsqlEncoding (NpgsqlEncoding encoding):base(encoding)
		{
			this.Name = encoding.Name;
			this.Aliases = encoding.Aliases;
			this.BytesChar = encoding.BytesChar;
			this.Description = encoding.Description;
			this.Language = encoding.Language;
			this.Server = encoding.Server;
			
		}
		
		public override object Clone ()
		{
			return new NpgsqlEncoding (this);
		}
 
		
		
	}
}
