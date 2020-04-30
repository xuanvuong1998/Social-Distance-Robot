import cv2
import numpy as np
import tensorflow as tf
import threading
import math
import datetime
import time
import ros_helper

#from keras.models import model_from_json
from utils.anchor_generator import generate_anchors
from utils.anchor_decode import decode_bbox
from utils.nms import single_class_non_max_suppression
from load_model.tensorflow_loader import load_tf_model, tf_inference

#for Orbbec depth value
from primesense import openni2

#############CONSTANTS################

MASK_DETECTION_ENABLED = True
SOCIAL_DISTANCE_DETECTION_ENABLED = True


MODEL_PATH = './ssd_inception_v2/frozen_inference_graph.pb'
SOCIAL_DIS_VIOLATION_FOLDER = 'C:/RobotReID/SocialDistancingEvidences/'
MASK_VIOLATION_EVIDENCE_PATH = 'C:/RobotReID/MaskViolationEvidences/'
REDIST_FOLDER_PATH = 'C:/RobotReID/OpenNI2/OpenNI-Windows-x64-2.3.0.63/Redist'
DEPTH_OUTPUT_FOLDER = "C:/RobotReID/Depth_Output/depth_value.txt"

LOGGING_ENABLED = True

#############CAMERA SETTINGS###############

START_CAMERA_ID = 2 # 2 ( 0, 1 are built-in windows cameras)
END_CAMERA_ID = 2 # 3
FRONT_CAMERA_ID = 2
BACK_CAMERA_ID = 3 

IMAGE_RESIZE_WIDTH = 640
IMAGE_RESIZE_HEIGHT = 480
IS_VIDEO_STREAMED_ENABLED = False
CAMERA_DEGREE_RANGE = 60 #degreee
LEFT_MOST_PIXEL = 65 #pixel
RIGHT_MOST_PIXEL = 578 #pixel

ZERO_DEGREE_PIXEL = LEFT_MOST_PIXEL
DEGREE_TO_PIXEL = (RIGHT_MOST_PIXEL - LEFT_MOST_PIXEL) / CAMERA_DEGREE_RANGE 
 
CAMERA_PIXEL_RANGE = RIGHT_MOST_PIXEL - LEFT_MOST_PIXEL #pixel

###########PEOPLE DETECTION SETTINGS############

MIN_CONFIDENCE_SCORE_REQUIRED = 0.5 #percent
DELAY_PER_FRAME = 0.05 #seconds 
MIN_SOCIAL_DIS_DETECTED_PER_INTERVAL = 1 #min times detected required per interval  (SOCIAL_DISTANCE_CHECKING_INTERVAL)
SOCIAL_DISTANCE_CHECKING_INTERVAL = 5 #seconds 

MAX_SOCIAL_DISTANCE_ALLOWED = 85 #cm
MIN_DEPTH_DIS_REQUIRED = 150 #cm
MAX_DEPTH_DIS_ACCEPTED = 800 #cm 
MIN_DIS_FROM_CAM_TO_PERSON = 160 #cm
PERSON_GO_AWAY_TIME = 12 #seconds

SOCIAL_DIS_WARNING_TIME = 15 #seconds
MASK_VIOLATION_WARNING_TIME = 8

#2 people close each other should have maximum a specific times scale bounding boxes  
MAX_AREA_SCALE_BOUNDING_BOX = 1.7 #times

X_DELTA_MIN_ACCEPTED = 40 #pixel xdelta bounding_box
Y_DELTA_MIN_ACCEPTED = 110 #pixel ydelta bounding_box

READ_DEPTH_FREQUENCY = 0.4 #seconds (read depth from file per interval)
READ_DEPTH_MAX_TRIES = 4 #times ->> Total time: READ_DEPTH_MAX_TRIES * READ_DEPTH_FREQUENCY

##############################################

######################################

###############NORMAL VARIABLES#################

first_detected_time = 0
detected_count = 0
violation_detected = False

first_mask_detected_time = 0
mask_detected_count = 0
MASK_CHECKING_INTERVAL = 3 #seconds


################################################

#########MASK DETECTION##########################

sess, graph = load_tf_model('models/face_mask_detection.pb')
# anchor configuration
feature_map_sizes = [[33, 33], [17, 17], [9, 9], [5, 5], [3, 3]]
anchor_sizes = [[0.04, 0.056], [0.08, 0.11], [0.16, 0.22], [0.32, 0.45], [0.64, 0.72]]
anchor_ratios = [[1, 0.62, 0.42]] * 5

