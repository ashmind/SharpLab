using System;

namespace SharpLab.Runtime.Internal
{
    public readonly struct ValuePresenterLimits
    {
        public static ValuePresenterLimits InspectValue { get; } = new(maxValueLength: 100);

        public ValuePresenterLimits(int maxValueLength, int maxEnumerableItemCount = 5)
        {
            MaxValueLength = maxValueLength;
            MaxEnumerableItemCount = maxEnumerableItemCount;
            MaxDepth = 2; // Might be dropped/hardcoded later
        }

        public int MaxValueLength { get; }
        public int MaxEnumerableItemCount { get; }
        public int MaxDepth { get; }

        internal ValuePresenterLimits WithMaxEnumerableItemCount(int maxEnumerableItemCount)
            => new(MaxValueLength, maxEnumerableItemCount);
    }
}
