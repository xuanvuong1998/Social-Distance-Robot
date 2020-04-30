


# -*- coding: utf-8 -*-
"""
Created on Sun Apr 26 16:43:26 2020

@author: Surface
"""

ROS_IP = "192.168.1.11"
ROS_PORT = 9090 
PUB_ANGLES_TOPIC_NAME = '/np_ros_general_message/object_detected_angle'
PUB_ANGLES_TOPIC_TYPE = 'std_msgs/String'
SUB_DEPTHS_TOPIC_NAME = '/np_ros_general_message/general'
#SUB_DEPTHS_TOPIC_NAME = '/np_ros_general_message/object_detected_angle'
SUB_DEPTHS_TOPIC_TYPE = 'std_msgs/String'



import roslibpy as ros

def connect():  
    global client
    try:
        client = ros.Ros(ROS_IP, ROS_PORT)
        client.run()
        sub_angles_ros()
    except Exception as e:
        print(type(e))
        print('failed to connect to ROS')
        disconnect()

def disconnect():
    try:
        #client.close()
        client.terminate()
        print('disconnected!')
    except:
        print('failed to disconnect!')

def pub_angles_to_ros(angles):
    
    print("angles are " + angles) 
    talker = ros.Topic(client, PUB_ANGLES_TOPIC_NAME 
                       , PUB_ANGLES_TOPIC_TYPE)
    
    print('publishing angles to ros...')
    talker.publish(ros.Message({'data': angles}))
    print('published!')
    talker.unadvertise()
    print('unadvertised!')
    
depths_received = ""

def clearPreviousDepths():
    global depths_received
    depths_received = ""
    
def getDepths():
    global depths_received
    
    tried_cnt = 0
    while(len(depths_received) == 0 && tried_cnt < 200):
        tried_cnt += 1
        time.sleep(10)
    return depths_received
import time
def general_message_received_handler(message):
    
    data = message['data']
    print("Depths received from LIDAR: " + data)
       
    global depths_received
    
    if ("object_detected_depth" in data):
        i = data.index(',')
        depths_received = data[i + 1:]
    else:
        depths_received = ""
            
def sub_angles_ros():
    
    listener = ros.Topic(client, SUB_DEPTHS_TOPIC_NAME
                         ,SUB_DEPTHS_TOPIC_TYPE)
    listener.subscribe(lambda message: general_message_received_handler(message))
    
    print('subscribed to get depths !')
    
    