using System;
using System.Globalization;
using System.Threading;
using System.Diagnostics;

public class Cells1
{
    static int[] cells;
    static int N;
    static int K;
    static double p;
    static Thread[] atomThreads;
    static volatile bool running = true;

    static ThreadLocal<Random> rnd =
        new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

    // false  => блокуємо весь масив (груба гранулярність)
    // true   => блокуємо окремі клітинки (тонка гранулярність)
    static bool useFineGrainedLocking = false;

    static readonly object globalLock = new object();
    static object[] cellLocks;

    public static int GetCell(int i)
    {
        return cells[i];
    }

    class Atom
    {
        public int Position;

        public void Run()
        {
            while (running)
            {
                double m = rnd.Value.NextDouble();
                int newPos = Position + (m > p ? 1 : -1);

                if (newPos < 0 || newPos >= N)
                {
                    // Атом залишається на місці
                }
                else
                {
                    if (!useFineGrainedLocking)
                    {
                        // Груба гранулярність: один замок на весь масив
                        lock (globalLock)
                        {
                            cells[Position]--;
                            cells[newPos]++;
                            Position = newPos;
                        }
                    }
                    else
                    {
                        // Тонка гранулярність: окремі замки на кожну клітинку
                        int from = Position;
                        int to = newPos;
                        int first = Math.Min(from, to);
                        int second = Math.Max(from, to);

                        // Завжди блокуємо в одному й тому самому порядку без дедлоку
                        lock (cellLocks[first])
                        {
                            lock (cellLocks[second])
                            {
                                cells[from]--;
                                cells[to]++;
                                Position = newPos;
                            }
                        }
                    }
                }

                Thread.Sleep(1);
            }
        }
    }

    public static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: dotenv run -- N K p");
            Console.WriteLine("Example: dotenv run -- 50 100 0.5");
            return;
        }

        N = int.Parse(args[0]);
        K = int.Parse(args[1]);
        p = double.Parse(args[2], CultureInfo.InvariantCulture);

        cells = new int[N];
        cells[0] = K;

        if (useFineGrainedLocking)
        {
            cellLocks = new object[N];
            for (int i = 0; i < N; i++)
                cellLocks[i] = new object();
        }

        atomThreads = new Thread[K];

        for (int i = 0; i < K; i++)
        {
            Atom atom = new Atom { Position = 0 };
            Thread t = new Thread(atom.Run);
            atomThreads[i] = t;
            t.Start();
        }

        var sw = Stopwatch.StartNew();

        int simulationSeconds = 60;
        for (int sec = 1; sec <= simulationSeconds; sec++)
        {
            PrintSnapshot(sec);
            Thread.Sleep(1000);
        }

        running = false;
        foreach (var t in atomThreads)
        {
            t.Join();
        }

        sw.Stop();

        int total = 0;
        for (int i = 0; i < N; i++)
            total += cells[i];

        Console.WriteLine($"[Cells1] Total atoms (must be {K}): {total}");
        Console.WriteLine($"Elapsed time: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Lock granularity: {(useFineGrainedLocking ? "per-cell (fine)" : "global (coarse)")}");
    }

    static void PrintSnapshot(int second)
    {
        Console.Write($"t={second,2}s: ");
        if (!useFineGrainedLocking)
        {
            lock (globalLock)
            {
                for (int i = 0; i < N; i++)
                    Console.Write(cells[i] + " ");
            }
        }
        else
        {
            for (int i = 0; i < N; i++)
                Console.Write(cells[i] + " ");
        }

        Console.WriteLine();
    }
}
