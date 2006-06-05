/*
Copyright (C) 2006  Matthias Braun <matze@braunis.de>
					Scott Ellington <scott.ellington@gmail.com>
 
This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the
Free Software Foundation, Inc., 59 Temple Place - Suite 330,
Boston, MA 02111-1307, USA.
*/

using System;
using System.IO;
using System.Text;
using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{
	public class SolutionMakefileHandler : IMakefileHandler
	{
		// Recurses into children and tests if they are deployable.
		public bool CanDeploy ( CombineEntry entry )
		{
			Combine combine = entry as Combine;
			
			if ( combine == null ) return false;
			foreach ( CombineEntry ce in combine.Entries )
			{
				IMakefileHandler handler = AutotoolsContext.GetMakefileHandler ( ce );
				if ( handler == null ) return false;
				if ( !handler.CanDeploy ( ce ) )  return false;
			}
			return true;
		}

		public Makefile Deploy ( AutotoolsContext ctx, CombineEntry entry )
		{
			if ( !CanDeploy ( entry ) )
				throw new Exception ( "Not a deployable combine." );

			Combine combine = entry as Combine;

			StringBuilder subdirs = new StringBuilder();
			subdirs.Append ("#Warning: This is an automatically generated file, do not edit!\n");
			subdirs.Append ("SUBDIRS = ");
			foreach ( CombineEntry ce in ctx.CalculateSubDirOrder ( combine ) )
			{
				string path = Path.GetDirectoryName (ce.RelativeFileName);
				if (path.StartsWith ("./") )
					path = path.Substring (2);
				subdirs.Append (" ");
				subdirs.Append ( AutotoolsContext.EscapeStringForAutomake (path) );

				IMakefileHandler handler = AutotoolsContext.GetMakefileHandler ( ce );
				Makefile makefile;
				if ( handler.CanDeploy ( ce ) )
				{
					makefile = handler.Deploy ( ctx, ce );
					string outpath = Path.GetDirectoryName(ce.FileName) + "/Makefile";
					StreamWriter writer = new StreamWriter ( outpath + ".am" );
					makefile.Write ( writer );
					writer.Close ();
					ctx.AddAutoconfFile ( outpath );
				}
			}

			Makefile mfile = new Makefile ();
			mfile.Append ( subdirs.ToString () );
			return mfile;
		}
	}
}

