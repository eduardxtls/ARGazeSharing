using UnityEngine;
using System.IO;

public class ExperimentController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CustomObjectDetectionVisualizer visualizer;
    [SerializeField] private UDPListener listener;
    [SerializeField] private CueManager cueManager;

    [Header("Hand Tracking")]
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;
    [SerializeField] private OVRSkeleton leftSkeleton;
    [SerializeField] private OVRSkeleton rightSkeleton;

    [Header("Automation Parameters")]
    [Tooltip("Distance threshold to trigger grab interaction (m)")]
    [SerializeField] private float grabProximityRadius = 0.05f;
    [Tooltip("Time threshold to trigger grab interaction (s)")]
    [SerializeField] private float grabDwellTime = 0.5f;

    [Header("Audio Debugging")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip mistakeClip;

    private string _currentLabel = "";
    private bool _isTrialActive = false;
    private bool _isWaitingForCue = false;

    private float _trialStartTime;
    private int _mistakesInCurrentTrial = 0;
    private float _dwellTimer = 0f;
    private float _mistakeDwellTimer = 0f;
    private bool _wasInWrongObjectZone = false;

    private string _logFilePath;

    private void Start()
    {
        _logFilePath = Path.Combine(Application.persistentDataPath, "experiment_log.csv");
        InitializeCSV();
    }

    public void StartTrial(string targetIngredient)
    {
        // Reset visuals and state for new trial
        if (visualizer != null)
        {
            visualizer.ClearTarget();
        }

        _currentLabel = targetIngredient.Trim().ToLower();
        _mistakesInCurrentTrial = 0;
        _dwellTimer = 0f;
        _wasInWrongObjectZone = false;
        
        _isTrialActive = false;
        _isWaitingForCue = true;
    }

    private void Update()
    {
        if (_isWaitingForCue)
        {
            if (visualizer != null && visualizer.IsCueVisible)
            {
                _isTrialActive = true;
                _isWaitingForCue = false;
                _trialStartTime = Time.time;
            }
            else
            {
                return;
            }
        }

        if (!_isTrialActive) return;

        Vector3 leftPos = (leftHand != null && leftHand.IsTracked && leftSkeleton != null && leftSkeleton.IsInitialized)
            ? GetIndexPosition(leftSkeleton)
            : Vector3.zero;


        Vector3 rightPos = (rightHand != null && rightHand.IsTracked && rightSkeleton != null && rightSkeleton.IsInitialized)
            ? GetIndexPosition(rightSkeleton)
            : Vector3.zero;

        if (leftPos == Vector3.zero && rightPos == Vector3.zero) return;

        if (visualizer.HasTarget)
        {
            bool leftInTarget = leftPos != Vector3.zero && Vector3.Distance(leftPos, visualizer.TargetPosition) <= grabProximityRadius;
            bool rightInTarget = rightPos != Vector3.zero && Vector3.Distance(rightPos, visualizer.TargetPosition) <= grabProximityRadius;

            if (leftInTarget || rightInTarget)
            {
                _dwellTimer += Time.deltaTime;
                if (_dwellTimer >= grabDwellTime)
                {
                    CompleteTrial();
                    return;
                }
            }
            else
            {
                _dwellTimer = 0f;
            }
        }

        bool handInWrongZone = false;

        if (visualizer.LiveDetectionsData != null)
        {
            foreach (var detection in visualizer.LiveDetectionsData)
            {
                if (detection.IsTarget) continue;

                bool leftInWrong = leftPos != Vector3.zero && Vector3.Distance(leftPos, detection.Position) <= grabProximityRadius;
                bool rightInWrong = rightPos != Vector3.zero && Vector3.Distance(rightPos, detection.Position) <= grabProximityRadius;

                if (leftInWrong || rightInWrong)
                {
                    handInWrongZone = true;
                    break;
                }
            }
        }

        if (handInWrongZone)
        {
            if (!_wasInWrongObjectZone)
            {
                _mistakeDwellTimer += Time.deltaTime;

                if (_mistakeDwellTimer >= grabDwellTime)
                {
                    _mistakesInCurrentTrial++;
                    _wasInWrongObjectZone = true;
                    _mistakeDwellTimer = 0f;

                    audioSource.PlayOneShot(mistakeClip);
                    Debug.Log($"Mistake detected! Total mistakes: {_mistakesInCurrentTrial}");
                }
            }
        }
        else
        {
            _wasInWrongObjectZone = false;
            _mistakeDwellTimer = 0f;
        }
    }

    public void CompleteTrial()
    {
        _isTrialActive = false;

        float timeToReach = (Time.time - _trialStartTime) - grabDwellTime;
        timeToReach = Mathf.Max(0f, timeToReach);

        audioSource.PlayOneShot(successClip);

        SaveTrialData(timeToReach, _mistakesInCurrentTrial);

        if (listener != null)
        {
            visualizer.ClearTarget();
            listener.ResetLatestLabel();
        }
    }

    private Vector3 GetIndexPosition(OVRSkeleton skeleton)
    {
        foreach (var bone in skeleton.Bones)
        {
            if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
            {
                return bone.Transform.position;
            }
        }
        return skeleton.transform.position; // Fallback to hand position if index tip is not found
    }

    private void InitializeCSV()
    {
        if (!File.Exists(_logFilePath))
        {
            File.WriteAllText(_logFilePath, "Timestamp,TargetItem,TotalTimeSec,MistakesCount\n");
        }
    }

    private void SaveTrialData(float total, int mistakes)
    {
        string entry = $"{System.DateTime.Now},{_currentLabel},{total},{mistakes}\n";

        if (NetworkLogger.Instance != null)
        {
            NetworkLogger.Instance.SendLog(entry);
        }
        else
        {
            Debug.Log("NetworkLogger instance not found in scene. Logging locally...");

            try
            {
                System.IO.File.AppendAllText(_logFilePath, entry);
            }
            catch (System.Exception e)
            {
                Debug.Log($"Local fallback write error: {e.Message}");
            }
        }
    }
}
