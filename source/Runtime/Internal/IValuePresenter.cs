using System;
using System.Collections.Generic;
using System.Text;

namespace SharpLab.Runtime.Internal {
    public interface IValuePresenter {
        void AppendTo<T>(StringBuilder builder, ReadOnlySpan<T> value, ValuePresenterLimits limits);
        void AppendTo<T>(StringBuilder builder, T value, ValuePresenterLimits limits);
        void AppendStringTo(StringBuilder builder, string value, ValuePresenterLimits limits);
        void AppendEnumerableTo<T>(StringBuilder builder, IEnumerable<T> enumerable, int depth, ValuePresenterLimits limits);
        StringBuilder ToStringBuilder<T>(ReadOnlySpan<T> value, ValuePresenterLimits limits);
        StringBuilder ToStringBuilder<T>(T value, ValuePresenterLimits limits);
    }
}