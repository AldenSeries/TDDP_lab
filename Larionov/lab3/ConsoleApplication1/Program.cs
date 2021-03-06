﻿using System;
using Microsoft.Ccr.Core;
using System.Threading;

namespace ConsoleApplication3
{
    public class InputData
    {
        public int start; // начало диапазона 
        public int stop;  // конец диапазона 
        //public int i;
    }

    class Program
    {
        static int[,] A; //хранение матрицы
        static int[] B;  //хранение вектор-столбца для умножения
        static int[] C;  //хранение результата
        static int m;    //количество строк матрицы
        static int n;    //количество столбцов матрицы
        static int nc;   //количество ядер

        static void Test()
        {
            nc = 2;

            m = 15000;
            n = 15000;
            Console.WriteLine("\nРазмеры матрицы заданы автоматически и составляют {0} x {1}\n",m,n);
            

            A = new int[m, n];
            B = new int[n];
            C = new int[m];

           
                Console.WriteLine("Заполнение случайными значениями...\n");
                Random r = new Random();
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                        A[i, j] = r.Next(100);
                }
                for (int j = 0; j < n; j++)
                    B[j] = r.Next(100);

                Console.WriteLine("Исходная матрица и вектор-столбец успешно заполнены случайными значениями!\n");
        }

        static void SequentialMul()
        {
            System.Diagnostics.Stopwatch sWatch = new System.Diagnostics.Stopwatch();
            sWatch.Start();
            for (int i = 0; i < m; i++)
            {
                C[i] = 0;
                for (int j = 0; j < n; j++)
                {
                    C[i] += A[i, j] * B[j];
                }
            }
            sWatch.Stop();
            Console.WriteLine("Последовательный алгоритм = {0} мс.",
            sWatch.ElapsedMilliseconds.ToString());

        }

        static void ParallelMul()
        {
            // создание массива объектов для хранения параметров
            InputData[] ClArr = new InputData[nc];
            for (int i = 0; i < nc; i++)
                ClArr[i] = new InputData();

            //Далее, задаются исходные данные для каждого экземпляра
            //вычислительного метода:
            // делим количество строк в матрице на nc частей
            int step = (Int32)(m / nc);
            // заполняем массив параметров
            int c = -1;
            for (int i = 0; i < nc; i++)
            {
                ClArr[i].start = c + 1;
                ClArr[i].stop = c + step;
                c = c + step;
            }
            //Создаётся диспетчер с пулом из двух потоков:
            Dispatcher d = new Dispatcher(nc, "Test Pool");
            DispatcherQueue dq = new DispatcherQueue("Test Queue", d);
            //Описывается порт, в который каждый экземпляр метода Mul()
            //отправляет сообщение после завершения вычислений:
            Port<int> p = new Port<int>();
            //Метод Arbiter.Activate помещает в очередь диспетчера две задачи(два
            //экземпляра метода Mul):
            for (int i = 0; i < nc; i++)
                Arbiter.Activate(dq, new Task<InputData, Port<int>>(ClArr[i], p, Mul));
            //Первый параметр метода Arbiter.Activate – очередь диспетчера,
            //который будет управлять выполнением задачи, второй параметр –
            //запускаемая задача.

            //С помощью метода Arbiter.MultipleItemReceive запускается задача
            //(приёмник), которая обрабатывает получение двух сообщений портом p:
            Arbiter.Activate(dq, Arbiter.MultipleItemReceive(true, p, nc, delegate (int[] array)
            {
                Console.WriteLine("Вычисления завершены");
                Console.ReadKey(true);
                Environment.Exit(0);
            }));
        }


        static void Mul(InputData data, Port<int> resp)
        {
            System.Diagnostics.Stopwatch sWatch = new System.Diagnostics.Stopwatch();
            sWatch.Start();

            for (int i = data.start; i < data.stop; i++)
            {
                C[i] = 0;
                for (int j = 0; j < n; j++)
                    C[i] += A[i, j] * B[j];
            }
            sWatch.Stop();
            Console.WriteLine("Поток № {0}: Паралл. алгоритм = {1} мс.",
           Thread.CurrentThread.ManagedThreadId,
           sWatch.ElapsedMilliseconds.ToString());
            resp.Post(1);
        }

       

        static void Main(string[] args)
        {
            Test();
            SequentialMul();
            ParallelMul();
        }
    }
}

       