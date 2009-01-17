// 
// UserInformation.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Ide.Gui
{
	[DataItem]
	public sealed class UserInformation
	{
		
		internal UserInformation (string name, string email)
		{
			this.Name = name;
			this.Email = email;
		}
		
		internal UserInformation ()
		{
		}
		
		[ItemProperty]
		public string Name { get; private set; }
		
		[ItemProperty]
		public string Email { get; private set; }
		
		internal static UserInformation Default {
			get {
				string name = GetValueOrMigrate<string> ("User.Name", "ChangeLogAddIn.Name") ?? Environment.UserName;
				string email = GetValueOrMigrate<string> ("User.Email", "ChangeLogAddIn.Email");
				return new UserInformation (name, email);
			}
		}
		
		static T GetValueOrMigrate<T> (string name, string oldName)
		{
			T val = PropertyService.Get<T> (name);
			if (val != null)
				return val;
			
			val = PropertyService.Get<T> (oldName);
			if (val != null) {
				PropertyService.Set (oldName, null);
				PropertyService.Set (name, val);
			}
			return val;
		}
	}
}
