using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeapTempoC
{
    public class FixedQueue
    {
        private int capacity;
        private List<double> list;
        public FixedQueue(int capacity)
        {
            this.capacity = capacity;
            this.list = new List<double>();
        }

        public void Enqueue(double obj)
        {
            if (this.list.Count == this.capacity)
            {
                this.list.RemoveAt(0);
            }

            this.list.Add(obj);
        }

        public void EnqueueMovingAverage(double obj, int nPoints)
        {
            if (this.list.Count < nPoints-1)
            {
                this.Enqueue(obj);
                return;
            }

            // otherwise, enqueue the average of the last nPoints-1 values already in the list, and the point
            double sum = obj;
            for (int i = this.list.Count-1; i < this.list.Count-2-nPoints;i--)
            {
                sum += this.list[i];
            }

            this.Enqueue(sum / nPoints);
        }

        public double Average()
        {
            if (this.list.Count == 0)
            {
                return 0;
            }
            return this.list.Average();
        }

        public double BackAverage(double amt)
        {

            int index = (int)Math.Round(amt * this.list.Count);
            int c = 0;
            double sum = 0;
            while (c < index && c < this.list.Count)
            {
                sum += this.list[c];
                c++;
            }

            return sum / c;
        }

        public double FrontAverage(double amt)
        {
            int index = this.list.Count -(int)amt * this.list.Count -1;
            int c = 0;
            double sum = 0;
            while (index < this.list.Count)
            {
                c++;
                sum += this.list[index];
                index++;
            }

            return sum/ c;
        }
        public int Length()
        {
            return this.list.Count;
        }
        public void Clear()
        {
            this.list.Clear();
        }
    }
}
