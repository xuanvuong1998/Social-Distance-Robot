import cv2
import numpy as np
import tensorflow as tf
import threading
import math
import datetime
import time
from run import Reid

#for Orbbec depth value
from primesense import openni2
from primesense import _openni2 as c_api

#############CONSTANTS################

MODEL_PATH = './ssd_inception_v2/frozen_inference_graph.pb'
EVIDENCE_FOLDER_PATH = 'C:/RobotReID/SocialDistancingEvidences'
REDIST_FOLDER_PATH = 'C:/RobotReID/OpenNI2/OpenNI-Windows-x64-2.3.0.63/Redist'
DEPTH_OUTPUT_FOLDER = "C:/RobotReID/Depth_Output/depth_value.txt"

#############CAMERA SETTINGS###############

START_CAMERA_ID = 2 # 0, 1 are built-in windows cameras
END_CAMERA_ID = 3
FRONT_CAMERA_ID = 3
BACK_CAMERA_ID = 2

IMAGE_RESIZE_WIDTH = 640
IMAGE_RESIZE_HEIGHT = 480
IS_VIDEO_STREAMED_ENABLED = True
CAMERA_DEGREE_RANGE = 60 #degreee
LEFT_MOST_PIXEL = 65 #pixel
RIGHT_MOST_PIXEL = 578 #pixel
X_ERROR = 10 #cm

ZERO_DEGREE_PIXEL = LEFT_MOST_PIXEL
DEGREE_TO_PIXEL = (RIGHT_MOST_PIXEL - LEFT_MOST_PIXEL) / CAMERA_DEGREE_RANGE 
 
CAMERA_PIXEL_RANGE = RIGHT_MOST_PIXEL - LEFT_MOST_PIXEL #pixel


###########PEOPLE DECTION SETTINGS############

MIN_CONFIDENCE_SCORE_REQUIRED = 0.5 #percent
DELAY_PER_FRAME = 0.01 #seconds
MIN_DETECTED_COUNT_REQUIRED = 1 #min times detected required per interval  (FRAME_CHECKING_INTERVAL)
FRAME_CHECKING_INTERVAL = 5 #seconds 

MAX_SOCIAL_DISTANCE_ALLOWED = 90 #cm
MIN_DEPTH_DIS_REQUIRED = 40 #cm
MAX_DEPTH_DIS_ACCEPTED = 1000 #cm 

WARNING_TIME_TOTAL = 15 #seconds

READ_DEPTH_DATA_INTERVAL = 0.4 #seconds (read depth from file per interval)
READ_DEPTH_MAX_TRIES = 5 #times ->> Total time: READ_DEPTH_MAX_TRIES * READ_DEPTH_DATA_INTERVAL

##############################################


######################################

###############NORMAL VARIABLES#################

first_detected_time = 0
detected_count = 0
social_distance_detected = False

################################################

def printMessage(mess):
    print(mess, flush =True)

def openDepthCamera():
    
    print("opening depth camera", flush=True)
    global dev
    try:
        openni2.initialize(REDIST_FOLDER_PATH)
    except:
        print("***Device not initialized***")
    try:
        dev = openni2.Device.open_all()
    except:
        print("***Unable to open the device***")
            
def wait(delayTime):
    if (IS_VIDEO_STREAMED_ENABLED):       
        key = cv2.waitKey(int(delayTime * 1000))   
        if key & 0xFF == ord('q'):
            return False
    else:    
        time.sleep(DELAY_PER_FRAME)    

