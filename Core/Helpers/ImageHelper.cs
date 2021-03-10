using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace Dofus.Retro.Supertools.Core.Helpers
{
    public static class ImageHelper
    {
        [DllImport(".\\Resources\\_ImageSearch_DLL\\_ImageSearch_x64.dll")]
        private static extern IntPtr ImageSearch(int x, int y, int right, int bottom, [MarshalAs(UnmanagedType.LPStr)] string imagePath);

        private const string ERROR_MESSAGE = "Could not recognize image format.";

        private static readonly Dictionary<byte[], Func<BinaryReader, Size>> ImageFormatDecoders = new Dictionary<byte[], Func<BinaryReader, Size>>
        {
            { new byte[]{ 0x42, 0x4D }, DecodeBitmap},
            { new byte[]{ 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, DecodeGif },
            { new byte[]{ 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, DecodeGif },
            { new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, DecodePng },
            { new byte[]{ 0xff, 0xd8 }, DecodeJfif }
        };

        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognized format.</exception>
        public static Size GetDimensions(string path)
        {
            using (var binaryReader = new BinaryReader(File.OpenRead(path.Replace("*", null))))
            {
                try
                {
                    return GetDimensions(binaryReader);
                }
                catch (ArgumentException e)
                {
                    if (e.Message.StartsWith(ERROR_MESSAGE))
                    {
                        throw new ArgumentException(ERROR_MESSAGE, nameof(path), e);
                    }

                    throw;
                }
            }
        }


        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognized format.</exception>    
        public static Size GetDimensions(BinaryReader binaryReader)
        {
            var maxMagicBytesLength = ImageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;

            var magicBytes = new byte[maxMagicBytesLength];

            for (var i = 0; i < maxMagicBytesLength; i += 1)
            {
                magicBytes[i] = binaryReader.ReadByte();

                foreach (var kvPair in ImageFormatDecoders.Where(kvPair => magicBytes.StartsWith(kvPair.Key)))
                {
                    return kvPair.Value(binaryReader);
                }
            }

            throw new ArgumentException(ERROR_MESSAGE, nameof(binaryReader));
        }

        private static bool StartsWith(this IReadOnlyList<byte> thisBytes, IReadOnlyList<byte> thatBytes)
        {
            for (var i = 0; i < thatBytes.Count; i += 1)
            {
                if (thisBytes[i] != thatBytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static short ReadLittleEndianInt16(this BinaryReader binaryReader)
        {
            var bytes = new byte[sizeof(short)];
            for (var i = 0; i < sizeof(short); i += 1)
            {
                bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt16(bytes, 0);
        }

        private static int ReadLittleEndianInt32(this BinaryReader binaryReader)
        {
            var bytes = new byte[sizeof(int)];
            for (var i = 0; i < sizeof(int); i += 1)
            {
                bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        private static Size DecodeBitmap(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(16);
            var width = binaryReader.ReadInt32();
            var height = binaryReader.ReadInt32();
            return new Size(width, height);
        }

        private static Size DecodeGif(BinaryReader binaryReader)
        {
            int width = binaryReader.ReadInt16();
            int height = binaryReader.ReadInt16();
            return new Size(width, height);
        }

        private static Size DecodePng(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(8);
            var width = binaryReader.ReadLittleEndianInt32();
            var height = binaryReader.ReadLittleEndianInt32();
            return new Size(width, height);
        }

        private static Size DecodeJfif(BinaryReader binaryReader)
        {
            while (binaryReader.ReadByte() == 0xff)
            {
                var marker = binaryReader.ReadByte();
                var chunkLength = binaryReader.ReadLittleEndianInt16();

                if (marker == 0xc0)
                {
                    binaryReader.ReadByte();

                    int height = binaryReader.ReadLittleEndianInt16();
                    int width = binaryReader.ReadLittleEndianInt16();
                    return new Size(width, height);
                }

                binaryReader.ReadBytes(chunkLength - 2);
            }

            throw new ArgumentException(ERROR_MESSAGE);
        }

        public static LoginHelper.FindResult SearchImage(string imgPath, int tolerance)
        {
            var dimensions = GetDimensions(imgPath);
            imgPath = "*" + tolerance + " " + imgPath;

            var result = ImageSearch(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, imgPath);
            var res = Marshal.PtrToStringAnsi(result);

            if (string.IsNullOrEmpty(res) || res[0] == '0')
                return null;

            var data = res.Split('|');
            int.TryParse(data[1], out var x);
            int.TryParse(data[2], out var y);

            return new LoginHelper.FindResult((int)(x + dimensions.Width / 2), (int)(y + dimensions.Height / 2));
        }
    }
}
