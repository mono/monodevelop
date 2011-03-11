// 
// MonoMacPackagingTool.cs
//  
// Author:
//       David Siegel <djsiegel@gmail.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.IO;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.MacDev;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;

using MonoDevelop.MonoMac.Gui;

namespace MonoDevelop.MonoMac
{
    public class MonoMacPackagingTool : IApplication
    {
        MonoMacPackagingSettings JustLinkMonoSettings =
            new MonoMacPackagingSettings { IncludeMono   = true,
                                           LinkerMode    = MonoMacLinkerMode.LinkAll,
                                           SignBundle    = false,
                                           SignPackage   = false,
                                           CreatePackage = false
                                         };
                
        public int Run (string [] arguments)
        {
            var monitor = new ConsoleProgressMonitor ();
            var project = FindMonoMacProject (monitor);
            
            if (project == null) {
                Console.WriteLine (GettextCatalog.GetString ("Error: Could not find a MonoMac project to bundle."));
                return 1;
            }
            
            MonoMacPackaging.BuildPackage (monitor, project, project.DefaultConfiguration.Selector,
                JustLinkMonoSettings, project.Name + ".app");
    
            return 0;
        }
        
        MonoMacProject FindMonoMacProject (IProgressMonitor monitor)
        {
            var projects =
                from solutionFile in Directory.GetFiles (".", "*.sln")
                let solution = Services.ProjectService.ReadWorkspaceItem (monitor, solutionFile)
                from project in solution.GetAllProjects ().OfType<MonoMacProject> ()
                select project;
            
            return projects.FirstOrDefault ();
        }
    }
}
