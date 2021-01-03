export default function defineState<T>(initialValue: T, { beforeSet }: { beforeSet?: (value: T) => void } = {}) {
    const state = {
        value: initialValue
    };

    const setState = (value: T) => {
        if (beforeSet)
            beforeSet(value);
        state.value = value;
    };

    return [state as Readonly<typeof state>, setState] as const;
}