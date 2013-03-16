using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/* Adapted from http://content.gpwiki.org/index.php/C_sharp:BinaryHeapOfT */
namespace CS124Project.Sequencing
{
    class BinaryHeap<T>
    {
        private static int maxCount = 0;
        private T[] _array;
        private readonly Comparison<T> _comparison;
        public int Count { get; private set; }

        public BinaryHeap(Comparison<T> comparison)
        {
            _comparison = comparison;
            _array = new T[16];
            Count = 0;
        }

        public void Add(T item)
        {
            if (Count == _array.Length)
            {
                var array = new T[Count*2];
                Array.Copy(_array, array, Count);
                _array = array;
            }

            _array[Count] = item;
            int itemIndex = Count;
            int parentIndex = Parent(itemIndex);
            while (parentIndex > -1 && _comparison(item, _array[parentIndex]) < 0)
            {
                _array[itemIndex] = _array[parentIndex]; //Swap nodes
                itemIndex = parentIndex;
                parentIndex = Parent(itemIndex);
            }
            _array[itemIndex] = item;

            Count++;
            /*if (Count > Volatile.Read(ref maxCount))
            {
                Interlocked.Exchange(ref maxCount, Count);
                Console.WriteLine("max heap {0}", maxCount);
            }*/
        }

        public T Remove()
        {
            if (Count == 0)
                throw new InvalidOperationException();
            T returnItem = _array[0];
            Count--;
            _array[0] = _array[Count];
            _array[Count] = default(T);

            int itemIndex = 0;
            T item = _array[itemIndex];
            while (true)
            {
                int ch1 = Child1(itemIndex);
                if (ch1 >= Count) break;
                int ch2 = Child2(itemIndex);
                int n;
                if (ch2 >= Count)
                {
                    n = ch1;
                }
                else
                {
                    n =  _comparison(_array[ch1], _array[ch2]) < 0 ? ch1 : ch2;
                }
                if (_comparison(item, _array[n]) > 0)
                {
                    _array[itemIndex] = _array[n]; //Swap nodes
                    itemIndex = n;
                }
                else
                {
                    break;
                }
            }
            _array[itemIndex] = item;

            return returnItem;
        }

        private static int Child1(int index)
        {
            return (index << 1) + 1;
        }
        private static int Child2(int index)
        {
            return (index << 1) + 2;
        }

        static int Parent(int index)
        {
            return (index - 1) >> 1;
        }
    }
}
