using System;

namespace GitSharp.Core
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    sealed class CompleteAttribute : Attribute
    {
    }
}
