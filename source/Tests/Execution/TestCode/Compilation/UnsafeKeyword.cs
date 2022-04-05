public static class Program {
    public static void Main() {
        unsafe {
            var node = new Node();
        }
    }

    public unsafe struct Node
    {
        public int Value;
        public Node* Left;
        public Node* Right;
    }
}