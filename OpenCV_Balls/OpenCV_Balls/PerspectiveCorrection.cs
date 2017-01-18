using System;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace OpenCV_Balls
{
    static class PerspectiveCorretoin
    {
        public static double calDP = 1;
        public static double calMinDist = 100;
        public static double calP1 = 70;
        public static double calP2 = 29;
        public static int calMinRadius = 9;
        public static int calMaxRadius = 15;

        public static bool calibrationDone = false;
        private static CvMat correctionMatrix = CvMat.Identity(3,3,MatrixType.F32C1);

        static IplImage image;

        // returns the image corrected, based on the last calibration
        public static IplImage GetCorrectedImage(IplImage image)
        {
            Cv.WarpPerspective(image, image, correctionMatrix);
            return image;
        }
        // execute a four point transform calibration
        public static void ApplyFourPointTransform(CvCapture cap, CvWindow win)
        {
            Console.WriteLine("******* Starting Calibration *******");
            CvPoint2D32f[] sPts = null;
            image = cap.QueryFrame();

            while (CvWindow.WaitKey(10) != 27)
            {
                image = cap.QueryFrame();
                sPts = GetPoints(image, win);

                if (sPts != null)
                {
                    calibrationDone = true;
                    Console.WriteLine("********* Calibration DONE *********\n");
                    break;
                }
            }

            CvPoint tl, tr, br, bl;
            tl = sPts[0];
            tr = sPts[1];
            br = sPts[2];
            bl = sPts[3];

            double widthA = Math.Sqrt((Math.Pow(br.X - bl.X, 2)) + (Math.Pow(br.Y - bl.Y, 2)));
            double widthB = Math.Sqrt((Math.Pow(tr.X - tl.X, 2)) + (Math.Pow(tr.Y - tl.Y, 2)));
            int maxWidth = Math.Max((int)widthA, (int)widthB);

            double heightA = Math.Sqrt((Math.Pow(tr.X - br.X, 2)) + (Math.Pow(tr.Y - br.Y, 2)));
            double heightB = Math.Sqrt((Math.Pow(tl.X - bl.X, 2)) + (Math.Pow(tl.Y - bl.Y, 2)));
            int maxHeight = Math.Max((int)heightA, (int)heightB);

            CvPoint2D32f[] dPts = new CvPoint2D32f[4];
            dPts[0] = new CvPoint2D32f(0, 0);
            dPts[1] = new CvPoint2D32f(image.Width, 0);
            dPts[2] = new CvPoint2D32f(image.Width, image.Height);
            dPts[3] = new CvPoint2D32f(0, image.Height);

            correctionMatrix = Cv.GetPerspectiveTransform(sPts, dPts);
            Cv.WarpPerspective(image, image, correctionMatrix);
        }
        // sort 4 points clockwise and return
        private static CvPoint2D32f[] SortPoints(CvPoint2D32f[] pts) {
            CvPoint2D32f tl, tr, br, bl;
            tl = tr = br = bl = pts[0];

            CvPoint2D32f center;
            center.X = (pts[0].X + pts[1].X + pts[2].X + pts[3].X) / 4;
            center.Y = (pts[0].Y + pts[1].Y + pts[2].Y + pts[3].Y) / 4;

            foreach (CvPoint2D32f item in pts) {
                if (item.X < center.X && item.Y < center.Y) {
                    tl = item;
                } else if (item.X > center.X && item.Y < center.Y) {
                    tr = item;
                }
                else if (item.X > center.X && item.Y > center.Y) {
                    br = item;
                }
                else if (item.X < center.X && item.Y > center.Y) {
                    bl = item;
                }
            }

            return new CvPoint2D32f[4] { tl, tr, br, bl };
        }
        // find and return the 4 corner markers positions
        private static CvPoint2D32f[] GetPoints(IplImage src, CvWindow win)
        {
            IplImage gray = new IplImage(src.Size, BitDepth.U8, 1);

            Cv.CvtColor(src, gray, ColorConversion.RgbToGray);

            Mat m = new Mat(gray);

            m.GaussianBlur(new Size(9, 9), 2, 2);
            InputArray ia = InputArray.Create(m);

            CvCircleSegment[] circles = Cv2.HoughCircles(ia, HoughCirclesMethod.Gradient, calDP, calMinDist, calP1, calP2, calMinRadius, calMaxRadius);

            foreach (CvCircleSegment item in circles)
            {
                Cv.DrawCircle(gray, item.Center, 32, CvColor.Green);
            }

            win.Image = gray;

            if (circles.Length > 3)
            {
                Cv.DrawCircle(gray, circles[0].Center, 64, CvColor.Green);

                CvPoint2D32f[] pts = new CvPoint2D32f[4];
                for (int i = 0; i < 4; i++)
                {
                    Console.WriteLine("Point " + i + " | radius = " + circles[i].Radius);
                    pts[i] = new CvPoint2D32f(circles[i].Center.X, circles[i].Center.Y);
                }

                return SortPoints(pts);
            }

            return null;
        }
    }
}