// <copyright file="ProtocolAdversarialFuzz.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.Tests.Unit.Messaging.Fuzz
{
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    ///     Adversarial fuzz: feed random bytes into selected parsers and assert that only documented
    ///     exception types escape. Anything else (NullReferenceException, OverflowException,
    ///     IndexOutOfRangeException, OutOfMemoryException, AccessViolationException) is a council
    ///     finding: the parser failed to enforce its validation contract on hostile input.
    ///
    ///     This is the protocol-fuzz counterpart of CSL0001: the analyzer catches the structural shape
    ///     of an unprotected allocation, this test catches the runtime behavior on adversarial bytes.
    /// </summary>
    [Trait("Category", "Fuzz")]
    public class ProtocolAdversarialFuzz
    {
        private const int IterationsPerParser = 500;

        // Parsers under test, keyed by a stable label. Each parser is invoked with a random byte
        // buffer of varying length; we only require that the parser either succeeds or throws one
        // of the allowed exception types.
        private static readonly IReadOnlyList<(string Label, Action<byte[]> Parse)> Parsers = new List<(string, Action<byte[]>)>
        {
            ("Distributed.BranchLevel",  bytes => Soulseek.Messaging.Messages.DistributedBranchLevel.FromByteArray(bytes)),
            ("Distributed.ChildDepth",   bytes => Soulseek.Messaging.Messages.DistributedChildDepth.FromByteArray(bytes)),
            ("Distributed.BranchRoot",   bytes => Soulseek.Messaging.Messages.DistributedBranchRoot.FromByteArray(bytes)),
            ("Distributed.PingRequest",  bytes => Soulseek.Messaging.Messages.DistributedPingRequest.FromByteArray(bytes)),
            ("Distributed.PingResponse", bytes => Soulseek.Messaging.Messages.DistributedPingResponse.FromByteArray(bytes)),
            ("Distributed.SearchRequest", bytes => Soulseek.Messaging.Messages.DistributedSearchRequest.FromByteArray(bytes)),
            ("Peer.TransferRequest",     bytes => Soulseek.Messaging.Messages.TransferRequest.FromByteArray(bytes)),
            ("Peer.TransferResponse",    bytes => Soulseek.Messaging.Messages.TransferResponse.FromByteArray(bytes)),
            ("Peer.UploadFailed",        bytes => Soulseek.Messaging.Messages.UploadFailed.FromByteArray(bytes)),
            ("Peer.UploadDenied",        bytes => Soulseek.Messaging.Messages.UploadDenied.FromByteArray(bytes)),
            ("Peer.PeerSearchRequest",   bytes => Soulseek.Messaging.Messages.PeerSearchRequest.FromByteArray(bytes)),
            ("Peer.QueueDownloadRequest", bytes => Soulseek.Messaging.Messages.QueueDownloadRequest.FromByteArray(bytes)),
            ("Server.PrivateMessage",    bytes => Soulseek.Messaging.Messages.PrivateMessageNotification.FromByteArray(bytes)),
            ("Server.GlobalMessage",     bytes => Soulseek.Messaging.Messages.GlobalMessageNotification.FromByteArray(bytes)),
            ("Server.ConnectToPeer",     bytes => Soulseek.Messaging.Messages.ConnectToPeerResponse.FromByteArray(bytes)),
            ("Server.PrivateRoomToggle", bytes => Soulseek.Messaging.Messages.PrivateRoomToggle.FromByteArray(bytes)),
        };

        [Fact]
        public void Random_Bytes_Produce_Only_Documented_Exceptions()
        {
            var rng = new Random(0x5111_5110);
            var unexpectedFailures = new List<string>();

            foreach (var (label, parse) in Parsers)
            {
                for (var i = 0; i < IterationsPerParser; i++)
                {
                    var length = rng.Next(0, 256);
                    var bytes = new byte[length];
                    rng.NextBytes(bytes);

                    try
                    {
                        parse(bytes);
                    }
                    catch (Exception ex) when (IsDocumentedFailure(ex))
                    {
                        // expected
                    }
                    catch (Exception ex)
                    {
                        unexpectedFailures.Add($"{label} threw {ex.GetType().FullName} on input length {length}: {ex.Message}");
                    }
                }
            }

            var message = $"Adversarial fuzz produced {unexpectedFailures.Count} undocumented failures:\n  - "
                + string.Join("\n  - ", unexpectedFailures);
            Assert.True(unexpectedFailures.Count == 0, message);
        }

        [Fact]
        public void Empty_Input_Produces_Only_Documented_Exceptions()
        {
            // Empty input is an explicit boundary case; every parser must handle it without
            // dereferencing into the buffer.
            foreach (var (label, parse) in Parsers)
            {
                try
                {
                    parse(Array.Empty<byte>());
                }
                catch (Exception ex) when (IsDocumentedFailure(ex))
                {
                    // expected
                }
                catch (Exception ex)
                {
                    Assert.Fail($"{label} threw undocumented {ex.GetType().FullName} on empty input: {ex.Message}");
                }
            }
        }

        private static bool IsDocumentedFailure(Exception ex)
        {
            // Walk the type hierarchy by name to avoid pulling Soulseek.* types into this file's
            // direct using-set; the fuzz file should remain dependency-free above Xunit.
            for (var t = ex.GetType(); t != null; t = t.BaseType)
            {
                switch (t.FullName)
                {
                    case "Soulseek.MessageException":
                    case "Soulseek.MessageReadException":
                    case "Soulseek.SoulseekClientException":
                    case "Soulseek.ConnectionException":
                    case "System.ArgumentException":
                    case "System.ArgumentOutOfRangeException":
                    case "System.ArgumentNullException":
                    case "System.InvalidOperationException":
                    case "System.FormatException":
                        return true;
                }
            }

            return false;
        }
    }
}
