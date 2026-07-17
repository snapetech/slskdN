// <copyright file="Compute.cs" company="slskd Team">
//     Copyright (c) slskd Team. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU Affero General Public License as published
//     by the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Affero General Public License for more details.
//
//     You should have received a copy of the GNU Affero General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

// <copyright file="Compute.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd
{
    using System;
    using System.Buffers;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    ///     Computational functions.
    /// </summary>
    public static class Compute
    {
        public static (int Delay, int Jitter) ExponentialBackoffDelay(int iteration, int maxDelayInMilliseconds = int.MaxValue)
            => ExponentialBackoffDelay(iteration, 1000, maxDelayInMilliseconds);

        public static (int Delay, int Jitter) ExponentialBackoffDelay(int iteration, int baseDelayInMilliseconds, int maxDelayInMilliseconds)
        {
            iteration = Math.Min(100, iteration);
            baseDelayInMilliseconds = Math.Max(0, baseDelayInMilliseconds);
            maxDelayInMilliseconds = Math.Max(0, maxDelayInMilliseconds);

            var computedDelay = Math.Floor((Math.Pow(2, iteration) - 1) / 2) * baseDelayInMilliseconds;
            var clampedDelay = (int)Math.Min(computedDelay, maxDelayInMilliseconds);

            var jitter = clampedDelay == 0 ? 0 : Random.Shared.Next(1000);

            return (clampedDelay, jitter);
        }

        public static string Sha1Hash(string str)
            => HashUtf8(str, useSha256: false);

        public static string Sha256Hash(string str)
            => HashUtf8(str, useSha256: true);

        private static string HashUtf8(string value, bool useSha256)
        {
            var byteCount = Encoding.UTF8.GetByteCount(value);
            byte[]? rentedBytes = null;
            Span<byte> bytes = byteCount <= 512
                ? stackalloc byte[byteCount]
                : (rentedBytes = ArrayPool<byte>.Shared.Rent(byteCount));

            try
            {
                _ = Encoding.UTF8.GetBytes(value, bytes);
                Span<byte> hash = stackalloc byte[32];
                if (useSha256)
                {
                    SHA256.HashData(bytes[..byteCount], hash);
                    return Convert.ToHexString(hash);
                }

                SHA1.HashData(bytes[..byteCount], hash[..20]);
                return Convert.ToHexString(hash[..20]);
            }
            finally
            {
                if (rentedBytes != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBytes, clearArray: true);
                }
            }
        }
    }
}
