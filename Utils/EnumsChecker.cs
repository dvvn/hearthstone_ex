using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace hearthstone_ex.Utils
{
    internal class EnumsCheckerInfo<T>
    {
        public readonly IReadOnlyCollection<T> KnownEnums;
        public readonly bool IsRange;

        public EnumsCheckerInfo()
        {
            this.KnownEnums = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var counter = 0;
            this.IsRange = this.KnownEnums.Cast<int>().OrderBy(x => x).All(i => i == counter++);
        }

        protected EnumsCheckerInfo([NotNull] EnumsCheckerInfo<T> other)
        {
            this.KnownEnums = other.KnownEnums;
            this.IsRange = other.IsRange;
        }

        [NotNull]
        protected T[] GetOtherEnums(T ignore) => this.KnownEnums.Where(e => e.Equals(ignore) == false).ToArray();

        [CanBeNull]
        protected string GetErrorMsgBase([NotNull] IReadOnlyCollection<T> enums)
        {
            if (enums.Count > 1)
            {
                if (enums.Count == 0)
                    return "Value cannot be an empty collection.";
                if (enums.Count > this.KnownEnums.Count)
                    return "Duplicated enum found";
            }

            return null;
        }

        [CanBeNull]
        protected string GetErrorMsgUnordered([NotNull] IReadOnlyList<T> enums)
        {
            for (var i = 0; i < enums.Count - 1; i++)
            {
                var first_enum = enums[i];
                var other_enums = enums.Skip(i + 1);
                if (other_enums.Contains(first_enum))
                    return "Duplicated enum found (different Length)";
            }

            return null;
        }
    }

    internal abstract class EnumsCheckerBase<T> : EnumsCheckerInfo<T>
    {
        protected EnumsCheckerBase([NotNull] EnumsCheckerInfo<T> info) : base(info)
        {
        }

        public abstract IReadOnlyCollection<T> OtherEnums(T ignore);
        public abstract string GetErrorMsg([NotNull] IReadOnlyList<T> enums);

        [Conditional("DEBUG")]
        public void Check([NotNull] IReadOnlyList<T> enums)
        {
            var error_msg = this.GetErrorMsg(enums);
            if (error_msg != null)
                throw new ArgumentException(error_msg, nameof(enums));
        }
    }

    internal class EnumsCheckerRange<T> : EnumsCheckerBase<T>
    {
        public readonly int AllSum;
        private readonly IList<T[]> m_storage;

        [CanBeNull]
        private string GetErrorMsgRange([NotNull] IReadOnlyCollection<T> enums)
        {
            if (enums.Count == this.KnownEnums.Count)
            {
                var sum = enums.Cast<int>().Sum();
                if (sum != this.AllSum)
                    return "Duplicated enum found (same Length)";
            }

            return null;
        }

        public EnumsCheckerRange([NotNull] EnumsCheckerInfo<T> info) : base(info)
        {
            var storage = new List<T[]>(this.KnownEnums.Count);
            storage.AddRange(this.KnownEnums.Select(this.GetOtherEnums));

            this.AllSum = this.KnownEnums.Cast<int>().Sum();
            this.m_storage = storage;
        }

        public override IReadOnlyCollection<T> OtherEnums(T ignore) => this.m_storage[(int)(object)ignore];

        [CanBeNull]
        public override string GetErrorMsg(IReadOnlyList<T> enums) => this.GetErrorMsgBase(enums) ?? this.GetErrorMsgRange(enums) ?? this.GetErrorMsgUnordered(enums);
    }

    internal class EnumsCheckerUnordered<T> : EnumsCheckerBase<T>
    {
        private readonly IDictionary<T, T[]> m_storage;

        public EnumsCheckerUnordered([NotNull] EnumsCheckerInfo<T> info) : base(info)
        {
            this.m_storage = new Dictionary<T, T[]>(this.KnownEnums.Count);
            foreach (var e in this.KnownEnums)
                this.m_storage.Add(e, this.GetOtherEnums(e));
        }

        public override IReadOnlyCollection<T> OtherEnums([NotNull] T ignore) => this.m_storage[ignore];

        [CanBeNull]
        public override string GetErrorMsg(IReadOnlyList<T> enums) => this.GetErrorMsgBase(enums) ?? this.GetErrorMsgUnordered(enums);
    }

    internal class EnumsChecker<T>
    {
        private static EnumsCheckerBase<T> m_instance;

        [NotNull]
        public static EnumsCheckerBase<T> Get()
        {
            if (m_instance == null)
            {
                var info = new EnumsCheckerInfo<T>();
                m_instance = info.IsRange ? (EnumsCheckerBase<T>)new EnumsCheckerRange<T>(info) : new EnumsCheckerUnordered<T>(info);
            }

            return m_instance;
        }
    }
}
