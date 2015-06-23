// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace ICSharpCode.NRefactory6.CSharp.ExtractMethod
{
	public enum DeclarationBehavior
    {
        None,
        Delete,
        MoveIn,
        MoveOut,
        SplitIn,
        SplitOut
    }

	public enum ReturnBehavior
    {
        None,
        Initialization,
        Assignment
    }

	public enum ParameterBehavior
    {
        None,
        Input,
        Out,
        Ref
    }

    /// <summary>
    /// status code for extract method operations
    /// </summary>
    [Flags]
	public enum OperationStatusFlag
    {
        None = 0x0,

        /// <summary>
        /// operation has succeeded
        /// </summary>
        Succeeded = 0x1,

        /// <summary>
        /// operation has succeeded with a span that is different than original span
        /// </summary>
        Suggestion = 0x2,

        /// <summary>
        /// operation has failed but can provide some best effort result
        /// </summary>
        BestEffort = 0x4,
    }
}
