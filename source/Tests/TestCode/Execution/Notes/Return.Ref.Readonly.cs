public static class Program {
    public static void Main() => M(0);

    public static ref readonly int M(in int value) { // [value: 0]
        return ref value;
    } // [return: 0]
}