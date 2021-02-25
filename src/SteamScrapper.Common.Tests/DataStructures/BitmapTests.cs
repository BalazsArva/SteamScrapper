using System;
using NUnit.Framework;
using SteamScrapper.Common.DataStructures;

namespace SteamScrapper.Common.Tests.DataStructures
{
    public class BitmapTests
    {
        [TestCase(-1)]
        [TestCase(0)]
        public void New_CapacityIsTooSmall_ThrowsArgumentException(int capacity)
        {
            var exceptionThrown = Assert.Throws<ArgumentException>(() => new Bitmap(capacity));

            Assert.AreEqual("capacity", exceptionThrown.ParamName);
        }

        [Test]
        public void Get_BitmapNeverTouched_NoBitIsSet()
        {
            const int capacity = 3000;

            var bitmap = new Bitmap(capacity);

            for (var i = 0; i < capacity; ++i)
            {
                Assert.IsFalse(bitmap.Get(i), $"Expected the bit at index {i} to be 0, but it was 1.");
            }
        }

        [TestCase(-10, 10)]
        [TestCase(-1, 10)]
        [TestCase(10, 10)]
        [TestCase(110, 10)]
        public void Set_IndexOutOfRange_ThrowsArgumentOutOfRangeException(int index, int capacity)
        {
            var bitmap = new Bitmap(capacity);

            var exceptionThrown = Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Set(index, true));

            Assert.AreEqual("index", exceptionThrown.ParamName);
        }

        [Test]
        public void Set_IndexWithinRange_BitIsCorrectlySet()
        {
            const int capacity = 3000;

            var bitmap = new Bitmap(capacity);

            // True round
            // Set and check every bit individually
            for (var i = 0; i < capacity; ++i)
            {
                bitmap.Set(i, true);

                Assert.IsTrue(bitmap.Get(i), $"Expected the bit at index {i} to be 1, but it was 0.");
            }

            // Re-check everything to ensure that subsequent operations don't do some bad logic, negating previous operation(s).
            for (var i = 0; i < capacity; ++i)
            {
                Assert.IsTrue(bitmap.Get(i), $"Expected the bit at index {i} to be 1, but it was 0.");
            }

            // False round
            // Set and check every bit individually
            for (var i = 0; i < capacity; ++i)
            {
                bitmap.Set(i, false);

                Assert.IsFalse(bitmap.Get(i), $"Expected the bit at index {i} to be 0, but it was 1.");
            }

            // Re-check everything to ensure that subsequent operations don't do some bad logic, negating previous operation(s).
            for (var i = 0; i < capacity; ++i)
            {
                Assert.IsFalse(bitmap.Get(i), $"Expected the bit at index {i} to be 0, but it was 1.");
            }
        }
    }
}