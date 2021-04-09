using UnityEngine;
using System.Linq;
using UI = UnityEngine.UI;

public class AprilDetectAndWebcamRender : MonoBehaviour
{
    [SerializeField] Vector2Int _resolution = new Vector2Int(1920, 1080);
    [SerializeField] int _decimation = 4;
    [SerializeField] float _tagSize = 0.05f;
    [SerializeField] UI.RawImage _webcamPreview = null;
    [SerializeField] UI.Text _debugText = null;
    [SerializeField] GameObject _detectionPivot = null;
    [SerializeField] GameObject _cameraPivot = null;

    // Webcam input and buffer
    WebCamTexture _webcamRaw;
    RenderTexture _webcamBuffer;
    Color32[] _readBuffer;
    GameObject detection;

    // AprilTag detector and drawer
    AprilTag.TagDetector _detector;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        // Webcam initialization
        _webcamRaw = new WebCamTexture(devices[2].name, _resolution.x, _resolution.y, 60); // Best camera for samsung s10 = [2]
        Debug.Log(devices[0].availableResolutions);
        
        _webcamBuffer = new RenderTexture(_resolution.x, _resolution.y, 0);
        _readBuffer = new Color32[_resolution.x * _resolution.y];

        detection = Instantiate(_detectionPivot, new Vector3(0, 0, 0), Quaternion.identity);
        detection.transform.SetParent(_cameraPivot.transform);

        _webcamRaw.Play();
        _webcamPreview.texture = _webcamBuffer;
        // fix image orientation
        int orient = -_webcamRaw.videoRotationAngle;

        _webcamPreview.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
        _cameraPivot.transform.Rotate(0, 0, orient);

        // Detector and drawer
        _detector = new AprilTag.TagDetector(_resolution.x, _resolution.y, _decimation);
    }

    void OnDestroy()
    {
        Destroy(_webcamRaw);
        Destroy(_webcamBuffer);

        _detector.Dispose();
    }

    void Update()
    {
        // Check if the webcam is ready (needed for macOS support)
        if (_webcamRaw.width <= 16) return;

        // Check if the webcam is flipped (needed for iOS support)
        if (_webcamRaw.videoVerticallyMirrored)
            _webcamPreview.transform.localScale = new Vector3(1, -1, 1);

        // Webcam image buffering
        _webcamRaw.GetPixels32(_readBuffer);
        Graphics.Blit(_webcamRaw, _webcamBuffer);

        // AprilTag detection
        var fov = GetComponent<Camera>().fieldOfView * Mathf.Deg2Rad;
        _detector.ProcessImage(_readBuffer, fov, _tagSize);

        // Detected tag visualization
        foreach (var tag in _detector.DetectedTags)
        {
            detection.transform.localRotation = tag.Rotation;
            detection.transform.localPosition = tag.Position;
        }

        // Profile data output (with 30 frame interval)
        if (Time.frameCount % 30 == 0)
            _debugText.text = _detector.ProfileData.Aggregate
              ("Profile (usec)", (c, n) => $"{c}\n{n.name} : {n.time}");
    }
}
