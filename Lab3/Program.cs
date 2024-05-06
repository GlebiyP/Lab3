using System;
using System.Reflection.PortableExecutable;
using System.Threading;

class CoinMachine
{
    private int[] coinCounts;

    public CoinMachine(int[] initialCoinCounts)
    {
        if (initialCoinCounts.Length != 7)
            throw new ArgumentException("Початковий масив повинен мiстити 7 елементiв.");

        coinCounts = new int[7];
        Array.Copy(initialCoinCounts, coinCounts, 7);
    }

    public bool AcceptCoin(int coinValue)
    {
        // Перевірка номіналу монети
        int coinIndex = GetCoinIndex(coinValue);
        if (coinIndex != -1)
        {
            Console.WriteLine($"Прийнята монета: {coinValue} копiйок");
            return true;
        }
        else
        {
            return false;
        }
    }

    public int MakeChange(int fromCoinValue, int toCoinValue)
    {
        int fromCoinIndex = GetCoinIndex(fromCoinValue);
        int toCoinIndex = GetCoinIndex(toCoinValue);

        if (fromCoinIndex == -1 || toCoinIndex == -1)
        {
            // Невiрний номiнал монети
            return -1;
        }

        if (fromCoinValue % toCoinValue != 0)
        {
            // Неможливо здійснити обмін {fromCoinValue} копiйок на {toCoinValue} копiйок
            return -2;
        }

        int numToCoinNeeded = fromCoinValue / toCoinValue;

        if (coinCounts[toCoinIndex] < numToCoinNeeded) 
        {
            // Недостатньо монет номiналу {toCoinValue} для обмiну
            return -3;
        }

        coinCounts[toCoinIndex] -= numToCoinNeeded;
        coinCounts[fromCoinIndex]++;

        // Здiйснено обмiн монети номiналом {fromCoinValue} на {numToCoinNeeded} монет номiналу {toCoinValue}
        return numToCoinNeeded;
    }

    private int GetCoinIndex(int coinValue)
    {
        switch (coinValue)
        {
            case 1: return 0;
            case 2: return 1;
            case 5: return 2;
            case 10: return 3;
            case 25: return 4;
            case 50: return 5;
            case 100: return 6;
            default: return -1;
        }
    }
}

class Program
{
    static Mailbox<int> coinMailbox = new Mailbox<int>(); // Поштова скринька для обміну монетами
    static Mailbox<string> messageMailbox = new Mailbox<string>(); // Поштова скринька для обміну повідомленнями

    static void Main(string[] args)
    {
        // Задаємо кількість монет різного номіналу, що містить автомат
        int[] coins = { 10, 10, 10, 10, 10, 10, 0 }; // Кількість монет різного номіналу (1, 2, 5, 10, 25, 50 коп. та 1 грн)
        CoinMachine coinMachine = new CoinMachine(coins);

        // Запускаємо процеси
        Thread processAThread = new Thread(() => ProcessA(coinMachine));
        processAThread.Start();

        Thread processBThread = new Thread(() => ProcessB(coinMachine));
        processBThread.Start();
    }

    static void ProcessA(CoinMachine coinMachine)
    {
        Random rand = new Random();

        while (true)
        {
            // Генеруємо випадковий номінал монети
            int coinValue = rand.Next(1, 20) * 5;

            // Перевірка номіналу
            while (!coinMachine.AcceptCoin(coinValue))
            {
                Console.WriteLine($"Монета не була прийнята! Невiрний номiнал - {coinValue}.");
                coinValue = rand.Next(1, 20) * 5;
            };

            // Відправляємо номінал монети в поштову скриньку для процесу В
            coinMailbox.Put(coinValue);

            // Очікуємо повідомлення від процесу В про можливість обміну
            string message = messageMailbox.Take();

            // Виводимо повідомлення
            Console.WriteLine(message);

            // Пауза
            Thread.Sleep(2000);
        }
    }

    static void ProcessB(CoinMachine coinMachine)
    {
        while (true)
        {
            // Отримуємо номінал монети з поштової скриньки
            int coinValue = coinMailbox.Take();

            // Отримуємо номінал монети, на яку потрібно здійснити обмін
            Console.WriteLine("Введiть номiнал монети, на яку потрiбно здiйснити обмiн:");
            int changeCoinValue;

            // Перевіряємо та здійснюємо обмін
            if (int.TryParse(Console.ReadLine(), out changeCoinValue))
            {
                int res = coinMachine.MakeChange(coinValue, changeCoinValue);
                if(res > 0)
                {
                    messageMailbox.Put($"Здiйснено обмiн монети номiналом {coinValue} на {res} монет номiналу {changeCoinValue}.\n");
                }
                else if(res == -1)
                {
                    messageMailbox.Put("Невiрний номiнал монети!\n");
                }
                else if(res == -2)
                {
                    messageMailbox.Put($"Неможливо здiйснити обмiн {coinValue} копiйок на {changeCoinValue} копiйок!\n");
                }
                else if(res == -3)
                {
                    messageMailbox.Put($"Недостатньо монет номiналу {changeCoinValue} для обмiну!\n");
                }
            }
            else
            {
                messageMailbox.Put("Невiрний формат!\n");
            }
        }
    }
}

// Реалізація поштової скриньки
class Mailbox<T>
{
    private T message;
    private bool hasMessage = false;
    private object lockObject = new object();

    public void Put(T message)
    {
        lock (lockObject)
        {
            // Чекаємо, поки попереднє повідомлення не буде взяте
            while (hasMessage)
            {
                Monitor.Wait(lockObject);
            }

            this.message = message;
            hasMessage = true;
            Monitor.Pulse(lockObject); // Повідомляємо інші потоки, що є повідомлення
        }
    }

    public T Take()
    {
        lock(lockObject)
        {
            // Чекаємо, поки не буде відправлено нове повідомлення
            while (!hasMessage) 
            {
                Monitor.Wait(lockObject);
            }

            T recceivedMessage = message;
            hasMessage = false;
            Monitor.Pulse(lockObject); // Повідомляємо інші потоки, що повідомлення було взяте
            return recceivedMessage;
        }
    }
}