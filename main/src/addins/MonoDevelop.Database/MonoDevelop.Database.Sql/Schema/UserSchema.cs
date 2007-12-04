//
// Authors:
//	Christian Hergert  <chris@mosaix.net>
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
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
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public class UserSchema : AbstractSchema
	{
		protected string userId;
		protected string password;
		protected DateTime expires = DateTime.MinValue;
		
		//TODO: list of allowed hosts?
		
		public UserSchema (ISchemaProvider schemaProvider)
			: base (schemaProvider)
		{
		}
		
		public UserSchema (UserSchema user)
			: base (user)
		{
			this.userId = user.userId;
			this.password = user.password;
			this.expires = user.expires;
		}
		
		public virtual ICollection<RoleSchema> Roles {
			get {
				throw new NotImplementedException();
			}
		}
		
		public virtual string UserId {
			get { return userId; }
			set {
				if (userId != value) {
					userId = value;
					OnChanged ();
				}
			}
		}
		
		public virtual string Password {
			get { return password; }
			set {
				if (password != value) {
					password = value;
					OnChanged ();
				}
			}
		}
		
		public virtual DateTime Expires {
			get { return expires; }
			set {
				if (expires != value) {
					expires = value;
					OnChanged ();
				}
			}
		}
		
		public override object Clone ()
		{
			return new UserSchema (this);
		}
	}
}