class DetectorAPI:
    def __init__(self, path_to_ckpt):
        self.reid = Reid()
        self.path_to_ckpt = path_to_ckpt
        # self.module = import_module('run')
        self.detection_graph = tf.Graph()
        with self.detection_graph.as_default():
            od_graph_def = tf.GraphDef()
            with tf.gfile.GFile(self.path_to_ckpt, 'rb') as fid:
                serialized_graph = fid.read()
                od_graph_def.ParseFromString(serialized_graph)
                tf.import_graph_def(od_graph_def, name='')

        self.default_graph = self.detection_graph.as_default()
        self.sess = tf.Session(graph=self.detection_graph)

        self.image_tensor = self.detection_graph.get_tensor_by_name('image_tensor:0')
        self.detection_boxes = self.detection_graph.get_tensor_by_name('detection_boxes:0')
        self.detection_scores = self.detection_graph.get_tensor_by_name('detection_scores:0')
        self.detection_classes = self.detection_graph.get_tensor_by_name('detection_classes:0')
        self.num_detections = self.detection_graph.get_tensor_by_name('num_detections:0')

    def processFrame(self, image):
        image_np_expanded = np.expand_dims(image, axis=0)
        (boxes, scores, classes, num) = self.sess.run(
            [self.detection_boxes, self.detection_scores, self.detection_classes, self.num_detections],
            feed_dict={self.image_tensor: image_np_expanded})

        im_height, im_width, _ = image.shape
        boxes_list = [None for i in range(boxes.shape[1])]
        for i in range(boxes.shape[1]):
            boxes_list[i] = (int(boxes[0, i, 0] * im_height),
                             int(boxes[0, i, 1] * im_width),
                             int(boxes[0, i, 2] * im_height),
                             int(boxes[0, i, 3] * im_width))

        return boxes_list, scores[0].tolist(), [int(x) for x in classes[0].tolist()], int(num[0])

    def close(self):
        self.sess.close()
        self.default_graph.close()


class cameraThread(threading.Thread):
    def __init__(self, camID):
        threading.Thread.__init__(self)
        self.camID = camID
    def run(self):
        print ("Starting thread ")
        loopDetecting(self.camID)       

def loopDetecting(camID):
    cap = cv2.VideoCapture(camID)
    
    print("looping ")
    while True:    
        if (social_distance_detected == False): 
            detectImage(cap, camID)

        cont = wait(DELAY_PER_FRAME)
        
        if (cont == False):
            break
        
    cap.release()
    openni2.unload()
    cv2.destroyAllWindows()



def isCloseEachOther(left, right):
    
    leftX1 = left[0]
    rightX1 = left[1]
    d1 = left[2]
    
    leftX2 = right[0]
    rightX2 = right[1]
    d2 = right[2]
    
    #swap if frame1 X > frame2 X 
    if (leftX1 > leftX2):
        tmp = leftX1
        leftX1 = leftX2
        leftX2 = tmp
        tmp = rightX1
        rightX1 = rightX2
        rightX2 = tmp
    
    '''
    averageCmPerPixel = ((d1 + d2) / 2) / CAMERA_PIXEL_RANGE
        
    xDelta = max(0, leftX2 - rightX1) * averageCmPerPixel
    xDelta += X_ERROR
    dDelta = abs(d1 - d2)
     
    finalDis = round(math.sqrt(xDelta * xDelta + dDelta * dDelta))
    '''
    
    angleInDegree = CAMERA_DEGREE_RANGE - ((leftX2 - rightX1) + CAMERA_PIXEL_RANGE) / DEGREE_TO_PIXEL
    
    angleInRad = angleInDegree * math.pi / 180 
    
    exactFinalDis = math.sqrt(d1 * d1 + d2 * d2 - 2 * d1 * d2 * math.cos(angleInRad))
    
    exactFinalDis = round(exactFinalDis)
    
    print('-------FINAL DISTANCE = ' + str(exactFinalDis) , end = " ** ")
    
    return exactFinalDis < MAX_SOCIAL_DISTANCE_ALLOWED
    
def findAnyPair(allDetectedPeople):
    for i in range(0, len(allDetectedPeople) - 1):
        for j in range(i + 1,len(allDetectedPeople)):
            if (isCloseEachOther(allDetectedPeople[i], allDetectedPeople[j])):
                return True
    return False



