using System;
using System.IO;
using System.Collections;
using System.Text;

namespace DHTSpider.Test.BEncoding
{
    /// <summary>
    /// Class representing a BEncoded string
    /// </summary>
    public class BEncodedString : BEncodedValue, IComparable<BEncodedString>
    {
        #region Member Variables

        /// <summary>
        /// The value of the BEncodedString
        /// </summary>
        public string Text
        {
            get { return Encoding.UTF8.GetString(textBytes); }
            set { textBytes = Encoding.UTF8.GetBytes(value); }
        }

        /// <summary>
        /// The underlying byte[] associated with this BEncodedString
        /// </summary>
        public byte[] TextBytes
        {
            get { return this.textBytes; }
        }
        private byte[] textBytes;
        #endregion


        #region Constructors
        /// <summary>
        /// Create a new BEncodedString using UTF8 encoding
        /// </summary>
        public BEncodedString()
            : this(new byte[0])
        {
        }

        /// <summary>
        /// Create a new BEncodedString using UTF8 encoding
        /// </summary>
        /// <param name="value"></param>
        public BEncodedString(char[] value)
            : this(System.Text.Encoding.UTF8.GetBytes(value))
        {
        }

        /// <summary>
        /// Create a new BEncodedString using UTF8 encoding
        /// </summary>
        /// <param name="value">Initial value for the string</param>
        public BEncodedString(string value)
            : this(System.Text.Encoding.UTF8.GetBytes(value))
        {
        }


        /// <summary>
        /// Create a new BEncodedString using UTF8 encoding
        /// </summary>
        /// <param name="value"></param>
        public BEncodedString(byte[] value)
        {
            this.textBytes = value;
        }


        public static implicit operator BEncodedString(string value)
        {
            return new BEncodedString(value);
        }
        public static implicit operator BEncodedString(char[] value)
        {
            return new BEncodedString(value);
        }
        public static implicit operator BEncodedString(byte[] value)
        {
            return new BEncodedString(value);
        }
        #endregion


        #region Encode/Decode Methods


        /// <summary>
        /// Encodes the BEncodedString to a byte[] using the supplied Encoding
        /// </summary>
        /// <param name="buffer">The buffer to encode the string to</param>
        /// <param name="offset">The offset at which to save the data to</param>
        /// <param name="e">The encoding to use</param>
        /// <returns>The number of bytes encoded</returns>
        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;
            written += WriteAscii(buffer, written, textBytes.Length.ToString());
            written += WriteAscii(buffer, written, ":");
            written += Write(buffer, written, textBytes);
            return written - offset;
        }

        #region Write
        static public int Write(byte[] buffer, int offset, byte value)
        {
            buffer[offset] = value;
            return 1;
        }

        static public int Write(byte[] dest, int destOffset, byte[] src, int srcOffset, int count)
        {
            Buffer.BlockCopy(src, srcOffset, dest, destOffset, count);
            return count;
        }

        static public int Write(byte[] buffer, int offset, ushort value)
        {
            return Write(buffer, offset, (short)value);
        }

        static public int Write(byte[] buffer, int offset, short value)
        {
            offset += Write(buffer, offset, (byte)(value >> 8));
            offset += Write(buffer, offset, (byte)value);
            return 2;
        }

        static public int Write(byte[] buffer, int offset, int value)
        {
            offset += Write(buffer, offset, (byte)(value >> 24));
            offset += Write(buffer, offset, (byte)(value >> 16));
            offset += Write(buffer, offset, (byte)(value >> 8));
            offset += Write(buffer, offset, (byte)(value));
            return 4;
        }

        static public int Write(byte[] buffer, int offset, uint value)
        {
            return Write(buffer, offset, (int)value);
        }

        static public int Write(byte[] buffer, int offset, long value)
        {
            offset += Write(buffer, offset, (int)(value >> 32));
            offset += Write(buffer, offset, (int)value);
            return 8;
        }

