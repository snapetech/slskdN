// <copyright file="Base64Extensions.cs" company="slskd Team">
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
namespace slskd
{
    using System;
    using System.Buffers;
    using System.Text;

    /// <summary>
    ///     Base 64 extensions.
    /// </summary>
    public static class Base64Extensions
    {
        /// <summary>
        ///     Encode this string in Base 64.
        /// </summary>
        /// <param name="str">The string to encode.</param>
        /// <returns>The encoded string.</returns>
        public static string ToBase64(this string str)
        {
            var byteCount = Encoding.UTF8.GetByteCount(str);
            byte[]? rentedBytes = null;
            Span<byte> bytes = byteCount <= 512
                ? stackalloc byte[byteCount]
                : (rentedBytes = ArrayPool<byte>.Shared.Rent(byteCount));

            try
            {
                _ = Encoding.UTF8.GetBytes(str, bytes);
                return Convert.ToBase64String(bytes[..byteCount]);
            }
            finally
            {
                if (rentedBytes != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBytes, clearArray: true);
                }
            }
        }

        /// <summary>
        ///     Decode this string from Base 64.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <returns>The decoded string.</returns>
        public static string FromBase64(this string str)
        {
            ArgumentNullException.ThrowIfNull(str);

            var maxByteCount = (str.Length / 4 * 3) + 3;
            byte[]? rentedBytes = null;
            Span<byte> bytes = maxByteCount <= 512
                ? stackalloc byte[maxByteCount]
                : (rentedBytes = ArrayPool<byte>.Shared.Rent(maxByteCount));

            try
            {
                if (!Convert.TryFromBase64String(str, bytes, out var bytesWritten))
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(str));
                }

                return Encoding.UTF8.GetString(bytes[..bytesWritten]);
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
