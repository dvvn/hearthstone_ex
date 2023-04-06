using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace hearthstone_ex.Utils
{
	internal class EnumsCheckerInfo<T>
	{
		public readonly ICollection<T> KnownEnums;
		public readonly bool IsRange;

		public EnumsCheckerInfo()
		{
			KnownEnums = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
			var counter = 0;
			IsRange = KnownEnums.Cast<int>().OrderBy(x => x).All(i => i == counter++);
		}

		protected EnumsCheckerInfo( EnumsCheckerInfo<T> other)
		{
			KnownEnums = other.KnownEnums;
			IsRange = other.IsRange;
		}

		
		protected T[] GetOtherEnums(T ignore) => KnownEnums.Where(e => e.Equals(ignore) == false).ToArray();

		
		protected string GetErrorMsgBase( ICollection<T> enums)
		{
			if (enums.Count > 1)
			{
				if (enums.Count == 0)
					return "Value cannot be an empty collection.";
				if (enums.Count > KnownEnums.Count)
					return "Duplicated enum found";
			}

			return null;
		}

		
		protected string GetErrorMsgUnordered( IList<T> enums)
		{
			for (var i = 0; i < enums.Count - 1; i++)
			{
				var firstEnum = enums[i];
				var otherEnums = enums.Skip(i + 1);
				if (otherEnums.Contains(firstEnum))
					return "Duplicated enum found (different Length)";
			}

			return null;
		}
	}

	internal abstract class EnumsCheckerBase<T> : EnumsCheckerInfo<T>
	{
		protected EnumsCheckerBase( EnumsCheckerInfo<T> info)
			: base(info)
		{
		}

		public abstract ICollection<T> OtherEnums(T ignore);
		public abstract string GetErrorMsg( IList<T> enums);

		[Conditional("DEBUG")]
		public void Check( IList<T> enums)
		{
			var errorMsg = GetErrorMsg(enums);
			if (errorMsg != null)
				throw new ArgumentException(errorMsg, nameof(enums));
		}
	}

	internal class EnumsCheckerRange<T> : EnumsCheckerBase<T>
	{
		public readonly int AllSum;
		private readonly IList<T[]> _storage;

		
		private string GetErrorMsgRange( ICollection<T> enums)
		{
			if (enums.Count == KnownEnums.Count)
			{
				var sum = enums.Cast<int>().Sum();
				if (sum != AllSum)
					return "Duplicated enum found (same Length)";
			}

			return null;
		}

		public EnumsCheckerRange( EnumsCheckerInfo<T> info)
			: base(info)
		{
			var storage = new List<T[]>(KnownEnums.Count);
			storage.AddRange(KnownEnums.Select(GetOtherEnums));

			AllSum = KnownEnums.Cast<int>().Sum();
			_storage = storage;
		}

		public override ICollection<T> OtherEnums(T ignore) => _storage[(int)(object)ignore];

		
		public override string GetErrorMsg(IList<T> enums) =>
			GetErrorMsgBase(enums) ?? GetErrorMsgRange(enums) ?? GetErrorMsgUnordered(enums);
	}

	internal class EnumsCheckerUnordered<T> : EnumsCheckerBase<T>
	{
		private readonly IDictionary<T, T[]> _storage;

		public EnumsCheckerUnordered( EnumsCheckerInfo<T> info)
			: base(info)
		{
			_storage = new Dictionary<T, T[]>(KnownEnums.Count);
			foreach (var e in KnownEnums)
				_storage.Add(e, GetOtherEnums(e));
		}

		public override ICollection<T> OtherEnums( T ignore) => _storage[ignore];

		
		public override string GetErrorMsg(IList<T> enums) =>
			GetErrorMsgBase(enums) ?? GetErrorMsgUnordered(enums);
	}

	internal class EnumsChecker<T>
	{
		private static EnumsCheckerBase<T> _instance;

		
		public static EnumsCheckerBase<T> Get()
		{
			if (_instance == null)
			{
				var info = new EnumsCheckerInfo<T>();
				_instance = info.IsRange
					? (EnumsCheckerBase<T>)new EnumsCheckerRange<T>(info)
					: new EnumsCheckerUnordered<T>(info);
			}

			return _instance;
		}
	}
}