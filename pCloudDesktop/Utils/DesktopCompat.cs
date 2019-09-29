using System.Collections.Generic;

namespace PCloud
{
	/// <summary>Compatibility stuff to make it build for desktop version of .NET</summary>
	static class DesktopCompat
	{
		public static bool TryDequeue<T>( this Queue<T> queue, out T result )
		{
			if( queue.Count > 0 )
			{
				result = queue.Dequeue();
				return true;
			}
			result = default( T );
			return false;
		}
	}
}