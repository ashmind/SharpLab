unsafe {
    delegate*<void> m = &M;
    m();
}

static void M() {}