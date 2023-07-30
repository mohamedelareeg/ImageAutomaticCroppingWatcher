import cv2
import numpy as np


def largest_contour(contours):
    largest = None
    max_area = 0
    for contour in contours:
        area = cv2.contourArea(contour)
        if area > 1000:
            peri = cv2.arcLength(contour, True)
            approx = cv2.approxPolyDP(contour, 0.04 * peri, True)
            if area > max_area and len(approx) == 4:
                largest = approx
                max_area = area
    return largest


def detect_lines(image):
    # Convert the image to grayscale
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    # Apply adaptive thresholding to extract lines
    thresh = cv2.adaptiveThreshold(gray, 255, cv2.ADAPTIVE_THRESH_MEAN_C, cv2.THRESH_BINARY_INV, 21, 10)

    # Perform morphological operations to enhance line detection
    kernel = np.ones((3, 3), np.uint8)
    opening = cv2.morphologyEx(thresh, cv2.MORPH_OPEN, kernel, iterations=1)
    dilated = cv2.dilate(opening, kernel, iterations=2)

    # Find contours of lines
    contours, hierarchy = cv2.findContours(dilated, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    # Create a mask for the lines
    mask = np.zeros_like(image)

    # Draw contours on the mask
    cv2.drawContours(mask, contours, -1, (255, 255, 255), 2)

    return mask


def correct_perspective(image):
    # Convert the image to grayscale
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    # Apply Canny edge detection
    edges = cv2.Canny(gray, 50, 150, apertureSize=3)

    # Perform Hough Line Transform to detect lines
    lines = cv2.HoughLinesP(edges, rho=1, theta=np.pi/180, threshold=100, minLineLength=100, maxLineGap=5)

    # Calculate the average angle of detected lines
    angles = []
    for line in lines:
        x1, y1, x2, y2 = line[0]
        angle = np.arctan2(y2 - y1, x2 - x1) * 180 / np.pi
        angles.append(angle)

    avg_angle = np.mean(angles)

    # Rotate the image to correct the lines
    rows, cols = image.shape[:2]
    M = cv2.getRotationMatrix2D((cols / 2, rows / 2), -avg_angle, 1)
    corrected_image = cv2.warpAffine(image, M, (cols, rows))

    return corrected_image
def make_lines_perpendicular(image):
    # Convert the image to grayscale
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    # Apply Canny edge detection
    edges = cv2.Canny(gray, 50, 150, apertureSize=3)

    # Perform Hough Line Transform to detect lines
    lines = cv2.HoughLines(edges, 1, np.pi/180, threshold=100)

    # Calculate the angle of the most prominent line
    angles = []
    for line in lines:
        rho, theta = line[0]
        angle = theta * 180 / np.pi
        angles.append(angle)

    avg_angle = np.mean(angles)
    desired_angle = np.pi/2  # 90 degrees (perpendicular)

    # Calculate the rotation angle
    rotation_angle = desired_angle - avg_angle

    # Rotate the image to make the lines perpendicular
    rows, cols = image.shape[:2]
    M = cv2.getRotationMatrix2D((cols / 2, rows / 2), rotation_angle, 1)
    rotated_image = cv2.warpAffine(image, M, (cols, rows))

    return rotated_image
def correct_lines(image):
    # Convert the image to grayscale
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    # Apply Canny edge detection
    edges = cv2.Canny(gray, 50, 150, apertureSize=3)

    # Perform Hough Line Transform to detect lines
    lines = cv2.HoughLinesP(edges, rho=1, theta=np.pi/180, threshold=100, minLineLength=100, maxLineGap=5)

    # Calculate the average angle of detected lines
    angles = []
    for line in lines:
        x1, y1, x2, y2 = line[0]
        angle = np.arctan2(y2 - y1, x2 - x1) * 180 / np.pi
        angles.append(angle)

    avg_angle = np.mean(angles)

    # Calculate the rotation angle needed to make the lines perpendicular
    rotation_angle = 90 - avg_angle

    # Get the center of the image
    rows, cols = image.shape[:2]
    center = (cols / 2, rows / 2)

    # Get the rotation matrix for the affine transformation
    M = cv2.getRotationMatrix2D(center, rotation_angle, 1)

    # Perform the affine transformation to correct the lines
    corrected_image = cv2.warpAffine(image, M, (cols, rows))

    return corrected_image

def correct_skew(image):
    # Convert the image to grayscale
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    # Apply Canny edge detection
    edges = cv2.Canny(gray, 50, 150, apertureSize=3)

    # Perform Hough Line Transform to detect lines
    lines = cv2.HoughLinesP(edges, rho=1, theta=np.pi / 180, threshold=100, minLineLength=100, maxLineGap=5)

    # Calculate the average angle of detected lines
    angles = []
    for line in lines:
        x1, y1, x2, y2 = line[0]
        angle = np.arctan2(y2 - y1, x2 - x1) * 180 / np.pi
        angles.append(angle)

    avg_angle = np.mean(angles)

    # Rotate the image to correct the skew
    rows, cols = image.shape[:2]
    M = cv2.getRotationMatrix2D((cols / 2, rows / 2), -avg_angle, 1)
    corrected_image = cv2.warpAffine(image, M, (cols, rows))

    return corrected_image

imagename = "input/2.jpg"
# read image
img = cv2.imread(imagename)
hh, ww = img.shape[:2]

# Convert img to grayscale
gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

# Do otsu threshold on gray image
thresh = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)[1]

