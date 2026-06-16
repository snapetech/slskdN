// <copyright file="ProtocolTextEncoding.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
//
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    internal static class ProtocolTextEncoding
    {
        private static readonly char[] Windows1251UpperHalf =
        {
            '\u0402', '\u0403', '\u201A', '\u0453', '\u201E', '\u2026', '\u2020', '\u2021',
            '\u20AC', '\u2030', '\u0409', '\u2039', '\u040A', '\u040C', '\u040B', '\u040F',
            '\u0452', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014',
            '\u0098', '\u2122', '\u0459', '\u203A', '\u045A', '\u045C', '\u045B', '\u045F',
            '\u00A0', '\u040E', '\u045E', '\u0408', '\u00A4', '\u0490', '\u00A6', '\u00A7',
            '\u0401', '\u00A9', '\u0404', '\u00AB', '\u00AC', '\u00AD', '\u00AE', '\u0407',
            '\u00B0', '\u00B1', '\u0406', '\u0456', '\u0491', '\u00B5', '\u00B6', '\u00B7',
            '\u0451', '\u2116', '\u0454', '\u00BB', '\u0458', '\u0405', '\u0455', '\u0457',
        };

        private static readonly IReadOnlyDictionary<char, byte> Windows1251ReverseMap = BuildWindows1251ReverseMap();

        public static string Decode(byte[] bytes, CharacterEncoding encoding)
        {
            encoding ??= CharacterEncoding.UTF8;

            if (encoding == CharacterEncoding.Windows1251)
            {
                return DecodeWindows1251(bytes);
            }

            return Encoding.GetEncoding(encoding, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetString(bytes);
        }

        public static (string Value, CharacterEncoding Encoding) DecodeWithFallback(byte[] bytes, CharacterEncoding requestedEncoding)
        {
            requestedEncoding ??= CharacterEncoding.UTF8;

            try
            {
                return (Decode(bytes, requestedEncoding), requestedEncoding);
            }
            catch (Exception)
            {
                var windows1251 = DecodeWindows1251(bytes);

                if (LooksLikeWindows1251(bytes, windows1251))
                {
                    return (windows1251, CharacterEncoding.Windows1251);
                }

                return (Encoding.GetEncoding(CharacterEncoding.ISO88591).GetString(bytes), CharacterEncoding.ISO88591);
            }
        }

        public static byte[] Encode(string value, CharacterEncoding encoding)
        {
            encoding ??= CharacterEncoding.UTF8;

            if (encoding == CharacterEncoding.Windows1251)
            {
                return EncodeWindows1251(value);
            }

            return Encoding.GetEncoding(encoding, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(value);
        }

        private static IReadOnlyDictionary<char, byte> BuildWindows1251ReverseMap()
        {
            var map = new Dictionary<char, byte>();

            for (var i = 0; i < Windows1251UpperHalf.Length; i++)
            {
                map[Windows1251UpperHalf[i]] = (byte)(0x80 + i);
            }

            for (var i = 0; i <= 0x3F; i++)
            {
                map[(char)('\u0410' + i)] = (byte)(0xC0 + i);
            }

            return map;
        }

        private static string DecodeWindows1251(byte[] bytes)
        {
            var chars = new char[bytes.Length];

            for (var i = 0; i < bytes.Length; i++)
            {
                var value = bytes[i];

                if (value < 0x80)
                {
                    chars[i] = (char)value;
                }
                else if (value < 0xC0)
                {
                    chars[i] = Windows1251UpperHalf[value - 0x80];
                }
                else
                {
                    chars[i] = (char)('\u0410' + value - 0xC0);
                }
            }

            return new string(chars);
        }

        private static byte[] EncodeWindows1251(string value)
        {
            return value.Select(EncodeWindows1251Char).ToArray();
        }

        private static byte EncodeWindows1251Char(char value)
        {
            if (value < 0x80)
            {
                return (byte)value;
            }

            if (Windows1251ReverseMap.TryGetValue(value, out var encoded))
            {
                return encoded;
            }

            throw new EncoderFallbackException($"Unable to encode character U+{(int)value:X4} as windows-1251");
        }

        private static bool LooksLikeWindows1251(byte[] bytes, string decoded)
        {
            var highByteCount = bytes.Count(b => b >= 0x80);

            if (highByteCount < 4)
            {
                return false;
            }

            var cyrillicCount = decoded.Count(IsCyrillic);
            return cyrillicCount >= 4 && cyrillicCount >= highByteCount * 0.6;
        }

        private static bool IsCyrillic(char value)
        {
            return value >= '\u0400' && value <= '\u04FF';
        }
    }
}
