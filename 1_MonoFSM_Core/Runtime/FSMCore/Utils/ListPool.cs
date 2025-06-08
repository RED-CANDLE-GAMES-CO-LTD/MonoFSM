namespace Fusion.Addons.FSM
{
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;

	public partial class ListPool<T>
	{
		// CONSTANTS

		private const int POOL_CAPACITY = 4;
		private const int LIST_CAPACITY = 16;

		// PUBLIC MEMBERS

		public static readonly ListPool<T> Shared = new ListPool<T>();

		// PRIVATE MEMBERS

		private List<List<T>> _pool = new List<List<T>>(POOL_CAPACITY);

		// PUBLIC METHODS

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public List<T> Get(int capacity)
		{
			lock (_pool)
			{
				int poolCount = _pool.Count;

				if (poolCount == 0)
				{
					return new List<T>(capacity > 0 ? capacity : LIST_CAPACITY);
				}

				for (int i = 0; i < poolCount; ++i)
				{
					List<T> list = _pool[i];

					if (list.Capacity < capacity)
						continue;

					_pool.RemoveBySwap(i);
					return list;
				}

				int lastListIndex = poolCount - 1;

				List<T> lastList = _pool[lastListIndex];
				lastList.Capacity = capacity;

				_pool.RemoveAt(lastListIndex);

				return lastList;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Return(List<T> list)
		{
			if (list == null)
				return;

			list.Clear();

			lock (_pool)
			{
				_pool.Add(list);
			}
		}
	}

	public static class ListPool
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static List<T> Get<T>(int capacity)
		{
			return ListPool<T>.Shared.Get(capacity);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Return<T>(List<T> list)
		{
			ListPool<T>.Shared.Return(list);
		}
	}

	public static partial class IListExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool RemoveBySwap<T>(this IList<T> list, int index)
		{
			list[index] = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			return true;
		}
	}
}
