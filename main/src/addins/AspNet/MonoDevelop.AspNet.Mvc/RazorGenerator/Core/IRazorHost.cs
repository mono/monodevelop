//
// From Razor Generator (http://razorgenerator.codeplex.com/)
// Licensed under the Microsoft Public License (MS-PL)
//

using System;

namespace RazorGenerator.Core
{
	public interface IRazorHost
	{
		string DefaultNamespace { get; set; }

		bool EnableLinePragmas { get; set; }

		event EventHandler<GeneratorErrorEventArgs> Error;

		event EventHandler<ProgressEventArgs> Progress;

		string GenerateCode();
	}
}