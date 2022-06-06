using System;

using var a = new Disposable();
using var b = new Disposable();

public class Disposable : IDisposable
{
    public void Dispose() { }
}