using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Oculus.Interaction.Editor;

public class ExperimentController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CustomObjectDetectionVisualizer visualizer;
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

    private string _currentLabel = "";
    private bool _isTrialActive = false;
    private bool _isWaitingForCue = false;

    private float _trialStartTime;
    private int _mistakesInCurrentTrial = 0;
    private float _dwellTimer = 0f;

    private bool _hasReachedTarget = false;
    private float _reachTime;
    private bool _wasInWrongObjectZone = false;

    private string _logFilePath;

    private void Start()
    {
        _logFilePath = Path.Combine(Application.persistentDataPath, "experiment_log.csv");
        InitializeCSV();
    }

    public void StartTrial(string targetIngredient)
    {
        _currentLabel = targetIngredient.Trim().ToLower();
        _mistakesInCurrentTrial = 0;
        _dwellTimer = 0f;
        _hasReachedTarget = false;
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
            else if (visualizer != null && !visualizer.HasTarget)
            {
                _isWaitingForCue = false;
                _dwellTimer = 0f;

                if (cueManager != null)
                {
                    cueManager.SetVisibility(false);
                }
            }
            else
            {
                return;
            }
        }

        if (!_isTrialActive) return;

        Vector3 handPos = GetActiveHandPosition();
        if (handPos == Vector3.zero) return;

        if (visualizer.HasTarget)
        {
            float distanceToTarget = Vector3.Distance(handPos, visualizer.TargetPosition);

            if (distanceToTarget <= grabProximityRadius)
            {
                if (!_hasReachedTarget)
                {
                    _hasReachedTarget = true;
                    _reachTime = Time.time - _trialStartTime;
                    Debug.Log("Target reached! Time: " + _reachTime);
                }

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

                float distanceToWrongObject = Vector3.Distance(handPos, detection.Position);

                if (distanceToWrongObject <= grabProximityRadius)
                {
                    handInWrongZone = true;
                    
                    if (!_wasInWrongObjectZone)
                    {
                        _mistakesInCurrentTrial++;
                        _wasInWrongObjectZone = true;
                        Debug.Log("Mistake made! Total mistakes: " + _mistakesInCurrentTrial);
                    }
                }
            }
        }

        if (!handInWrongZone)
        {
            _wasInWrongObjectZone = false;
        }
    }

    public void CompleteTrial()
    {
        _isTrialActive = false;
        float totalTime = Time.time - _trialStartTime;

        SaveTrialData(totalTime, _reachTime, _mistakesInCurrentTrial);

        if (cueManager != null)
        {
            cueManager.SetVisibility(false);
        }
    }

    private Vector3 GetActiveHandPosition()
    {
        if (leftHand != null && leftHand.IsTracked && leftSkeleton != null && leftSkeleton.IsInitialized)
        {
            return GetIndexPosition(leftSkeleton);
        }
        else if (rightHand != null && rightHand.IsTracked && rightSkeleton != null && rightSkeleton.IsInitialized)
        {
            return GetIndexPosition(rightSkeleton);
        }
        return Vector3.zero;
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
            File.WriteAllText(_logFilePath, "Timestamp,TargetItem,ReachTime_Sec,MistakesCount\n");
        }
    }

    private void SaveTrialData(float total, float reach, int mistakes)
    {
        try
        {
            string entry = $"{System.DateTime.Now},{_currentLabel},{reach},{total},{mistakes}\n";
            File.AppendAllText(_logFilePath, entry);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CSV write error: {e}");
        }
    }
}
