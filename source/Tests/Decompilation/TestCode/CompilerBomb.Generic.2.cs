// https://github.com/ashmind/SharpLab/issues/1223
class Z<A, B, C, D> {
    class X : Z<
        Z<Z<X, X, X, X>, X, X, X>,
        Z<X, X, X, X>,
        Z<X, X, X, X>,
        Z<X, X, X, X>> {
        class Y : X.X.X.X { }
    }
}