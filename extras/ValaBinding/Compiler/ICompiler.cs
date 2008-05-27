//
// ICompiler.cs: interface that must be implemented by any class that wants
// to provide a compiler for the ValaBinding addin.
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

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.ValaBinding
{
	[TypeExtensionPoint ("/ValaBinding/Compilers")]
	public interface ICompiler
	{
		string Name {
			get;
		}
		
		string CompilerCommand {
			get;
		}
		
		string GetCompilerFlags (ValaProjectConfiguration configuration);
		
		string GetDefineFlags (ValaProjectConfiguration configuration);
		
		BuildResult Compile (
			ProjectFileCollection projectFiles,
			ProjectPackageCollection packages,
			ValaProjectConfiguration configuration,
			IProgressMonitor monitor);
		
		void Clean (ProjectFileCollection projectFiles, ValaProjectConfiguration configuration, IProgressMonitor monitor);
	}
}
