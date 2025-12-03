# Unity-PassthroughCameraAPISamples

# Table of Contents

1. [Project Overview](#project-overview)
2. [Requirements](#requirements)
3. [Current Limitations & Known Issues](#current-limitations--known-issues)
4. [Download the Project](#download-the-project)
5. [Project Content](#project-content)
6. [How to use the Passthrough Camera API](#how-to-use-the-passthrough-camera-api)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)
9. [Unity Sentis for On-Device ML/CV Models](#unity-sentis-for-on-device-mlcv-models)
10. [License](#license)

## Project Overview
The **Unity-PassthroughCameraAPISamples** project helps Unity developers access Quest Camera data via the standard [`WebCamTexture`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/WebCamTexture.html) and [`Android Camera2 API`](https://developer.android.com/media/camera/camera2). It provides:
- **Two helper classes:** `WebCamTextureManager` for handling permissions and initialization, and `PassthroughCameraUtils` for retrieving camera metadata and converting 2D to 3D coordinates.
- **Five sample implementations:** including basic camera feed, brightness estimation, object detection with Unity Sentis, and shader-based effects.

| CameraToWorld | BrightnessEstimation | MultiObjectDectection | ShaderSample |
|:-------------:|:--------------------:|:---------------------:|:------------:|
| ![GIF 1](./Media/CameraToWorld.gif) | ![GIF 2](./Media/BrightnessEstimation.gif) | ![GIF 3](./Media/ObjectDetectionSentis.gif) | ![GIF 4](./Media/ShaderSample.gif) |

For detailed project information, visit the [Meta Developers Documentation](https://developers.meta.com/horizon/documentation/unity/unity-pca-overview).

## Requirements

### Software

- **Unity:**
  - Tested with Unity **`2022.3.58f1`** and **`6000.0.38f1`**.
  - Older minor versions may work but are not fully validated.
- **Packages / Dependencies:**
  - [**`Meta MRUK`**](https://assetstore.unity.com/packages/tools/integration/meta-mr-utility-kit-272450?srsltid=AfmBOorj1QQDtt7_6vcIWgu0Tw2Q8YLTQB3hRN5QHORRmjaj8sUEmrkv) (com.meta.xr.mrutilitykit, v74.0.0 or higher)
  - [**`Unity Sentis`**](https://unity.com/sentis) (com.unity.sentis, v2.1.1)

> [!IMPORTANT]
> When updating the project to **`Unity 6`**, the Android Manifest will need to be updated. Find more information in our [Troubleshooting guide](#troubleshooting--known-issues) below.

### Hardware

- **Meta Quest Devices:**
  - **`Quest 3 / Quest 3S`**
  - Must be running **`Horizon OS v74`** or higher

### Permissions

- **Android Permissions:**
  - `android.permission.CAMERA` - Required by `WebCamTexture`
  - `horizonos.permission.HEADSET_CAMERA` - Custom Meta permission for passthrough camera access

### Additional features

- **Passthrough feature:** must be **enabled** to access the Passthrough Camera API.

## Current Limitations & Known Issues

- **Single Camera Access:**
  - Only one passthrough camera (left or right) can be accessed at a time. Switching between cameras requires disabling and re-enabling the camera manager component. This is a Unity `WebCamTexture` limitation.
- **WebCamTexture Constraints:**
  - Unity’s `WebCamTexture` implementation does not support image timestamps, leading to slight misalignments (approximately 40-60ms delay) between the captured feed and real-world events.
  - The maximum supported resolution is 1280x960, with a rectangular capture area that may result in visual cropping.
  - v74 currently needs to one frame delay before `WebCamTexture.Play()`.
- **Permission Handling:**
  - The API currently handles one permission request at a time. Using other permission management systems concurrently (e.g., OVRManager or OVRPermissionsRequester) might cause conflicts.
- **Model Limitations (MultiObjectDetection Sample):**
  - The integrated object detection model, optimized for Quest 3/3S, may not achieve 100% accuracy. Certain objects can be misclassified (e.g., cell phones might be recognized as remote) and grouped under broad classes.
  - The model is trained on 80 classes, meaning that individual objects within the same class may not be distinctly recognized.
- **Performance Considerations:**
  - Processing high-resolution camera feeds and running on-device ML/CV models can impact performance, particularly on the main thread. Optimizations such as asynchronous data handling or layer-by-layer inference are recommended.

## Download the project

First, ensure you have Git LFS installed by running this command:

```
git lfs install
```

Then, clone this repo using the "Code" button above, or this command:

```
git clone https://github.com/oculus-samples/Unity-PassthroughCameraApiSamples
```

## Project Content

The `Unity-PassthroughCameraApiSamples` Unity project contains **five samples** that demonstrate how to use `WebCamTexture` class to access the camera data. All sample code and resources are located inside the [**`PassthroughCameraApiSamples`**](./Assets/PassthroughCameraApiSamples/) folder:

* **[`CameraViewer`](./Assets/PassthroughCameraApiSamples/CameraViewer)**: Shows a 2D canvas with the camera data inside.
* **[`CameraToWorld`](./Assets/PassthroughCameraApiSamples/CameraToWorld)**: Demonstrates how to align the pose of the RGB camera images with Passthrough, and how a 2D image coordinates can be transformed into 3D rays in world space.
* **[`BrightnessEstimation`](./Assets/PassthroughCameraApiSamples/BrightnessEstimation)**: Illustrates brightness estimation and how it can be used to adapt the experience to the user’s environment.
* **[`MultiObjectDetection`](./Assets/PassthroughCameraApiSamples/MultiObjectDetection)**: Shows how to feed camera data to Unity Sentis to recognize real-world objects.
* **[`ShaderSample`](./Assets/PassthroughCameraApiSamples/ShaderSample)**: Demonstrates how to apply custom effects to camera texture on GPU.

And a C# classes to access the Quest Camera data using `WebCamTexture` object:
* **[`PassthroughCamera`](./Assets/PassthroughCameraApiSamples/PassthroughCamera)**: Contains all the c# classes and prefabs to use `WebCamTexture` object, manage permissions and access some camera metadata.
* **[`StartScene`](./Assets/PassthroughCameraApiSamples/StartScene)**: The scene containing the menu to switch between the above-mentioned samples.

# How to use the Passthrough Camera API

## Configuring A Unity Project To Use PCA

1) Clone the GitHub project or download a zip archive as described [above](#download-the-project).
2) Open the project with **`Unity 2022.3.58f1`** or **`Unity 6000.0.38f1`**.
3) Open `Meta > Tools > Project Setup Tool` and fix any issues that it finds in the configuration of your project.
4) Create a new empty scene.
5) Use `Meta > Tools > Building Blocks` to add **Camera Rig** building blocks to your scene.
6) To integrate Passthrough Camera API in your scene, **drag and drop** the **`WebCamTextureManagerPrefab`** prefab to your scene.
7) To access the camera texture from a custom C# script, get a reference to the `WebCamTextureManager` and access its **`WebCamTexture`** property. The property will return a valid non-null value only after all permissions have been granted and texture is initialized, so check it is not null before accessing properties of the returned `WebCamTexture`. E.g., in the `CameraViewer` example, we assign the `WebCamTexture` to the `RawImage.texture` to display the texture with the Unity UI system.

Depending on the selected PassthroughCameraEye.eye, the `WebCamTextureManager` will select a corresponding [`WebCamDevice`](https://docs.unity3d.com/ScriptReference/WebCamDevice.html) by mapping [`WebCamTexture.devices`](https://docs.unity3d.com/ScriptReference/WebCamTexture-devices.html) to CameraManager.getCameraIdList() by index.

Each camera supports resolutions of `320 x 240`, `640 x 480`, `800 x 600` and `1280 x 960`. Thes can also be accessed via the `PassthroughCameraUtils.GetOutputSizes()` method.

> [!TIP]
> If you don't assign any resolution on the `WebCamTextureManagerPrefab` the system will default to the highest available resolution.

The **`WebCamTextureManager`** script is responsible for:

* **Initializing the `WebCamTexture` object** to access the camera data.
* **Stopping and disposing of the `WebCamTexture` object** when the scene is unloaded or the application is closed.

Also, this prefab contains the PassthroughCameraPermissions C# class responsible for requesting necessary permissions: `android.permission.CAMERA` and `horizonos.permission.HEADSET_CAMERA`.

> [!NOTE]
> this class uses [`UnityEngine.Android.Permission`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Android.Permission.html) class which can only handle one permission request at a time. This script should not be used with any other scripts that manage permissions.

## Converting 2D Image Coordinates to 3D World Space

When working with the Passthrough Camera API, a common challenge is converting detected 2D image coordinates into accurate 3D world positions. For instance, if your app recognizes a can of soda, you might want to render a virtual augment directly over it. Achieving this requires determining the object's position and orientation in the 3D world.

The **PassthroughCameraUtils** class provides several methods to assist with this:

- **`List<Vector2Int> GetOutputSizes(PassthroughCameraEye cameraEye)`**
  Retrieves a list of all supported resolutions for the specified camera eye.
- **`PassthroughCameraIntrinsics GetCameraIntrinsics(PassthroughCameraEye cameraEye)`**
  Returns the camera’s intrinsic parameters, including focal length, principal point, resolution, and skew.
- **`Pose GetCameraPoseInWorld(PassthroughCameraEye cameraEye)`**
  Provides the most recent world pose of the passthrough camera.
- **`ScreenPointToRayInWorld(PassthroughCameraEye cameraEye, Vector2Int screenPoint)`**
  Converts a 2D screen coordinate into a 3D ray in world space, starting from the camera's origin.

Typically, you would use **`ScreenPointToRayInWorld()`** by passing the screen coordinate (e.g., the center of a detected object) to obtain a corresponding 3D ray.

However, knowing the ray alone isn't enough to pinpoint an exact position. To accurately locate an object, you can combine the ray with a raycasting technique. The [**`EnvironmentRaycastManager`**](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-features#environmentraycastmanager) from the **[`MR Utility Kit`](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-overview)** provides the [`Raycast()`](https://developers.meta.com/horizon/reference/mruk/v72/class_meta_x_r_environment_raycast_manager) method. This method uses real-time depth data to determine the intersection point between the ray and the physical environment.

Below is a code snippet demonstrating this process:

```csharp
var cameraScreenPoint = new Vector2Int(x, y);
var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(PassthroughCameraEye.Left, cameraScreenPoint);

if (environmentRaycastManager.Raycast(ray, out EnvironmentRaycastHit hitInfo))
{
    // Place a GameObject at the hit point (position) and rotation (normal)
    anchorGo.transform.SetPositionAndRotation(
        hitInfo.point,
        Quaternion.LookRotation(hitInfo.normal, Vector3.up));
}
```
To learn more about the capabilities of **`PassthroughCameraUtils`** class, refer to the **[`CameraToWorld`](./Assets/PassthroughCameraApiSamples/CameraToWorld)** sample.

## Best Practices

- **Centralize Permission Handling:**
  Ensure that all Android permission requests are managed from a single location. The samples use the **PassthroughCameraPermissions** script for this purpose. When integrating Camera Passthrough into an existing project, double-check that no other permission mechanisms (e.g., `OVRPermissionsRequester` or `OVRManager / Permission Requests On Startup`) are in use to avoid conflicts.
- **Accessing Camera Metadata:**
  The Unity `WebCamTexture` class does not expose all camera metadata properties. For more advanced access, consider:
  - Using Unity’s built-in tools to communicate directly with Android APIs ([`Android Plugins`](https://docs.unity3d.com/6000.0/Documentation/Manual/android-plugins-java-code-from-c-sharp.html)).
  - Developing a native plugin that leverages the Camera2 API via the [`NDK`](https://developer.android.com/ndk).
  Refer to our native sample for practical examples.
- **Performance Optimization:**
  - **Asynchronous Operations:** Process camera data and handle permission requests asynchronously to avoid blocking the main thread.
  - **Resource Management:** Ensure that resources such as the `WebCamTexture` are properly stopped and disposed of when no longer needed (e.g., in the `OnDisable()` method).
- **Error Handling and Logging:**
  Implement robust error handling for scenarios like permission denials and camera initialization failures. Utilize descriptive log messages to facilitate troubleshooting during development and in production.
- **Consistent API Usage:**
  Leverage the provided helper classes (`WebCamTextureManager` and `PassthroughCameraUtils`) to ensure a consistent and reliable approach when accessing and processing camera data.
- **Testing on Target Devices:**
  Since performance and behavior may vary, thoroughly test your implementation on the target Quest hardware (Quest 3/3S running Horizon OS v74 or later) to ensure optimal performance and compatibility. Meta XR Simulator and Meta Link app are currently not supported.

## Troubleshooting & Known Issues

* **Upgrading to Unity 6:**
   - When updating the project to **`Unity 6`**, the Android Manifest will need to be updated. This can be done either manually or by using one of two Meta tools, **Meta** > **Tools** > **Update AndroidManiest.xml** or **Meta** > **Tools** > **Create store-compatible AndroidManiest.xml**.

   - Android Manifest *android:name="com.unity3d.player.UnityPlayerActivity"* must be changed to *android:name="com.unity3d.player.UnityPlayer**Game**Activity"*.

* **Using Open XR:**
   - When using **`Open XR`** plugin, the **Environment Depth** feature will be ONLY supported under **`Unity 6`**. Please read the [Depth API documentation](https://developers.meta.com/horizon/documentation/unity/unity-depthapi-overview/) for more information.
   - **MultiObject Detection** sample built using Unity 2022 will not place the markers correctly, because of this limitation with Open XR and **Environment Depth**.
   - Android Manifest will need the *android:theme="@style/Theme.AppCompat.DayNight.NoActionBar"* attribute added to the activity tag.
   - We recommend to install (com.unity.xr.meta-openxr@2.1.0 or latest) and select all **Meta SDK features** in the **`Project Settings`** > **`XR Plug-in Management`** > **`Open XR`**.

> [!IMPORTANT]
> The `horizonos.permission.HEADSET_CAMERA` permission has to be added back into the Manifest manually after updating.

* **App cannot be accessed after denying all permissions:**
   - Currently, if users click on **Don't Allow** for all permissions, they are unable to access the app even after changing Settings in the device.
   - The only solution right now is to uninstall and re-install the app.
* **Check Log Output:**
   - Inspect the Unity Console and device logs for descriptive error messages.
   - Both the sample and the Camera2 implementation provide detailed logging to help narrow down issues.
* **Verify Permissions:**
   - Ensure that both `android.permission.CAMERA` and `horizonos.permission.HEADSET_CAMERA` are granted.
   - Missing permissions can prevent the camera feed from initializing properly.
* **Handling Permission Request Failures:**
   - If permissions are not granted on the first attempt, note that the current implementation does not support re-requesting them.
   - In such cases, uninstall the app and perform a new build from Unity.
   - Alternatively, it is also possible to grant permissions via ADB. Replace `{PACKAGE.NAME}` with the name of your project, e.g. `com.john.cameraSamples`. You can find your package name in the `Player Settings` under `Identification > Override Default Package Name > Package Name`.
   ```
   adb shell pm grant {PACKAGE.NAME} com.horizonos.permission.HEADSET_CAMERA
   ```
* **Configuration and Setup:**
   - Ensure that your project configuration is correct by resolving any issues flagged by the Meta/Tools Project Setup Tool.
   - Confirm that you are using a supported Unity version (e.g., `Unity 2022.3.58f1` or `Unity 6000.0.38f1`).
* **Device Compatibility:**
   - Verify that you are testing on supported hardware (`Quest 3/3S` running `Horizon OS v74` or higher).
   - Check that your device meets the necessary hardware requirements for passthrough camera access.

Following these steps should help you diagnose and resolve common issues when working with the `Passthrough Camera API`.

# Unity Sentis for On-Device ML/CV Models

Unity Sentis offers a framework to load models from popular open-source platforms and compile them directly on-device. This sample demonstrates how to use Sentis with a YOLO model for real-time object detection on Quest 3 devices. For more details, visit [`Unity Sentis`](https://unity.com/products/sentis).

## Prerequisites

- **Horizon OS:** v74 or higher
- **Devices:** Quest 3 / 3S
- **Permissions:** Grant Camera and Spatial Data permissions
- **Unity:** 2022.3.58f1 with Sentis package 2.1.1 (com.unity.sentis)
- **MR Utility Kit (MRUK):** v74.0.0 or higher

## Known Issues

1. **Model Accuracy:**
   The model is optimized for Quest 3 performance; accuracy may not reach 100%.
2. **Class Granularity:**
   The model is trained on 80 classes, meaning similar objects (e.g., Monitor and TV) are grouped together (e.g., TV_Monitor).
3. **Object Recognition Challenges:**
   Some classes (e.g., cell phones) may often be misidentified.
4. **Device Recognition:**
   New devices like Quest 3 controllers might not be recognized correctly or may appear as remote controllers.

## Building the Sample

1. Download the [`Unity-PassthroughCameraApiSamples`](#download-the-project) repository.
2. Open the project in the Unity Editor.
3. In **Build Profiles**, set the Active Platform to **Android**.
4. Open the sample scene `MultiObjectDectection.unity`
5. In `Meta > Tools > Project Setup Tool`, if prompted with "MR Utility Kit recommends Scene Support to be set to Required", select "...", ignore it, then fix and apply any other issues.
6. Build the app and test it on your headset.

## Using the Sample

- **Description:**
  Demonstrates how to detect multiple objects using Sentis with a pretrained YOLO model.
- **Controls (using Quest 3 controllers):**
  - **Menus (Start/Pause):**
    - **Button A:** Start playing
  - **In-Game:**
    - **Button A:** Place a marker at each detected object’s world position
  - **Global:**
    - **Button MENU:** Return to Samples selection
- **How to Play:**
  1. Start the application and look around.
  2. When an object is detected, 2D floating boxes appear around it.
  3. Press **Button A** to place a 3D marker with the object’s class name.
  4. The model identifies 80 classes – refer to [`Sentis YOLO classes`](./Assets/PassthroughCameraApiSamples/MultiObjectDetection/SentisInference/Model/SentisYoloClasses.txt) for details.

> [!NOTE]
> You can modify this sample to load an ML/CV model of your choice. Models vary widely in complexity, so select one that meets your performance needs. Unity also offers samples for other tasks, such as digit recognition.

## Recommendations for Using Sentis on Meta Quest Devices

### Model Architecture

- **Keep it Simple:**
  Use models with less complex architectures. Large generative models and LLMs perform poorly on Quest devices.
- **Main Thread Processing:**
  Sentis runs on the main thread, affecting render pipeline performance.

> [!TIP]
> Implement a layer-by-layer inference technique (splitting inference across frames) to avoid blocking the main thread.

### Model Size

- **Resource Considerations:**
  Large models can lead to long load times, main thread lag during the first inference, and reduced memory for other resources.
- **Recommendations:**
  - Use the smallest version of your model (e.g., the 8MB version of YOLO instead of the 146MB version).
  - Convert and quantize the model to Sentis format (e.g., to Uint8) to reduce loading times.

### GPU vs. CPU Processing

- **Graphics-Related Tasks:**
  If the model is used for graphics (e.g., image processing directly for shaders), keep processing on the GPU.
  - Use the **GPUCompute backend** for Sentis and feed camera data directly.
- **CPU Processing:**
  If you need to access output data on the CPU:
  - Run the model on the GPU and transfer results asynchronously, or
  - Run the model directly on the CPU.
- **General Tip:**
  Always retrieve output data asynchronously to avoid main thread blocking. See [`Sentis Async Output`](https://docs.unity3d.com/Packages/com.unity.sentis@2.1/manual/read-output-async.html) for more details.

### NPU Backend

- **Current Status:**
  Sentis does not currently leverage any NPU or hardware acceleration. It operates as a regular Android platform application on Quest devices, so factor this into model selection.

### Report an issue

If you encounter any issues, please report them with the following details:

- **Unity Engine version** used in your project.
- **XR plugin** used in your project (Oculus XR or Open XR) and the version number.
- **Quest device** model and **Horizon OS version**.
- Attach in your report any useful **logcat logs**.
  - You can use `adb logcat >> log.txt` to save the logs to a file.
- Attach any **video or screenshot** of the issue.
- Any **relevant information** about your specific use case, e.g. other sdk or plugins used in your project.

## License

The [`Oculus License`](./LICENSE.txt) applies to the SDK and supporting material. The [`MIT License`](./Assets/PassthroughCameraApiSamples/LICENSE.txt) applies to only certain, clearly marked documents. If an individual file does not indicate which license it is subject to, then the Oculus License applies.

However,
* Files from [`Assets/PassthroughCameraApiSamples/MultiObjectDetection/SentisInference/Model`](./Assets/PassthroughCameraApiSamples/MultiObjectDetection/SentisInference/Model) are licensed under [`MIT`](https://github.com/MultimediaTechLab/YOLO/blob/main/LICENSE).

See the [`CONTRIBUTING`](./CONTRIBUTING.md) file for how to help out.
