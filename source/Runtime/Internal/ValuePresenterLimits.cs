namespace SharpLab.Runtime.Internal
{
    public struct ValuePresenterLimits
    {
        private bool _maxDepthSet;
        private int? _maxDepth;

        public ValuePresenterLimits(int? maxDepth = 5, int? maxEnumerableItemCount = null, int? maxValueLength = null)
        {
            _maxDepth = maxDepth;
            _maxDepthSet = true;
            MaxEnumerableItemCount = maxEnumerableItemCount;
            MaxValueLength = maxValueLength;
        }

        public int? MaxDepth
        {
            get { return _maxDepthSet ? _maxDepth : 5; }
        }
        public int? MaxValueLength { get; }
        public int? MaxEnumerableItemCount { get; }
    }
}
