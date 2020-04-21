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
EVIDENCE_FOLDER_PATH = 'C:/RobotReID/SocialDistancingEvidences/'
REDIST_FOLDER_PATH = 'C:/RobotReID/OpenNI2/OpenNI-Windows-x64-2.3.0.63/Redist'

START_CAMERA_ID = 2

isVideoStreamedEnabled = True

DELAY_PER_FRAME = 0.02 #seconds

MIN_CONFIDENCE_SCORE_REQUIRED = 0.5 #percent

MIN_DETECTED_COUNT_REQUIRED = 2 #times min detect count per interval  (below)
MAX_FRAME_CHECKING_TIME_INTERVAL = 6 #seconds  
CAMERA_VIEW_RANGE_IN_PIXEL = 500 #pixel
MAX_ALLOWED_DISTANCE = 90 #cm
MIN_DEPTH_TO_CAMERA = 40 #cm
WARNING_TIME_TOTAL = 17 #seconds
X_ERROR = 10 #cm
DEPTH_ERROR = 0.1 #percent


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
    if (isVideoStreamedEnabled):       
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
    
    
    averageCmPerPixel = 0.342
       
    xDelta = max(0, leftX2 - rightX1) * averageCmPerPixel
    xDelta += X_ERROR
    
    return xDelta < MAX_ALLOWED_DISTANCE   
    
def findAnyPair(allDetectedPeople):
    for i in range(0, len(allDetectedPeople) - 1):
        for j in range(i + 1,len(allDetectedPeople)):
            if (isCloseEachOther(allDetectedPeople[i], allDetectedPeople[j])):
                return True
    return False



def detectImage(cap, camID):
    
    r, img = cap.read()
    
  
    img = cv2.resize(img, (640, 480))
   
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
    
    if len(boxes_cur) > 0:
      
        for i in range(len(boxes_cur)):

            x_output = int(boxes_cur[i][1] + (boxes_cur[i][3] - boxes_cur[i][1])/2)
            y_cen = int(boxes_cur[i][0] + (boxes_cur[i][2] - boxes_cur[i][0])/2)
            
            '''
            # Grab a new depth frame
            frame = depth_stream.read_frame()       
            frame_data = frame.get_buffer_as_uint16()
            # Put the depth frame into a numpy array and reshape it
            img_d = np.frombuffer(frame_data, dtype=np.uint16)
                    
            img_d.shape = (1, 480, 640)
            #img_d.shape = (1, 720, 1280)
            img_d = np.concatenate((img_d, img_d, img_d), axis=0)
            img_d = np.swapaxes(img_d, 0, 2)
            img_d = np.swapaxes(img_d, 0, 1)    
            
            depthWindowName = "Depth camera " + str(camID)
            
            showStreamingVideo(depthWindowName, img_d)
                             
            x_mirror = int(im_w/2) + int(im_w/2 - x_output)
            z = img_d[y_cen][x_mirror]
            distance = round(z[0]/100)
            '''
                              
            #if (distance > MIN_DEPTH_TO_CAMERA): 
            distance = 100
            detectedInfo.append((boxes_cur[i][1], boxes_cur[i][3], distance)) 
            my_string += "(" + str(boxes_cur[i][1]) + ", " + str(boxes_cur[i][3]) + ", " + str(distance)+ ") , "

            #cv2.putText(img,str(x_output) + " , " + str(distance) + "m",(30,50),cv2.FONT_HERSHEY_SIMPLEX,1,(0,0,255),2,cv2.LINE_AA)
                   
        
        print(my_string, end = "=>>>> ", flush = True)
        
        warning = findAnyPair(detectedInfo)
        
        if (warning == True):
            print("CLOSED!!!", flush = True)
            global detected_count, first_detected_time
            
            if (detected_count >= 1):
                
                current_dectected_time = datetime.datetime.now()
                
                timeDelta = (current_dectected_time - first_detected_time).total_seconds()
                              
                if (timeDelta > MAX_FRAME_CHECKING_TIME_INTERVAL):
                    printMessage("Time out! Reset from 1")
                    detected_count = 0
                            
            detected_count += 1
            
            if (detected_count == 1):
                printMessage("---------FIRST DETECTED--------")
                first_detected_time = datetime.datetime.now()    
                
            if (detected_count == MIN_DETECTED_COUNT_REQUIRED):                                      
                printMessage('----------DANGEROUS!!!!!!!!!!---------------')
                saveEvidence(img)
                sendWarningToWinForm(camID)
                global social_distance_detected
                social_distance_detected = True
                time.sleep(WARNING_TIME_TOTAL)
                social_distance_detected = False
                detected_count = 0
        else:
            print('------', flush=True)

    else:        
        pass
                          
    windowName = "Camera " + str(camID)
    showStreamingVideo(windowName, img)

def showStreamingVideo(windowsName, img):
    
    if (isVideoStreamedEnabled):
        cv2.imshow(windowsName, img)
    
def saveEvidence(image):
    
    #savedTime = datetime.datetime.now()
    
    #year = savedTime.year
    #day = savedTime.day
    #month = savedTime.month
    #hour = savedTime.hour
    #minute = savedTime.minute
    #second = savedTime.second
    
    #fileName = str(year) + "_" + str(month) + "_" + str(day) + "..." + str(hour) + "h" + str(minute) + "m" + str(second) + "s"
    
    fileName = "Evidence.jpg"
    cv2.imwrite(EVIDENCE_FOLDER_PATH + fileName, image)
    

def sendWarningToWinForm(camID):
    if (camID == 2):
        print('social_distancing_warning_front', flush=True)
    else:
        print('social_distancing_warning_back', flush=True)
        

def detectSocialDistancing():
    
    printMessage("Start detecting")
    global odapi
   
    
    depth_streams = []
    
    '''
    for i in range(len(dev)):  
        
        #print(dev[i].get_device_info())
        
        depth_stream = dev[i].create_depth_stream()
        
        depth_stream.set_video_mode(c_api.OniVideoMode(pixelFormat=c_api.OniPixelFormat.ONI_PIXEL_FORMAT_DEPTH_100_UM,
                                                           resolutionX=640,
                                                           resolutionY=480,
                                                           fps=30))
        
        depth_stream.start()
        depth_streams.append(depth_stream)
    '''
    threads = []
    
    '''
    print("depth stream started", flush=True)
    for i in range(len(depth_streams)):    
        thread = cameraThread(START_CAMERA_ID + i, depth_streams[i])
        thread.start()
        threads.append(thread) 
    
  
    for t in threads:
        t.join()
    '''
    thread1 = cameraThread(2)
    thread2 = cameraThread(3)

    thread1.start()
    thread2.start()

    thread1.join()
    thread2.join()
    
        


if __name__ == "__main__":
    
    global odapi
    
    printMessage('before odapi')
    odapi = DetectorAPI(path_to_ckpt=MODEL_PATH)
    
    printMessage('after odapi')
    #openDepthCamera()
   
    detectSocialDistancing() 
    