# generate anchors
anchors = generate_anchors(feature_map_sizes, anchor_sizes, anchor_ratios)

# for inference , the batch size is 1, the model output shape is [1, N, 4],
# so we expand dim for anchors to [1, anchor_num, 4]
anchors_exp = np.expand_dims(anchors, axis=0)

id2class = {0: 'Mask', 1: 'NoMask'}

MASK_DETECT_CAMERA_ID = FRONT_CAMERA_ID
MASK_DETECTION_MIN_CONFIDENCE_SCORE = 0.8
MIN_BOX_WIDTH_FOR_MASK_DECTECT = 40 #pixel
MIN_MASK_VIOLATION_CHECKING_INTERVAL = 3 #seconds

#################################################

def printMessage(mess):
    if (LOGGING_ENABLED):    
        print(mess, flush =True)

def openDepthCamera():
    
    printMessage("opening depth camera")
    global dev
    try:
        openni2.initialize(REDIST_FOLDER_PATH)
    except:
        printMessage("***Device not initialized***")
    try:
        dev = openni2.Device.open_all()
    except:
        printMessage("***Unable to open the device***")
            
def wait(delayTime):
    if (IS_VIDEO_STREAMED_ENABLED):       
        key = cv2.waitKey(int(delayTime * 1000))   
        if key & 0xFF == ord('q'):
            return False
    else:    
        time.sleep(delayTime)    

class DetectorAPI:
    def __init__(self, path_to_ckpt):
        
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
        printMessage("Starting thread ")
        loopDetecting(self.camID)       

def loopDetecting(camID):
    cap = cv2.VideoCapture(camID)
    
    printMessage("looping ")
    while True:    
        if (violation_detected == False): 
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
    box_area1 = left[3]
    
    leftX2 = right[0]
    rightX2 = right[1]
    d2 = right[2]
    box_area2 = right[3]
    
    area_rate = box_area1 / box_area2
    
    if (area_rate < 1): 
        area_rate = 1 / area_rate
    
    if (area_rate >= MAX_AREA_SCALE_BOUNDING_BOX):
        return False
    
    #swap if frame1 X > frame2 X 
    if (leftX1 > leftX2):
        tmp = leftX1
        leftX1 = leftX2
        leftX2 = tmp
        tmp = rightX1
        rightX1 = rightX2
        rightX2 = tmp
    
    xDelta = max(0, leftX2 - rightX1)
    
    angleInDegree = CAMERA_DEGREE_RANGE - (xDelta + CAMERA_PIXEL_RANGE) / DEGREE_TO_PIXEL
    
    angleInRad = angleInDegree * math.pi / 180 
    
    exactFinalDis = math.sqrt(d1 * d1 + d2 * d2 - 2 * d1 * d2 * math.cos(angleInRad))
    
    exactFinalDis = round(exactFinalDis)
    
    if (exactFinalDis < MAX_SOCIAL_DISTANCE_ALLOWED):
        printMessage('-------FINAL DISTANCE = ' + str(exactFinalDis))
    
    return exactFinalDis < MAX_SOCIAL_DISTANCE_ALLOWED
    
def findAnyPair(allDetectedPeople):
    for i in range(0, len(allDetectedPeople) - 1):
        for j in range(i + 1,len(allDetectedPeople)):
            if (isCloseEachOther(allDetectedPeople[i], allDetectedPeople[j])):
                return True
    return False

        
def getDepthsFromAngles(angles):
    
    ros_helper.pub_angles_to_ros(angles)
    
    d = ros_helper.getDepths()
    
    ros_helper.clearPreviousDepths()
    
    return d 
    
def isFrontCAM(camID):
    return camID == FRONT_CAMERA_ID

