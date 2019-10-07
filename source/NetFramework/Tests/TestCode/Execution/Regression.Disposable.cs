using System;

class Program
{
    static void Main()
    {
        using (new D())
        {
            throw new Exception();
        }
    }

    class D : IDisposable
    {
        public void Dispose() { }
    }
}