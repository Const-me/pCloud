using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PCloud
{
	static class Utils
	{
		public static void write( this Stream stm, byte[] buffer )
		{
			stm.Write( buffer, 0, buffer.Length );
		}

		public static void write( this Stream stm, byte[] buffer, int length )
		{
			stm.Write( buffer, 0, length );
		}

		/// <summary>Retrieves a buffer that is at least the requested length.</summary>
		public static byte[] bufferRent( int cb )
		{
			return ArrayPool<byte>.Shared.Rent( cb );
		}

		/// <summary>Returns an array to the pool that was previously obtained from <see cref="bufferRent" />.</summary>
		public static void bufferReturn( byte[] buffer )
		{
			ArrayPool<byte>.Shared.Return( buffer );
		}

		/// <summary>Write string to stream with specified encoding, using ArrayPool to save RAM allocations.</summary>
		public static void write( this Stream stm, string str, int bytesCount, Encoding enc )
		{
#if DEBUG
			Debug.Assert( bytesCount == enc.GetByteCount( str ) );
#endif
			byte[] buffer = bufferRent( bytesCount );
			int cb = enc.GetBytes( str, 0, str.Length, buffer, 0 );
			Debug.Assert( cb == bytesCount );
			stm.write( buffer, bytesCount );
			bufferReturn( buffer );
		}

		public static void rewind( this Stream stm )
		{
			stm.Seek( 0, SeekOrigin.Begin );
		}

		public static V lookup<K, V>( this IReadOnlyDictionary<K, V> dict, K key )
		{
			V v;
			dict.TryGetValue( key, out v );
			return v;
		}

		public static async Task<byte[]> read( this Stream stm, int length )
		{
			byte[] buffer = new byte[ length ];
			int offset = 0;
			while( true )
			{
				int cb = await stm.ReadAsync( buffer, offset, length );
				if( 0 == cb )
					throw new EndOfStreamException();
				offset += cb;
				length -= cb;
				if( length <= 0 )
					return buffer;
			}
		}

		/// <summary>Compute SHA1 of UTF8 bytes of the input string, convert result to lowercase hexadecimal string without delimiters between bytes</summary>
		public static string sha1( string input )
		{
			using( SHA1Managed sha1 = new SHA1Managed() )
			{
				// Not using ArrayPool here, login is not performance critical but it is security critical.
				var hash = sha1.ComputeHash( Encoding.UTF8.GetBytes( input ) );
				var sb = new StringBuilder( hash.Length * 2 );
				foreach( byte b in hash )
					sb.Append( b.ToString( "x2" ) );
				return sb.ToString();
			}
		}

		/// <summary>Similar to Stream.CopyToAsync, but copies exactly specified count of bytes. Throws EndOfStreamException when the source doesn't have enough bytes.</summary>
		public static async Task copyData( this Stream from, Stream to, long length, int bufferSize = 256 * 1024 )
		{
			if( length < bufferSize )
				bufferSize = (int)length;
			byte[] buffer = bufferRent( bufferSize );
			try
			{
				while( length > 0 )
				{
					int cbRequest = bufferSize;
					if( cbRequest > length )
						cbRequest = (int)length;

					int cbRead = await from.ReadAsync( buffer, 0, cbRequest );
					if( 0 == cbRead )
						throw new EndOfStreamException();

					await to.WriteAsync( buffer, 0, cbRead );
					length -= cbRead;
				}
			}
			finally
			{
				bufferReturn( buffer );
			}
		}

		/// <summary>Enumerate items of the array. Don't crash if it's null.</summary>
		public static IEnumerable<T> enumerate<T>( this T[] arr )
		{
			return null == arr ? Enumerable.Empty<T>() : arr;
		}

		public static bool notEmpty<T>( this T[] arr )
		{
			return ( arr?.Length ?? 0 ) > 0;
		}
		public static bool isEmpty<T>( this T[] arr )
		{
			return null == arr || arr.Length <= 0;
		}

		/// <summary>Read and discard specified count of bytes from the stream.</summary>
		public static async Task fastForward( this Stream from, long length )
		{
			int bufferSize = 256 * 1024;
			byte[] buffer = bufferRent( bufferSize );
			try
			{
				while( length > 0 )
				{
					int cbRequest = bufferSize;
					if( cbRequest > length )
						cbRequest = (int)length;
					int cbRead = await from.ReadAsync( buffer, 0, cbRequest );
					if( 0 == cbRead )
						throw new EndOfStreamException();
					length -= cbRead;
				}
			}
			finally
			{
				bufferReturn( buffer );
			}
		}
	}
}