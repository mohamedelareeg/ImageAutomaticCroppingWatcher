import cv2
import numpy as np

imagename = "2135_page-0009.jpg"
# read image
img = cv2.imread(imagename)
hh, ww = img.shape[:2]

# read template
template = cv2.imread("2135_page-0009.jpg")
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

# print the number of found corners and the corner coordinates
# They seem to be listed counter-clockwise from the top most corner
print(len(corners))
print(corners)

# reformat input corners to x,y list
sortcorners = []
for corner in corners:
    pt = [ corner[0][0],corner[0][1] ]
    sortcorners.append(pt)
icorners = np.float32(sortcorners)

# sort corners on y
def takeSecond(elem):
    return elem[1]
sortcorners.sort(key=takeSecond)

# check if second corner x is left or right of first corner x
x1 = sortcorners[0][0]
x2 = sortcorners[1][0]
diff = x2 - x1
print(x1, x2)

# get corresponding output corners from width and height
if diff >= 0:
    ocorners = [ [0,0], [0,ht], [wd,ht], [wd,0] ]
else:
    ocorners = [ [wd,0], [0,0], [0,ht], [wd,ht]]
ocorners = np.float32(ocorners)


# get perspective tranformation matrix
M = cv2.getPerspectiveTransform(icorners, ocorners)

# do perspective 
warped = cv2.warpPerspective(img, M, (wd, ht))

# write results
#cv2.imwrite("output/thresh.jpg", thresh)
#cv2.imwrite("output/morph.jpg", morph)
#cv2.imwrite("output/polygon.jpg", polygon)
cv2.imwrite("output/" + imagename, warped)

# display it
#cv2.imshow("warped", warped)
#cv2.waitKey(0)