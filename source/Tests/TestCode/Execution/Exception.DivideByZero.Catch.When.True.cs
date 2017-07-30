public static class Program {
    public static void Main() {
        try {
            var x = 0;
            var y = x / 0;
        }
        catch when (true) {
        }
    }
}