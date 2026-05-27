using System.Threading;

namespace WarehouseAccessAPI.Common;

public static class FileTimeIdGenerator
{
    private static long _lastIssuedValue = DateTime.UtcNow.ToFileTimeUtc();

    public static long NextId()
    {
        while (true)
        {
            var observed = Volatile.Read(ref _lastIssuedValue);
            var nowFileTime = DateTime.UtcNow.ToFileTimeUtc();
            var nextValue = nowFileTime > observed ? nowFileTime : observed + 1;

            if (Interlocked.CompareExchange(ref _lastIssuedValue, nextValue, observed) == observed)
            {
                return nextValue;
            }
        }
    }
}

