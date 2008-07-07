// SyncContext.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Activation;

namespace MonoDevelop.Core.Gui
{
	public class SyncContext
	{
		[ThreadStatic]
		static SyncContext context;
		
		static Hashtable delegateFactories = new Hashtable ();
		static ModuleBuilder module;
		static AssemblyBuilder asmBuilder;
		static int typeCounter;
	
		public static void SetContext (SyncContext ctx)
		{
			context = ctx;
		}
		
		public static SyncContext GetContext ()
		{
			return context;
		}
		
		public virtual void Dispatch (StatefulMessageHandler cb, object ob)
		{
			cb (ob);
		}
		
		public virtual void AsyncDispatch (StatefulMessageHandler cb, object ob)
		{
			cb.BeginInvoke (ob, null, null);
		}
		
		public Delegate CreateSynchronizedDelegate (Delegate del)
		{
			lock (delegateFactories.SyncRoot)
			{
				Type delType = del.GetType();
				IDelegateFactory factory = delegateFactories [delType] as IDelegateFactory;
				if (factory == null)
				{
					Type t = GetDelegateFactoryType (delType);
					factory = Activator.CreateInstance (t) as IDelegateFactory;
					delegateFactories [delType] = factory;
				}
				return factory.Create (del, this);
			}
		}
		
		Type GetDelegateFactoryType (Type delegateType)
		{
			MethodInfo invoke = delegateType.GetMethod ("Invoke");
			ModuleBuilder module = GetModuleBuilder ();
			
			// *** Data class
			StringBuilder typeNameBuilder = new StringBuilder ();
			typeNameBuilder.Append ("__");
			typeNameBuilder.Append (delegateType.Name);
			Type[] generics = delegateType.GetGenericArguments ();
			if (generics != null) {
				foreach (Type t in generics) {
					typeNameBuilder.Append ("_");
					typeNameBuilder.Append (t.Name);
				}
			}
			TypeBuilder dataTypeBuilder = module.DefineType (typeNameBuilder + "_DelegateData_" + (typeCounter++), TypeAttributes.Public, typeof(object), Type.EmptyTypes);
			
			// Parameters
			ParameterInfo[] pars = invoke.GetParameters ();
			FieldBuilder[] paramFields = new FieldBuilder [pars.Length];
			Type[] paramTypes = new Type[pars.Length];
			for (int n=0; n<pars.Length; n++)
			{
				ParameterInfo pi = pars [n];
				paramFields [n] = dataTypeBuilder.DefineField ("p" + n, pi.ParameterType, FieldAttributes.Public);
				paramTypes [n] = pi.ParameterType;
			}
			
			// Return value
			FieldBuilder returnField = null;
			if (invoke.ReturnType != typeof(void))
				returnField = dataTypeBuilder.DefineField ("ret", invoke.ReturnType, FieldAttributes.Public);
			
			// Constructor
			ConstructorBuilder dataCtor = dataTypeBuilder.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
			ConstructorInfo baseCtor = typeof(object).GetConstructor (Type.EmptyTypes);
			ILGenerator gen = dataCtor.GetILGenerator();
			gen.Emit (OpCodes.Ldarg_0);
			gen.Emit (OpCodes.Call, baseCtor);
			gen.Emit (OpCodes.Ret);

			
			// *** Factory class
			
			TypeBuilder typeBuilder = module.DefineType (typeNameBuilder + "_DelegateFactory_" + typeCounter, TypeAttributes.Public, typeof(object), new Type[] {typeof(IDelegateFactory)});
			
			// Context and target delegate field
			
			FieldBuilder contextField = typeBuilder.DefineField ("context", typeof(SyncContext), FieldAttributes.Public);
			FieldBuilder targetField = typeBuilder.DefineField ("target", delegateType, FieldAttributes.Public);
			
			// Constructor
			
			ConstructorBuilder ctor = typeBuilder.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
			gen = ctor.GetILGenerator();
			gen.Emit (OpCodes.Ldarg_0);
			gen.Emit (OpCodes.Call, baseCtor);
			gen.Emit (OpCodes.Ret);
			
			// Dispatch method
			
			MethodBuilder methodDispatch = typeBuilder.DefineMethod ("Dispatch", MethodAttributes.Public, typeof(void), new Type[] {typeof(object)});
			gen = methodDispatch.GetILGenerator();
			
			LocalBuilder data = gen.DeclareLocal (dataTypeBuilder);
			gen.Emit (OpCodes.Ldarg_1);
			gen.Emit (OpCodes.Castclass, dataTypeBuilder);
			gen.Emit (OpCodes.Stloc, data);
			
			if (returnField != null)
				gen.Emit (OpCodes.Ldloc, data);

			gen.Emit (OpCodes.Ldarg_0);
			gen.Emit (OpCodes.Ldfld, targetField);
			
			for (int n=0; n<pars.Length; n++) {
				gen.Emit (OpCodes.Ldloc, data);
				gen.Emit (OpCodes.Ldfld, paramFields[n]);
			}
			gen.Emit (OpCodes.Callvirt, invoke);
			
			if (returnField != null)
				gen.Emit (OpCodes.Stfld, returnField);
	
			gen.Emit (OpCodes.Ret);
			
			// ProxyCall method
			
			MethodBuilder methodProxyCall = typeBuilder.DefineMethod ("ProxyCall", MethodAttributes.Public, invoke.ReturnType, paramTypes);
			gen = methodProxyCall.GetILGenerator();
			
			data = gen.DeclareLocal (dataTypeBuilder);
			gen.Emit (OpCodes.Newobj, dataCtor);
			gen.Emit (OpCodes.Stloc, data);
			
			for (int n=0; n<paramFields.Length; n++) {
				gen.Emit (OpCodes.Ldloc, data);
				gen.Emit (OpCodes.Ldarg, n+1);
				gen.Emit (OpCodes.Stfld, paramFields[n]);
			}
			gen.Emit (OpCodes.Ldarg_0);
			gen.Emit (OpCodes.Ldfld, contextField);
			gen.Emit (OpCodes.Ldarg_0);
			gen.Emit (OpCodes.Ldftn, methodDispatch);
			gen.Emit (OpCodes.Newobj, typeof(StatefulMessageHandler).GetConstructor (new Type[] {typeof(object), typeof(IntPtr)} ));
			gen.Emit (OpCodes.Ldloc, data);
			
			if (returnField != null) {
				gen.Emit (OpCodes.Callvirt, typeof(SyncContext).GetMethod ("Dispatch"));
				gen.Emit (OpCodes.Ldloc, data);
				gen.Emit (OpCodes.Ldfld, returnField);
			}
			else {
				gen.Emit (OpCodes.Callvirt, typeof(SyncContext).GetMethod ("AsyncDispatch"));
			}
			gen.Emit (OpCodes.Ret);
			
			// Create method
			
			MethodBuilder methodCreate = typeBuilder.DefineMethod ("Create", MethodAttributes.Public | MethodAttributes.Virtual, typeof(Delegate), new Type[] {typeof(Delegate), typeof(SyncContext)});
			gen = methodCreate.GetILGenerator();
			LocalBuilder vthis = gen.DeclareLocal (typeBuilder);
			gen.Emit (OpCodes.Newobj, ctor);
			gen.Emit (OpCodes.Stloc, vthis);
			gen.Emit (OpCodes.Ldloc, vthis);
			gen.Emit (OpCodes.Ldarg_1);
			gen.Emit (OpCodes.Castclass, delegateType);
			gen.Emit (OpCodes.Stfld, targetField);
			gen.Emit (OpCodes.Ldloc, vthis);
			gen.Emit (OpCodes.Ldarg_2);
			gen.Emit (OpCodes.Stfld, contextField);
			gen.Emit (OpCodes.Ldloc, vthis);
			gen.Emit (OpCodes.Ldftn, methodProxyCall);
			gen.Emit (OpCodes.Newobj, delegateType.GetConstructor (new Type[] {typeof(object), typeof(IntPtr)} ));
			gen.Emit (OpCodes.Ret);
			typeBuilder.DefineMethodOverride (methodCreate, typeof(IDelegateFactory).GetMethod ("Create"));
			
			dataTypeBuilder.CreateType ();
			return typeBuilder.CreateType ();
		}
		
