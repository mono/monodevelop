
using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Projects.Gui.Completion
{
	public interface IParameterDataProvider
	{
		// Returns the number of methods
		int OverloadCount { get; }
		
		// Returns the index of the parameter where the cursor is currently positioned.
		// -1 means the cursor is outside the method parameter list
		// 0 means no parameter entered
		// > 0 is the index of the parameter (1-based)
		int GetCurrentParameterIndex (ICodeCompletionContext ctx);
		
		// Returns the markup to use to represent the specified method overload
		// in the parameter information window.
		string GetMethodMarkup (int overload, string[] parameterMarkup);
		
		// Returns the text to use to represent the specified parameter
		string GetParameterMarkup (int overload, int paramIndex);
		
		// Returns the number of parameters of the specified method
		int GetParameterCount (int overload);
	}
}
