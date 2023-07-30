using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ImageAutomaticCroppingWatcher.Helpers
{
    public static class PerspectiveCorrectionHelper
    {
        public static async Task<BitmapImage> DoPerspectiveTransformAsync(BitmapImage image)
        {
            return await Task.Run(() =>
            {
                using (Mat img = BitmapImageToMat(image))
                {
                    int hh = img.Height;
                    int ww = img.Width;

                    // read template
                    using (Mat template = img.Clone())
                    {
                        int ht = template.Height;
                        int wd = template.Width;

                        // Check if the image is grayscale or BGR
                        bool isGrayscale = img.Channels() == 1;

                        // Convert the image to grayscale if it's BGR
                        using (Mat grayImage = isGrayscale ? img.Clone() : new Mat())
                        {
                            if (!isGrayscale)
                            {
                                Cv2.CvtColor(img, grayImage, ColorConversionCodes.BGR2GRAY);
                            }

                            /*
                            // convert img to grayscale
                            Mat gray = new Mat();
                            Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
                            */
                            // do otsu threshold on gray image
                            Mat thresh = new Mat();
                            Cv2.Threshold(grayImage, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                            // pad thresh with black to preserve corners when applying morphology
                            Mat pad = new Mat();
                            Cv2.CopyMakeBorder(thresh, pad, 20, 20, 20, 20, BorderTypes.Constant, Scalar.Black);

                            // apply morphology
                            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(15, 15));
                            Mat morph = new Mat();
                            Cv2.MorphologyEx(pad, morph, MorphTypes.Close, kernel);

                            // remove padding
                            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(20, 20, ww, hh);
                            Mat croppedMorph = new Mat(morph, roi);

                            // get largest external contour
                            OpenCvSharp.Point[][] contours;
                            HierarchyIndex[] hierarchy;
                            Cv2.FindContours(croppedMorph, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                            var bigContour = FindLargestContour(contours);

                            // get perimeter and approximate a polygon
                            double peri = Cv2.ArcLength(bigContour, true);
                            Point2f[] corners = Cv2.ApproxPolyDP(bigContour, 0.04 * peri, true);

                            // draw polygon on input image from detected corners
                            Mat polygon = img.Clone();
                            OpenCvSharp.Point[] cornersInt = Array.ConvertAll(corners, point => new OpenCvSharp.Point((int)point.X, (int)point.Y));
                            Cv2.Polylines(polygon, new[] { cornersInt }, true, Scalar.Green, 2, LineTypes.AntiAlias);



                            // Calculate the angle between the first and second corners
                            double angle = GetAngle(corners[0], corners[1]);



                            Point2f[] oCorners;
                            if (angle >= -45 && angle < 45) // Horizontal
                            {
                                // Check if the image is mirrored vertically
                                bool isMirrored = corners[0].Y > corners[3].Y;
                                if (isMirrored)
                                {
                                    oCorners = new Point2f[] { new Point2f(wd, ht), new Point2f(wd, 0), new Point2f(0, 0), new Point2f(0, ht) };
                                }
                                else
                                {
                                    oCorners = new Point2f[] { new Point2f(wd, ht), new Point2f(0, ht), new Point2f(0, 0), new Point2f(wd, 0) };
                                }

                            }
                            else if (angle >= 45 && angle < 135) // Vertical
                            {
                                // Check if the image is mirrored vertically
                                bool isMirrored = corners[0].Y > corners[1].Y;

                                if (isMirrored)
                                {
                                    oCorners = new Point2f[] { new Point2f(wd, ht), new Point2f(wd, 0), new Point2f(0, 0), new Point2f(0, ht) };
                                }
                                else
                                {
                                    oCorners = new Point2f[] { new Point2f(0, 0), new Point2f(0, ht), new Point2f(wd, ht), new Point2f(wd, 0) };
                                }
                            }
                            else if (angle >= -135 && angle < -45) // Vertical (upside down)
                            {
                                // Check if the image is mirrored vertically (upside down)
                                bool isMirrored = corners[2].Y > corners[3].Y;

                                if (isMirrored)
                                {
                                    oCorners = new Point2f[] { new Point2f(0, ht), new Point2f(0, 0), new Point2f(wd, 0), new Point2f(wd, ht) };
                                }
                                else
                                {
                                    oCorners = new Point2f[] { new Point2f(wd, 0), new Point2f(wd, ht), new Point2f(0, ht), new Point2f(0, 0) };
                                }
                            }
                            else // Upside down
                            {
                                // Check if the image is mirrored vertically (upside down)
                                bool isMirrored = corners[0].Y > corners[3].Y;
                                if (isMirrored)
                                {

                                    oCorners = new Point2f[] { new Point2f(wd, ht), new Point2f(wd, 0), new Point2f(0, 0), new Point2f(0, ht) };
                                }
                                else
                                {
                                    oCorners = new Point2f[] { new Point2f(0, 0), new Point2f(wd, 0), new Point2f(wd, ht), new Point2f(0, ht) };
                                    oCorners = new Point2f[] { new Point2f(wd, 0), new Point2f(0, 0), new Point2f(0, ht), new Point2f(wd, ht) };
                                }

                            }
                            /*
                              Point2f[] oCorners;
                            if (diff >= 0)
                            {
                                oCorners = new Point2f[] { new Point2f(0, 0), new Point2f(0, ht), new Point2f(wd, ht), new Point2f(wd, 0) };
                            }
                            else
                            {
                                oCorners = new Point2f[] { new Point2f(wd, 0), new Point2f(0, 0), new Point2f(0, ht), new Point2f(wd, ht) };
                            }
                             */

                            /*
                            Point2f[] oCorners = new Point2f[]
                           {
                    new Point2f(0, 0),          // top-left
                    new Point2f(wd, 0),         // top-right
                    new Point2f(wd, ht),        // bottom-right
                    new Point2f(0, ht)          // bottom-left
                           };
                            */


                            // get perspective transformation matrix
                            Mat M = Cv2.GetPerspectiveTransform(corners, oCorners);

                            // Check if the perspective transformation matrix is valid
                            if (M == null || M.Rows != 3 || M.Cols != 3 || M.Type() != MatType.CV_64FC1)
                            {
                                Console.WriteLine("Invalid perspective transformation matrix.");
                                return null;
                            }
                            // do perspective transformation
                            Mat warped = new Mat();
                            try
                            {
                                Cv2.WarpPerspective(img, warped, M, new OpenCvSharp.Size(wd, ht), InterpolationFlags.Linear);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error during perspective transformation: " + ex.Message);
                                return null;
                            }

                            // Convert BGR to RGB for displaying the image in WPF
                            using (Mat rgbWarped = new Mat())
                            {
                                Cv2.CvtColor(warped, rgbWarped, ColorConversionCodes.BGR2RGB);

                                // Convert the warped Mat back to a BitmapImage
                                BitmapImage warpedImage = MatToBitmapImage(rgbWarped);

                                // Clear resources by releasing Mat instances
                                grayImage.Dispose();
                                rgbWarped.Dispose();
                                warped.Dispose();

                                return warpedImage;
                            }
                        }
                    }
                }
            });
        }
        private static double GetAngle(Point2f point1, Point2f point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            return Math.Atan2(dy, dx) * 180 / Math.PI;
        }
        private static OpenCvSharp.Point2f[] FindLargestContour(OpenCvSharp.Point[][] contours)
        {
            double maxArea = 0;
            OpenCvSharp.Point2f[] largestContour = null;
            foreach (var contour in contours)
            {
                double area = Cv2.ContourArea(contour);
                if (area > maxArea)
                {
                    maxArea = area;
                    largestContour = Array.ConvertAll(contour, point => new OpenCvSharp.Point2f(point.X, point.Y));
                }
            }
            return largestContour;
        }
        public static Mat BitmapImageToMat(BitmapImage bitmapImage)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(stream);

                using (Bitmap bmp = new Bitmap(stream))
                {
                    return OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                }
            }
        }

        public static BitmapImage MatToBitmapImage(Mat mat)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat))
                {
                    bitmap.Save(stream, ImageFormat.Bmp);
                    stream.Seek(0, SeekOrigin.Begin);

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
        }
    }

}

