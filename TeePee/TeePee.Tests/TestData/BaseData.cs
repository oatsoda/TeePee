using System.Collections;
using System.Collections.Generic;

namespace TeePee.Tests.TestData
{
    public abstract class BaseData : IEnumerable<object[]>
    {
        public abstract IEnumerator<object[]> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}