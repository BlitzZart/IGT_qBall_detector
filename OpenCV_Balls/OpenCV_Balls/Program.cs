using OpenCV_Balls;
using OpenCvSharp;
using OpenCvSharp.Blob;
using OpenCvSharp.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;


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
        // blob thresholds
        static int minArea = 100;
        static int maxArea = 17000;
        // circle thresholds
        static float minRadius = 40;
        static float maxRadius = 50;
        // blop/circle center offset
        static float blopToCircleTolerance = 30;

        static bool foundBall = false;

        private static bool runProgram;

        // main thread and input thread
        private static Thread inputThread;

        static void Main(string[] args) {
            runProgram = true;

            TCP_Server.InitServer();

            using (CvCapture cap = CvCapture.FromCamera(0)) {

                using (CvWindow winBin = new CvWindow("Camera Binary"))
                using (CvWindow winScr = new CvWindow("Camera Source")) {
                    //FindCircle(cap, winScr, winBin);
                    BlobFinderOne(cap, winScr, winBin);
                }
            }
            TCP_Server.CloseAll();
        }

        static void FoundIt(int x, int y, int width, int height) {
            float x01 = (float)x / (float)width;
            float y01 = (float)y / (float)height;

            Console.WriteLine("01-> X " + x01 + " Y " + y01);

            TCP_Server.SendPosition(x01, y01);
        }

        static void FindCircle(IplImage src, CvWindow winScr, double x, double y) {
            IplImage gray = new IplImage(src.Size, BitDepth.U8, 1);

            Cv.CvtColor(src, gray, ColorConversion.RgbToGray);
            Mat m = new Mat(gray);

            m.GaussianBlur(new Size(9, 9), 2, 2);
            InputArray ia = InputArray.Create(m);

            CvCircleSegment[] circles = Cv2.HoughCircles(ia, HoughCirclesMethod.Gradient, 1, 150 , 100, 50, 0, 0);

            foreach (CvCircleSegment item in circles) {
                Cv.DrawCircle(src, item.Center, (int)item.Radius, CvColor.Magenta);

                if (item.Radius > minRadius && item.Radius < maxRadius) {
                    FoundIt((int)item.Center.X, (int)item.Center.Y, m.Width, m.Height);
                    winScr.Image = src;
                    break;
                }
            }
        }

        static void BlobFinderOne(CvCapture cap, CvWindow winScr, CvWindow winBin) {
            CvBlob blob = new CvBlob();
            CvBlobs blobs = new CvBlobs();

            while (CvWindow.WaitKey(10) != 27) {
                IplImage src = cap.QueryFrame();
                IplImage binary = new IplImage(src.Size, BitDepth.U8, 1);

                Cv.CvtColor(src, binary, ColorConversion.BgrToGray);
                Cv.Threshold(binary, binary, 110, 215, ThresholdType.Binary); // TODO: change to BinaryInv!!!!!

                blobs.Clear();
                blobs.Label(binary);
                blob = blobs.LargestBlob();

               // FindCircle(src, winScr, 0, 0);

                if (blob != null && blobs.Count > 0) {
                    if (blob.Area > minArea && blob.Area < maxArea) {
                        Cv.DrawCircle(src, blob.Centroid, 64, CvColor.Green);
                       // Console.WriteLine(blob.Area);
                        if (!foundBall) {
                            foundBall = true;
                            //FindCircle(src, winScr, blob.Centroid.X, blob.Centroid.Y);
                            FoundIt((int)blob.Centroid.X, (int)blob.Centroid.Y, binary.Width, binary.Height);
                        }
                    }
                    else {
                        foundBall = false;
                    }
                }
                else {
                    foundBall = false;
                }

                //blobs.RenderBlobs(binary, src);
                //winScr.Image = src;
                winBin.Image = binary;
            }
        }
    }
}