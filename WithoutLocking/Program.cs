using System;
using System.Globalization;
using System.Threading;

public class Cells0
{
    static int[] cells;        // кристал
    static int N;              // кількість клітинок
    static int K;              // кількість атомів
    static double p;           // поріг імовірності
    static Thread[] atomThreads;
    static volatile bool running = true;

    // Окремий Random для кожного потоку
    static ThreadLocal<Random> rnd =
        new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

    // Метод з умови — повертає кількість атомів у клітинці i
    public static int GetCell(int i) => cells[i];

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
                    // БЕЗ БУДЬ-ЯКИХ БЛОКУВАНЬ
                    cells[Position]--;
                    cells[newPos]++;
                    Position = newPos;
                }

                // Щоб не крутитися в циклі
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
        cells[0] = K; // всі атоми в лівій клітинці

        atomThreads = new Thread[K];

        // Створюємо та запускаємо K потоків
        for (int i = 0; i < K; i++)
        {
            Atom atom = new Atom { Position = 0 };
            Thread t = new Thread(atom.Run);
            atomThreads[i] = t;
            t.Start();
        }

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

        int total = 0;
        for (int i = 0; i < N; i++)
            total += cells[i];

        Console.WriteLine($"[Cells0] Total atoms (expected {K}, but race conditions may break this): {total}");
    }

    static void PrintSnapshot(int second)
    {
        Console.Write($"t={second,2}s: ");
        for (int i = 0; i < N; i++)
        {
            Console.Write(cells[i] + " ");
        }
        Console.WriteLine();
    }
}
