﻿using System.Collections.Generic;

namespace PCloud
{
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