using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DAN_XXXVII_Milica_Karetic
{
    class Program
    {
        private static object locker = new object();
        static Random rnd = new Random();
        public static string fileName = "FileRoutes.txt";
        public static List<int> bestRoutes = new List<int>(10);
        public static List<Thread> trucks = new List<Thread>();
        public static SemaphoreSlim semaphore = new SemaphoreSlim(2, 2);

        public static CountdownEvent countdown = new CountdownEvent(10);

        static int restartCount = 0, enterCount = 0, count = 0;

        /// <summary>
        /// Generate 1000 random numbers and write them to file
        /// </summary>
        public static void GenerateNumbers()
        {
            int num;

            lock (fileName)
            {
                using (StreamWriter sw = File.CreateText(fileName))
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        num = rnd.Next(1, 5001);
                        sw.WriteLine(num);
                    }
                }
                Monitor.Pulse(fileName);
            }
        }

        /// <summary>
        /// Method that pick best routes for trucks from file
        /// </summary>
        public static void PickRoutes()
        {
            Console.WriteLine("Manager is waiting for routes...");
            //temporary list for all numbers from file
            List<int> tempList = new List<int>();
            lock (fileName)
            {
                Monitor.Wait(fileName, 3000);
                using (StreamReader sr = File.OpenText(fileName))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (int.Parse(line) % 3 == 0)
                            tempList.Add(int.Parse(line));
                    }
                }

                //sort list ascending
                tempList.Sort();

                //get distinct values from temporary list
                IEnumerable<int> distinctRoutes = tempList.Distinct();
                int a = 0;
                foreach (int route in distinctRoutes)
                {
                    if (a < 10)
                    {
                        //add 10 best routes to bestRoutes list (first 10 routes from sorted list)
                        bestRoutes.Add(route);
                        a++;
                    }
                    else
                        break;
                }

                Console.WriteLine("Routes picked. You can start loading. After loading you can go.");
                Console.WriteLine("Best routes:");
                for (int i = 0; i < bestRoutes.Count; i++)
                {
                    Console.Write(bestRoutes[i] + " ");
                }
                Console.WriteLine();
                Console.WriteLine();
            }

        }


        /// <summary>
        /// Method that ensure trucks to load two by two
        /// </summary>
        public static void TwoTrucksLoading()
        {
            while (true)
            {
                lock (locker)
                {
                    enterCount++;
                    if (enterCount > 2)
                        Thread.Sleep(0);
                    else
                    {
                        restartCount++;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Method for loading truck
        /// </summary>
        /// <param name="loadingTime">Loading time</param>
        public static void LoadTrucks(int loadingTime)
        {
            semaphore.Wait();

            Console.WriteLine(Thread.CurrentThread.Name + " is loading " + loadingTime + " ms.");
            Thread.Sleep(loadingTime);

            Console.WriteLine(Thread.CurrentThread.Name + " is loaded...");

            semaphore.Release();
            countdown.Signal();
        }

        /// <summary>
        /// Method for loading trucks
        /// </summary>
        /// <param name="route">Route on which truck will go</param>
        public static void Loading(object route)
        {
            //call method
            TwoTrucksLoading();

            int loadingTime = rnd.Next(500, 5001);

            LoadTrucks(loadingTime);

            restartCount--;
            if (restartCount == 0)
            {
                enterCount = 0;
            }

            //wait all trucks to load
            countdown.Wait();

            //set route for each truck
            Console.WriteLine(Thread.CurrentThread.Name + " will drive on route " + route);

            Console.WriteLine(Thread.CurrentThread.Name + "'s on his way. You can expect delivery between 500 ms and 5 sec");

            //delivery time
            int deliveryTime = rnd.Next(500, 5001);

            UnloadTrucks(loadingTime, deliveryTime);

        }

        /// <summary>
        /// Method for unloading trucks
        /// </summary>
        /// <param name="loadingTime">Loading time</param>
        /// <param name="deliveryTime">Delivery time</param>
        public static void UnloadTrucks(int loadingTime, int deliveryTime)
        {
            //if delivery time is >3000 delivery cancels and truck returns to starting point
            if (deliveryTime > 3000)
            {
                //go
                Thread.Sleep(3000);
                Console.WriteLine("Delivery canceled beacuse expected delivery time was " + deliveryTime + ". " + Thread.CurrentThread.Name + " returns to the starting point for 3000 ms.");
                //return to start point
                Thread.Sleep(3000);
                Console.WriteLine(Thread.CurrentThread.Name + " returned to the starting point.");
            }
            else
            {
                //go
                Thread.Sleep(deliveryTime);
                Console.WriteLine(Thread.CurrentThread.Name + " arrived to the destination for " + deliveryTime + " ms.");

                int unloadingTime = Convert.ToInt32(loadingTime / 1.5);
                Console.WriteLine(Thread.CurrentThread.Name + " is unloading " + unloadingTime + " ms.");
                //unloading
                Thread.Sleep(unloadingTime);

                Console.WriteLine(Thread.CurrentThread.Name + " is unloaded...");
            }
        }

        static void Main(string[] args)
        {
            Thread t1 = new Thread(GenerateNumbers)
            {
                Name = "GenerateNumbers"
            };
            Thread t2 = new Thread(PickRoutes)
            {
                Name = "PickRoutes"
            };

            //start 1. i 2. threads
            t1.Start();
            t2.Start();

            t1.Join();
            t2.Join();

            //create 10 trucks
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(Loading)
                {
                    Name = string.Format("Truck_{0}", i + 1)
                };
                trucks.Add(t);
            }
            //start all 10 threads
            for (int i = 0; i < trucks.Count; i++)
            {
                trucks[i].Start(bestRoutes[i]);
            }
            //join them
            for (int i = 0; i < trucks.Count; i++)
            {
                trucks[i].Join();
            }

            Console.ReadKey();
        }
    }
}
