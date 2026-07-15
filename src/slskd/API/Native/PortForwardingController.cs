// <copyright file="PortForwardingController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using slskd.Common.Security;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace slskd.API.Native;

using slskd.Core.Security;

/// <summary>
/// Controller for managing local port forwarding through VPN tunnels.
/// </summary>
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("0")]
[ApiController]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly] // CSRF protection for cookie-based auth (exempts JWT/API key)
public class PortForwardingController : ControllerBase
{
    private readonly LocalPortForwarder _portForwarder;

    public PortForwardingController(LocalPortForwarder portForwarder)
    {
        _portForwarder = portForwarder;
    }

    /// <summary>
    /// Starts port forwarding from a local port to a remote service through a VPN tunnel.
    /// </summary>
    /// <param name="request">The port forwarding configuration.</param>
    /// <returns>A success response.</returns>
    [HttpPost("start")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    public async Task<IActionResult> StartForwarding([FromBody] StartPortForwardingRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { Error = "Request is required" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var podId = request.PodId?.Trim() ?? string.Empty;
        var destinationHost = request.DestinationHost?.Trim() ?? string.Empty;
        var serviceName = string.IsNullOrWhiteSpace(request.ServiceName) ? null : request.ServiceName.Trim();

        if (string.IsNullOrWhiteSpace(podId))
        {
            return BadRequest(new { Error = "PodId is required" });
        }

        if (string.IsNullOrWhiteSpace(destinationHost))
        {
            return BadRequest(new { Error = "DestinationHost is required" });
        }

        try
        {
            await _portForwarder.StartForwardingAsync(
                request.LocalPort,
                podId,
                destinationHost,
                request.DestinationPort,
                serviceName);

            return Ok(new { Message = "Port forwarding started" });
        }
        catch (InvalidOperationException)
        {
            return Conflict(new { Error = "Port forwarding is already configured for this local port" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Failed to start port forwarding" });
        }
    }

    /// <summary>
    /// Stops port forwarding on a specific local port.
    /// </summary>
    /// <param name="localPort">The local port to stop forwarding.</param>
    /// <returns>A success response.</returns>
    [HttpPost("stop/{localPort:int}")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    public async Task<IActionResult> StopForwarding(int localPort)
    {
        try
        {
            await _portForwarder.StopForwardingAsync(localPort);
            return Ok(new { Message = "Port forwarding stopped" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Failed to stop port forwarding" });
        }
    }

    /// <summary>
    /// Gets the status of all active port forwarders.
    /// </summary>
    /// <returns>A list of port forwarding status information.</returns>
    [HttpGet("status")]
    public IActionResult GetForwardingStatus()
    {
        try
        {
            var status = _portForwarder.GetForwardingStatus();
            return Ok(status);
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Failed to get forwarding status" });
        }
    }

    /// <summary>
    /// Gets the status of a specific port forwarder.
    /// </summary>
    /// <param name="localPort">The local port to check.</param>
    /// <returns>The port forwarding status information.</returns>
    [HttpGet("status/{localPort:int}")]
    public IActionResult GetForwardingStatus(int localPort)
    {
        try
        {
            var status = _portForwarder.GetForwardingStatus(localPort);
            if (status == null)
            {
                return NotFound(new { Error = "No forwarding configured for this port" });
            }

            return Ok(status);
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Failed to get forwarding status" });
        }
    }

    /// <summary>
    /// Lists all available local ports that can be used for forwarding (excluding already used ports).
    /// </summary>
    /// <param name="startPort">The starting port number (default: 1024).</param>
    /// <param name="endPort">The ending port number (default: 65535).</param>
    /// <param name="limit">Optional maximum number of available ports to return.</param>
    /// <returns>A list of available port numbers.</returns>
    [HttpGet("available-ports")]
    public IActionResult GetAvailablePorts(
    [FromQuery] int startPort = 1024,
    [FromQuery] int endPort = 65535,
    [FromQuery] int? limit = null)
    {
        try
        {
            // Validate port range
            if (startPort < 1 || startPort > 65535 || endPort < 1 || endPort > 65535 || startPort > endPort)
            {
                return BadRequest(new { Error = "Invalid port range" });
            }

            if (limit is <= 0 or > 1000)
            {
                return BadRequest(new { Error = "Limit must be between 1 and 1000" });
            }

            var usedPorts = _portForwarder.GetForwardingStatus()
                .Select(s => s.LocalPort)
                .Where(port => port >= startPort && port <= endPort)
                .ToHashSet();

            var availablePortCount = endPort - startPort + 1 - usedPorts.Count;
            var returnedPortCount = Math.Min(limit ?? availablePortCount, availablePortCount);
            var availablePorts = new List<int>(returnedPortCount);
            for (int port = startPort; port <= endPort && availablePorts.Count < returnedPortCount; port++)
            {
                if (!usedPorts.Contains(port))
                {
                    availablePorts.Add(port);
                }
            }

            return Ok(new
            {
                AvailablePortCount = availablePortCount,
                AvailablePorts = availablePorts,
                UsedPortCount = usedPorts.Count,
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Failed to get available ports" });
        }
    }

    /// <summary>
    /// Gets detailed stream mapping statistics for port forwarding.
    /// </summary>
    /// <returns>Detailed statistics about stream mappings and performance.</returns>
    [HttpGet("stream-stats")]
    public IActionResult GetStreamStatistics()
    {
        try
        {
            var status = _portForwarder.GetForwardingStatus().ToList();

            var stats = new
            {
                TotalForwardingRules = status.Count(),
                ActiveRules = status.Count(s => s.IsActive),
                TotalConnections = status.Sum(s => s.ActiveConnections),
                TotalBytesForwarded = status.Sum(s => s.BytesForwarded),
                Rules = status.Select(s => new
                {
                    s.LocalPort,
                    s.PodId,
                    s.DestinationHost,
                    s.DestinationPort,
                    s.ServiceName,
                    s.IsActive,
                    s.ActiveConnections,
                    s.BytesForwarded,
                    s.StreamMappingEnabled,
                    s.StreamStats,
                    s.Performance,
                }).ToList()
            };

            return Ok(stats);
        }
        catch (Exception)
        {
            return StatusCode(500, new { Error = "Failed to get stream statistics" });
        }
    }
}

/// <summary>
/// Request model for starting port forwarding.
/// </summary>
public class StartPortForwardingRequest
{
    /// <summary>
    /// The local port to listen on (must be between 1024 and 65535).
    /// </summary>
    [Required]
    [Range(1024, 65535, ErrorMessage = "Local port must be between 1024 and 65535")]
    public int LocalPort { get; init; }

    /// <summary>
    /// The pod ID to use for the VPN tunnel.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "PodId must be between 1 and 100 characters")]
    public string PodId { get; init; } = string.Empty;

    /// <summary>
    /// The remote destination hostname or IP address.
    /// </summary>
    [Required]
    [StringLength(253, MinimumLength = 1, ErrorMessage = "Destination host must be between 1 and 253 characters")]
    public string DestinationHost { get; init; } = string.Empty;

    /// <summary>
    /// The remote destination port.
    /// </summary>
    [Required]
    [Range(1, 65535, ErrorMessage = "Destination port must be between 1 and 65535")]
    public int DestinationPort { get; init; }

    /// <summary>
    /// Optional service name for registered services in the pod.
    /// </summary>
    [StringLength(100, ErrorMessage = "Service name must be at most 100 characters")]
    public string? ServiceName { get; init; }
}
