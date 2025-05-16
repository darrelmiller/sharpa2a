// Copyright (c) Microsoft. All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP
#pragma warning disable IDE0005 // Using directive is unnecessary.
using System;  

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class SetsRequiredMembersAttribute : Attribute  
{  
    // This attribute can be empty. It's just a marker.  
    public SetsRequiredMembersAttribute() { }  
}
#endif