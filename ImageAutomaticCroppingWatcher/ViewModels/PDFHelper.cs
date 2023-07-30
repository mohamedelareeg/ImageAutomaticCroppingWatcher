using ImageAutomaticCroppingWatcher.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Reflection.PortableExecutable;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Components.Utils;
using System.Drawing;

namespace ImageAutomaticCroppingWatcher.ViewModels
{
    public static class PDFHelper
    {
        public static async Task<bool> AutoMaticCroppingPDFImages(string inputFilePath)
        {
            try
            {
                // Read the PDF file into memory using a FileStream
                byte[] pdfBytes;
                using (FileStream fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                {
                    pdfBytes = new byte[fileStream.Length];
                    await fileStream.ReadAsync(pdfBytes, 0, pdfBytes.Length);
                }

                // Use Task.WhenAll to process images in parallel and await for all tasks to complete
                List<Task<string>> processingTasks = new List<Task<string>>();
                using (MemoryStream memoryStream = new MemoryStream(pdfBytes))
                {
                    using (PdfReader reader = new PdfReader(memoryStream))
                    {
                        for (int pageNumber = 1; pageNumber <= reader.NumberOfPages; pageNumber++)
                        {
                            PdfDictionary page = reader.GetPageN(pageNumber);
                            PdfDictionary resources = page.GetAsDict(PdfName.RESOURCES);
                            PdfDictionary xObject = resources.GetAsDict(PdfName.XOBJECT);

                            if (xObject != null)
                            {
                                foreach (var key in xObject.Keys)
                                {
                                    PdfObject obj = xObject.Get(key);

                                    if (obj.IsIndirect())
                                    {
                                        PdfDictionary imgObject = (PdfDictionary)PdfReader.GetPdfObject(obj);

                                        if (imgObject != null)
                                        {
                                            PdfName subType = (PdfName)PdfReader.GetPdfObject(imgObject.Get(PdfName.SUBTYPE));

                                            if (PdfName.IMAGE.Equals(subType))
                                            {
                                                PdfImageObject pdfImage = new PdfImageObject((PRStream)imgObject);

                                                // Extract the image bytes
                                                byte[] bytes = pdfImage.GetImageAsBytes();

                                                // Process the image in parallel and save it to a temporary file
                                                processingTasks.Add(ProcessAndSaveImageAsync(bytes, pageNumber, inputFilePath));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Wait for all processing tasks to complete and get the paths to the saved images
                    string[] editedImagePaths = await Task.WhenAll(processingTasks);

                    // Save the edited images as separate .jpg files
                    string pdfName = System.IO.Path.GetFileNameWithoutExtension(inputFilePath);
                    string outputFolder = System.IO.Path.Combine(SharedSettings.Instance.LoadedSettings.WatcherReleasePath, pdfName);
                    Directory.CreateDirectory(outputFolder);

                    // Create a PDF from the edited images
                    string outputPdfPath = System.IO.Path.Combine(outputFolder, $"{pdfName}.pdf");
                    SaveImagesToPdf(editedImagePaths.ToList(), outputPdfPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static async Task<string> ProcessAndSaveImageAsync(byte[] imageBytes, int pageNumber, string inputFilePath)
        {
            using (MemoryStream stream = new MemoryStream(imageBytes))
            {
                // Convert the image bytes to a BitmapImage without caching
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                //stream.Seek(0, SeekOrigin.Begin); // Reset the stream position

                // Process the image
                BitmapImage editedImage = await PerspectiveCorrectionHelper.DoPerspectiveTransformAsync(image);

                // Compress the image to PNG format with lossless compression
                byte[] compressedBytes = CompressToJpeg2000(editedImage);

                // Save the edited image to a temporary file in the folder named "temp_images" within the exported PDF path
                string pdfName = System.IO.Path.GetFileNameWithoutExtension(inputFilePath);
                string outputFolder = System.IO.Path.Combine(SharedSettings.Instance.LoadedSettings.WatcherReleasePath, pdfName, "temp_images");
                Directory.CreateDirectory(outputFolder);
                string tempFilePath = System.IO.Path.Combine(outputFolder, $"Image_{pageNumber}.png");
                File.WriteAllBytes(tempFilePath, compressedBytes);

                // Dispose of the MemoryStream after creating the BitmapImage
                stream.Dispose();

                return tempFilePath;
            }
        }
        private static byte[] CompressToJpeg2000(BitmapImage image)
        {
            // Convert the BitmapImage to a Bitmap and then encode to JPEG 2000 format
            using (MemoryStream outputStream = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.QualityLevel = 1; // Set the quality level (0 to 1, 1 being the lowest size and lower quality)
                encoder.Save(outputStream);
                return outputStream.ToArray();
            }
        }




        // Save a BitmapImage to a file
        private static void SaveBitmapImageToFile(BitmapImage image, string filePath)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);

                File.WriteAllBytes(filePath, ms.ToArray());
            }
        }

        public static void SaveImagesToPdf(List<string> imagePaths, string outputFilePath)
        {
            try
            {
                using (FileStream fs = new FileStream(outputFilePath, FileMode.Create))
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document();
                    PdfWriter writer = PdfWriter.GetInstance(doc, fs);

                    // Apply compression settings to the writer
                    writer.SetPdfVersion(PdfWriter.PDF_VERSION_1_5); // Use PDF version 1.5 for better compression
                    writer.SetFullCompression(); // Enable full compression

                    doc.Open();

                    foreach (string imagePath in imagePaths)
                    {
                        // Get the width and height of the image
                        int imageWidth;
                        int imageHeight;

                        using (FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                        {
                            Bitmap image = new Bitmap(imageStream);
                            imageWidth = image.Width;
                            imageHeight = image.Height;
                        }

                        // Set the page size of the document to match the size of the image
                        doc.SetPageSize(new iTextSharp.text.Rectangle(imageWidth, imageHeight));
                        doc.NewPage();

                        // Create iTextSharp Image directly from the file path (this avoids using BitmapImage)
                        iTextSharp.text.Image exportedImage = iTextSharp.text.Image.GetInstance(imagePath);

                        // Compress the image to JPEG format with lossy compression (adjust the compression level as needed)
                        exportedImage.CompressionLevel = 9; // 0 (lowest) to 9 (highest)

                        // Reduce the image DPI and apply downsampling (adjust as needed)
                        int targetDpi = 72; // Adjust to the desired DPI value
                        exportedImage.SetDpi(targetDpi, targetDpi);

                        doc.Add(exportedImage);

                        // Delete the temporary image file
                        File.Delete(imagePath);
                    }
                    doc.Close();
                    /*
                    // Optimize the PDF file to remove unnecessary data and reduce size
                    PdfReader reader = new PdfReader(outputFilePath);
                    using (FileStream optimizedFileStream = new FileStream(outputFilePath + "_optimized", FileMode.Create))
                    {
                        PdfStamper stamper = new PdfStamper(reader, optimizedFileStream);
                        stamper.Writer.SetFullCompression();
                        stamper.Writer.CompressionLevel = 9;
                        stamper.FormFlattening = true; // Flatten form fields if necessary
                        //stamper.Writer.RemoveUnusedObjects();
                        stamper.Writer.SetLinearPageMode(); // Enable linearization for fast web view
                        stamper.Writer.SetPdfVersion(PdfWriter.PDF_VERSION_1_5); // Use PDF version 1.5 for better compression
                        stamper.Close();
                        reader.Close();
                    }

                    // Delete the original file and replace it with the optimized one
                    File.Delete(outputFilePath);
                    File.Move(outputFilePath + "_optimized", outputFilePath);
                    */

                  
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /*
        public static void SaveImagesToPdf(List<string> imagePaths, string outputFilePath)
        {
            try
            {
                using (FileStream fs = new FileStream(outputFilePath, FileMode.Create))
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document();
                    PdfWriter writer = PdfWriter.GetInstance(doc, fs);

                    // Apply compression settings to the writer
                    writer.SetPdfVersion(PdfWriter.PDF_VERSION_1_5); // Use PDF version 1.5 for better compression
                    writer.SetFullCompression(); // Enable full compression

                    doc.Open();

                    foreach (string imagePath in imagePaths)
                    {
                        // Get the width and height of the image
                        int imageWidth;
                        int imageHeight;

                        using (FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                        {
                            Bitmap image = new Bitmap(imageStream);
                            imageWidth = image.Width;
                            imageHeight = image.Height;
                        }

                        // Set the page size of the document to match the size of the image
                        doc.SetPageSize(new iTextSharp.text.Rectangle(imageWidth, imageHeight));
                        doc.NewPage();

                        // Create iTextSharp Image directly from the file path (this avoids using BitmapImage)
                        iTextSharp.text.Image exportedImage = iTextSharp.text.Image.GetInstance(imagePath);

                        // Compress the image to JPEG format with lossy compression (adjust the compression level as needed)
                        exportedImage.CompressionLevel = 9; // 0 (lowest) to 9 (highest)
                        exportedImage.SetDpi(300, 300); // Set the image resolution (adjust as needed)
                        doc.Add(exportedImage);

                        // Delete the temporary image file
                        File.Delete(imagePath);
                    }

                    doc.Close();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        */
        public static List<BitmapImage> ExtractImagesFromPDF(FileStream fileStream)
        {
            List<BitmapImage> images = new List<BitmapImage>();

            using (PdfReader reader = new PdfReader(fileStream))
            {
                for (int pageNumber = 1; pageNumber <= reader.NumberOfPages; pageNumber++)
                {
                    PdfDictionary page = reader.GetPageN(pageNumber);
                    PdfDictionary resources = page.GetAsDict(PdfName.RESOURCES);
                    PdfDictionary xObject = resources.GetAsDict(PdfName.XOBJECT);

                    if (xObject != null)
                    {
                        foreach (var key in xObject.Keys)
                        {
                            PdfObject obj = xObject.Get(key);

                            if (obj.IsIndirect())
                            {
                                PdfDictionary imgObject = (PdfDictionary)PdfReader.GetPdfObject(obj);

                                if (imgObject != null)
                                {
                                    PdfName subType = (PdfName)PdfReader.GetPdfObject(imgObject.Get(PdfName.SUBTYPE));

                                    if (PdfName.IMAGE.Equals(subType))
                                    {
                                        PdfImageObject pdfImage = new PdfImageObject((PRStream)imgObject);

                                        // Extract the image bytes
                                        byte[] bytes = pdfImage.GetImageAsBytes();
                                        using (MemoryStream stream = new MemoryStream(bytes))
                                        {

                                            // Convert the image bytes to a BitmapImage
                                            BitmapImage image = new BitmapImage();
                                            image.BeginInit();
                                            image.CacheOption = BitmapCacheOption.OnLoad;
                                            image.StreamSource = stream;
                                            image.EndInit();
                                            images.Add(image);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return images;
        }

    }
}
