using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace OpenCV_Balls
{
    static class PerspectiveCorretoin
    {
        public static bool calibrationDone = false;
        private static CvMat correctionMatrix;

        static IplImage image;

        public static IplImage GetCorrectedImage(IplImage image)
        {
            Cv.WarpPerspective(image, image, correctionMatrix);
            return image;
        }

        public static void ApplyFourPointTransform(CvCapture cap, CvWindow win)
        {

            CvPoint2D32f[] sPts = null;
            image = cap.QueryFrame();

            while (CvWindow.WaitKey(10) != 27)
            {
                image = cap.QueryFrame();
                sPts = GetPoints(image, win);
                if (sPts != null)
                {
                    calibrationDone = true;
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

            //while (CvWindow.WaitKey(10) != 27)
            //{
            //    image = cap.QueryFrame();
            //    Cv.WarpPerspective(image, image, m);
            //    win.Image = image;
            //}
        }



        private static CvPoint2D32f[] SortPoints(CvPoint2D32f[] ip)
        {

            CvPoint2D32f tl, tr, br, bl;  

            tl = tr = br = bl = ip[0];
            for (int i = 0; i < 4; i++)
            {
                // top left
                if (ip[i].X + ip[i].Y < tl.X + tl.Y)
                    tl = ip[i];
                // bottom right
                if (ip[i].X + ip[i].Y > br.X + br.Y)
                    br = ip[i];
            }

            for (int i = 0; i < 4; i++)
            {
                // top right
                if (ip[i].X > tl.X)
                {
                    if (ip[i].Y < br.Y)
                    {
                        tr = ip[i];
                    }
                }
                // bottom left
                if (ip[i].X < br.X)
                {
                    if (ip[i].Y > tr.Y)
                    {
                        bl = ip[i];
                    }
                }
            }

            Console.WriteLine(ip[0] + " " + tl + " " + br + " " + bl);

            return new CvPoint2D32f[4] { tl, tr, br, bl };

        }

        private static CvPoint2D32f[] GetPoints(IplImage src, CvWindow win)
        {
            IplImage gray = new IplImage(src.Size, BitDepth.U8, 1);

            Cv.CvtColor(src, gray, ColorConversion.RgbToGray);

            Mat m = new Mat(gray);

            m.GaussianBlur(new Size(9, 9), 2, 2);
            InputArray ia = InputArray.Create(m);

            CvCircleSegment[] circles = Cv2.HoughCircles(ia, HoughCirclesMethod.Gradient, 1, 80, 80, 30, 1, 50);


            //Console.WriteLine("Circles " + circles.Length);
            foreach (CvCircleSegment item in circles)
            {
                Cv.DrawCircle(gray, item.Center, 64, CvColor.Green);
            }

            win.Image = gray;

            if (circles.Length > 3)
            {
                Console.WriteLine("Calibration DONE");
                Cv.DrawCircle(gray, circles[0].Center, 64, CvColor.Green);


                CvPoint2D32f[] pts = new CvPoint2D32f[4];
                for (int i = 0; i < 4; i++)
                {
                    pts[i] = new CvPoint2D32f(circles[i].Center.X, circles[i].Center.Y);
                }

                return SortPoints(pts);
            }

            return null;
        }
    }
}
