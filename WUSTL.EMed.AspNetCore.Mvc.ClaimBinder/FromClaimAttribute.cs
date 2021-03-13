// <copyright file="FromClaimAttribute.cs" company="Washington University in St. Louis">
// Copyright (c) 2021 Washington University in St. Louis. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// </copyright>

namespace WUSTL.EMed.AspNetCore.Mvc.ClaimBinder
{
    using System;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using WUSTL.EMed.AspNetCore.Mvc.ClaimBinder.ModelBinders;

    /// <summary>
    /// Specifies that a parameter or property should be bound using the request claims.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class FromClaimAttribute : Attribute, IModelNameProvider, IBinderTypeProviderMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FromClaimAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the claim type to bind using.</param>
        public FromClaimAttribute(string name)
        {
            Name = name;
        }

        /// <inheritdoc/>
        public Type BinderType => typeof(ClaimModelBinder);

        /// <inheritdoc/>
        public BindingSource BindingSource => ClaimModelBinder.BindingSource;

        /// <inheritdoc/>
        public string Name { get; }
    }
}
