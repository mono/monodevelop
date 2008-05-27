//
// ValaProjectServiceExtension.cs
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
using System.IO;
using System.Text;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.ValaBinding
{
	public class ValaProjectServiceExtension : ProjectServiceExtension
	{
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem entry, string configuration)
		{
			ValaProject project = entry as ValaProject;
			
			if (project == null)
				return base.Build (monitor, entry, configuration);
			
			foreach (ValaProject p in project.DependedOnProjects ()) {
				p.Build (monitor, configuration, true);
			}
			
			ValaProjectConfiguration conf = (ValaProjectConfiguration)project.GetConfiguration(configuration);
			
			if (conf.CompileTarget != CompileTarget.Bin)
				project.WriteMDPkgPackage (configuration);
			
			return base.Build (monitor, entry, configuration);
		}
		
		protected override void Clean (IProgressMonitor monitor, SolutionEntityItem entry, string configuration)
		{
			base.Clean (monitor, entry, configuration);
			
			ValaProject project = entry as ValaProject;
			if (project == null)
				return;
			
			project.Compiler.Clean (project.Files, (ValaProjectConfiguration) project.GetConfiguration(configuration), monitor);
		}
	}
}
