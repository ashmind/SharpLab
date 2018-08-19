class Program
{
    static int i;
    static ref int M() => ref i;
    static void Main() => M();
}