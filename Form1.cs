using System.Diagnostics;

namespace BezoutCoefficients;

public partial class Form1 : Form
{
    private readonly List<(long N, double Ms)> _points = new();
    private double _approxC;
    private double _approxR2;

    private const double MinMeasureMs = 2.0;
    private const int MaxRepeatsPerPoint = 200_000;
    private const int WarmupCalls = 200;

    public Form1()
    {
        InitializeComponent();

        AttachRangeNotice(numA, "a");
        AttachRangeNotice(numB, "b");
        AttachRangeNotice(numMaxN, "Макс. n");
        AttachRangeNotice(numStep, "Точек");
        AttachRangeNotice(numRuns, "Серий");
    }

    // Подписываем поле на проверку введённого текста до того, как
    // NumericUpDown сам молча обрежет значение до Min/Max при потере фокуса —
    // иначе пользователь видит введённое число, а потом оно без объяснений
    // "прыгает" к границе диапазона.
    private void AttachRangeNotice(NumericUpDown control, string fieldName)
    {
        control.Validating += (s, e) => CheckRange(control, fieldName);
    }

    private void CheckRange(NumericUpDown control, string fieldName)
    {
        string groupSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
        string raw = control.Text.Replace(groupSep, "").Replace(" ", "").Trim();

        if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out var typed))
            return;

        if (typed > control.Maximum)
        {
            rangeToolTip.Show($"«{fieldName}»: не больше {control.Maximum:N0}", control, 0, -24, 4000);
        }
        else if (typed < control.Minimum)
        {
            rangeToolTip.Show($"«{fieldName}»: не меньше {control.Minimum:N0}", control, 0, -24, 4000);
        }
    }

    private void BtnCompute_Click(object? sender, EventArgs e)
    {
        long a = (long)numA.Value;
        long b = (long)numB.Value;
        var (g, x, y) = BezoutMath.ExtendedGcd(a, b);

        lblResult.Text =
            $"НОД({a}, {b}) = {g}\r\n" +
            $"{a}·({x}) + {b}·({y}) = {g}\r\n" +
            $"Проверка: {a} * {x} + {b} * {y} = {a * x + b * y}  →  " +
            (a * x + b * y == g ? "верно" : "ОШИБКА");
    }

    private async void BtnRunExperiment_Click(object? sender, EventArgs e)
    {
        btnRunExperiment.Enabled = false;
        _points.Clear();

        long maxN = (long)numMaxN.Value;
        int pointCount = (int)numStep.Value;
        int series = (int)numRuns.Value;

        // Точки берём с геометрическим (а не линейным) шагом по n — иначе
        // на диапазоне до long.MaxValue почти все линейные точки легли бы
        // в самый конец шкалы, а log(n) как раз растёт медленно и ровно.
        const long minN = 10;
        var ns = BuildGeometricSizes(minN, maxN, pointCount);

        progressBar1.Minimum = 0;
        progressBar1.Maximum = ns.Count;
        progressBar1.Value = 0;

        var prevPriority = Thread.CurrentThread.Priority;

        await Task.Run(() =>
        {
            // Меньше дёрганий планировщиком ОС во время замера.
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            try
            {
                var rng = new Random();

                foreach (var n in ns)
                {
                    double ms = MeasureBest(n, rng, series);

                    // _points меняем только на UI-потоке — иначе Paint-обработчик
                    // панели (тоже на UI-потоке) может итерироваться по списку
                    // в момент, когда фоновый поток в него пишет.
                    Invoke(() =>
                    {
                        _points.Add((n, ms));
                        progressBar1.Value = Math.Min(progressBar1.Value + 1, progressBar1.Maximum);
                        panelChart.Invalidate();
                    });
                }
            }
            finally
            {
                Thread.CurrentThread.Priority = prevPriority;
            }
        });

        ComputeApproximation();
        btnRunExperiment.Enabled = true;
        panelChart.Invalidate();
    }

    private static List<long> BuildGeometricSizes(long minN, long maxN, int count)
    {
        var result = new List<long>();
        double logMin = Math.Log(minN);
        double logMax = Math.Log(Math.Max(maxN, minN + 1));

        for (int i = 0; i < count; i++)
        {
            double t = count == 1 ? 1 : (double)i / (count - 1);
            double logN = logMin + (logMax - logMin) * t;
            long n = (long)Math.Round(Math.Exp(logN));
            result.Add(Math.Max(minN, Math.Min(n, maxN)));
        }

        return result.Distinct().OrderBy(n => n).ToList();
    }

    // Реальное время в мс, с тремя приёмами против шума:
    // 1) прогрев JIT (первые вызовы в .NET компилируются на лету —
    //    искусственно медленнее, их не считаем);
    // 2) адаптивные повторы, пока не наберётся хотя бы MinMeasureMs —
    //    один вызов extgcd слишком быстрый для прямого измерения;
    // 3) из нескольких независимых серий берём МИНИМУМ, а не среднее —
    //    системный шум (переключения потоков, фоновые процессы) время
    //    только увеличивает, никогда не уменьшает, поэтому минимум
    //    ближе к истинной скорости работы алгоритма.
    private static double MeasureBest(long maxVal, Random rng, int series)
    {
        for (int w = 0; w < WarmupCalls; w++)
        {
            long a = RandomInRange(rng, maxVal);
            long b = RandomInRange(rng, maxVal);
            BezoutMath.ExtendedGcd(a, b);
        }

        double best = double.MaxValue;
        for (int s = 0; s < series; s++)
        {
            var sw = Stopwatch.StartNew();
            int reps = 0;
            while (reps < MaxRepeatsPerPoint && sw.Elapsed.TotalMilliseconds < MinMeasureMs)
            {
                long a = RandomInRange(rng, maxVal);
                long b = RandomInRange(rng, maxVal);
                BezoutMath.ExtendedGcd(a, b);
                reps++;
            }
            sw.Stop();

            if (reps > 0)
            {
                double avg = sw.Elapsed.TotalMilliseconds / reps;
                if (avg < best) best = avg;
            }
        }

        return best == double.MaxValue ? 0 : best;
    }

    private static long RandomInRange(Random rng, long maxVal)
    {
        long val = (long)(rng.NextDouble() * maxVal) + 1;
        return val < 1 ? 1 : val;
    }

    // Число шагов extgcd(a,b) ~ log(min(a,b)), поэтому аппроксимируем
    // T(n) ≈ C * ln(n).
    private void ComputeApproximation()
    {
        if (_points.Count < 2) return;

        double sumXY = 0, sumX2 = 0;
        foreach (var (n, ms) in _points)
        {
            double x = Math.Log(n);
            sumXY += x * ms;
            sumX2 += x * x;
        }
        _approxC = sumX2 > 0 ? sumXY / sumX2 : 0;

        double meanMs = _points.Average(p => p.Ms);
        double ssTot = _points.Sum(p => Math.Pow(p.Ms - meanMs, 2));
        double ssRes = _points.Sum(p => Math.Pow(p.Ms - _approxC * Math.Log(p.N), 2));
        _approxR2 = ssTot > 0 ? 1 - ssRes / ssTot : 0;

        lblApprox.Text =
            $"Аппроксимация: T(n) ≈ {_approxC * 1000:G4} мкс · ln(n), R² = {_approxR2:F4}.\r\n" +
            "Число шагов рекурсии extgcd(a,b) ~ log(min(a,b)), поэтому время растёт логарифмически от n — сложность O(log n).";
    }

    // Компактная подпись большого n: 1 200 000 -> "1,2 млн".
    private static string FormatN(double n)
    {
        if (n >= 1e12) return (n / 1e12).ToString("0.#", System.Globalization.CultureInfo.CurrentCulture) + " трлн";
        if (n >= 1e9) return (n / 1e9).ToString("0.#", System.Globalization.CultureInfo.CurrentCulture) + " млрд";
        if (n >= 1e6) return (n / 1e6).ToString("0.#", System.Globalization.CultureInfo.CurrentCulture) + " млн";
        if (n >= 1e3) return (n / 1e3).ToString("0.#", System.Globalization.CultureInfo.CurrentCulture) + " тыс";
        return n.ToString("0");
    }

    // Число знаков после запятой подбираем по масштабу, иначе соседние
    // деления при маленьком диапазоне времени округляются в одно и то же
    // число (например "0,1" трижды подряд).
    private static string FormatUs(double us)
    {
        if (us <= 0) return "0";
        if (us >= 100) return us.ToString("0");
        if (us >= 10) return us.ToString("0.0");
        if (us >= 1) return us.ToString("0.00");
        return us.ToString("0.000");
    }

    private void PanelChart_Paint(object? sender, PaintEventArgs e)
    {
        if (_points.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int padding = 55;
        int width = panelChart.Width - padding * 2;
        int height = panelChart.Height - padding * 2;
        if (width <= 0 || height <= 0) return;

        // Внутри откладываем ln(n) — n рос геометрически и на обычной линейной
        // шкале точки легли бы кучно у правого края. Но подписываем ось
        // настоящими значениями n, а не ln(n), чтобы график был понятен
        // без пересчёта в голове.
        double logMin = Math.Log(_points.Min(p => p.N));
        double logMax = Math.Log(_points.Max(p => p.N));
        double logSpan = Math.Max(1e-9, logMax - logMin);

        // Время показываем в микросекундах — на таких операциях миллисекунды
        // дают кучу нулей после запятой и выглядят пугающе.
        double maxUs = _points.Max(p => p.Ms) * 1000.0;
        if (maxUs <= 0) maxUs = 1;

        using var axisPen = new Pen(Color.Black, 2);
        g.DrawLine(axisPen, padding, padding + height, padding + width, padding + height);
        g.DrawLine(axisPen, padding, padding, padding, padding + height);

        using var smallFont = new Font("Arial", 8);
        using var labelFont = new Font("Arial", 9, FontStyle.Bold);

        g.DrawString("n (диапазон случайных a, b) — логарифмическая шкала", labelFont, Brushes.Black,
            padding + width / 2 - 140, padding + height + 25);

        for (int i = 0; i <= 6; i++)
        {
            double logN = logMin + logSpan * i / 6;
            double n = Math.Exp(logN);
            int x = padding + (int)(width * i / 6.0);
            g.DrawLine(Pens.LightGray, x, padding, x, padding + height);
            string text = FormatN(n);
            var textSize = g.MeasureString(text, smallFont);
            g.DrawString(text, smallFont, Brushes.Black, x - textSize.Width / 2, padding + height + 4);
        }

        for (int i = 0; i <= 5; i++)
        {
            double val = maxUs * i / 5;
            int y = padding + height - (int)(height * i / 5.0);
            g.DrawLine(Pens.LightGray, padding, y, padding + width, y);
            string text = FormatUs(val);
            var textSize = g.MeasureString(text, smallFont);
            g.DrawString(text, smallFont, Brushes.Black, padding - textSize.Width - 6, y - 7);
        }

        foreach (var (n, ms) in _points)
        {
            double logN = Math.Log(n);
            int x = padding + (int)((logN - logMin) * width / logSpan);
            int y = padding + height - (int)(ms * 1000.0 * height / maxUs);
            g.FillEllipse(Brushes.SteelBlue, x - 3, y - 3, 6, 6);
        }

        if (_approxC != 0)
        {
            using var approxPen = new Pen(Color.Red, 2)
                { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

            PointF? prev = null;
            for (int i = 0; i <= 100; i++)
            {
                double logN = logMin + logSpan * i / 100.0;
                double us = _approxC * logN * 1000.0;
                int x = padding + (int)(width * i / 100.0);
                int y = padding + height - (int)(us * height / maxUs);
                var pt = new PointF(x, y);
                if (prev.HasValue) g.DrawLine(approxPen, prev.Value, pt);
                prev = pt;
            }
        }

        using var titleFont = new Font("Arial", 10, FontStyle.Bold);
        g.DrawString("Время вычисления коэффициентов Безу (мкс) от n", titleFont, Brushes.Black, padding, 6);

        using var legendFont = new Font("Arial", 8);
        g.DrawString("синие точки — замер, красная пунктирная линия — аппроксимация C·ln(n)",
            legendFont, Brushes.DimGray, padding, 26);
    }
}
