using System;

public static class StaticRandom
{
    [ThreadStatic]
    private static Random? _local;
    private static readonly Random Global = new();

    private static Random Instance
    {
        get
        {
            if (_local is null)
            {
                int seed;
                lock (Global)
                {
                    seed = Global.Next();
                }

                _local = new Random(seed);
            }

            return _local;
        }
        }

    public static int randomInt(){
        return Instance.Next();
    }
    public static int randomRange(int lowerBound, int upperBound){
        return Instance.Next(lowerBound, upperBound+1);
    }
}
