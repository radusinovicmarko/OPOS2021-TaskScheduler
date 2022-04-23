using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace TaskScheduler
{
    [Serializable]
    public class EdgeDetectionTask : MyTask
    {

        private static readonly int[,] edgeDetectionKernel = new int[,] { { 0, -1, 0 }, { -1, 4, -1 }, { 0, -1, 0 } };

        private int noRows = 0;

        private int maxHeight = 0;

        private FolderResource outputFolder;

        public EdgeDetectionTask() 
        {
            Action = this.EdgeDetection;
            outputFolder = new FolderResource(".");
        }

        public EdgeDetectionTask(DateTime deadline, double maxExecTime, int maxDegreeOfParalellism, ControlToken? token, ControlToken? userToken, TaskPriority priority, FolderResource output, params Resource[] resources) 
            : base(null, deadline, maxExecTime, maxDegreeOfParalellism, token, userToken, priority, resources)
        {
            if (resources.Length == 0)
                throw new ArgumentException("At least one resource image must be specified.");
            outputFolder = output;
            Action = this.EdgeDetection;
        }

        private void EdgeDetection()
        {
            if (_resources.Count == 1)
            {
                string resourcePath = ((FileResource)_resources.ElementAt(0)).Path;
                Bitmap originalImage = (Bitmap)Bitmap.FromFile(resourcePath);
                maxHeight = originalImage.Height;
                Bitmap? newImage = EdgeDetectionAlgorithm(originalImage, MaxDegreeOfParalellism);
                string resName = resourcePath.Substring(resourcePath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                string outputPath = Path.Combine(outputFolder.Path, "EdgeDetection_" + resName);
                newImage?.Save(outputPath);
                _resourcesProcessed[0] = true;
            }
            else
            {
                foreach (var resource in _resources)
                    maxHeight += ((Bitmap)Bitmap.FromFile(((FileResource)resource).Path)).Height;
                Parallel.For(0, _resources.Count, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParalellism }, i =>
                 {
                     if (!_resourcesProcessed[i])
                     {
                         string resourcePath = ((FileResource)_resources.ElementAt(i)).Path;
                         Bitmap? newImage = EdgeDetectionAlgorithm((Bitmap)Bitmap.FromFile(resourcePath), 1);
                         string resName = resourcePath.Substring(resourcePath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                         string outputPath = Path.Combine(outputFolder.Path, "EdgeDetection_" + resName);
                         newImage?.Save(outputPath);
                         _resourcesProcessed[i] = true;
                     }
                 });
            }
        }

        public override void Execute()
        {
            if (_resources != null)
                foreach (Resource r in _resources)
                    r.Lock(this);
            Action.Invoke();
            if (_resources != null)
                foreach (Resource r in _resources)
                    r.Unlock();
            FinishedTaskCallback?.Invoke();
        }

        private Bitmap? EdgeDetectionAlgorithm(Bitmap clone, int degree)
        {
            Bitmap newImg = (Bitmap)clone.Clone();

            //for (int y = 0; y < b.Height - 2; y++)
            Parallel.For(0, clone.Height - 2, new ParallelOptions { MaxDegreeOfParallelism = degree }, i =>
            {
                //Check for Pause/Terminate
                if ((_controlToken != null && _controlToken.Terminated) || (_userControlToken != null && _userControlToken.Terminated)) 
                    return;
                if (_controlToken != null && _controlToken.Paused)
                {
                    lock (_controlToken.Lock)
                        Monitor.Wait(_controlToken.Lock);
                }
                if (_userControlToken != null && _userControlToken.Paused)
                {
                    lock (_userControlToken.Lock)
                        Monitor.Wait(_userControlToken.Lock);
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
                    _progress = (double)noRows / maxHeight;
                }
                if (_progressBarUpdateAction != null)
                    _progressBarUpdateAction.Invoke();
            });
            
            return ControlToken != null ? ControlToken.Terminated ? null : newImg : newImg;
        }

        private Bitmap? UnsafeEdgeDetectionAlgorithm(Bitmap clone, int degree)
        {
            Bitmap newImg = (Bitmap)clone.Clone();
            BitmapData bmd = newImg.LockBits(new Rectangle(0, 0, newImg.Width, newImg.Height),ImageLockMode.ReadWrite, newImg.PixelFormat);
            int PixelSize = 4;

            unsafe
            {
                for (int y = 0; y < bmd.Height - 2; y++)
                {
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);
                    byte* row1 = (byte*)bmd.Scan0 + ((y + 1) * bmd.Stride);
                    byte* row2 = (byte*)bmd.Scan0 + ((y + 2) * bmd.Stride);
                    Color[,] pixelColor = new Color[3, 3];
                    //int y = i;
                    int A, R, G, B;
                    Bitmap b;
                    for (int x = 0; x < bmd.Width - 2; x++)
                    {

                        /*B = row[x * PixelSize] = 0;   //Blue  0-255
                        G = row[x * PixelSize + 1] = 255; //Green 0-255
                        R = row[x * PixelSize + 2] = 0;   //Red   0-255*/
                        A = row[x * PixelSize + 3] = 50;  //Alpha 0-255

                        /* pixelColor[0, 0] = b.GetPixel(x, y);
                    pixelColor[0, 1] = b.GetPixel(x, y + 1);
                    pixelColor[0, 2] = b.GetPixel(x, y + 2);
                    pixelColor[1, 0] = b.GetPixel(x + 1, y);
                    pixelColor[1, 1] = b.GetPixel(x + 1, y + 1);
                    pixelColor[1, 2] = b.GetPixel(x + 1, y + 2);
                    pixelColor[2, 0] = b.GetPixel(x + 2, y);
                    pixelColor[2, 1] = b.GetPixel(x + 2, y + 1);
                    pixelColor[2, 2] = b.GetPixel(x + 2, y + 2);*/
                        R = (int)(row[x * PixelSize + 1] * edgeDetectionKernel[0, 0]) +
                                 (row[(x + 1) * PixelSize + 1] * edgeDetectionKernel[1, 0]) +
                                 (row[(x + 2) * PixelSize + 1] * edgeDetectionKernel[2, 0]) +
                                 (row1[x * PixelSize + 1] * edgeDetectionKernel[0, 1]) +
                                 (row1[(x + 1) * PixelSize + 1] * edgeDetectionKernel[1, 1]) +
                                 (row1[(x + 2) * PixelSize + 1] * edgeDetectionKernel[2, 1]) +
                                 (row2[x * PixelSize + 1] * edgeDetectionKernel[0, 2]) +
                                 (row2[(x + 1) * PixelSize + 1]  * edgeDetectionKernel[1, 2]) +
                                 (row2[(x + 2) * PixelSize + 1] * edgeDetectionKernel[2, 2]);

                        R = R < 0 ? 0 : R > 255 ? 255 : R;

                        G = (int)(row[x * PixelSize + 2] * edgeDetectionKernel[0, 0]) +
                                 (row[(x + 1) * PixelSize + 2] * edgeDetectionKernel[1, 0]) +
                                 (row[(x + 2) * PixelSize + 2] * edgeDetectionKernel[2, 0]) +
                                 (row1[x * PixelSize + 2] * edgeDetectionKernel[0, 1]) +
                                 (row1[(x + 1) * PixelSize + 2] * edgeDetectionKernel[1, 1]) +
                                 (row1[(x + 2) * PixelSize + 2] * edgeDetectionKernel[2, 1]) +
                                 (row2[x * PixelSize + 2] * edgeDetectionKernel[0, 2]) +
                                 (row2[(x + 1) * PixelSize + 2] * edgeDetectionKernel[1, 2]) +
                                 (row2[(x + 2) * PixelSize + 2] * edgeDetectionKernel[2, 2]);

                        G = G < 0 ? 0 : G > 255 ? 255 : G;

                        B = (int)(row[x * PixelSize + 3] * edgeDetectionKernel[0, 0]) +
                                 (row[(x + 1) * PixelSize + 3] * edgeDetectionKernel[1, 0]) +
                                 (row[(x + 2) * PixelSize + 3] * edgeDetectionKernel[2, 0]) +
                                 (row1[x * PixelSize + 3] * edgeDetectionKernel[0, 1]) +
                                 (row1[(x + 1) * PixelSize + 3] * edgeDetectionKernel[1, 1]) +
                                 (row1[(x + 2) * PixelSize + 3] * edgeDetectionKernel[2, 1]) +
                                 (row2[x * PixelSize + 3] * edgeDetectionKernel[0, 2]) +
                                 (row2[(x + 1) * PixelSize + 3] * edgeDetectionKernel[1, 2]) +
                                 (row2[(x + 2) * PixelSize + 3] * edgeDetectionKernel[2, 2]);
                        B = B < 0 ? 0 : B > 255 ? 255 : B;

                        row1[(x + 1) * PixelSize] = (byte)B;
                        row1[(x + 1) * PixelSize + 1] = (byte)G;
                        row1[(x + 1) * PixelSize + 2] = (byte)R;
                        row1[(x + 1) * PixelSize + 3] = (byte)A;
                    }
                }
            }

            newImg.UnlockBits(bmd);
            return newImg;
            //bmp.Save("test.png", ImageFormat.Png);
        }

        public override void Serialize(string folderPath)
        {
            string fileName = folderPath + Path.DirectorySeparatorChar + this.GetType().Name + "_" + DateTime.Now.Ticks + ".bin";

            //XmlSerializer serializer = new XmlSerializer(typeof(EdgeDetectionTask));
            //using StreamWriter writer = new StreamWriter(fileName);
            //serializer.Serialize(writer, this);

            //string jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            //File.WriteAllText(fileName, jsonString);

            
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
        }

        public static new EdgeDetectionTask? Deserialize(string fileName)
        {
            //XmlSerializer serializer = new XmlSerializer(typeof(EdgeDetectionTask));
            //using FileStream stream = new FileStream(fileName, FileMode.Open);
            //return (EdgeDetectionTask)serializer.Deserialize(stream);

            //string jsonString = File.ReadAllText(fileName);
            //return JsonSerializer.Deserialize<EdgeDetectionTask>(jsonString);

            
            IFormatter formatter = new BinaryFormatter();
            using Stream stream = new FileStream(fileName, FileMode.Open);
            EdgeDetectionTask task = (EdgeDetectionTask)formatter.Deserialize(stream);
            task.Action = task.EdgeDetection;
            return task;
        }

        /*private Bitmap EdgeDetectionAlgorithm2(Bitmap clone)
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
                            /*R = R < 0 ? 0 : R > 255 ? 255 : R;

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
                            /*G = G < 0 ? 0 : G > 255 ? 255 : G;

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
                            /*B = B < 0 ? 0 : B > 255 ? 255 : B;
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
        }*/
    }
}
