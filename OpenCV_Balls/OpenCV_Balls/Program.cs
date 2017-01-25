using OpenCV_Balls;
using OpenCvSharp;
using OpenCvSharp.Blob;
using OpenCvSharp.CPlusPlus;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

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

enum Mode
{
    Tracking, Live, Calibration
}

enum TrackingType {
    Blob, Cirle
}

namespace OpenCVSharpQBalls {
    
    class Program {

        #region configuration
        // circle parameters
        static double circDP = 1;
        static double circMinDist = 90;
        static double circP1 = 90;
        static double circP2 = 30;
        static int circMinRadius = 18;
        static int circMaxRadius = 26;

        // blob parameters
        static float blobMinThreshold = 100;
        static float blobMaxThreshold = 255;
        static float blobMinArea = 100;
        static float blobMaxArea = 200;
        static float blobMinCircularity = 0.87f;
        static float blobMinConvexity = 0.5f;

        // cooldown = minimum time between detected circles
        static float cooldown = 1.0f;
        static float currentCool = 0;

        private static XmlDocument LoadConfig() {
            string fileName = "Config.xml";
            string path = Path.Combine(Directory.GetCurrentDirectory() + "\\" + fileName);
            XmlDocument doc = new XmlDocument();
            try {
                doc.Load(path);
            }
            catch (Exception e) {
                if (e is FileNotFoundException)
                    Console.WriteLine(fileName + " not found.");
                else if (e is XmlException)
                    Console.WriteLine(fileName + " has invalid content.");

                Console.WriteLine("NO CONFIG LOADED - USING DEFAULT VALUES");
                return null;
            }
            return doc;
        }
        private static void ApplyConfig(XmlDocument doc) {
            if (doc == null)
                return;

            if (doc.DocumentElement.ChildNodes.Count > 0) {
                Console.WriteLine("********* ReadingConfig... *********");

                foreach (XmlNode node in doc.DocumentElement.ChildNodes) {

                    switch (node.Name) {
                        case "type":
                            if (node.InnerText == "Blob")
                                trackingType = TrackingType.Blob;
                            else if (node.InnerText == "Circle")
                                trackingType = TrackingType.Cirle;
                            break;
                        case "cooldown":
                            cooldown = float.Parse(node.InnerText, System.Globalization.NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                            break;

                        // CIRCLE
                        case "circDP":
                            circDP = double.Parse(node.InnerText);
                            break;
                        case "circMinDist":
                            circMinDist = double.Parse(node.InnerText);
                            break;
                        case "circP1":
                            circP1 = double.Parse(node.InnerText);
                            break;
                        case "circP2":
                            circP2 = double.Parse(node.InnerText);
                            break;
                        case "circMinRadius":
                            circMinRadius = int.Parse(node.InnerText);
                            break;
                        case "circMaxRadius":
                            circMaxRadius = int.Parse(node.InnerText);
                            break;


                        // BLOB
                        case "blobMinThreshold":
                            blobMinThreshold = float.Parse(node.InnerText);
                            break;
                        case "blobMaxThreshold":
                            blobMaxThreshold = float.Parse(node.InnerText);
                            break;
                        case "blobMinArea":
                            blobMinArea = float.Parse(node.InnerText);
                            break;
                        case "blobMaxArea":
                            blobMaxArea = float.Parse(node.InnerText);
                            break;
                        case "blobMinCircularity":
                            blobMinCircularity = float.Parse(node.InnerText, System.Globalization.NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                            break;
                        case "blobMinConvexity":
                            blobMinConvexity = float.Parse(node.InnerText, System.Globalization.NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                            break;

                        // CALIBRATION
                        case "calDP":
                            PerspectiveCorretoin.calDP = double.Parse(node.InnerText);
                            break;
                        case "calMinDist":
                            PerspectiveCorretoin.calMinDist = double.Parse(node.InnerText);
                            break;
                        case "calP1":
                            PerspectiveCorretoin.calP1 = double.Parse(node.InnerText);
                            break;
                        case "calP2":
                            PerspectiveCorretoin.calP2 = double.Parse(node.InnerText);
                            break;
                        case "calMinRadius":
                            PerspectiveCorretoin.calMinRadius = int.Parse(node.InnerText);
                            break;
                        case "calMaxRadius":
                            PerspectiveCorretoin.calMaxRadius = int.Parse(node.InnerText);
                            break;
                    }
                }

                // print usful configuration values
                Console.WriteLine("dp               = " + circDP);
                Console.WriteLine("Minimum distance = " + circMinDist);
                Console.WriteLine("parameter 1      = " + circP1);
                Console.WriteLine("parameter 1      = " + circP2);
                Console.WriteLine("Minimum radius   = " + circMinRadius);
                Console.WriteLine("Maximum radius   = " + circMaxRadius);
                Console.WriteLine("Cooldwon         = " + cooldown);

                Console.WriteLine("******** Configuration DONE ********\n");
            }
        }

        #endregion

        static IplImage gray;
        static IplImage srcImage;
        static Size blurKernelSize;
        static Mat bluredImageMat;
        static InputArray bluredInputArray;
        static CvCircleSegment[] foundCircles;

        static Mode mode = Mode.Tracking;
        static TrackingType trackingType = TrackingType.Cirle;

        static bool foundBall = false;

        private static float _deltaTime;
        private static Stopwatch _fpsStopWatch;

        static void Main(string[] args) {
            _fpsStopWatch = new Stopwatch();

            // load and apply config
            ApplyConfig(LoadConfig());

            TCP_Server.InitServer();

            using (CvCapture cap = CvCapture.FromCamera(1)) {
                //using (CvWindow winBin = new CvWindow("Camera Binary"))
                using (CvWindow winSrc = new CvWindow("Camera Source")) {

                    switch (mode) {
                        case Mode.Tracking:
                            Calibration(cap, winSrc);
                            if (trackingType == TrackingType.Blob)
                                FindBlob(cap, winSrc);
                            else
                                CircleOnly(cap, winSrc);
                            break;
                        case Mode.Live:
                            Live(cap, winSrc);
                            break;
                        case Mode.Calibration:
                            Calibration(cap, winSrc);
                            break;
                    }
                }
            }
            TCP_Server.CloseAll();
        }

        // send position of found object/ball via tcp
        static void FoundIt(float x, float y, int width, int height) {
            float x01 = (float)x / (float)width;
            float y01 = (float)y / (float)height;

            //Console.WriteLine("> X " + x01 + " Y " + y01);

            TCP_Server.SendPosition(x01, y01);
        }
        // search circles/balls via houghtransform
        static void FindCircle(IplImage src, CvWindow winScr) {
            Cv.CvtColor(src, gray, ColorConversion.RgbToGray);
            bluredImageMat = new Mat(gray);

            // NOTE: GC collects every loop
            GC.Collect();

            bluredImageMat.GaussianBlur(blurKernelSize, 2, 2);
            bluredInputArray = InputArray.Create(bluredImageMat);

            foundCircles = Cv2.HoughCircles(bluredInputArray, HoughCirclesMethod.Gradient, circDP, circMinDist, circP1, circP2, circMinRadius, circMaxRadius);

            if (!foundBall || mode == Mode.Live) {
                if (foundCircles.Length > 0) {
                    Console.WriteLine("Found circle | radius = " + foundCircles[0].Radius);
                    foundBall = true;
                    FoundIt(foundCircles[0].Center.X, foundCircles[0].Center.Y, bluredImageMat.Width, bluredImageMat.Height);
                    Cv.DrawCircle(gray, foundCircles[0].Center, 64, CvColor.Green);
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
            winScr.Image = gray; // m.ToIplImage();
        }
        // shows a live view of the current web cam
        private static void Live(CvCapture cap, CvWindow winScr) {
            while (CvWindow.WaitKey(10) != 27) {
                IplImage src = cap.QueryFrame();
                winScr.Image = src;
            }
        }
        // use circle detection only
        private static void CircleOnly(CvCapture cap, CvWindow winScr) {
            srcImage = PerspectiveCorretoin.GetCorrectedImage(cap.QueryFrame());
            gray = new IplImage(srcImage.Size, BitDepth.U8, 1);
            blurKernelSize = new Size(9, 9);
            while (CvWindow.WaitKey(10) != 27) {
                srcImage = PerspectiveCorretoin.GetCorrectedImage(cap.QueryFrame());
                ShowFPS();
                FindCircle(srcImage, winScr);
            }
        }
        // execute perspective correction
        private static void Calibration(CvCapture cap, CvWindow winSrc) {
            PerspectiveCorretoin.ApplyFourPointTransform(cap, winSrc);
        }
        // print FPS in console
        private static void ShowFPS() {
            _fpsStopWatch.Stop();
            _deltaTime = _fpsStopWatch.ElapsedMilliseconds * 0.001f;
            //Console.WriteLine("FPS : " + (int)(1 / _deltaTime));
            if (_deltaTime > 0.07f)
                Console.WriteLine("!!! FPS < 14: " + (int)(1 / _deltaTime));
            //if (_deltaTime != 0)
            //    Console.Write("  FPS: " + (int)(1 / _deltaTime));
            _fpsStopWatch.Reset();
            _fpsStopWatch.Start();
        }
        // find circles/dots using blob detection
        private static void FindBlob(CvCapture cap, CvWindow winScr) {
            SimpleBlobDetector.Params blobParameters = new SimpleBlobDetector.Params();

            // threshold (gray value)
            blobParameters.MinThreshold = blobMinThreshold;
            blobParameters.MaxThreshold = blobMaxThreshold;
            // area (pixel count)
            blobParameters.FilterByArea = true;
            blobParameters.MinArea = blobMinArea;
            blobParameters.MaxArea = blobMaxArea;
            // circularity
            blobParameters.FilterByCircularity = true;
            blobParameters.MinCircularity = blobMinCircularity;
            // convexity - probably not needed - maybe eleminates false positives
            blobParameters.FilterByConvexity = true;
            blobParameters.MinConvexity = blobMinConvexity;
            //// inertia - what does the values mean exactly
            //blobParameters.FilterByInertia = true;
            //blobParameters.MinInertiaRatio = 

            SimpleBlobDetector blobDetector = new SimpleBlobDetector(blobParameters);
            gray = new IplImage(cap.QueryFrame().Size, BitDepth.U8, 1);

            while (CvWindow.WaitKey(10) != 27) {
                IplImage iplImage = PerspectiveCorretoin.GetCorrectedImage(cap.QueryFrame());
                Cv.CvtColor(iplImage, gray, ColorConversion.RgbToGray);

                Mat mat = new Mat(gray);
                mat.PyrDown(new Size(mat.Width/2, mat.Height/2));

                KeyPoint[] keypoints = blobDetector.Detect(mat);

                foreach (KeyPoint item in keypoints) {
                    Cv.DrawCircle(gray, new CvPoint2D32f(item.Pt.X, item.Pt.Y), (int)(item.Size * 3), CvColor.Green);
                    Console.WriteLine("Found blob | size = " + item.Size);
                }
                winScr.Image = gray;
            }
        }
    }
}