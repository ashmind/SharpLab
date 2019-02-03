using System.ComponentModel;
using System.Text;
using SharpLab.Runtime.Internal;

public static class SharpLabObjectExtensions {
    // LinqPad/etc compatibility only
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static T Dump<T>(this T value) {
        value.Inspect(title: "Dump");
        return value;
    }

    public static void Inspect<T>(this T value, string title = "Inspect") {
        Output.Write(new SimpleInspection(title, ValuePresenter.ToStringBuilder(value)));
    }
}
