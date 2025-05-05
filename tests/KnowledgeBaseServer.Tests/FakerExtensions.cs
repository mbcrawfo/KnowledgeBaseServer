using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Bogus;

namespace KnowledgeBaseServer.Tests;

public static class FakerExtensions
{
    public static IEnumerable<T> MakeUnique<T>(this Faker faker, Func<Faker, T> generator)
    {
        return CreateItems().Distinct();

        [SuppressMessage(
            "Blocker Bug",
            "S2190:Loops and recursions should not be infinite",
            Justification = "The method is intended to generate a potentially infinite sequence of items..."
        )]
        IEnumerable<T> CreateItems()
        {
            while (true)
            {
                yield return generator(faker);
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }

    public static IEnumerable<TItem> MakeUnique<TItem, TProperty>(
        this Faker faker,
        Func<Faker, TItem> generator,
        Func<TItem, TProperty> uniquePropertySelector
    )
        where TProperty : notnull
    {
        return CreateItems().DistinctBy(uniquePropertySelector);

        [SuppressMessage(
            "Blocker Bug",
            "S2190:Loops and recursions should not be infinite",
            Justification = "The method is intended to generate a potentially infinite sequence of items."
        )]
        IEnumerable<TItem> CreateItems()
        {
            while (true)
            {
                yield return generator(faker);
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}
