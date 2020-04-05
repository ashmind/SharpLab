export default function extendType<T>(value: T) {
    return <TExtra>() => value as T & TExtra;
}