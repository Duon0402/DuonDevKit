using DuonDevKit.Core.Errors;
using DuonDevKit.Core.Results;

namespace DuonDevKit.Core.Options
{
    /// <summary>
    /// Represents the presence or absence of a value of type <typeparamref name="T"/>, with no
    /// reason attached to the absent case — unlike <see cref="Result{T}"/>, which always carries an
    /// <see cref="Error"/> on failure. Use <see cref="Option{T}"/> when "not found" needs no
    /// explanation; use <see cref="Result{T}"/> when it does.
    /// </summary>
    public readonly struct Option<T>
    {
        private readonly T? _value;

        /// <summary>Gets a value indicating whether this option holds a value.</summary>
        public bool HasValue { get; }

        /// <summary>Gets a value indicating whether this option holds no value. The default <c>Option&lt;T&gt;</c> is always <c>None</c>.</summary>
        public bool IsNone => !HasValue;

        /// <summary>
        /// Gets the held value. Throws <see cref="InvalidOperationException"/> when <see cref="IsNone"/>.
        /// </summary>
        public T Value
        {
            get
            {
                if (IsNone)
                    throw new InvalidOperationException("Cannot access the value of a None option. Check HasValue/IsNone first.");

                return _value!;
            }
        }

        private Option(bool hasValue, T? value)
        {
            HasValue = hasValue;
            _value = value;
        }

        /// <summary>Creates an option holding <paramref name="value"/>.</summary>
        public static Option<T> Some(T value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return new Option<T>(true, value);
        }

        /// <summary>The option holding no value.</summary>
        public static Option<T> None => default;

        /// <summary>Implicitly converts a value into <see cref="Some"/>, or into <see cref="None"/> when <paramref name="value"/> is <c>null</c>.</summary>
        public static implicit operator Option<T>(T value)
            => value is null ? None : Some(value);

        /// <summary>Invokes <paramref name="onSome"/> with the value when present, or <paramref name="onNone"/> otherwise, and returns the produced value.</summary>
        public TOut Match<TOut>(Func<T, TOut> onSome, Func<TOut> onNone)
        {
            ArgumentNullException.ThrowIfNull(onSome);
            ArgumentNullException.ThrowIfNull(onNone);

            return HasValue ? onSome(Value) : onNone();
        }

        /// <summary>Transforms the value using <paramref name="mapper"/> when present; propagates <see cref="None"/> unchanged otherwise, without invoking <paramref name="mapper"/>.</summary>
        public Option<TOut> Map<TOut>(Func<T, TOut> mapper)
        {
            ArgumentNullException.ThrowIfNull(mapper);

            return HasValue ? Option<TOut>.Some(mapper(Value)) : Option<TOut>.None;
        }

        /// <summary>Converts this option into a <see cref="Result{T}"/>, using <paramref name="error"/> as the failure reason when this option is <see cref="None"/>.</summary>
        public Result<T> ToResult(Error error)
            => HasValue ? Result.Success(Value) : Result.Fail<T>(error);

        /// <inheritdoc />
        public override string ToString()
            => HasValue ? $"Some: {Value}" : "None";
    }
}
