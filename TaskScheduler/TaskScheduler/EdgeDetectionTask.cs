using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class EdgeDetectionTask : MyTask
    {

        private static int[,] edgeDetectionKernel = new int[,] { { 0, -1, 0 }, { -1, 4, -1 }, { 0, -1, 0 } };

        private int noRows = 0;

        public EdgeDetectionTask(DateTime deadline, double maxExecTime, int maxDegreeOfParalellism, ControlToken? token, TaskPriority priority, params Resource[] resources) 
            : base(null, deadline, maxExecTime, maxDegreeOfParalellism, token, priority, resources)
        {
            if (resources.Length == 0)
                throw new ArgumentException("At least one resource image must be specified.");
            Action = this.EdgeDetection;
        }

        private void EdgeDetection()
        {
            if (_resources.Count == 1)
            {
                Bitmap? newImage = EdgeDetectionAlgorithm((Bitmap)Bitmap.FromFile(_resources.ElementAt(0).Path), MaxDegreeOfParalellism);
                newImage?.Save("test" + new Random().Next() + ".jfif");
            }
            else
            {
                Parallel.For(0, _resources.Count, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParalellism }, i =>
                 {
                     Bitmap? newImage = EdgeDetectionAlgorithm((Bitmap)Bitmap.FromFile(_resources.ElementAt(i).Path), 1);
                     newImage?.Save("test" + i + ".jfif");
                 });
            }
        }

        private Bitmap? EdgeDetectionAlgorithm(Bitmap clone, int degree)
        {
            Bitmap newImg = (Bitmap)clone.Clone();

            //for (int y = 0; y < b.Height - 2; y++)
            Parallel.For(0, clone.Height - 2, new ParallelOptions { MaxDegreeOfParallelism = degree }, i =>
            {
                //Check for Pause/Terminate
                if (_controlToken != null && _controlToken.Terminated)
                    return;
                if (_controlToken != null && _controlToken.Paused)
                {
                    lock (_controlToken.Lock)
                        Monitor.Wait(_controlToken.Lock);
                }
                Color[,] pixelColor = new Color[3, 3];
                int y = i;
                int A, R, G, B;
                Bitmap b;
                lock (clone)
                    b = (Bitmap)clone.Clone();
                for (int x = 0; x < b.Width - 2; x++)
                {
                    pixelColor[0, 0] = b.GetPixel(x, y);
                    pixelColor[0, 1] = b.GetPixel(x, y + 1);
                    pixelColor[0, 2] = b.GetPixel(x, y + 2);
                    pixelColor[1, 0] = b.GetPixel(x + 1, y);
                    pixelColor[1, 1] = b.GetPixel(x + 1, y + 1);
                    pixelColor[1, 2] = b.GetPixel(x + 1, y + 2);
                    pixelColor[2, 0] = b.GetPixel(x + 2, y);
                    pixelColor[2, 1] = b.GetPixel(x + 2, y + 1);
                    pixelColor[2, 2] = b.GetPixel(x + 2, y + 2);

                    A = pixelColor[1, 1].A;

                    R = (int)(pixelColor[0, 0].R * edgeDetectionKernel[0, 0]) +
                                 (pixelColor[1, 0].R * edgeDetectionKernel[1, 0]) +
                                 (pixelColor[2, 0].R * edgeDetectionKernel[2, 0]) +
                                 (pixelColor[0, 1].R * edgeDetectionKernel[0, 1]) +
                                 (pixelColor[1, 1].R * edgeDetectionKernel[1, 1]) +
                                 (pixelColor[2, 1].R * edgeDetectionKernel[2, 1]) +
                                 (pixelColor[0, 2].R * edgeDetectionKernel[0, 2]) +
                                 (pixelColor[1, 2].R * edgeDetectionKernel[1, 2]) +
                                 (pixelColor[2, 2].R * edgeDetectionKernel[2, 2]);

                    R = R < 0 ? 0 : R > 255 ? 255 : R;

                    G = (int)(pixelColor[0, 0].G * edgeDetectionKernel[0, 0]) +
                                 (pixelColor[1, 0].G * edgeDetectionKernel[1, 0]) +
                                 (pixelColor[2, 0].G * edgeDetectionKernel[2, 0]) +
                                 (pixelColor[0, 1].G * edgeDetectionKernel[0, 1]) +
                                 (pixelColor[1, 1].G * edgeDetectionKernel[1, 1]) +
                                 (pixelColor[2, 1].G * edgeDetectionKernel[2, 1]) +
                                 (pixelColor[0, 2].G * edgeDetectionKernel[0, 2]) +
                                 (pixelColor[1, 2].G * edgeDetectionKernel[1, 2]) +
                                 (pixelColor[2, 2].G * edgeDetectionKernel[2, 2]);

                    G = G < 0 ? 0 : G > 255 ? 255 : G;

                    B = (int)(pixelColor[0, 0].B * edgeDetectionKernel[0, 0]) +
                                 (pixelColor[1, 0].B * edgeDetectionKernel[1, 0]) +
                                 (pixelColor[2, 0].B * edgeDetectionKernel[2, 0]) +
                                 (pixelColor[0, 1].B * edgeDetectionKernel[0, 1]) +
                                 (pixelColor[1, 1].B * edgeDetectionKernel[1, 1]) +
                                 (pixelColor[2, 1].B * edgeDetectionKernel[2, 1]) +
                                 (pixelColor[0, 2].B * edgeDetectionKernel[0, 2]) +
                                 (pixelColor[1, 2].B * edgeDetectionKernel[1, 2]) +
                                 (pixelColor[2, 2].B * edgeDetectionKernel[2, 2]);

                    B = B < 0 ? 0 : B > 255 ? 255 : B;
                    lock (newImg)
                        newImg.SetPixel(x + 1, y + 1, Color.FromArgb(A, R, G, B));
                }
                lock (this)
                {
                    noRows++;
                    Progress = (double)noRows / b.Height;
                }
                Action2.Invoke();
            });
            
            return ControlToken != null ? ControlToken.Terminated ? null : newImg : newImg;
        }

        private Bitmap EdgeDetectionAlgorithm2(Bitmap clone)
        {
            Bitmap newImg = (Bitmap)clone.Clone();
            int chunk = clone.Height / MaxDegreeOfParalellism;
            int Y = 0;
            Thread[] threads = new Thread[MaxDegreeOfParalellism];
            for (int i = 0; i < MaxDegreeOfParalellism; i++) 
            //Parallel.For(0, clone.Height - 2, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParalellism }, i =>
            {
                int chunkCopy = chunk;
                int y = Y;
                //Check for Pause/Terminate
                threads[i] = new Thread(() =>
                {
                    Console.WriteLine(chunkCopy);
                    for (y = Y; y < Y + chunkCopy; y++)
                    {
                        //Console.WriteLine(y);
                        if (_controlToken != null && _controlToken.Terminated)
                            return;
                        if (_controlToken != null && _controlToken.Paused)
                        {
                            Console.WriteLine("Paused " + Thread.CurrentThread.ManagedThreadId);
                            lock (_controlToken.Lock)
                                Monitor.Wait(_controlToken.Lock);
                            Console.WriteLine("Cont " + Thread.CurrentThread.ManagedThreadId);
                        }
                        Color[,] pixelColor = new Color[3, 3];
                        int A, R, G, B;
                        Bitmap b;
                        lock (clone)
                            b = (Bitmap)clone.Clone();
                        for (int x = 0; x < b.Width - 2; x++)
                        {
                            pixelColor[0, 0] = b.GetPixel(x, y);
                            pixelColor[0, 1] = b.GetPixel(x, y + 1);
                            pixelColor[0, 2] = b.GetPixel(x, y + 2);
                            pixelColor[1, 0] = b.GetPixel(x + 1, y);
                            pixelColor[1, 1] = b.GetPixel(x + 1, y + 1);
                            pixelColor[1, 2] = b.GetPixel(x + 1, y + 2);
                            pixelColor[2, 0] = b.GetPixel(x + 2, y);
                            pixelColor[2, 1] = b.GetPixel(x + 2, y + 1);
                            pixelColor[2, 2] = b.GetPixel(x + 2, y + 2);

                            A = pixelColor[1, 1].A;

                            R = (int)(pixelColor[0, 0].R * edgeDetectionKernel[0, 0]) +
                                         (pixelColor[1, 0].R * edgeDetectionKernel[1, 0]) +
                                         (pixelColor[2, 0].R * edgeDetectionKernel[2, 0]) +
                                         (pixelColor[0, 1].R * edgeDetectionKernel[0, 1]) +
                                         (pixelColor[1, 1].R * edgeDetectionKernel[1, 1]) +
                                         (pixelColor[2, 1].R * edgeDetectionKernel[2, 1]) +
                                         (pixelColor[0, 2].R * edgeDetectionKernel[0, 2]) +
                                         (pixelColor[1, 2].R * edgeDetectionKernel[1, 2]) +
                                         (pixelColor[2, 2].R * edgeDetectionKernel[2, 2]);

                            /*if (R < 0)
                            {
                                R = 0;
                            }
                            else if (R > 255)
                            {
                                R = 255;
                            }*/
                            R = R < 0 ? 0 : R > 255 ? 255 : R;

                            G = (int)(pixelColor[0, 0].G * edgeDetectionKernel[0, 0]) +
                                         (pixelColor[1, 0].G * edgeDetectionKernel[1, 0]) +
                                         (pixelColor[2, 0].G * edgeDetectionKernel[2, 0]) +
                                         (pixelColor[0, 1].G * edgeDetectionKernel[0, 1]) +
                                         (pixelColor[1, 1].G * edgeDetectionKernel[1, 1]) +
                                         (pixelColor[2, 1].G * edgeDetectionKernel[2, 1]) +
                                         (pixelColor[0, 2].G * edgeDetectionKernel[0, 2]) +
                                         (pixelColor[1, 2].G * edgeDetectionKernel[1, 2]) +
                                         (pixelColor[2, 2].G * edgeDetectionKernel[2, 2]);

                            /*if (G < 0)
                            {
                                G = 0;
                            }
                            else if (G > 255)
                            {
                                G = 255;
                            }*/
                            G = G < 0 ? 0 : G > 255 ? 255 : G;

                            B = (int)(pixelColor[0, 0].B * edgeDetectionKernel[0, 0]) +
                                         (pixelColor[1, 0].B * edgeDetectionKernel[1, 0]) +
                                         (pixelColor[2, 0].B * edgeDetectionKernel[2, 0]) +
                                         (pixelColor[0, 1].B * edgeDetectionKernel[0, 1]) +
                                         (pixelColor[1, 1].B * edgeDetectionKernel[1, 1]) +
                                         (pixelColor[2, 1].B * edgeDetectionKernel[2, 1]) +
                                         (pixelColor[0, 2].B * edgeDetectionKernel[0, 2]) +
                                         (pixelColor[1, 2].B * edgeDetectionKernel[1, 2]) +
                                         (pixelColor[2, 2].B * edgeDetectionKernel[2, 2]);

                            /*if (B < 0)
                            {
                                B = 0;
                            }
                            else if (B > 255)
                            {
                                B = 255;
                            }*/
                            B = B < 0 ? 0 : B > 255 ? 255 : B;
                            lock (newImg)
                                newImg.SetPixel(x + 1, y + 1, Color.FromArgb(A, R, G, B));
                        }
                    }
                });
                Y += chunk;
                if (i == MaxDegreeOfParalellism - 2)
                    chunk = MaxDegreeOfParalellism - Y;
            }
            foreach (Thread t in threads)
                t.Start();
            foreach (Thread t in threads)
                t.Join();
            return newImg;
        }
    }
}
