using KitsuneLayoutManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper.CacheHandler
{
	public sealed class GZipCompressor
	{
		public static byte[] CompressAndBase64EncodeIfNeeded(byte[] inputBytes)
		{
			byte[] compressedBytes = null;

			using (var outputStream = new MemoryStream())
			{
				using (var inputStream = new MemoryStream(inputBytes))
				using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
				{
					inputStream.CopyTo(gzipStream);
					gzipStream.Close();
				}

				compressedBytes = outputStream.ToArray();
				if (KLM_Constants.Base64Response)
				{
					var base64 = Convert.ToBase64String(compressedBytes, 0, compressedBytes.Length);
					compressedBytes = Encoding.UTF8.GetBytes(base64);
				}
			}

			return compressedBytes;
		}

		public static byte[] Bas64DecodeIfNeededAndDecompress(byte[] value)
		{
			byte[] deCompressedBytes = null;

			if (value?.Length > 0)
			{
				try
				{
					value = Convert.FromBase64String(Encoding.UTF8.GetString(value));
				}
				catch
				{
					//Suppress exception, ex will occur only if given value is not a valid base64 string
				}

				using (var inputStream = new MemoryStream(value))
				using (var outputStream = new MemoryStream())
				{
					using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
					{
						gzipStream.CopyTo(outputStream);
					}

					deCompressedBytes = outputStream.ToArray();
				}
			}
			return deCompressedBytes;
		}
	}
}
