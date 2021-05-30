public static class Program {
    public static void Main() {
        var c = new C(); // [c: ]
    }
}

public class C {
    public override string ToString() => null;
}