# Pad thresh with black to preserve corners when applying morphology
pad = cv2.copyMakeBorder(thresh, 20, 20, 20, 20, borderType=cv2.BORDER_CONSTANT, value=0)

# Apply morphology
kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (15, 15))
morph = cv2.morphologyEx(pad, cv2.MORPH_CLOSE, kernel)

# Remove padding
morph = morph[20:hh + 20, 20:ww + 20]

# Get largest external contour
contours = cv2.findContours(morph, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
contours = contours[0] if len(contours) == 2 else contours[1]
big_contour = max(contours, key=cv2.contourArea)

# Get perimeter and approximate a polygon
peri = cv2.arcLength(big_contour, True)
corners = cv2.approxPolyDP(big_contour, 0.04 * peri, True)

# Reformat input corners to x,y list
sortcorners = []
for corner in corners:
    pt = [corner[0][0], corner[0][1]]
    sortcorners.append(pt)
icorners = np.float32(sortcorners)

# Sort corners on y
def takeSecond(elem):
    return elem[1]
sortcorners.sort(key=takeSecond)

# Check if the second corner x is left or right of the first corner x
x1 = sortcorners[0][0]
x2 = sortcorners[1][0]
diff = x2 - x1
print(x1, x2)

# Read template
template = cv2.imread("input/2.jpg")
ht, wd = template.shape[:2]

# Get corresponding output corners from width and height
if diff >= 0:
    ocorners = [[0, 0], [0, ht], [wd, ht], [wd, 0]]
else:
    ocorners = [[wd, 0], [0, 0], [0, ht], [wd, ht]]
ocorners = np.float32(ocorners)

# Get perspective transformation matrix
M = cv2.getPerspectiveTransform(icorners, ocorners)

# Do perspective transformation
warped = cv2.warpPerspective(img, M, (wd, ht))

# Detect lines in the paper
lines_mask = detect_lines(warped)

# Bitwise AND the lines mask with the warped image
warped_lines = cv2.bitwise_and(warped, lines_mask)

# Correct the skew
corrected_skew = correct_skew(warped_lines)

# Display the results
cv2.imshow("Original Image", img)
cv2.imshow("Polygon", cv2.polylines(img.copy(), [corners], True, (0, 255, 0), 2, cv2.LINE_AA))
cv2.imshow("Warped Perspective", warped)
cv2.imshow("Warped with Lines", warped_lines)
cv2.imshow("Corrected Skew", corrected_skew)

output_filename = "output/corrected_image.jpg"
cv2.imwrite(output_filename, corrected_skew)

cv2.waitKey(0)
cv2.destroyAllWindows()