        static public int Write(byte[] buffer, int offset, ulong value)
        {
            return Write(buffer, offset, (long)value);
        }

        static public int Write(byte[] buffer, int offset, byte[] value)
        {
            return Write(buffer, offset, value, 0, value.Length);
        }

        static public int WriteAscii(byte[] buffer, int offset, string text)
        {
            for (int i = 0; i < text.Length; i++)
                Write(buffer, offset + i, (byte)text[i]);
            return text.Length;
        }
        #endregion

        /// <summary>
        /// Decodes a BEncodedString from the supplied StreamReader
        /// </summary>
        /// <param name="reader">The StreamReader containing the BEncodedString</param>
        internal override void DecodeInternal(RawReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            int letterCount;
            string length = string.Empty;

            while ((reader.PeekByte() != -1) && (reader.PeekByte() != ':'))         // read in how many characters
                length += (char)reader.ReadByte();                                 // the string is

            if (reader.ReadByte() != ':')                                           // remove the ':'
                throw new BEncodingException(string.Format("Invalid data found at {0}. Aborting", reader.Position));

            if (!int.TryParse(length, out letterCount))
                throw new BEncodingException(string.Format("Invalid BEncodedString. Length was '{0}' instead of a number", length));

            this.textBytes = new byte[letterCount];
            if (reader.Read(textBytes, 0, letterCount) != letterCount)
                throw new BEncodingException("Couldn't decode string");
        }
        #endregion


        #region Helper Methods
        public string Hex
        {
            get { return BitConverter.ToString(TextBytes); }
        }

        public override int LengthInBytes()
        {
            // The length is equal to the length-prefix + ':' + length of data
            int prefix = 1; // Account for ':'

            // Count the number of characters needed for the length prefix
            for (int i = textBytes.Length; i != 0; i = i / 10)
                prefix += 1;

            if (textBytes.Length == 0)
                prefix++;

            return prefix + textBytes.Length;
        }

        public int CompareTo(object other)
        {
            return CompareTo(other as BEncodedString);
        }


        public int CompareTo(BEncodedString other)
        {
            if (other == null)
                return 1;

            int difference = 0;
            int length = this.textBytes.Length > other.textBytes.Length ? other.textBytes.Length : this.textBytes.Length;

            for (int i = 0; i < length; i++)
                if ((difference = this.textBytes[i].CompareTo(other.textBytes[i])) != 0)
                    return difference;

            if (this.textBytes.Length == other.textBytes.Length)
                return 0;

            return this.textBytes.Length > other.textBytes.Length ? 1 : -1;
        }

        #endregion


        #region Overridden Methods

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            BEncodedString other;
            if (obj is string)
                other = new BEncodedString((string)obj);
            else if (obj is BEncodedString)
                other = (BEncodedString)obj;
            else
                return false;

            return ByteMatch(this.textBytes, other.textBytes);
        }

        public static bool ByteMatch(byte[] array1, byte[] array2)
        {
            if (array1 == null)
                throw new ArgumentNullException("array1");
            if (array2 == null)
                throw new ArgumentNullException("array2");

            if (array1.Length != array2.Length)
                return false;

            return ByteMatch(array1, 0, array2, 0, array1.Length);
        }
        public static bool ByteMatch(byte[] array1, int offset1, byte[] array2, int offset2, int count)
        {
            if (array1 == null)
                throw new ArgumentNullException("array1");
            if (array2 == null)
                throw new ArgumentNullException("array2");

            // If either of the arrays is too small, they're not equal
            if ((array1.Length - offset1) < count || (array2.Length - offset2) < count)
                return false;

            // Check if any elements are unequal
            for (int i = 0; i < count; i++)
                if (array1[offset1 + i] != array2[offset2 + i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < this.textBytes.Length; i++)
                hash += this.textBytes[i];

            return hash;
        }

        public override string ToString()
        {
            return System.Text.Encoding.UTF8.GetString(textBytes);
        }

        #endregion
    }
}