def detectImage(cap, camID):
    
    status, img = cap.read()
    
    img = cv2.resize(img, (IMAGE_RESIZE_WIDTH, IMAGE_RESIZE_HEIGHT))
        
    img_raw = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    if (status and MASK_DETECTION_ENABLED):
        inference(img_raw,
                  MASK_DETECTION_MIN_CONFIDENCE_SCORE,
                  iou_thresh=MASK_DETECTION_MIN_CONFIDENCE_SCORE,
                  target_shape=(260, 260),
                  draw_result=True,
                  show_result=False) 
        
        showStreamingVideo("MASK DETECTION", img_raw)
            
    global violation_detected
    if (violation_detected):
        return
        
    if (SOCIAL_DISTANCE_DETECTION_ENABLED == False):
        return
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
    
    windowName = "SocialDistancing " + str(camID)
    showStreamingVideo(windowName, img)
    
    if len(boxes_cur) > 0:    
        x_angles = ""
        
        valid_boxes_cnt = 0
        for i in range(len(boxes_cur)):

            x_delta = boxes_cur[i][3] - boxes_cur[i][1]
            y_delta = boxes_cur[i][2] - boxes_cur[i][0]
            
            box_area = x_delta * y_delta
              
            x_center = int(boxes_cur[i][1] + (x_delta)/2)
            
            y_center = int(boxes_cur[i][0] + (y_delta)/2)
            x_delta_to_left_most = max(0, x_center - ZERO_DEGREE_PIXEL)
            x_angle = (x_delta_to_left_most / (DEGREE_TO_PIXEL))
            
            if (isFrontCAM(camID) == False): 
                x_angle += 170 #offset 10 degreee between back camera and LIDAR
                    
            if (x_delta >= X_DELTA_MIN_ACCEPTED and y_delta >= Y_DELTA_MIN_ACCEPTED):
                valid_boxes_cnt += 1
                x_angles = x_angles + str(x_angle)
                
                if (i < len(boxes_cur) - 1):
                    x_angles = x_angles + "," 
                        
            
        d = getDepthsFromAngles(x_angles)
        
        if (len(d) == 0):
            printMessage('cannot get depths from lidar')
            return
        
        printMessage('dis received from c# ' + d)
                         
        dis = d.split(',')
        
        if (len(dis) != valid_boxes_cnt):
            print('jeremy stupid!!!!')
            return
        
        for i in range(valid_boxes_cnt):
              
            distance = float(dis[i])
                              
            if (distance <= MIN_DIS_FROM_CAM_TO_PERSON):
                sendMessToWinform("person_too_close_to_robot")
                time.sleep(PERSON_GO_AWAY_TIME)
                return
                
            if (distance > MIN_DEPTH_DIS_REQUIRED and distance < MAX_DEPTH_DIS_ACCEPTED): 
                x_delta = boxes_cur[i][3] - boxes_cur[i][1]
                y_delta = boxes_cur[i][2] - boxes_cur[i][0]
            
                box_area = x_delta * y_delta
                detectedInfo.append((boxes_cur[i][1], boxes_cur[i][3], distance, box_area))
                my_string += "(" + str(boxes_cur[i][1]) + ", " + str(boxes_cur[i][3]) + ", " + str(distance)+ ") , "

                #cv2.putText(img,str(x_center) + " , " + str(x_angle) + " , " +  str(distance) + "m",(30,50),cv2.FONT_HERSHEY_SIMPLEX,1,(0,0,255),2,cv2.LINE_AA)
                           
        print(my_string, end = "=>>>> ", flush = True)
        
        warning = findAnyPair(detectedInfo)
        
        if (warning == True):
            printMessage("CLOSED!!!")
            global detected_count, first_detected_time
            
            if (detected_count >= 1):
                
                current_dectected_time = datetime.datetime.now()
                
                timeDelta = (current_dectected_time - first_detected_time).total_seconds()
                              
                if (timeDelta > SOCIAL_DISTANCE_CHECKING_INTERVAL):
                    printMessage("Time out! Reset from 1")
                    detected_count = 0
                            
            detected_count += 1
            
            if (detected_count == 1):
                printMessage("---------FIRST DETECTED--------")
                first_detected_time = datetime.datetime.now()    
                
            if (detected_count == MIN_SOCIAL_DIS_DETECTED_PER_INTERVAL):                                      
                printMessage('----------DANGEROUS!!!!!!!!!!---------------')
                saveEvidence(img, SOCIAL_DIS_VIOLATION_FOLDER)
                sendWarningToWinForm()                
                violation_detected = True
                time.sleep(SOCIAL_DIS_WARNING_TIME)
                violation_detected = False
                detected_count = 0
                mask_detected_count = 0
        else:
            print('------', flush=True)
        
    else:        
        pass
     
def sendMessToWinform(mess):
    print(mess, flush = True)
                        
def showStreamingVideo(windowsName, img):
    
    if (IS_VIDEO_STREAMED_ENABLED):
        cv2.imshow(windowsName, img)
    
def saveEvidence(image, folder):
      
   
    fileName = "Evidence.jpg"
    
    cv2.imwrite(folder + fileName, image)
    
    
def sendWarningToWinForm():
    sendMessToWinform('social_distancing_warning')

