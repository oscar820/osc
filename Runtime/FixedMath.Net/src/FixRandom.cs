using System;

namespace QTool
{
	public class FixRandom
    {
        private Random random;

        public FixRandom(int seed)
        {
            random = new Random(seed);
        }
        public int Range(int maxValue)
        {
            return random.Next(maxValue);
        }
        public int Range(int minValue, int maxValue)
        {
            return random.Next(minValue,maxValue);
        }
        public Fix64 One()
        {
            return (Fix64)random.Next(Fix) / Fix;
        }
        public Fix64 Range(Fix64 maxValue)
        {
            return One() * maxValue ;
        }
        const int Fix = int.MaxValue;
        public Fix64 Range(Fix64 minValue, Fix64 maxValue)
        {
            var range= maxValue - minValue;
            return Range(range)+minValue;
        }
    }
}
