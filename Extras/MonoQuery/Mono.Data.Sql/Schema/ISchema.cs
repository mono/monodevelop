//
// Schema/ISchema.cs
//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
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

namespace Mono.Data.Sql
{
	public interface ISchema
	{
		/// <summary>
		/// Event fired when the object is changed.
		/// </summary>
		event EventHandler Changed;

		/// <summary>
		/// SQL object name.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Full name of the object. This is typically `schema.objectname'.
		/// </summary>
		string FullName { get; }

		/// <summary>
		/// SQL Comment associated with this object.
		/// </summary>
		string Comment { get; set; }

		/// <summary>
		/// SQL Syntax Definition of this object.
		/// </summary>
		string Definition { get; set; }

		/// <summary>
		/// Name of schema
		/// </summary>
		string SchemaName { get; set; }

		/// <summary>
		/// Schema object representing the schema this object is part of.
		/// </summary>
		SchemaSchema Schema { get; set; }

		/// <summary>
		/// Set the owners name in the database by the literal string name
		/// of the user.
		/// </summary>
		string OwnerName { get; set; }

		/// <summary>
		/// Owner of this sql object.
		/// </summary>
		UserSchema Owner { get; }

		/// <summary>
		/// Privileges (sometimes known as ACL's) associated with this object.
		/// </summary>
		PrivilegeSchema [] Privileges { get; }

		/// <summary>
		/// The connection provider associated with this SQL object.
		/// </summary>
		DbProviderBase Provider { get; set; }

		/// <summary>
		/// Refresh the object from the database.
		/// </summary>
		void Refresh();
	}
}
