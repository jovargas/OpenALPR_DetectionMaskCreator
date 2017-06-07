# OpenALPR Detection Mask Tool
Tool made in C# that helps to create a detection mask for OpenALPR

How to use:
- Click on the "Load Image" button and open the image you want to use as reference.
- Click on the picture box and start drawing a polygon that represents the area you want to filter for detecting license plates.
- Use right-click to close a polygon area (you can draw as many polygons as you wish).
- If you want to clear the drawn polygons and start over, click on the "Clear Mask" button.
- If you want to save a mask that contains the whole image, just click on the "Clear Mask" button and then "Save".
- To show the process images, select the "Debug images" option.
- To save the mask image, click on the "Save" button and choose a path to save the file.
- In the openalpr.conf file set the parameter: detection_mask_image = [path/where/you/saved/the/mask.jpg]

The saved file will contain a B&W image where white spaces represent the areas of the frame that will be used for detection and the black spaces will be ignored, reducing the total processing time of the OpenALPR detector. 

The created mask will ALWAYS match the reference image size.
