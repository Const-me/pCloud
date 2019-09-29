using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCloud
{
	/// <summary>Miscellaneous RPCs</summary>
	public static class Misc
	{
		/// <summary>Return value of <see cref="Misc.getIp(Connection)" /> RPC, contains IP address and country code.</summary>
		public struct ClientIP
		{
			/// <summary>Remote address of the user that is connecting to the API.</summary>
			public readonly string ip;

			/// <summary>Lowercase two-letter code of the country that is defined according to the remote address.</summary>
			/// <remarks>If the country could not be defined, will be null.</remarks>
			public readonly string country;

			internal ClientIP( IReadOnlyDictionary<string, object> dict )
			{
				ip = dict.lookup( "ip" ) as string;
				country = dict.lookup( "country" ) as string;
			}
		}

		/// <summary>Get the IP address of the remote device from which the user connects to the API.</summary>
		/// <remarks>Works without authentication.</remarks>
		public static async Task<ClientIP> getIp( this Connection conn )
		{
			var req = new RequestBuilder( "getip" );
			var resp = await conn.send( req );
			return new ClientIP( resp.dict );
		}
	}
}