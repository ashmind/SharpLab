public static class Program {
    public static void Main() => M(0);

    public static ref readonly int M(in int value) {
        return ref value;
    }
}