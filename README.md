# Seeing Through Others' Eyes
This repository contains an **assistive application** developed during my Master's Thesis at the University of Stuttgart. It **communicates gaze data from a sighted peer to an individual with visual impairment using augmented reality** to **facilitate collaboration for mixed-vision pairs.** The thesis investigated the e**ffects of gaze sharing on individuals with visual impairment** and collected information about the user's experience with the application.

The system combines **real-time eye-tracking, object detection** and **augmented reality** to convey the otherwise difficult-to-perceive **gaze cues to individuals with visual impairment**. It provides accessible visual cues, as well as audio cues, which were tested with participants in a sandwich-making task. 

## Research Question: "How does gaze sharing influence the collaboration in mixed-vision pairs?"

The focus of this work was not to investigate the accuracy of gaze estimation, but the general experience of the participants when confronted with gaze cues coming from their partner. Furthermore, it put a large emphasis on the interaction and how it was affected by the access to this novel information.

## Overview of the System
The system consists of two components, namely the individual with visual impairment and the sighted individual, each having their own device. The **sighted individual wears a Pupil Labs Neon or Invisible eye-tracker**, while the **individual with visual impairment receives assistive cues corresponding to their peer's gaze on a Meta Quest 3S headset.**

The gaze information is processed by a Python backend running object detection and object-gaze fusion to determine the currently focused object category at any time. By doing so, the system facilitates shared attention on a common object, which aims to improve communication while collaborating.

## Main Technologies
| Component          | Technology    |
|--------------------|---------------|
| Eye-tracking       | Pupil Labs    |
| Object Detection   | YOLO11s       |
| Communication      | UDP, TCP     |
| AR Device          | Meta Quest 3S |
| AR Development     | Unity         |
| Backend + Frontend | Python        |


# Installation
### 1. Clone the repository
`git clone <URL>`

`cd Seeing-Through-Others-Eyes`

### 2. Create and activate Python virtual environment
`cd python`

`python -m venv .venv`

`.venv\Scripts\activate`

### 3. Install required packages
`pip install -r requirements.txt`

# Configuration
Most of the configuration is found in [the main script](python/src/main.py). Here, the YOLO model can be selected, as well as the IP address and port of the Meta Quest 3S device for UDP communication. This information also needs to be changed inside the [UDP sender](python/src/networking/udp_sender.py) and the [frontend](python/src/networking/frontend.py) scripts.

# Usage
The backend can be run using `python src/main.py`. In the main file, one of two modes can be chosen: live or replay.

### Live Mode
The system is mostly created to run live on real-time gaze data from the Pupil Labs device. In this mode, the following steps happen:
1. Create **connection to Pupil Labs device** and **start data stream**
2. Receive frame and **2D gaze location** within the frame
3. Detect objects in the frame **using custom** (trained on a dataset of our own) **YOLO11s model**
4. **Associate gaze location to the object**
5. (Filter selections using **dwell timer**) -- Only in **"selective" mode**
6. **Send the object label** to Quest 3S via UDP
7. **Visualize cue** in the view of the individual with visual impairment

### Replay mode
The application also allows for **post-hoc analysis of attention through gaze**. In **replay mode, existing video frames and IMU data** can be used in the same processing pipeline.

# Gaze-Object Fusion
To determine the currently focused object, we need to process the gaze position in 2D and the object detections. By checking whether the gaze point is located inside a detection's bounding box, or slightly outside given a small distance threshold, we get some information on the **user's attention.** By employing a **dwell filter of 0.75 seconds**, we can also filter out unintended, transient gaze or saccades. This way, we can more confidently tell which object was truly focused by the sighted person before transmitting the label to the Quest 3S.

# Spatial Mapping **(Work In Progress)**
To determine an absolute position of the gaze coordinate, spatial anchors can be used on both devices to create a reference coordinate system in which devices can communicate their 3D positions. To this end, we started implementing ArUco tracking inside the Python backend to estimate the absolute gaze location in relation to a printed marker. Using the intrinsics of the Pupil Labs webcam, we attempt to estimate the gaze location in 3D space. However, this feature is still experimental and needs further refinement before deploying it.

# Unity application
The Unity side of the application, which runs on the Quest 3S, listens for incoming label changes and runs the same object detection algorithm to find the current label within its view. It includes **three visual cues**, namely **OVERLAY, OUTLINE and SPOTLIGHT,** which are meant to emphasize the object's location in an accessible fashion. These **cues can be customized** using the **Python frontend** mentioned before, including parameters such as **color, size and transparency.** Cues are placed in 3D space according to the pre-computed location using the **_Object Detection_ building block** within the _Meta XR SDK_.

# Privacy Considerations
Any personal data or recordings may contain sensitive information and excluding them from this project was an important choice. Therefore, **raw data should not be committed to a repository without consent.** 
Please include additional .gitignore entries such as "data/", "recordings/", ".mp4" or ".csv" if applicable.

# Acknowledgements
For this project, documentation and corresponding code examples provided by [Meta](https://developers.meta.com/horizon/code-samples/unity/) as well as [Pupil Labs](https://docs.pupil-labs.com/neon/data-collection/) were used as references and guided the development of this application. These resources were useful in integrating the Quest 3S and Pupil Labs Neon within our project and understanding the functionality they provide.
