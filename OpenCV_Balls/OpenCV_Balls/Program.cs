using OpenCV_Balls;
using OpenCvSharp;
using OpenCvSharp.Blob;
using OpenCvSharp.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;


[StructLayout(LayoutKind.Explicit)]
public struct FloatUnion {
    [FieldOffset(0)]
    public float Value;
    [FieldOffset(0)]
    public byte Byte0;
    [FieldOffset(1)]
    public byte Byte1;
    [FieldOffset(2)]
    public byte Byte2;
    [FieldOffset(3)]
    public byte Byte3;

    public byte[] ToByteArray() {
        return new[] { Byte0, Byte1, Byte2, Byte3 };
    }

    public static byte[] FloatToBytes(float value) {
        return new FloatUnion { Value = value }.ToByteArray();
    }

    public static float BytesToFloat(byte[] bytes) {
        if (bytes.Length != 4) throw new ArgumentException("You must provide four bytes.");
        return new FloatUnion { Byte0 = bytes[0], Byte1 = bytes[1], Byte2 = bytes[2], Byte3 = bytes[3] }.Value;
    }
}

namespace OpenCVSharpLoda {
    class Program {
        // circle thresholds
        static int minRadius = 40;
        static int maxRadius = 70;
        // cooldown = minimum time between detected circles
        static float cooldown = 1.0f;
        static float currentCool = 0;

        public static bool calibrationMode = false;

        static bool foundBall = false;

        private static float _deltaTime;
        private static Stopwatch _fpsStopWatch;

        static void Main(string[] args) {
            _fpsStopWatch = new Stopwatch();

            TCP_Server.InitServer();

            using (CvCapture cap = CvCapture.FromCamera(0)) {

                //using (CvWindow winBin = new CvWindow("Camera Binary"))
                using (CvWindow winScr = new CvWindow("Camera Source")) {
                    if (calibrationMode) {
                        Calibration(cap, winScr);
                    } else {
                        CircleOnly(cap, winScr);
                    }
                }
            }
            TCP_Server.CloseAll();
        }

        static void FoundIt(float x, float y, int width, int height) {
            float x01 = (float)x / (float)width;
            float y01 = (float)y / (float)height;

            Console.WriteLine("> X " + x01 + " Y " + y01);

            TCP_Server.SendPosition(x01, y01);
        }

        static void FindCircle(IplImage src, CvWindow winScr) {
            IplImage gray = new IplImage(src.Size, BitDepth.U8, 1);

            Cv.CvtColor(src, gray, ColorConversion.RgbToGray);
            Mat m = new Mat(gray);

            m.GaussianBlur(new Size(9, 9), 2, 2);
            InputArray ia = InputArray.Create(m);
            
            CvCircleSegment[] circles = Cv2.HoughCircles(ia, HoughCirclesMethod.Gradient, 1, 100, 100, 30, minRadius, maxRadius);

            if (!foundBall) {
                if (circles.Length > 0) {
                    foundBall = true;
                    FoundIt(circles[0].Center.X, circles[0].Center.Y, m.Width, m.Height);
                    Cv.DrawCircle(gray, circles[0].Center, 64, CvColor.Green);
                }
            }
            else {
                if (currentCool > cooldown) {
                    foundBall = false;
                    currentCool = 0;
                }
                else {
                    currentCool += _deltaTime;
                }
            }
            winScr.Image = m.ToIplImage();
        }

        static void Calibration(CvCapture cap, CvWindow winScr) {

            while (CvWindow.WaitKey(10) != 27) {
                IplImage src = cap.QueryFrame();
                winScr.Image = src;
            }
        }

        static void CircleOnly(CvCapture cap, CvWindow winScr) {
            while (CvWindow.WaitKey(10) != 27) {
                IplImage src = cap.QueryFrame();
                ShowFPS();
                FindCircle(src, winScr);
            }
        }

        private static void ShowFPS() {
            _fpsStopWatch.Stop();
            _deltaTime = _fpsStopWatch.ElapsedMilliseconds * 0.001f;
            if (_deltaTime > 0.07f)
                Console.WriteLine("!!! FPS < 14: " + (int)(1 / _deltaTime));
            //if (s != 0)
            //    Console.WriteLine("FPS: " + (int)(1 / s));
            _fpsStopWatch.Reset();
            _fpsStopWatch.Start();
        }
    }
}

//static void BlobAndCircleCombined(CvCapture cap, CvWindow winScr, CvWindow winBin) {
//    CvBlob blob = new CvBlob();
//    CvBlobs blobs = new CvBlobs();
//    while (CvWindow.WaitKey(10) != 27) {
//        ShowFPS();

//        IplImage src = cap.QueryFrame();
//        IplImage binary = new IplImage(src.Size, BitDepth.U8, 1);

//        Cv.CvtColor(src, binary, ColorConversion.BgrToGray);
//        Cv.Threshold(binary, binary, 100, 120, ThresholdType.BinaryInv); // TODO: change to BinaryInv!!!!!

//        blobs.Clear();
//        blobs.Label(binary);
//        blob = blobs.LargestBlob();

//        FindCircle(src, winScr, 0, 0);

//        if (blob != null && blobs.Count > 0) {
//            if (blob.Area > minArea && blob.Area < maxArea) {
//                Cv.DrawCircle(src, blob.Centroid, 64, CvColor.Green);
//                // Console.WriteLine(blob.Area);
//                if (!foundBall) {
//                    foundBall = true;
//                    //FindCircle(src, winScr, blob.Centroid.X, blob.Centroid.Y);
//                    FoundIt((int)blob.Centroid.X, (int)blob.Centroid.Y, binary.Width, binary.Height);
//                }
//            }
//            else {
//                foundBall = false;
//            }
//        }
//        else {
//            foundBall = false;
//        }

//        //blobs.RenderBlobs(binary, src);
//        //winScr.Image = src;
//        winBin.Image = binary;
//    }

//}

//static void DoSome(Mat mat) {

//    MatOfByte mat1 = new MatOfByte(mat);
//    var indexer = mat1.GetIndexer();

//    for (int y = 0; y < mat.Height; y++) {
//        for (int x = 0; x < mat.Width; x++) {
//            byte val = indexer[y, x];

//            //val = (byte)(255 - val);

//            val = (byte)(Threshold(Clamp(255 - val, 0, 255), 90));
//            if (val > 100)
//                val = 255;

//            indexer[y, x] = val;
//        }
//    }
//}

//public static int GainAndClamp(int value, int min, int max, float gain) {
//    return (value < min) ? min : (value > max) ? max : (int)(value * gain);
//}
//public static int Clamp(int value, int min, int max) {
//    return (value < min) ? min : (value > max) ? max : value;
//}
//public static int Threshold(int value, int threshold) {
//    return (value > threshold) ? value : 0;
//}