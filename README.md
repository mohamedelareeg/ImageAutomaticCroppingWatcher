The Image Automatic Cropping Watcher is a tool designed to automatically detect
PDF files, convert them to images, perform automatic skew correction, and then
recompile the corrected images back into a PDF file. This process is fully
automated and can be used to enhance the quality of scanned documents or images
with perspective distortion.

-------------------------------------------------------------------------------

Functionality
-------------

The main functionalities of the Image Automatic Cropping Watcher include:

1. **PDF Detection**: The application monitors a specified directory for incoming
   PDF files.

2. **PDF to Image Conversion**: Upon detecting a PDF file, the application
   converts each page of the PDF into an image.

3. **Automatic Perspective Correction**: The images extracted from the PDF are
   automatically processed to correct any perspective distortion or skew.

4. **Compilation into PDF**: Finally, the corrected images are compiled into a
   new PDF file, resulting in a document with improved image quality.
-------------------------------------------------------------------------------

Usage
-----

To utilize the Image Automatic Cropping Watcher:

1. **Clone the Repository**: Clone the repository to your local machine using
   the following command:

   git clone https://github.com/mohamedelareeg/ImageAutomaticCroppingWatcher.git

2. **Build the Solution**: Open the solution in Visual Studio or your preferred
IDE and build it to compile the project.

3. **Install Dependencies**: Ensure that the necessary dependencies are
installed and configured. This project relies on the following dependencies:
- iTextSharp: Used for handling PDF files and compilation of corrected images
  into a PDF.
- OpenCVSharp: Used for image processing tasks such as perspective correction
  and skew detection.
- Newtonsoft.Json: Used for JSON serialization and deserialization.

4. **Run the Application**: Start the application. It will begin monitoring the
specified directory for incoming PDF files.

5. **Monitor Process**: Once a PDF file is detected, the application will
automatically convert its pages to images, correct any perspective distortion,
and compile the corrected images into a new PDF file.

-------------------------------------------------------------------------------

Dependencies
------------

The Image Automatic Cropping Watcher relies on the following dependencies:

- **iTextSharp**: Used for handling PDF files and compilation of corrected images
into a PDF.
- **OpenCVSharp**: Used for image processing tasks such as perspective correction
and skew detection.
- **Newtonsoft.Json**: Used for JSON serialization and deserialization.

Ensure that these dependencies are installed and properly configured in your
development environment before running the application.

-------------------------------------------------------------------------------

Contributing
------------

Contributions are welcome! If you'd like to contribute to the Image Automatic
Cropping Watcher, feel free to open a pull request or submit an issue on the
GitHub repository.

-------------------------------------------------------------------------------

License
-------

This project is licensed under the MIT License - see the LICENSE file for
details.

-------------------------------------------------------------------------------

Acknowledgments
---------------

- iTextSharp: For PDF handling functionality.
- OpenCVSharp: For image processing capabilities.
- Newtonsoft.Json: For JSON serialization and deserialization.
