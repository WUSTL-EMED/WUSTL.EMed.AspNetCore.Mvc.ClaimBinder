// <copyright file="ClaimModelBinder.cs" company="Washington University in St. Louis">
// Copyright (c) 2021 Washington University in St. Louis. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// </copyright>

namespace WUSTL.EMed.AspNetCore.Mvc.ClaimBinder.ModelBinders
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Extensions.Logging;
    using WUSTL.EMed.AspNetCore.Mvc.ClaimBinder.Properties;

    /// <summary>
    /// Specifies that a parameter or property should be bound using a request claim.
    /// </summary>
    public class ClaimModelBinder : IModelBinder
    {
        /// <summary>
        /// A <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource"/> for the request claims.
        /// </summary>
        public static readonly BindingSource BindingSource = new BindingSource(
            "Claim",
            Resources.BindingSource_Claim,
            isGreedy: true,
            isFromRequest: true);

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimModelBinder"/> class.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public ClaimModelBinder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ClaimModelBinder>();
        }

        /// <inheritdoc/>
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var userClaims = bindingContext.HttpContext?.User?.Claims;
            if (userClaims is null)
            {
                _logger.FoundNoValueInRequest(bindingContext);

                // no entry
                _logger.DoneAttemptingToBindModel(bindingContext);
                return Task.CompletedTask;
            }

            // TODO: Allow binding of claim types with multiple claims to a collection similar to https://github.com/dotnet/aspnetcore/blob/master/src/Mvc/Mvc.Core/src/ModelBinding/Binders/HeaderModelBinder.cs
            var claim = userClaims.SingleOrDefault(_ => _.Type.Equals(bindingContext.FieldName, StringComparison.OrdinalIgnoreCase));
            if (claim is null)
            {
                _logger.FoundNoValueInRequest(bindingContext);

                // no entry
                _logger.DoneAttemptingToBindModel(bindingContext);
                return Task.CompletedTask;
            }

            _logger.AttemptingToBindModel(bindingContext);

            var valueProviderResult = new ValueProviderResult(claim.Value);
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var typeConverter = TypeDescriptor.GetConverter(bindingContext.ModelType);

            try
            {
                var value = valueProviderResult.FirstValue;

                object model;
                if (bindingContext.ModelType == typeof(string))
                {
                    // Already have a string. No further conversion required but handle ConvertEmptyStringToNull.
                    if (bindingContext.ModelMetadata.ConvertEmptyStringToNull && string.IsNullOrWhiteSpace(value))
                    {
                        model = null;
                    }
                    else
                    {
                        model = value;
                    }
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    // Other than the StringConverter, converters Trim() the value then throw if the result is empty.
                    model = null;
                }
                else
                {
                    model = typeConverter.ConvertFrom(
                        context: null,
                        culture: valueProviderResult.Culture,
                        value: value);
                }

                CheckModel(bindingContext, valueProviderResult, model);

                _logger.DoneAttemptingToBindModel(bindingContext);
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                var isFormatException = exception is FormatException;
                if (!isFormatException && exception.InnerException != null)
                {
                    // TypeConverter throws System.Exception wrapping the FormatException,
                    // so we capture the inner exception.
                    exception = ExceptionDispatchInfo.Capture(exception.InnerException).SourceException;
                }

                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    exception,
                    bindingContext.ModelMetadata);

                // Were able to find a converter for the type but conversion failed.
                return Task.CompletedTask;
            }
        }

        private static void CheckModel(ModelBindingContext bindingContext, ValueProviderResult valueProviderResult, object model)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // When converting newModel a null value may indicate a failed conversion for an otherwise required
            // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
            // current bindingContext. If not, an error is logged.
            if (model == null && !bindingContext.ModelMetadata.IsReferenceOrNullableType)
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(
                        valueProviderResult.ToString()));
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Success(model);
            }
        }
    }
}
