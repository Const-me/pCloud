using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PCloud
{
	/// <summary>Parse pCloud's proprietary binary JSON into untyped .NET objects.</summary>
	/// <remarks>All integers are unsigned, i.e. you'll get byte, ushort, uint, or ulong for integer values.</remarks>
	class ResponseParser
	{
		static readonly Encoding encStrings = Encoding.UTF8;

		readonly List<string> strings = new List<string>();
		readonly byte[] buffer;
		int offset = 0;
		/// <summary>`Data` objects in the response, if any, are accumulated in this list.</summary>
		public List<Response.Data> payloads { get; private set; } = null;

		/// <summary>Construct from a downloaded buffer.</summary>
		public ResponseParser( byte[] buffer )
		{
			this.buffer = buffer;
		}

		/// <summary>Parse into the sequence of .NET objects.</summary>
		public IEnumerable<object> parse()
		{
			// Too bad there're no algebraic types in current C#, boxing all these numbers is relatively inefficient.
			while( true )
			{
				if( offset >= buffer.Length )
					yield break;
				yield return parseValue();
			}
		}

		static Exception unknownType( byte tp )
		{
			return new ArgumentException( $"Unknown value type byte 0x{ tp.ToString( "x2" ) }" );
		}

		static ulong makeUlong( uint low, uint hi )
		{
			ulong res = hi;
			res = res << 32;
			return res | low;
		}

		object parseValue()
		{
			byte tp = buffer[ offset ];
			offset++;

			switch( tp )
			{
				// New string types
				case 0:
					return readString( readInt1() );
				case 1:
					return readString( readInt2() );
				case 2:
					return readString( (int)readInt3() );
				case 3:
					return readString( (int)readInt4() );
				// Reused string types
				case 4:
					return strings[ readInt1() ];
				case 5:
					return strings[ readInt2() ];
				case 6:
					return strings[ (int)readInt3() ];
				case 7:
					return strings[ (int)readInt4() ];
				// Numbers types
				case 8:
					return readInt1();
				case 9:
					return readInt2();
				case 10:
					return readInt3();
				case 11:
					return readInt4();
				case 12:
					return makeUlong( readInt4(), readInt1() );
				case 13:
					return makeUlong( readInt4(), readInt2() );
				case 14:
					return makeUlong( readInt4(), readInt3() );
				case 15:
					return readInt8();
				// Miscellaneous
				case 16:
					return parseHash();
				case 17:
					return parseArray().ToArray();
				case 18:
					return false;
				case 19:
					return true;
				case 20:
					return readData();
			}

			if( tp < 100 )
				throw unknownType( tp );

			// [ 100, 149 ] - short string between 0 and 49 bytes in len (type-100)
			if( tp < 150 )
				return readString( tp - 100 );
			// [ 150, 199 ] - for string ids between 0 and 49 id is directly encoded in type
			if( tp < 200 )
				return strings[ tp - 150 ];

			// [ 200, 219 ] - numbers between 0 and 19 are directly encoded in the type parameter
			if( tp < 220 )
				return (byte)( tp - 200 );

			throw unknownType( tp );
		}

		byte readInt1()
		{
			byte res = buffer[ offset ];
			offset++;
			return res;
		}
		ushort readInt2()
		{
			ushort res = BitConverter.ToUInt16( buffer, offset );
			offset += 2;
			return res;
		}
		uint readInt3()
		{
			uint res = BitConverter.ToUInt16( buffer, offset );
			res |= (uint)( buffer[ offset + 2 ] ) << 16;
			offset += 3;
			return res;
		}
		uint readInt4()
		{
			uint res = BitConverter.ToUInt32( buffer, offset );
			offset += 4;
			return res;
		}
		ulong readInt8()
		{
			ulong res = BitConverter.ToUInt64( buffer, offset );
			offset += 8;
			return res;
		}

		string readString( int length )
		{
			string result = encStrings.GetString( buffer, offset, length );
			offset += length;
			strings.Add( result );
			return result;
		}

		Dictionary<string, object> parseHash()
		{
			Dictionary<string, object> result = new Dictionary<string, object>();
			while( true )
			{
				if( buffer[ offset ] == 0xFF )
				{
					offset++;
					return result;
				}
				object key = parseValue();
				string strKey = key as string;
				if( strKey == null )
					throw new ArgumentException( $"Hash keys must be strings, got { key.GetType().Name } instead." );
				result.Add( strKey, parseValue() );
			}
		}

		IEnumerable<object> parseArray()
		{
			while( true )
			{
				if( buffer[ offset ] == 0xFF )
				{
					offset++;
					yield break;
				}
				yield return parseValue();
			}
		}

		Response.Data readData()
		{
			Response.Data rd = new Response.Data( (long)readInt8() );
			if( null == payloads )
				payloads = new List<Response.Data>( 1 );
			payloads.Add( rd );
			return rd;
		}
	}
}