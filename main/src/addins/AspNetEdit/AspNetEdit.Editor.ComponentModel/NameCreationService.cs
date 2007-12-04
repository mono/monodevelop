 /* 
 * NameCreationService.cs - Creates names for components, and checks name validity
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.ComponentModel.Design.Serialization;
using System.ComponentModel;

namespace AspNetEdit.Editor.ComponentModel
{
	public class NameCreationService : INameCreationService
	{
		public NameCreationService ()
		{
		}

		#region INameCreationService Members

		public string CreateName (System.ComponentModel.IContainer container, Type dataType)
		{
			int suffixNumber = 1;

			//check existing components with name of same form
			// and make suffixNumber bigger than the greatest of them
			foreach (IComponent comp in container.Components) {
				if (comp.Site.Name.ToLowerInvariant().StartsWith (dataType.Name.ToLowerInvariant())) {
					string str = comp.Site.Name.Substring(dataType.Name.Length);
					int val;
					if (int.TryParse(str, out val) && val >= suffixNumber)
						suffixNumber = val + 1;
					}
			}

			return dataType.Name + suffixNumber.ToString ();
		}

		//TODO: make legal name checking less severe, more correct. Project language dependency.
		public bool IsValidName (string name)
		{
			if (name.Length < 1)
				return false;

			char[] nameChar = name.ToCharArray ();
			
			if (!char.IsLetter (nameChar[0]))
				return false;

			for (int i = 1; i < nameChar.Length; i++)
			{
				if (!char.IsLetterOrDigit (nameChar[i]) && !(nameChar[i] == '_'))
					return false;
			}

			return true;
		}

		public void ValidateName (string name)
		{
			if (!IsValidName (name))
				throw new Exception ("The name is not valid.");
		}

		#endregion
}
}