def detectAllViolation():
    
    printMessage("Start detecting")
    
    caps = []
    
    for i in range(START_CAMERA_ID, END_CAMERA_ID + 1):
        try:
            cap = cv2.VideoCapture(i)
            if (cap.isOpened()):
                 caps.append((cap, i))           
        except Exception as e:
            print(e)
            printMessage(str(i) + " is not a valid cam id")

    printMessage(str(len(caps)) + " cameras are running!")
    
    if (len(caps) == 0):
        sendMessToWinform("no_camera_detected")
        return
    
    sendMessToWinform("camera_ready");
    while True:
        for i in range(len(caps)): 
            if (violation_detected == False): 
                detectImage(caps[i][0], caps[i][1])

        cont = wait(DELAY_PER_FRAME)
        
        if (cont == False):
            break
            
    cap.release()
    openni2.unload()
    cv2.destroyAllWindows()
         
        
######################FACE MASK DETECTION#########################
       

def inference(image, 
              conf_thresh=MASK_DETECTION_MIN_CONFIDENCE_SCORE,
              iou_thresh=0.5,
              target_shape=(160, 160),
              draw_result=True,
              show_result=True
              ):
    '''
    Main function of detection inference
    :param image: 3D numpy array of image
    :param conf_thresh: the min threshold of classification probabity.
    :param iou_thresh: the IOU threshold of NMS
    :param target_shape: the model input size.
    :param draw_result: whether to daw bounding box to the image.
    :param show_result: whether to display the image.
    :return:
    '''
    height, width, _ = image.shape
    
    image_resized = cv2.resize(image, target_shape)
    image_np = image_resized / 255.0  # 归一化到0~1
    image_exp = np.expand_dims(image_np, axis=0)
    y_bboxes_output, y_cls_output = tf_inference(sess, graph, image_exp)

    # remove the batch dimension, for batch is always 1 for inference.
    y_bboxes = decode_bbox(anchors_exp, y_bboxes_output)[0]
    y_cls = y_cls_output[0]
    # To speed up, do single class NMS, not multiple classes NMS.
    bbox_max_scores = np.max(y_cls, axis=1)
    bbox_max_score_classes = np.argmax(y_cls, axis=1)

    # keep_idx is the alive bounding box after nms.
    keep_idxs = single_class_non_max_suppression(y_bboxes,
                                                 bbox_max_scores,
                                                 conf_thresh=conf_thresh,
                                                 iou_thresh=iou_thresh,
                                                 )

    for idx in keep_idxs:
        conf = float(bbox_max_scores[idx])
        class_id = bbox_max_score_classes[idx]
        bbox = y_bboxes[idx]
        # clip the coordinate, avoid the value exceed the image boundary.
        xmin = max(0, int(bbox[0] * width))
        ymin = max(0, int(bbox[1] * height))
        xmax = min(int(bbox[2] * width), width)
        ymax = min(int(bbox[3] * height), height)

        if draw_result:
            if class_id == 0:
                color = (0, 255, 0)
            else:               
                color = (255, 0, 0)
                                
            cv2.rectangle(image, (xmin, ymin), (xmax, ymax), color, 2)
            
            box_size = (xmax - xmin)
            
            if (box_size < MIN_BOX_WIDTH_FOR_MASK_DECTECT):
                printMessage('<<<<<< box size too small! ignore!')
                return
            
            cv2.putText(image, "%s: %.2f" % (id2class[class_id], conf), (xmin + 2, ymin - 2),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.8, color)
            
            global mask_detected_count
            if class_id == 1:
                
                mask_detected_count = mask_detected_count + 1
                
                global first_mask_detected_time
                current_dectected_time = datetime.datetime.now()
                if (mask_detected_count == 1):
                    printMessage("first time detected, confirming in few more seconds")
                    first_mask_detected_time = current_dectected_time
                    return
                                      
                timeDelta = (current_dectected_time - first_mask_detected_time).total_seconds()
                                 
                if (timeDelta >= MASK_CHECKING_INTERVAL):
                    saveEvidence(image[:, :, ::-1], MASK_VIOLATION_EVIDENCE_PATH)
                    sendMessToWinform("facial_mask_violation")
                
                    global violation_detected 
                    violation_detected = True
                    wait(MASK_VIOLATION_WARNING_TIME)
                    violation_detected = False
                    mask_detected_count = 0
            else:
                mask_detected_count = 0 
                                     
#################################################################

if __name__ == "__main__":
     
    ros_helper.connect()
    
    global odapi
      
    printMessage('Initilizing detector api')
    odapi = DetectorAPI(path_to_ckpt=MODEL_PATH)
    
    printMessage('Init done!')
   
    detectAllViolation() 
    
    ros_helper.disconnect()
    