def detectImage(cap, camID):
    
    r, img = cap.read()
    
    img = cv2.resize(img, (IMAGE_RESIZE_WIDTH, IMAGE_RESIZE_HEIGHT))
   
    im_h, im_w, _ = img.shape
   
    boxes, scores, classes, num = odapi.processFrame(img) 
    
    boxes_cur = []

    for i in range(len(boxes)):
        # Class 1 represents human
        if classes[i] == 1 and scores[i] > MIN_CONFIDENCE_SCORE_REQUIRED:
            box = boxes[i]

            # draw the bounding box on the image
            cv2.rectangle(img, (box[1], box[0]), (box[3], box[2]), (255, 0, 0), 2)
            
            boxes_cur.append(box) 
                        
    my_string = ""
    
    detectedInfo = []
    
    windowName = "Camera " + str(camID)
    showStreamingVideo(windowName, img)
    
    if len(boxes_cur) > 0:    
        x_angles = ""
        for i in range(len(boxes_cur)):

            x_output = int(boxes_cur[i][1] + (boxes_cur[i][3] - boxes_cur[i][1])/2)
            y_cen = int(boxes_cur[i][0] + (boxes_cur[i][2] - boxes_cur[i][0])/2)
            
            x_angle = round((x_output - ZERO_DEGREE_PIXEL) / (DEGREE_TO_PIXEL))
                    
            x_angles = x_angles +str( x_angle)
            
            if (i < len(boxes_cur) - 1):
                x_angles = x_angles + ","
            
        print("x_angle," + x_angles, flush = True)
          
        d = "" 
                
        wait_for_depth_vl_cnt = 0
        
        while len(d) == 0:     
            wait_for_depth_vl_cnt = wait_for_depth_vl_cnt + 1
            
            if (wait_for_depth_vl_cnt >= READ_DEPTH_MAX_TRIES):
                print('give up get depth val', flush = True)
                break
            try:
                
                f = open(DEPTH_OUTPUT_FOLDER, "r+")
                d = f.readline()
                
                f.close()
                time.sleep(READ_DEPTH_DATA_INTERVAL)
            except:
                print('read file err')
         
        clearContentDone = False
        
        while(clearContentDone == False):           
            try:
                f = open(DEPTH_OUTPUT_FOLDER, "r+")
                
                f.truncate(0)
                
                f.close()
                
                clearContentDone = True
            except:               
                print('truncate not successfully!')
                                
        #clear content inside
        
        if (len(d) == 0):
            return
        
        print('dis received from c# ' + d)
                         
        dis = d.split(',')
     
        for i in range(len(boxes_cur)):
          
            distance = float(dis[i])
                              
            if (distance > MIN_DEPTH_DIS_REQUIRED and distance < MAX_DEPTH_DIS_ACCEPTED): 
                detectedInfo.append((boxes_cur[i][1], boxes_cur[i][3], distance))
                my_string += "(" + str(boxes_cur[i][1]) + ", " + str(boxes_cur[i][3]) + ", " + str(distance)+ ") , "

                cv2.putText(img,str(x_output) + " , " + str(x_angle) + " , " +  str(distance) + "m",(30,50),cv2.FONT_HERSHEY_SIMPLEX,1,(0,0,255),2,cv2.LINE_AA)
                   
        
        print(my_string, end = "=>>>> ", flush = True)
        
        warning = findAnyPair(detectedInfo)
        
        if (warning == True):
            print("CLOSED!!!", flush = True)
            global detected_count, first_detected_time
            
            if (detected_count >= 1):
                
                current_dectected_time = datetime.datetime.now()
                
                timeDelta = (current_dectected_time - first_detected_time).total_seconds()
                              
                if (timeDelta > FRAME_CHECKING_INTERVAL):
                    printMessage("Time out! Reset from 1")
                    detected_count = 0
                            
            detected_count += 1
            
            if (detected_count == 1):
                printMessage("---------FIRST DETECTED--------")
                first_detected_time = datetime.datetime.now()    
                
            if (detected_count == MIN_DETECTED_COUNT_REQUIRED):                                      
                printMessage('----------DANGEROUS!!!!!!!!!!---------------')
                saveEvidence(img)
                sendWarningToWinForm()
                global social_distance_detected
                social_distance_detected = True
                #time.sleep(WARNING_TIME_TOTAL)
                social_distance_detected = False
                detected_count = 0
        else:
            print('------', flush=True)
        
    else:        
        pass
                          
    

def showStreamingVideo(windowsName, img):
    
    
    if (IS_VIDEO_STREAMED_ENABLED):
        cv2.imshow(windowsName, img)
    
def saveEvidence(image):
      
    fileName = "Evidence.jpg"
    cv2.imwrite(EVIDENCE_FOLDER_PATH + fileName, image)
    

def sendWarningToWinForm():
    print('social_distancing_warning', flush=True)

def detectSocialDistancing():
    
    printMessage("Start detecting")
    global odapi
   
    threads = []
    
    print("depth stream started", flush=True)
    for i in range(START_CAMERA_ID, END_CAMERA_ID + 1):    
        thread = cameraThread(i)
        thread.start()
        threads.append(thread) 
        
    for t in threads:
        t.join()
        

if __name__ == "__main__":
     
    global odapi
    
    printMessage('before odapi')
    odapi = DetectorAPI(path_to_ckpt=MODEL_PATH)
    
    printMessage('after odapi')
    #openDepthCamera()
   
    detectSocialDistancing() 
    