		static ModuleBuilder GetModuleBuilder ()
		{
			if (module == null)
			{
				AppDomain myDomain = System.Threading.Thread.GetDomain();
				AssemblyName myAsmName = new AssemblyName();
				myAsmName.Name = "MonoDevelop.DelegateGenerator.GeneratedAssembly";
	
				asmBuilder = myDomain.DefineDynamicAssembly (myAsmName, AssemblyBuilderAccess.RunAndSave);
				module = asmBuilder.DefineDynamicModule ("MonoDevelop.DelegateGenerator.GeneratedAssembly", "MonoDevelop.DelegateGenerator.GeneratedAssembly.dll");
			}
			return module;
		}
	}
	
	public interface IDelegateFactory
	{
		Delegate Create (Delegate del, SyncContext ctx);
	}
	
	
	/* Sample class generated for the EventHandler delegate
	
	class __EventHandler_DelegateData
	{
		public object psender;
		public EventArgs pargs;
	}
	
	class __EventHandler_DelegateFactory: IDelegateFactory
	{
		EventHandler target;
		SyncContext context;
		
		public Delegate Create (Delegate del, SyncContext ctx)
		{
			__EventHandler_DelegateFactory vthis = new __EventHandler_DelegateFactory ();
			vthis.target = del;
			vthis.context = ctx;
			return new EventHandler (vthis.ProxyCall);
		}
		
		public void ProxyCall (object sender, EventArgs args)
		{
			__EventHandler_DelegateData data = new __EventHandler_DelegateData ();
			data.psender = sender;
			data.pargs = args;
			StatefulMessageHandler msg = new StatefulMessageHandler (Dispatch);
			context.AsyncDispatch (msg, data);
		}
		
		public void Dispatch (object obj)
		{
			__EventHandler_DelegateData data = (__EventHandler_DelegateData) obj;
			target (data.psender, data.pargs);
		}
	}
	
	*/
}
