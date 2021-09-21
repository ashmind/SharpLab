using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Fragile.Internal {
    internal static class SafeDispose {
        private static void InnerDispose<TContext>(TContext context, Action<TContext> dispose, IList<Exception>? exceptions) {
            try {
                dispose(context);
            }
            catch (Exception ex) {
                exceptions ??= new List<Exception>();
                exceptions.Add(ex);
            }
        }

        public static void CloseSafeHandle(SafeHandle? handle) {
            handle?.Close();
        }

        public static void FreeGCHandle(GCHandle handle) {
            if (handle.IsAllocated)
                handle.Free();
        }

        public static void DisposeOnException<TContext>(
            Exception exception,
            TContext context,
            Action<TContext> dispose
        ) {
            try {
                dispose(context);
            }
            catch (Exception disposeException) {
                throw new AggregateException(exception, disposeException);
            }
        }

        public static void DisposeOnException<TContext1, TContext2>(
            Exception exception,
            TContext1 context1,
            Action<TContext1> dispose1,
            TContext2 context2,
            Action<TContext2> dispose2
        ) {
            DisposeOnException(exception, (context1, dispose1, context2, dispose2), static x => Dispose(
                x.context1, x.dispose1,
                x.context2, x.dispose2
            ));
        }

        public static void DisposeOnException<TContext1, TContext2, TContext3>(
            Exception exception,
            TContext1 context1,
            Action<TContext1> dispose1,
            TContext2 context2,
            Action<TContext2> dispose2,
            TContext3 context3,
            Action<TContext3> dispose3
        ) {
            DisposeOnException(exception, (context1, dispose1, context2, dispose2, context3, dispose3), static x => Dispose(
                x.context1, x.dispose1,
                x.context2, x.dispose2,
                x.context3, x.dispose3
            ));
        }

        public static void DisposeOnException<TContext1, TContext2, TContext3, TContext4>(
            Exception exception,
            TContext1 context1,
            Action<TContext1> dispose1,
            TContext2 context2,
            Action<TContext2> dispose2,
            TContext3 context3,
            Action<TContext3> dispose3,
            TContext4 context4,
            Action<TContext4> dispose4
        ) {
            DisposeOnException(exception, (context1, dispose1, context2, dispose2, context3, dispose3, context4, dispose4), static x => Dispose(
                x.context1, x.dispose1,
                x.context2, x.dispose2,
                x.context3, x.dispose3,
                x.context4, x.dispose4
            ));
        }

        public static void DisposeOnException<TContext1, TContext2, TContext3, TContext4, TContext5>(
            Exception exception,
            TContext1 context1,
            Action<TContext1> dispose1,
            TContext2 context2,
            Action<TContext2> dispose2,
            TContext3 context3,
            Action<TContext3> dispose3,
            TContext4 context4,
            Action<TContext4> dispose4,
            TContext5 context5,
            Action<TContext5> dispose5
        ) {
            DisposeOnException(exception, (context1, dispose1, context2, dispose2, context3, dispose3, context4, dispose4, context5, dispose5), static x => Dispose(
                x.context1, x.dispose1,
                x.context2, x.dispose2,
                x.context3, x.dispose3,
                x.context4, x.dispose4,
                x.context5, x.dispose5
            ));
        }

        public static void Dispose<TContext1, TContext2>(
            TContext1 context1,
            Action<TContext1> dispose1,
            TContext2 context2,
            Action<TContext2> dispose2
        ) {
            var exceptions = (IList<Exception>?)null;
            InnerDispose(context1, dispose1, exceptions);
            InnerDispose(context2, dispose2, exceptions);
            if (exceptions != null)
                throw new AggregateException(exceptions);
        }

        public static void Dispose<TContext1, TContext2, TContext3>(
            TContext1 context1,
            Action<TContext1> dispose1,
            TContext2 context2,
            Action<TContext2> dispose2,
            TContext3 context3,
            Action<TContext3> dispose3
        ) {
            var exceptions = (IList<Exception>?)null;
            InnerDispose(context1, dispose1, exceptions);
            InnerDispose(context2, dispose2, exceptions);
            InnerDispose(context3, dispose3, exceptions);
            if (exceptions != null)
                throw new AggregateException(exceptions);
        }

        public static void Dispose<TContext1, TContext2, TContext3, TContext4>(
            TContext1 context1,
            Action<TContext1> dispose1,
            TContext2 context2,
            Action<TContext2> dispose2,
            TContext3 context3,
            Action<TContext3> dispose3,
            TContext4 context4,
            Action<TContext4> dispose4
        ) {
            var exceptions = (IList<Exception>?)null;
            InnerDispose(context1, dispose1, exceptions);
            InnerDispose(context2, dispose2, exceptions);
            InnerDispose(context3, dispose3, exceptions);
            InnerDispose(context4, dispose4, exceptions);
            if (exceptions != null)
                throw new AggregateException(exceptions);
        }

        public static void Dispose<TContext1, TContext2, TContext3, TContext4, TContext5>(
            TContext1 context1,
            Action<TContext1> dispose1,
            TContext2 context2,
            Action<TContext2> dispose2,
            TContext3 context3,
            Action<TContext3> dispose3,
            TContext4 context4,
            Action<TContext4> dispose4,
            TContext5 context5,
            Action<TContext5> dispose5
        ) {
            var exceptions = (IList<Exception>?)null;
            InnerDispose(context1, dispose1, exceptions);
            InnerDispose(context2, dispose2, exceptions);
            InnerDispose(context3, dispose3, exceptions);
            InnerDispose(context4, dispose4, exceptions);
            InnerDispose(context5, dispose5, exceptions);
            if (exceptions != null)
                throw new AggregateException(exceptions);
        }
    }
}
