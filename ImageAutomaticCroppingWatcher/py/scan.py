import cv2
import numpy as np
import fitz  # PyMuPDF
from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas

# Function to read PDF and extract images
def read_pdf_extract_images(pdf_file):
    images = []
    pdf_document = fitz.open(pdf_file)
    for page_number in range(pdf_document.page_count):
        pdf_page = pdf_document.load_page(page_number)
        pix = pdf_page.get_pixmap()
        image = np.frombuffer(pix.samples, dtype=np.uint8).reshape(pix.h, pix.w, pix.n)
        images.append(image)
    pdf_document.close()
    return images

# Function to save images as a multi-page PDF
def save_images_to_pdf(images, output_file):
    pdf_canvas = canvas.Canvas(output_file, pagesize=letter)
    for image in images:
        image_pil = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        pdf_canvas.drawImage(image_pil.tobytes(), 0, 0, width=image.shape[1], height=image.shape[0])
        pdf_canvas.showPage()
    pdf_canvas.save()

# Read PDF and extract images
pdf_input_file = "2135.pdf"
images = read_pdf_extract_images(pdf_input_file)

# Process each image and store the results
processed_images = []
for img in images:
    hh, ww = img.shape[:2]

    # read template
    template = cv2.imread("3.jpg")
    ht, wd = template.shape[:2]

    # convert img to grayscale
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    # do otsu threshold on gray image
    thresh = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY+cv2.THRESH_OTSU)[1]

    # pad thresh with black to preserve corners when apply morphology
    pad = cv2.copyMakeBorder(thresh, 20, 20, 20, 20, borderType=cv2.BORDER_CONSTANT, value=0)

    # apply morphology
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (15,15))
    morph = cv2.morphologyEx(pad, cv2.MORPH_CLOSE, kernel)

    # remove padding
    morph = morph[20:hh+20, 20:ww+20]

    # get largest external contour
    contours = cv2.findContours(morph, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    contours = contours[0] if len(contours) == 2 else contours[1]
    big_contour = max(contours, key=cv2.contourArea)

    # get perimeter and approximate a polygon
    peri = cv2.arcLength(big_contour, True)
    corners = cv2.approxPolyDP(big_contour, 0.04 * peri, True)

    # draw polygon on input image from detected corners
    polygon = img.copy()
    cv2.polylines(polygon, [corners], True, (0,255,0), 2, cv2.LINE_AA)

    # reformat input corners to x,y list
    sortcorners = []
    for corner in corners:
        pt = [corner[0][0], corner[0][1]]
        sortcorners.append(pt)
    icorners = np.float32(sortcorners)

    # sort corners on y
    def takeSecond(elem):
        return elem[1]
    sortcorners.sort(key=takeSecond)

    # check if the second corner x is left or right of the first corner x
    x1 = sortcorners[0][0]
    x2 = sortcorners[1][0]
    diff = x2 - x1

    # get corresponding output corners from width and height
    if diff >= 0:
        ocorners = [[0, 0], [0, ht], [wd, ht], [wd, 0]]
    else:
        ocorners = [[wd, 0], [0, 0], [0, ht], [wd, ht]]
    ocorners = np.float32(ocorners)

    # get perspective transformation matrix
    M = cv2.getPerspectiveTransform(icorners, ocorners)

    # do perspective transformation
    warped = cv2.warpPerspective(img, M, (wd, ht))

    # Add the processed image to the list
    processed_images.append(warped)

# Save processed images as a new PDF
pdf_output_file = "output.pdf"
save_images_to_pdf(processed_images, pdf_output_file)
print ("finish")