// <copyright file="WebServiceCollectionExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Bootstrap;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using slskd.Authentication;
using slskd.Bootstrap;
using Xunit;

public class WebServiceCollectionExtensionsTests
{
    [Fact]
    public void SelectAuthenticationScheme_uses_api_key_scheme_for_api_key_header()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-Key"] = "configured-key";

        var scheme = WebServiceCollectionExtensions.SelectAuthenticationScheme(context);

        Assert.Equal(ApiKeyAuthentication.AuthenticationScheme, scheme);
    }

    [Fact]
    public void SelectAuthenticationScheme_uses_jwt_scheme_without_api_key_header()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer token";

        var scheme = WebServiceCollectionExtensions.SelectAuthenticationScheme(context);

        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, scheme);
    }
}
