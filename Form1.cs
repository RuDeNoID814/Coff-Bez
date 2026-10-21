using System.Diagnostics;

namespace BezoutCoefficients;

public partial class Form1 : Form
{
    private readonly List<(long N, double T)> _points = new();
    private double _approxC;
    private double _approxR2;

    private const double MinMeasureMs = 2.0;
    private const int MaxRepeatsPerPoint = 200_000;

    public Form1()
    {
        InitializeComponent();
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
        long step = (long)numStep.Value;
        int runs = (int)numRuns.Value;

        var sizes = new List<long>();
        for (long n = step; n <= maxN; n += step) sizes.Add(n);
        if (sizes.Count == 0 || sizes[^1] != maxN) sizes.Add(maxN);

        progressBar1.Minimum = 0;
        progressBar1.Maximum = sizes.Count;
        progressBar1.Value = 0;

        await Task.Run(() =>
        {
            var rng = new Random();

            foreach (var n in sizes)
            {
                double total = 0;
                for (int r = 0; r < runs; r++)
                    total += MeasureOnce(n, rng);
                double avg = total / runs;

                // _points меняем только на UI-потоке — иначе Paint-обработчик
                // панели (тоже на UI-потоке) может итерироваться по списку
                // в момент, когда фоновый поток в него пишет.
                Invoke(() =>
                {
                    _points.Add((n, avg));
                    progressBar1.Value = Math.Min(progressBar1.Value + 1, progressBar1.Maximum);
                    panelChart.Invalidate();
                });
            }
        });

        ComputeApproximation();
        btnRunExperiment.Enabled = true;
        panelChart.Invalidate();
    }

    // Замер как в лабе 1: один вызов extgcd слишком быстрый для прямого
    // измерения таймером, поэтому повторяем вызов, пока не наберётся
    // хотя бы MinMeasureMs суммарного времени, и делим на число повторов.
    private static double MeasureOnce(long maxVal, Random rng)
    {
        var sw = Stopwatch.StartNew();
        int reps = 0;
        while (reps < MaxRepeatsPerPoint && sw.Elapsed.TotalMilliseconds < MinMeasureMs)
        {
            long a = (long)(rng.NextDouble() * maxVal) + 1;
            long b = (long)(rng.NextDouble() * maxVal) + 1;
            BezoutMath.ExtendedGcd(a, b);
            reps++;
        }
        sw.Stop();
        return reps > 0 ? sw.Elapsed.TotalMilliseconds / reps : 0;
    }

    // Число шагов рекурсии extgcd ~ log(min(a,b)), поэтому аппроксимируем
    // через T(n) ≈ C * ln(n), а не через линейную/квадратичную модель.
    private void ComputeApproximation()
    {
        if (_points.Count < 2) return;

        double sumXY = 0, sumX2 = 0;
        foreach (var (n, t) in _points)
        {
            double x = Math.Log(n);
            sumXY += x * t;
            sumX2 += x * x;
        }
        _approxC = sumX2 > 0 ? sumXY / sumX2 : 0;

        double meanT = _points.Average(p => p.T);
        double ssTot = _points.Sum(p => Math.Pow(p.T - meanT, 2));
        double ssRes = _points.Sum(p => Math.Pow(p.T - _approxC * Math.Log(p.N), 2));
        _approxR2 = ssTot > 0 ? 1 - ssRes / ssTot : 0;

        lblApprox.Text =
            $"Аппроксимация: T(n) ≈ {_approxC:G4} · ln(n), R² = {_approxR2:F4}.\r\n" +
            "Число шагов рекурсии extgcd(a,b) ~ log(min(a,b)), поэтому время растёт логарифмически от n — сложность O(log n).";
    }

    private void PanelChart_Paint(object? sender, PaintEventArgs e)
    {
        if (_points.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int padding = 50;
        int width = panelChart.Width - padding * 2;
        int height = panelChart.Height - padding * 2;
        if (width <= 0 || height <= 0) return;

        long maxN = _points.Max(p => p.N);
        long minN = _points.Min(p => p.N);
        double maxT = _points.Max(p => p.T);
        if (maxT <= 0) maxT = 1;
        double nSpan = Math.Max(1.0, maxN - minN);

        using var axisPen = new Pen(Color.Black, 2);
        g.DrawLine(axisPen, padding, padding + height, padding + width, padding + height);
        g.DrawLine(axisPen, padding, padding, padding, padding + height);

        using var smallFont = new Font("Arial", 7);
        using var labelFont = new Font("Arial", 9, FontStyle.Bold);

        g.DrawString("n (диапазон случайных a, b)", labelFont, Brushes.Black,
            padding + width / 2 - 70, padding + height + 25);

        long xStep = Math.Max(1, (maxN - minN) / 6);
        for (long n = minN; n <= maxN; n += xStep)
        {
            int x = padding + (int)((n - minN) * width / nSpan);
            g.DrawLine(Pens.LightGray, x, padding, x, padding + height);
            g.DrawString(n.ToString("N0"), smallFont, Brushes.Black, x - 15, padding + height + 3);
        }

        for (int i = 0; i <= 5; i++)
        {
            double val = maxT * i / 5;
            int y = padding + height - (int)(height * i / 5.0);
            g.DrawLine(Pens.LightGray, padding, y, padding + width, y);
            g.DrawString(val.ToString("F4"), smallFont, Brushes.Black, padding - 45, y - 6);
        }

        foreach (var (n, t) in _points)
        {
            int x = padding + (int)((n - minN) * width / nSpan);
            int y = padding + height - (int)(t * height / maxT);
            g.FillEllipse(Brushes.SteelBlue, x - 3, y - 3, 6, 6);
        }

        if (_approxC != 0)
        {
            using var approxPen = new Pen(Color.Red, 2)
                { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

            PointF? prev = null;
            long sampleStep = Math.Max(1, (maxN - minN) / 100);
            for (long n = Math.Max(1, minN); n <= maxN; n += sampleStep)
            {
                double t = _approxC * Math.Log(n);
                int x = padding + (int)((n - minN) * width / nSpan);
                int y = padding + height - (int)(t * height / maxT);
                var pt = new PointF(x, y);
                if (prev.HasValue) g.DrawLine(approxPen, prev.Value, pt);
                prev = pt;
            }
        }

        g.DrawString("Замер времени extgcd(a, b) от n", labelFont, Brushes.Black, padding, 10);
    }
}
