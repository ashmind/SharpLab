using System;
using System.Linq.Expressions;

public static class Program {
    public static void Main() {
        Expression<Func<int, int, string>> f = (a, b) => $"{a}{b}";
    }
}