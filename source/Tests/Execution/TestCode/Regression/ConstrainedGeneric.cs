Test(new S());

static int Test<T>(T value)
    where T : struct, I
{
    return value.Value;
}

interface I {
    int Value { get; }
}

struct S : I {
    public int Value => 1;
}