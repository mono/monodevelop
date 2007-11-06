//  ReflectionReturnType.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class ReflectionReturnType : DefaultReturnType
	{
		public ReflectionReturnType(TypeReference type)
		{
			// The following method extracts array, pointer, byref and generic
			// info. It also extracts the name of the type without array,
			// pointer, byref and generic decorations
			Stack<int> arrays = CheckType(type);
			// Convert to '.' delimited names
			FullyQualifiedName = FullyQualifiedName.Replace("+", ".").Replace("/", ".");
			arrayDimensions = (arrays == null) ? new int[0] : arrays.ToArray();
		}
		
		/// <summary>Extracts all relevant info about a return type (as returned
		/// by Mono.Cecil).
		/// </summary>
		/// <remarks>
		/// Relevant info is:
		/// <ul>
		/// <li>array dimensions and ranks</li>
		/// <li>generic arguments</li>
		/// <li>level of pointer nesting</li>
		/// </ul>
		/// </remarks>
		/// <returns>
		/// An array of ranks. The length of the return value is the dimension
		/// of the array.
		/// </returns>
		Stack<int> CheckType(TypeReference type)
		{
			// Create the ArrayList on demand
			Stack<int> arrays = null;
			do
			{
				// Check if 'type' has some decorations applied to it
				if (type is Mono.Cecil.TypeSpecification) {
					// Go through all levels of 'indirection', 'array dimensions'
					// and 'generic types' - in the end, we should get the actual
					// type of the ReturnType (but all data about its array
					// dimensions, levels of indirection and even its generic
					// parameters is correctly stored within ArrayCount and
					// ArrayDimensions, PointerNestingLevel and GenericArguments
					// respectively).
					if (type is ArrayType) {
						// This return type is obviously an array - add the rank
						ArrayType at = (ArrayType) type;
						if (arrays == null)
							arrays = new Stack<int>();
						arrays.Push(at.Rank);
						type = at.ElementType;
					} else if (type is GenericInstanceType) {
						// This return type is obviously a generic type - add its
						// generic arguments
						GenericInstanceType git = (GenericInstanceType) type;
						GenericArguments = new ReturnTypeList();
						foreach (TypeReference tr in git.GenericArguments) {
							GenericArguments.Add(new ReflectionReturnType(tr));
						}
						// Go down one level... we have the generic info
						type = git.ElementType;
					} else if (type is Mono.Cecil.ReferenceType) {
						Mono.Cecil.ReferenceType rt = (Mono.Cecil.ReferenceType) type;
						byRef = true;
						type = rt.ElementType;
					} else if (type is PointerType) {
						// The type is a pointer
						PointerType pt = (PointerType) type;
						++pointerNestingLevel;
						type = pt.ElementType;
						// Go down one level
					} else {
						// TODO: Check if we loose some relevant info here
						type = ((TypeSpecification)type).ElementType;
					}
				} else {
					// At any event - we should end here
					FullyQualifiedName = type.FullName;
					// The method returns the stripped type...
					return arrays;
				}
			} while (true);
		}
	}
}
