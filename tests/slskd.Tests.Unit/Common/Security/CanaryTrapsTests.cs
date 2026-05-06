// <copyright file="CanaryTrapsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Common.Security;
using Xunit;

public class CanaryTrapsTests
{
    [Fact]
    public void GenerateCanary_DoesNotKeepReferenceToProvidedSecret()
    {
        var logger = Mock.Of<ILogger<CanaryTraps>>();
        var secretKey = Enumerable.Range(0, 4).Select(i => (byte)i).ToArray();
        var canary = new CanaryTraps(logger, secretKey);

        secretKey[0] = 0xFF;

        var field = typeof(CanaryTraps).GetField("_secretKey", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var storedSecret = (byte[]?)field!.GetValue(canary);
        Assert.NotNull(storedSecret);
        Assert.NotEqual(secretKey[0], storedSecret![0]);
    }
}
