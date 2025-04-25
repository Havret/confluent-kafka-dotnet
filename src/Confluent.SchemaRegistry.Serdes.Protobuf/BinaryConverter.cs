// Copyright 2024 Confluent Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Refer to LICENSE for more information.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Confluent.SchemaRegistry.Serdes.Protobuf;

internal static class BinaryConverter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteInt32(Span<byte> destination, int value)
    {
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination),
            BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);
        return sizeof(int);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteByte(Span<byte> destination, byte value)
    {
        destination[0] = value;
        return sizeof(byte);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteBytes(Span<byte> destination, byte[] value)
    {
        value.CopyTo(destination);
        return value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadByte(ReadOnlySpan<byte> source, out byte value)
    {
        value = source[0];
        return sizeof(byte);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32(ReadOnlySpan<byte> source, out int value)
    {
        value = BinaryPrimitives.ReadInt32BigEndian(source);
        return sizeof(int);
    }
    
    /// <remarks>
    /// Inspired by: https://github.com/apache/kafka/blob/2.5/clients/src/main/java/org/apache/kafka/common/utils/ByteUtils.java#L142
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadUnsignedVarint(ReadOnlySpan<byte> source, out uint value)
    {
        value = 0;
        int i = 0;
        int bytesRead = 0;

        for (var index = 0; index < source.Length; index++)
        {
            var b = source[index];
            bytesRead++;
            if ((b & 0x80) == 0)
            {
                value |= (uint)(b << i);
                break;
            }

            value |= (uint)((b & 0x7f) << i);
            i += 7;
            if (i > 28)
            {
                throw new OverflowException("Encoded varint is larger than uint.MaxValue");
            }
        }

        if (bytesRead == 0)
        {
            throw new InvalidOperationException("Unexpected end of span reading varint.");
        }

        return bytesRead;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadVarint(ReadOnlySpan<byte> source, out uint value)
    {
        int bytesRead = ReadUnsignedVarint(source, out var unsignedValue);
        value = (uint)((unsignedValue >> 1) ^ -(unsignedValue & 1));
        return bytesRead;
    }
}