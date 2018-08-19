public static class Program {
    public static void Main() => M();

    static int value;
    public static ref int M() {
        return ref value;
    }
}