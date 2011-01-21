// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX {

// "Smart" pointer type, to automatically add-ref/release native
// interface pointers retrieved from managed wrappers during calls
// to native methods.
template<typename T>
class KeepAlivePointer
{
public:

	KeepAlivePointer(T* ptr)
	{
		setPointer(ptr);
	}

	T* operator->()
	{
		return _ptr;
	}

	~KeepAlivePointer()
	{
		releasePointer();
	}

    // Technically, the rest of these public members are not needed.
    // The Code Pack doesn't use the result of the CastInterface()
    // method in a way where these would be used.  But as a template,
    // when these aren't used they aren't adding anything to the compiled
    // output, and they might as well remain just in case they do become
    // needed some time in the future.

	KeepAlivePointer(const KeepAlivePointer &src)
	{
		setPointer(src._ptr);
	}

	KeepAlivePointer &operator=(const KeepAlivePointer &src)
	{
		releasePointer();
		setPointer(src._ptr);
		return *this;
	}

	operator T*() const
	{
		return _ptr;
	}

	T& operator*() const
	{
		return *_ptr;
	}

	T** operator&()
	{
		return &_ptr;
	}

	bool operator!() const
	{
		return (_ptr == NULL);
	}

	bool operator<(T* pT) const
	{
		return _ptr < pT;
	}

	bool operator!=(T* pT) const
	{
		return !operator==(pT);
	}

	bool operator==(T* pT) const
	{
		return _ptr == pT;
	}

private:

    T* _ptr;

	void setPointer(T* ptr)
	{
		if (ptr != NULL)
		{
			ptr->AddRef();
		}
		_ptr = ptr;
	}

	void releasePointer(void)
	{
		if (_ptr != NULL)
		{
			_ptr->Release();
			_ptr = NULL;
		}
	}
};

/// <summary>
/// Base class for all classes supporting internal IUnknown interfaces
/// </summary>
public ref class DirectUnknown abstract
{
internal:
    // REVIEW: this is convenient for converting arbitrary objects. But types
    // generally have exactly one interface to which they correspond.  Compile-time
    // type safety would be improved if managed types had a typed property for getting
    // the unmanaged interface (e.g. via a generic base class it inherits, or a
    // typed member method that calls this one).
    //
    // In particular, by making DirectUnknown and DirectObject generic, they could
    // have a strongly-typed Interface property that performed this cast, and the client
    // could could _only_ get the correct interface type.  (Rather than, as in the case
    // of one question I've already answered for a CodePack client, having them mistakenly
    // think that GetInterface() actually performs a QueryInterface() for them).
    //
	// One possible 'gotcha': The Utilities::Convert::CreateIUnknownWrapper method
	// needs to be able to instantiate wrapper objects using reflection. But matching
	// constructors to the unmanaged interface type might be difficult, as all that
	// the CreateIUnknownWrapper (or even its callers) have is an IUnknown interface
	// and a GUID.
	//
    // Another 'gotcha': probably can't inherit generic classes using the unmanaged
    // types for types that are public in managed code.  We'd need a managed interface
    // wrapper for that to work.
    //
	// It would be great to fix this somehow. We could get rid of all the default
	// constructors, requiring the caller to provide a strongly-typed interface.
	// 
	// For now, leave the Attach() method 'internal' so that CreateIUnknownWrapper
	// and its sibling, DirectHelpers::CreateInterfaceWrapper, can call it.
	//
    // Code Analysis note: the current approach results in FxCop not understanding
    // why a specific type is passed to a method when only the DirectUnknown method
    // here is ever used, resulting in a large number of suppressions. Making the above
    // change would allow removal of those suppressions.
    //
    // See also DirectObject type

    // XML comments can't be applied to templates
	// <summary>
	// Casts the native interface pointer in this object to the given type.
	// </summary>
	// <returns>
	// The native interface pointer, as the given type.
	// </returns>
    template <typename T>
    KeepAlivePointer<T> CastInterface(void)
    {
        return KeepAlivePointer<T>(static_cast<T*>(nativeUnknown.Get()));
    }

    DirectUnknown(IUnknown* _iUnknown);

    DirectUnknown(void);

	// Should be used ONLY by Utilities::Convert::CreateIUnknownWrapper!
    void Attach(IUnknown* _right);

public:
    /// <summary>
    /// Get the internal native pointer for the wrapped IUnknown Interface
    /// </summary>
    /// <returns>
    /// A pointer to the wrapped native IUnknown.
    /// </returns>
    property IntPtr NativeInterface
    {
        IntPtr get()
        {
            return IntPtr(nativeUnknown.Get());
        }
    }

    generic<typename T> where T : DirectUnknown
    T QueryInterface(void);

private:
    AutoIUnknown<IUnknown> nativeUnknown;
};
} } }
