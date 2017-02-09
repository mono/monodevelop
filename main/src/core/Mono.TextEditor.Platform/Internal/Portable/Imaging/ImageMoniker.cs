using System;
using System.Diagnostics;
//using Microsoft.Internal.VisualStudio.Shell;
//using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Imaging
{
    /// <summary>
    /// This type matches the definition of its corresponding interop type of 
    /// the same name, and was created to facilitate serialization and high 
    /// performance comparison.
    /// </summary>
    internal struct ImageMoniker : IComparable<ImageMoniker>, IEquatable<ImageMoniker>, IFormattable
    {
        public Guid Guid;
        public int Id;

        public const char ImageMonikerSeparator          = ':';
        public const char AlternateImageMonikerSeparator = ';';
        public const char FilenameImageMonikerSeparator  = '.';

        private const string ImageMonikerSeparatorAsString          = ":";
        private const string AlternateImageMonikerSeparatorAsString = ";";
        private const string FilenameImageMonikerSeparatorAsString  = ".";


        public ImageMoniker(Guid guid, int id)
        {
            Guid = guid;
            Id   = id;
        }

#if false
#region Serialization

		/// <summary>
		/// The version of the serialization stream.  This should be incremented every time the
		/// serialization format changes.
		/// </summary>
		const int SerializationVersion = 1;

        /// <summary>
        /// Serializes the object to <paramref name="stream"/>
        /// </summary>
        /// <param name="writer">The writer to serialize to</param>
        internal void Serialize(VersionedBinaryWriter writer)
        {
            Validate.IsNotNull(writer, "writer");

            Guid guid = Guid;
            int  id   = Id;

            writer.WriteVersioned(SerializationVersion, (_1, _2) =>
            {
                writer.Write(guid);
                writer.Write(id);
            });
        }

        /// <summary>
        /// Deserialization constructor
        /// </summary>
        /// <param name="reader">The reader to deserialize from</param>
        ImageMoniker(VersionedBinaryReader reader)
        {
            Validate.IsNotNull(reader, "reader");

            Guid guid = Guid.Empty;
            int  id   = 0;

            reader.ReadVersioned(SerializationVersion, _ =>
            {
                guid = reader.ReadGuid();
                id   = reader.ReadInt32();
            }, throwOnUnexpectedVersion: true);

            Guid = guid;
            Id   = id;
        }

        /// <summary>
        /// Deserializes an instance of this type from <paramref name="stream"/>
        /// </summary>
        /// <param name="reader">The reader to deserialize from</param>
        internal static ImageMoniker Deserialize(VersionedBinaryReader reader)
        {
            return new ImageMoniker(reader);
        }

#endregion Serialization
#endif
        public override string ToString()
        {
            return ToString("g", formatProvider: null);
        }

        public string ToString(string format)
        {
            return ToString(format, formatProvider: null);
        }

        public Interop.ImageMoniker ToInteropType()
        {
            return new Interop.ImageMoniker { Guid = Guid, Id = Id };
        }

#region IFormattable Members

        /// <summary>
        /// Returns a string representation of the value of this ImageMoniker,
        /// according to the provided format specifier.
        /// </summary>
        /// <param name="format">The format specifier.  In can be either "G", "A", or "F"</param>
        /// <returns>The string representation of the object</returns>
        /// <remarks>
        /// The format of the returned string depends on <paramref name="format"/>:
        /// 
        /// G:  General format.    The string looks like "{guid}:{id}"
        /// A:  Alternate format.  The string looks like "{guid};{id}"
        /// F:  Filename format.   The string looks like "guid.id"
        /// </remarks>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (String.IsNullOrEmpty(format))
                format = "g";

            switch (format[0])
            {
                // alternate format
                case 'a':
                case 'A':
                    return Guid.ToString("B") + AlternateImageMonikerSeparatorAsString + Id.ToString();

                // filename format
                case 'f':
                case 'F':
                    return Guid.ToString("D") + FilenameImageMonikerSeparatorAsString + Id.ToString("D6");

                // general format
                case 'g':
                case 'G':
                    return Guid.ToString("B") + ImageMonikerSeparatorAsString + Id.ToString();

                default:
                    throw new FormatException("Invalid format " + format);
            }
        }

#endregion

        public override int GetHashCode()
        {
            return Guid.GetHashCode() ^ (int) Id;
        }

        public static bool TryParse(string s, out ImageMoniker imageMoniker)
        {
            Guid imageGuid;
            int imageId;

            if (TryParse(s, out imageGuid, out imageId))
            {
                imageMoniker = new ImageMoniker(imageGuid, imageId);
                return true;
            }

            imageMoniker = default(ImageMoniker);
            return false;
        }

        public static bool TryParse(string s, out Guid imageGuid, out int imageId)
        {
            imageGuid = Guid.Empty;
            imageId   = 0;

            if (s == null)
                return false;
            
            string[] tokens = s.Split(ImageMonikerSeparator);

            if (tokens.Length != 2)
                tokens = s.Split(AlternateImageMonikerSeparator);

            if (tokens.Length != 2)
                tokens = s.Split(FilenameImageMonikerSeparator);

            // Expecting EXACTLY 2 tokens, one for the Guid and one for the Id
            if (tokens.Length != 2)
                return false;

            Guid localGuid;
            if (!Guid.TryParse(tokens[0], out localGuid))
                return false;

            if (!int.TryParse(tokens[1], out imageId))
                return false;

            imageGuid = localGuid;
            return true;
        }

#region Comparison methods

        public static bool operator==(ImageMoniker moniker1, ImageMoniker moniker2)
        {
            return moniker1.Equals(moniker2);
        }

        public static bool operator!=(ImageMoniker moniker1, ImageMoniker moniker2)
        {
            return !moniker1.Equals(moniker2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ImageMoniker))
                return false;

            return Equals((ImageMoniker) obj);
        }

#region IEquatable<ImageMoniker> Members

        public bool Equals(ImageMoniker other)
        {
            return Id == other.Id && Guid == other.Guid;
        }

#endregion IEquatable<ImageMoniker> Members
#region IComparable<ImageMoniker> Members

        public int CompareTo(ImageMoniker other)
        {
            // order first by Guid (ascending)...
            int result = this.Guid.CompareTo(other.Guid);
            if (result != 0)
                return result;

            // ...then by Id (ascending)
            result = (this.Id - other.Id);

            return result;
        }

#endregion IComparable<ImageMoniker> Members
#endregion Comparison methods
    }
}
