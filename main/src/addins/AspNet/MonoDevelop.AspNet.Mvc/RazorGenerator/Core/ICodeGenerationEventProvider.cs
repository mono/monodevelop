//
// From Razor Generator (http://razorgenerator.codeplex.com/)
// Licensed under the Microsoft Public License (MS-PL)
//

using System;

namespace RazorGenerator.Core
{
	public interface ICodeGenerationEventProvider
	{
		event EventHandler<GeneratorErrorEventArgs> Error;

		event EventHandler<ProgressEventArgs> Progress;
	}
}