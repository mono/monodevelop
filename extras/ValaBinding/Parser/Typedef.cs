//
// Typedef.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//


using System;

using MonoDevelop.Projects;

namespace MonoDevelop.ValaBinding.Parser
{
	public class Typedef : LanguageItem
	{		
		public Typedef (Tag tag, Project project, string ctags_output) : base (tag, project)
		{			
			if (GetNamespace (tag, ctags_output)) return;
			if (GetClass (tag, ctags_output)) return;
			if (GetStructure (tag, ctags_output)) return;
			if (GetUnion (tag, ctags_output)) return;
		}
	}
}
