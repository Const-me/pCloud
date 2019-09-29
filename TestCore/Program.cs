using PCloud;
using PCloud.Metadata;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestCore
{
	static partial class Program
	{
		// TODO: specify e-mail & password to test login. Specify a file path to test downloads.
		const string accountMail = "";
		const string accountPassword = "";
		const string localFile = @"C:\Temp\1.zip";
		const string remoteFile = @"";

		const bool ssl = true;

		static async Task<bool> logout( Connection conn )
		{
			if( conn.isDesynced )
				return false;
			await conn.logout();
			Console.WriteLine( "Logout" );
			return true;
		}

		static async Task testListFolder()
		{
			using( var conn = await Connection.open( ssl ) )
			{
				await conn.login( accountMail, accountPassword );
				Console.WriteLine( "Login OK" );

				try
				{
					var fi = await conn.listFolder();
					Console.WriteLine( "List folder: {0}", fi );

					// Test multiplexed access: list subfolders in parallel
					var subfolders = fi.children.OfType<FolderInfo>().ToArray();

					// Start them from the current thread
					// var tasks = subfolders.Select( f => conn.getFiles( f ) ).ToArray();

					// Start them from different threads of the thread pool
					var tasks = subfolders.Select( f => Task.Run( () => conn.getFiles( f ) ) ).ToArray();

					await Task.WhenAll( tasks );

					// I know they're parallel because I've disabled SSL and looked at the traffic in Wireshark.
					Console.WriteLine( "Completed {0} parallel tasks", tasks.Length );
				}
				finally
				{
					await logout( conn );
				}
			}
		}

		static async Task testDownloads()
		{
			using( var conn = await Connection.open( ssl ) )
			{
				await conn.login( accountMail, accountPassword );
				Console.WriteLine( "Login OK" );

				try
				{
					using( var tmp = File.Create( localFile ) )
					 	await conn.downloadFile( remoteFile, tmp );
					Console.WriteLine( "Downloaded" );
				}
				finally
				{
					await logout( conn );
				}
			}
		}

		static async Task Main( string[] args )
		{
			try
			{
				// await testPublicUpload();
				await testDownloads();
			}
			catch( Exception ex )
			{
				Console.WriteLine( "Failed: {0}\n{1}", ex.Message, ex.ToString() );
			}
		}
	}
}