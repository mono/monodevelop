//  ICompilerResult.cs
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

using System.Collections;
using System.CodeDom.Compiler;
using System.Xml;

namespace MonoDevelop.Projects
{
	/// <summary>
	/// Each language module which is capable of compiling source
	/// files gives back an ICompilerResult object which contains
	/// the output of the compiler and the compilerresults which contain
	/// all warnings and errors.
	/// </summary>
	public interface ICompilerResult
	{
		/// <summary>
		/// the compilerresults which contain all warnings and errors the compiler
		/// produces.
		/// </summary>
		CompilerResults CompilerResults {
			get;
		}
		
		int WarningCount  { get; }
		
		int ErrorCount { get; }
		
		int BuildCount { get; }
		
		int FailedBuildCount { get; }
		
		/// <summary>
		/// The console output of the compiler as string.
		/// </summary>
		string CompilerOutput {
			get;
		}
		
		void AddError (string file, int line, int col, string errorNum, string text);
		void AddError (string text);
		void AddWarning (string file, int line, int col, string errorNum, string text);
		void AddWarning (string text);
	}
}
