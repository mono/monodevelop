using System;
using System.Collections.Generic;
using System.Reflection;
using Jurassic.Compiler;

namespace Jurassic.Library
{
    /// <summary>
    /// Provides functionality common to all JavaScript objects.
    /// </summary>
    [Serializable]
    public class ObjectInstance
    {
        // The script engine associated with this object.
        [NonSerialized]
        private ScriptEngine engine;

        // public prototype chain.
        private ObjectInstance prototype;

        [Flags]
        private enum ObjectFlags
        {
            /// <summary>
            /// Indicates whether properties can be added to this object.
            /// </summary>
            Extensible = 1,
        }

        // Stores flags related to this object.
        private ObjectFlags flags = ObjectFlags.Extensible;



        //     INITIALIZATION
        //_________________________________________________________________________________________


        /// <summary>
        /// Called by derived classes to create a new object instance.
        /// </summary>
        /// <param name="engine"> The script engine associated with this object. </param>
        /// <param name="prototype"> The next object in the prototype chain.  Can be <c>null</c>. </param>
        protected ObjectInstance(ScriptEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException("engine");
            this.engine = engine;
        }

        /// <summary>
        /// Creates an Object with no prototype to serve as the base prototype of all objects.
        /// </summary>
        /// <param name="engine"> The script engine associated with this object. </param>
        /// <returns> An Object with no prototype. </returns>
        public static ObjectInstance CreateRootObject(ScriptEngine engine)
        {
            return new ObjectInstance(engine);
        }


        //     .NET ACCESSOR PROPERTIES
        //_________________________________________________________________________________________

        /// <summary>
        /// Gets a reference to the script engine associated with this object.
        /// </summary>
        public ScriptEngine Engine
        {
            get { return this.engine; }
        }

        /// <summary>
        /// Gets the public class name of the object.  Used by the default toString()
        /// implementation.
        /// </summary>
        protected virtual string publicClassName
        {
            get { return this is ObjectInstance ? "Object" : this.GetType().Name; }
        }

        /// <summary>
        /// Gets the next object in the prototype chain.  There is no corresponding property in
        /// javascript (it is is *not* the same as the prototype property), instead use
        /// Object.getPrototypeOf().
        /// </summary>
        public ObjectInstance Prototype
        {
            get { return this.prototype; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the object can have new properties added
        /// to it.
        /// </summary>
        public bool IsExtensible
        {
            get { return (this.flags & ObjectFlags.Extensible) != 0; }
            set
            {
                if (value == true)
                    throw new InvalidOperationException("Once an object has been made non-extensible it cannot be made extensible again.");
                this.flags &= ~ObjectFlags.Extensible;
            }
        }

    }
}
