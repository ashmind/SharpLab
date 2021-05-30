namespace SharpLab.Runtime.Internal
{
    public readonly struct ValuePresenterLimits
    {
        public static ValuePresenterLimits InspectValue { get; } = new(maxValueLength: 100);

        public ValuePresenterLimits(int maxValueLength, int maxEnumerableItemCount = 5, int maxDepth = 1)
        {
            MaxValueLength = maxValueLength;
            MaxEnumerableItemCount = maxEnumerableItemCount;
            MaxDepth = maxDepth;
        }

        public int MaxValueLength { get; }
        public int MaxEnumerableItemCount { get; }
        public int MaxDepth { get; }
    }
}
