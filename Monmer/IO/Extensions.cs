using System.Reflection;
using System.Runtime.InteropServices;

namespace Monmer.IO
{
    public static class Extensions
    {
        /// <summary>
        /// Reads a byte array from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        /// <param name="max">The maximum size of the byte array.</param>
        /// <returns>The byte array read from the <see cref="BinaryReader"/>.</returns>
        public static byte[] ReadVariableBytes(this BinaryReader reader, int max = 0x1000000)
        {
            return reader.ReadFixedBytes((int)reader.ReadVariableInt((ulong)max));
        }

        /// <summary>
        /// Reads a byte array of the specified size from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        /// <param name="size">The size of the byte array.</param>
        /// <returns>The byte array read from the <see cref="BinaryReader"/>.</returns>
        public static byte[] ReadFixedBytes(this BinaryReader reader, int size)
        {
            var index = 0;
            var data = new byte[size];

            while (size > 0)
            {
                var bytesRead = reader.Read(data, index, size);

                if (bytesRead <= 0)
                {
                    throw new FormatException();
                }

                size -= bytesRead;
                index += bytesRead;
            }

            return data;
        }

        /// <summary>
        /// Reads an integer from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        /// <param name="max">The maximum value of the integer.</param>
        /// <returns>The integer read from the <see cref="BinaryReader"/>.</returns>
        public static ulong ReadVariableInt(this BinaryReader reader, ulong max = ulong.MaxValue)
        {
            byte fb = reader.ReadByte();
            ulong value;

            if (fb == 0xFD)
            {
                value = reader.ReadUInt16();
            }
            else if (fb == 0xFE)
            {
                value = reader.ReadUInt32();
            }
            else if (fb == 0xFF)
            {
                value = reader.ReadUInt64();
            }
            else
            {
                value = fb;
            }

            return value > max ? throw new FormatException() : value;
        }


        /// <summary>
        /// Writes a byte array into a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        /// <param name="value">The byte array to be written.</param>
        public static void WriteVariableBytes(this BinaryWriter writer, ReadOnlySpan<byte> value)
        {
            writer.WriteVariableInt(value.Length);
            writer.Write(value);
        }

        /// <summary>
        /// Writes an integer into a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        /// <param name="value">The integer to be written.</param>
        public static void WriteVariableInt(this BinaryWriter writer, long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (value < 0xFD)
            {
                writer.Write((byte)value);
            }
            else if (value <= 0xFFFF)
            {
                writer.Write((byte)0xFD);
                writer.Write((ushort)value);
            }
            else if (value <= 0xFFFFFFFF)
            {
                writer.Write((byte)0xFE);
                writer.Write((uint)value);
            }
            else
            {
                writer.Write((byte)0xFF);
                writer.Write(value);
            }
        }


        /// <summary>
        /// Gets the size of variable-length of the data.
        /// </summary>
        /// <param name="value">The length of the data.</param>
        /// <returns>The size of variable-length of the data.</returns>
        public static int GetVariableSize(int value)
        {
            if (value < 0xFD)
            {
                return sizeof(byte);
            }
            else if (value <= 0xFFFF)
            {
                return sizeof(byte) + sizeof(ushort);
            }
            else
            {
                return sizeof(byte) + sizeof(uint);
            }
        }

        /// <summary>
        /// Gets the size of the specified array encoded in variable-length encoding.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="value">The specified array.</param>
        /// <returns>The size of the array.</returns>
        public static int GetVariableSize<T>(this IReadOnlyCollection<T> value)
        {
            int value_size;
            Type t = typeof(T);
            if (typeof(ISerializable).IsAssignableFrom(t))
            {
                value_size = value.OfType<ISerializable>().Sum(p => p.Size);
            }
            else if (t.GetTypeInfo().IsEnum)
            {
                int element_size;
                Type u = t.GetTypeInfo().GetEnumUnderlyingType();
                if (u == typeof(sbyte) || u == typeof(byte))
                    element_size = 1;
                else if (u == typeof(short) || u == typeof(ushort))
                    element_size = 2;
                else if (u == typeof(int) || u == typeof(uint))
                    element_size = 4;
                else //if (u == typeof(long) || u == typeof(ulong))
                    element_size = 8;
                value_size = value.Count * element_size;
            }
            else
            {
                value_size = value.Count * Marshal.SizeOf<T>();
            }
            return GetVariableSize(value.Count) + value_size;
        }


        /// <summary>
        /// Converts a byte array to an <see cref="ISerializable"/> object.
        /// </summary>
        /// <param name="value">The byte array to be converted.</param>
        /// <param name="type">The type to convert to.</param>
        /// <returns>The converted <see cref="ISerializable"/> object.</returns>
        public static ISerializable AsSerializable(this byte[] value, Type type)
        {
            if (!typeof(ISerializable).GetTypeInfo().IsAssignableFrom(type))
            {
                throw new InvalidCastException();
            }

            ISerializable serializable = (ISerializable)Activator.CreateInstance(type);
            using MemoryStream ms = new(value, false);
            using BinaryReader reader = new(ms, System.Text.Encoding.UTF8, true);
            serializable.Deserialize(reader);
            return serializable;
        }

        /// <summary>
        /// Converts a byte array to an <see cref="ISerializable"/> object.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="value">The byte array to be converted.</param>
        /// <param name="start">The offset into the byte array from which to begin using data.</param>
        /// <returns>The converted <see cref="ISerializable"/> object.</returns>
        public static T AsSerializable<T>(this byte[] value, int start = 0) where T : ISerializable, new()
        {
            using MemoryStream ms = new(value, start, value.Length - start, false);
            using BinaryReader reader = new(ms, System.Text.Encoding.UTF8, true);
            return reader.ReadSerializable<T>();
        }

        /// <summary>
        /// Reads an <see cref="ISerializable"/> object from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ISerializable"/> object.</typeparam>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        /// <returns>The object read from the <see cref="BinaryReader"/>.</returns>
        public static T ReadSerializable<T>(this BinaryReader reader) where T : ISerializable, new()
        {
            T obj = new();
            obj.Deserialize(reader);
            return obj;
        }

        /// <summary>
        /// Converts an <see cref="ISerializable"/> object to a byte array.
        /// </summary>
        /// <param name="value">The <see cref="ISerializable"/> object to be converted.</param>
        /// <returns>The converted byte array.</returns>
        public static byte[] ToArray(this ISerializable value)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms, System.Text.Encoding.UTF8, true);
            value.Serialize(writer);
            writer.Flush();
            return ms.ToArray();
        }
    }
}