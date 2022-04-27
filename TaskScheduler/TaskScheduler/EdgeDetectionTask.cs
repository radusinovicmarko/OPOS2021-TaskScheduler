using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TaskScheduler
{
    [Serializable]
    public class EdgeDetectionTask : MyTask
    {

        private static readonly int[,] edgeDetectionKernel = new int[,] { { 0, -1, 0 }, { -1, 4, -1 }, { 0, -1, 0 } };

        private int noRows = 0;

        private int maxHeight = 0;

        private readonly string outputFolder;

        public EdgeDetectionTask()
        {
            Action = this.EdgeDetection;
            outputFolder = ".";
        }

        public EdgeDetectionTask(string id, DateTime deadline, double maxExecTime, int maxDegreeOfParalellism, ControlToken? token, ControlToken? userToken, TaskPriority priority, string output, params Resource[] resources)
            : base(null, id, deadline, maxExecTime, maxDegreeOfParalellism, token, userToken, priority, resources)
        {
            if (resources.Length == 0)
                throw new ArgumentException("At least one resource image must be specified.");
            if (maxExecTime < 0 || maxDegreeOfParalellism <= 0)
                throw new ArgumentException("Invalid arguments.");
            outputFolder = output;
            Action = this.EdgeDetection;
        }

        private void EdgeDetection()
        {
            maxHeight = 0;
            noRows = 0;
            if (_resources.Count == 1)
            {
                _progress = 0;
                Stream stream = _resources[0].GetData();
                Bitmap originalImage = new(stream);
                maxHeight = originalImage.Height;
                Bitmap? newImage = UnsafeEdgeDetectionAlgorithm(originalImage, MaxDegreeOfParalellism);
                string outputPath = Path.Combine(outputFolder, "EdgeDetection_" + new Random().Next() + ".jpg");
                if (newImage != null)
                    newImage.Save(outputPath, ImageFormat.Jpeg);
                _resourcesProcessed[0] = true;
                stream.Close();
            }
            else
            {
                foreach (var resource in _resources)
                {
                    Stream stream = resource.GetData();
                    maxHeight += new Bitmap(stream).Height;
                    stream.Close();
                }
                for (int i = 0; i < _resourcesProcessed.Count; i++)
                    if (_resourcesProcessed[i])
                    {
                        Stream stream = _resources[i].GetData();
                        int height = new Bitmap(stream).Height;
                        stream.Close();
                        noRows += height;
                    }
                _progress = (double)noRows / maxHeight;
                Parallel.For(0, _resources.Count, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParalellism }, i =>
                 {
                     if (!_resourcesProcessed[i])
                     {
                         Stream stream = _resources[i].GetData();
                         Bitmap? newImage = UnsafeEdgeDetectionAlgorithm(new Bitmap(stream), 1);
                         string outputPath = Path.Combine(outputFolder, "EdgeDetection_" + new Random().Next() + ".jpg");
                         if (newImage != null)
                             newImage.Save(outputPath, ImageFormat.Jpeg);
                         _resourcesProcessed[i] = true;
                         stream.Close();
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
            dateTimeFinished = DateTime.Now;
            if (_resources != null)
                foreach (Resource r in _resources)
                    r.Unlock();
            FinishedTaskCallback?.Invoke();
        }

        private Bitmap? EdgeDetectionAlgorithm(Bitmap clone, int degree)
        {
            Bitmap newImg = (Bitmap)clone.Clone();

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

        private Bitmap? UnsafeEdgeDetectionAlgorithm(Bitmap original, int degree)
        {
            Bitmap bSrc = (Bitmap)original.Clone();
            BitmapData bmData = original.LockBits(new Rectangle(0, 0, original.Width, original.Height),
                               ImageLockMode.ReadWrite, original.PixelFormat);
            BitmapData bmSrc = bSrc.LockBits(new Rectangle(0, 0, bSrc.Width, bSrc.Height),
                               ImageLockMode.ReadWrite, bSrc.PixelFormat);
            int stride = bmData.Stride;
            int stride2 = stride * 2;

            System.IntPtr Scan0 = bmData.Scan0;
            System.IntPtr SrcScan0 = bmSrc.Scan0;

            int bytesPerPixel = Image.GetPixelFormatSize(original.PixelFormat) / 8;
            int nOffset = stride - original.Width * bytesPerPixel;
            int nWidth = original.Width - 2;
            int nHeight = original.Height - 2;

            Parallel.For(0, nHeight, new ParallelOptions() { MaxDegreeOfParallelism = degree }, y =>
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
                unsafe
                {
                    byte* p = (byte*)(void*)Scan0 + y * (nOffset + nWidth * bytesPerPixel);
                    byte* pSrc = (byte*)(void*)SrcScan0 + y * (nOffset + nWidth * bytesPerPixel);
                    for (int x = 0; x < nWidth; x++)
                    {
                        int nPixelR = (pSrc[2] * edgeDetectionKernel[0, 0]) +
                            (pSrc[5] * edgeDetectionKernel[0, 1]) +
                            (pSrc[8] * edgeDetectionKernel[0, 2]) +
                            (pSrc[2 + stride] * edgeDetectionKernel[1, 0]) +
                            (pSrc[5 + stride] * edgeDetectionKernel[1, 1]) +
                            (pSrc[8 + stride] * edgeDetectionKernel[1, 2]) +
                            (pSrc[2 + stride2] * edgeDetectionKernel[2, 0]) +
                            (pSrc[5 + stride2] * edgeDetectionKernel[2, 1]) +
                            (pSrc[8 + stride2] * edgeDetectionKernel[2, 2]);

                        if (nPixelR < 0) nPixelR = 0;
                        if (nPixelR > 255) nPixelR = 255;
                        p[5 + stride] = (byte)nPixelR;

                        int nPixelG = (pSrc[1] * edgeDetectionKernel[0, 0]) +
                            (pSrc[4] * edgeDetectionKernel[0, 1]) +
                            (pSrc[7] * edgeDetectionKernel[0, 2]) +
                            (pSrc[1 + stride] * edgeDetectionKernel[1, 0]) +
                            (pSrc[4 + stride] * edgeDetectionKernel[1, 1]) +
                            (pSrc[7 + stride] * edgeDetectionKernel[1, 2]) +
                            (pSrc[1 + stride2] * edgeDetectionKernel[2, 0]) +
                            (pSrc[4 + stride2] * edgeDetectionKernel[2, 1]) +
                            (pSrc[7 + stride2] * edgeDetectionKernel[2, 2]);

                        if (nPixelG < 0) nPixelG = 0;
                        if (nPixelG > 255) nPixelG = 255;
                        p[4 + stride] = (byte)nPixelG;

                        int nPixelB = (pSrc[0] * edgeDetectionKernel[0, 0]) +
                                       (pSrc[3] * edgeDetectionKernel[0, 1]) +
                                       (pSrc[6] * edgeDetectionKernel[0, 2]) +
                                       (pSrc[0 + stride] * edgeDetectionKernel[1, 0]) +
                                       (pSrc[3 + stride] * edgeDetectionKernel[1, 1]) +
                                       (pSrc[6 + stride] * edgeDetectionKernel[1, 2]) +
                                       (pSrc[0 + stride2] * edgeDetectionKernel[2, 0]) +
                                       (pSrc[3 + stride2] * edgeDetectionKernel[2, 1]) +
                                       (pSrc[6 + stride2] * edgeDetectionKernel[2, 2]);

                        if (nPixelB < 0) nPixelB = 0;
                        if (nPixelB > 255) nPixelB = 255;
                        p[3 + stride] = (byte)nPixelB;

                        p += bytesPerPixel;
                        pSrc += bytesPerPixel;
                    }
                }
                lock (this)
                {
                    noRows++;
                    _progress = (double)noRows / maxHeight;
                }
                if (_progressBarUpdateAction != null)
                    _progressBarUpdateAction.Invoke();
            });

            original.UnlockBits(bmData);
            bSrc.UnlockBits(bmSrc);
            return ControlToken != null ? ControlToken.Terminated ? null : original : original;
        }

        public override void Serialize(string folderPath)
        {
            string fileName = folderPath + Path.DirectorySeparatorChar + this.GetType().Name + "_" + DateTime.Now.Ticks + ".bin";

            IFormatter formatter = new BinaryFormatter();
            using Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
        }

        public static new EdgeDetectionTask? Deserialize(string fileName)
        {

            IFormatter formatter = new BinaryFormatter();
            using Stream stream = new FileStream(fileName, FileMode.Open);
            EdgeDetectionTask task = (EdgeDetectionTask)formatter.Deserialize(stream);
            task.Action = task.EdgeDetection;
            return task;
        }
    }
}
