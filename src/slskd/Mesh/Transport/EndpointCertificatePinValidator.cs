// <copyright file="EndpointCertificatePinValidator.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Mesh.Transport;

using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Validates a peer certificate against pins explicitly configured for its endpoint.
/// </summary>
public static class EndpointCertificatePinValidator
{
    public static bool Validate(
        IPEndPoint endpoint,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors errors,
        IReadOnlyDictionary<string, List<string>> trustedPins)
    {
        return trustedPins.TryGetValue(endpoint.ToString(), out var pins) &&
               SecurityUtils.CreatePinningValidationCallback(pins)(certificate, chain, errors);
    }
}
