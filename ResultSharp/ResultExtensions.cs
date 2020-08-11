using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static ResultSharp.Prelude;

namespace ResultSharp
{
	public static class ResultExtensions
	{
		public static Result<IEnumerable<T>, IEnumerable<E>> Combine<T, E>(
			this IEnumerable<Result<T, E>> results)
		{
			var data = results as Result<T, E>[] ?? results.ToArray();
			var errors = data.Where(x => x.IsErr);
			if (errors.Any())
			{
				return Err(errors.Select(x => x.UnwrapErr()));
			}
			else
			{
				return Ok(data.Select(x => x.Unwrap()));
			}
		}

		public static Result Combine(
			this IEnumerable<Result> results,
			string errorMessageSeparator) =>
			results
				.Select(x => x.Inner)
				.Combine(
					_ => Unit.Default,
					errs => string.Join(errorMessageSeparator, errs)
				);

		public static Result<IEnumerable<T>> Combine<T>(
			this IEnumerable<Result<T>> results,
			string errorMessageSeparator) =>
			results
				.Select(x => x.Inner)
				.Combine(combineErr: errs => string.Join(errorMessageSeparator, errs));

		public static Result<U, F> Combine<T, U, E, F>(
			this IEnumerable<Result<T, E>> results,
			Func<IEnumerable<T>, U> combineOk,
			Func<IEnumerable<E>, F> combineErr) =>
			results
				.Combine()
				.BiMap(combineOk, combineErr);

		public static Result<IEnumerable<T>, F> Combine<T, E, F>(
			this IEnumerable<Result<T, E>> results,
			Func<IEnumerable<E>, F> combineErr) =>
			results
				.Combine()
				.MapErr(combineErr);

		public static Result<U, IEnumerable<E>> Combine<T, U, E>(
			this IEnumerable<Result<T, E>> results,
			Func<IEnumerable<T>, U> combineOk) =>
			results
				.Combine()
				.Map(combineOk);

		public static Result<IEnumerable<T>, IEnumerable<E>> CombineMany<T, E>(
			this IEnumerable<Result<IEnumerable<T>, IEnumerable<E>>> results) =>
			results
				.Combine()
				.BiMap(EnumerableExtensions.Flatten, EnumerableExtensions.Flatten);

		public static Result<U, E> AndThenTry<T, U, E>(this Result<T, E> result, Func<T, U> f)
			where E : Exception =>
			result
				.AndThen(x => Try<U, E>(() => f(x)));

		static IEnumerable<object> GetErrors(ITuple tuple) =>
			from item in tuple.EnumerateItems()
			let result = (IResult)item
			where result.IsErr
			select result.UnwrapErrUntyped();

		public static Result<(T1, T2)> Combine<T1, T2>(
			this (Result<T1>,Result<T2>) resultTuple,
			string errorMessageSeparator)
		{
			var (r1, r2) = resultTuple;
			var errors = GetErrors(resultTuple)
				.Select(x => (string)x)
				.ToArray();

			if (errors.Any())
				return Err(string.Join(errorMessageSeparator, errors));
			else
				return Ok((r1.Unwrap(), r2.Unwrap()));
		}
	}

	internal static class EnumerableExtensions
	{
		public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> nestedEnumerable) =>
			nestedEnumerable.SelectMany(x => x);
	}

	internal static class ITupleExtensions
	{
		public static IEnumerable<object> EnumerateItems(this ITuple tuple)
		{
			var len = tuple.Length;
			for (var i = 0; i < len; i++)
				yield return tuple[i];
		}
	}
}
