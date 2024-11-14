using System.Security.Cryptography;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Services;

public interface IRandomService
{
    /// <summary>
    /// Generate a random number in [min and max)
    /// </summary>
    /// <param name="min">minimum value inclusive</param>
    /// <param name="max">maximum value exclusive</param>
    /// <returns></returns>
    int Next(int min, int max);

    /// <summary>
    /// Generate a random number between [0 and 1)
    /// </summary>
    /// <returns></returns>
    double Next();

    /// <summary>
    /// Select one from this list with an equal chance for each
    /// </summary>
    /// <param name="items"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T PickFrom<T>(List<T> items);
}

public class RandomService(RandomNumberGenerator generator) : IRandomService
{
    private RandomNumberGenerator RandomNumberGenerator { get; } = generator;

    public int Next(int min, int max)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
        if (min == max)
        {
            return min;
        }

        var b = new byte[4];
        RandomNumberGenerator.GetBytes(b);
        var v = Math.Abs(BitConverter.ToInt32(b));
        return min + v % (max - min);
    }

    public double Next()
    {
        var b = new byte[4];
        RandomNumberGenerator.GetBytes(b);
        return BitConverter.ToUInt32(b) / (double)uint.MaxValue;
    }

    public T PickFrom<T>(List<T> items)
    {
        return items[Next(0, items.Count)];
    }
}
