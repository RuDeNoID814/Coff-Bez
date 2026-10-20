namespace BezoutCoefficients;

public static class BezoutMath
{
    // Рекурсивная триада:
    // параметризация — пара чисел (a, b);
    // база — b == 0: gcd(a, 0) = a, коэффициенты (1, 0), т.к. a*1 + 0*0 = a;
    // декомпозиция — gcd(a, b) = gcd(b, a mod b); коэффициенты пересчитываются
    // из решения для меньшей пары при разворачивании рекурсии.
    public static (long Gcd, long X, long Y) ExtendedGcd(long a, long b)
    {
        if (b == 0)
            return (a, 1, 0);

        var (g, x1, y1) = ExtendedGcd(b, a % b);
        long x = y1;
        long y = x1 - (a / b) * y1;
        return (g, x, y);
    }
}
