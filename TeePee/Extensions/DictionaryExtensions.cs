﻿using System.Collections.Generic;
using System.Linq;

namespace TeePee.Extensions
{
    internal static class DictionaryExtensions
    {
        internal static string Flat<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => string.Join(',', dictionary.Select(kvp => $"{kvp.Key} = {kvp.Value}"));
        internal static string Flat<TKey, TValue>(this IDictionary<TKey, IEnumerable<TValue>> dictionary) => string.Join(',', dictionary.Select(kvp => $"{kvp.Key} = {string.Join(';', kvp.Value)}"));
    }
}