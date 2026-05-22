// <copyright file="DeprecatedEndpointAttribute.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Core.Web
{
    using System;
    using Microsoft.AspNetCore.Mvc.Filters;

    /// <summary>
    ///     Marks an action or controller as deprecated. Sets the
    ///     <c>Deprecation: true</c> response header (RFC 8594) and an optional
    ///     <c>Link: ...; rel="successor-version"</c> pointing clients at the
    ///     replacement endpoint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DeprecatedEndpointAttribute : Attribute, IActionFilter
    {
        public DeprecatedEndpointAttribute(string? successor = null)
        {
            Successor = successor;
        }

        public string? Successor { get; }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var headers = context.HttpContext.Response.Headers;
            if (!headers.ContainsKey("Deprecation"))
            {
                headers.Append("Deprecation", "true");
            }

            if (!string.IsNullOrWhiteSpace(Successor) && !headers.ContainsKey("Link"))
            {
                headers.Append("Link", $"<{Successor}>; rel=\"successor-version\"");
            }
        }
    }
}
