using PCloud;
using PCloud.Metadata;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace TestCore;

static partial class Program
{
	// TODO: specify e-mail & password to test login. Specify a file path to test downloads.
	const string accountMail = "";
	const string accountPassword = "";
	const string localFile = @"C:\Temp\1.zip";
	const string remoteFile = @"";

	// TODO: `code=` parameters of the "Request files" link
	const string uploadLinkCode = "";

	const bool ssl = true;

	static async Task<bool> logout( Connection conn )
	{
		if( conn.isDesynced )
			return false;
		await conn.logout();
		Console.WriteLine( "Logout" );
		return true;
	}

	static async Task testPublicUpload()
	{
		using var conn = await Connection.open( ssl, Endpoint.hostEurope );
		Console.WriteLine( "Connected OK" );

		byte[] payload = "Hello, world!"u8.ToArray();
		using var ms = new MemoryStream( payload, false );
		const string from = "Test";
		await conn.uploadToLink( "hello.txt", ms, uploadLinkCode, from );
		Console.WriteLine( "Uploaded" );
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

				// Start list operations from the current thread
				// var tasks = subfolders.Select( f => conn.getFiles( f ) ).ToArray();

				// Start list operations from different threads / each, of the thread pool.
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

	static async Task testFoldersAndFiles()
	{
		using( var conn = await Connection.open( ssl ) )
		{
			await conn.login( accountMail, accountPassword );
			Console.WriteLine( "Login OK" );

			try
			{
				var dir = await conn.createFolder( "NetCoreTest" );
				Console.WriteLine( "Created folder: {0}", dir );

				dir = await conn.renameFolder( dir, "OtherName" );
				Console.WriteLine( "Renamed folder: {0}", dir );

				var fd = await conn.createFile( dir, "test.txt", FileMode.Create, FileAccess.Write );
				Console.WriteLine( "Opened a file: {0}", fd );

				string sourceString = "Hello, world";
				MemoryStream ms = new MemoryStream( Encoding.UTF8.GetBytes( sourceString ), false );
				await conn.writeFile( fd, ms, ms.Length );
				Console.WriteLine( "Wrote the file" );
				await conn.closeFile( fd );
				Console.WriteLine( "Closed the file" );

				fd = await conn.createFile( fd.fileId, FileMode.Open, FileAccess.Read );
				Console.WriteLine( "Opened the file for reading: {0}", fd );
				var fileSize = await conn.getFileSize( fd );
				Console.WriteLine( "Got the file size: {0}", fileSize );
				MemoryStream msRead = new MemoryStream();
				await conn.readFile( fd, msRead, fileSize.length );
				Console.WriteLine( "Read file: \"{0}\"", Encoding.UTF8.GetString( msRead.ToArray() ) );

				var checksum = await conn.checksumFileSlice( fd, 0, fileSize.length );
				Console.WriteLine( "checksumFileSlice: {0}", checksum );

				await conn.closeFile( fd );
				Console.WriteLine( "Closed the file" );

				dir = await conn.listFolder( dir.id );
				Console.WriteLine( "Refreshed the folder: {0}", dir );

				await conn.deleteFile( fd.fileId );
				Console.WriteLine( "Deleted the file" );

				dir = await conn.listFolder( dir.id );
				Console.WriteLine( "Refreshed the folder: {0}", dir );

				await conn.deleteFolder( dir );
				Console.WriteLine( "Deleted the folder" );
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
			await testPublicUpload();
			// await testDownloads();
			// await testListFolder();
			// await testFoldersAndFiles();
		}
		catch( Exception ex )
		{
			Console.WriteLine( "Failed: {0}\n{1}", ex.Message, ex.ToString() );
		}
	}
}