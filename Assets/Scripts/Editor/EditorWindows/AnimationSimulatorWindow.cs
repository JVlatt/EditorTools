using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System;
using UnityEditor.SceneManagement;

public class AnimationSimulatorWindow : EditorWindow
{
    public List<Animator> animators = new List<Animator>();
    public Dictionary<int, List<AnimationClip>> animations = new Dictionary<int, List<AnimationClip>>();
    public string[] animatorsNames;
    public Dictionary<int, List<string>> animationsNames = new Dictionary<int, List<string>>();
    public List<Vector3> positionsList = new List<Vector3>();


    public Texture2D animatorIndicator;

    int _animatorIndex = 0;
    int _animationIndex = 0;

    private float _lastEditorTime = 0f;
    private float _animationTime = 0f;
    private int _activeFrame;
    private Vector3 _savePos = Vector3.zero;

    private bool _isPlaying = false;
    private bool _isPaused = false;
    private bool _isLooping = false;
    private bool _inPlace = false;
    private bool _isReverse = false;
    private float _animationPlayRate = 1.0f;

    [MenuItem("Toolbox/Animation Window")]
    [MenuItem("Window/Toolbox/Animation Window")]
    static void InitWindow()
    {
        Debug.Log("Init Animation Window");
        AnimationSimulatorWindow window = GetWindow<AnimationSimulatorWindow>();
        window.Show();
        window.titleContent = new GUIContent("Animation Simulation Window");
    }

    private void OnEnable()
    {
        autoRepaintOnSceneChange = true;
        GetAnimatorsInScene();
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnModeChange;
        EditorApplication.hierarchyChanged += GetAnimatorsInScene;
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemShow;
    }

    private void HierarchyItemShow(int instanceID, Rect selectionRect)
    {
        Rect r = new Rect(selectionRect);
        r.x = r.width - 20;
        r.width = 18;

        if (animators[_animatorIndex].gameObject != null && animators[_animatorIndex].gameObject.GetInstanceID() == instanceID)
        {
            GUI.Label(r, animatorIndicator);
        }
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorApplication.playModeStateChanged -= OnModeChange;
        EditorApplication.hierarchyChanged -= GetAnimatorsInScene;
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        _isPlaying = false;
        AnimationMode.StopAnimationMode();
    }

    private void OnModeChange(PlayModeStateChange obj)
    {
        switch (obj)
        {
            case PlayModeStateChange.EnteredEditMode:
                break;
            case PlayModeStateChange.ExitingEditMode:
                _isPlaying = false;
                AnimationMode.StopAnimationMode();
                break;
            case PlayModeStateChange.EnteredPlayMode:
                _isPlaying = false;
                AnimationMode.StopAnimationMode();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                break;
            default:
                break;
        }
    }

    private void OnGUI()
    {
        if (animators.Count < 1)
        {
            if (GUILayout.Button("Get Animators In Scene"))
            {
                GetAnimatorsInScene();
            }
        }
        else
        {
            GUILayout.Label("Animators : ");
            Selection.activeObject = animators[_animatorIndex].gameObject;

            int tmpIndex = EditorGUILayout.Popup(_animatorIndex, animatorsNames);
            if (tmpIndex != _animatorIndex)
            {
                
                _animationIndex = 0;
                _animatorIndex = tmpIndex;
                if (_isPlaying)
                {
                    _animationTime = 0f;
                    _isPlaying = false;
                    AnimationMode.StopAnimationMode();
                }
            }
            GUILayout.Label("Animations : ");
            _animationIndex = EditorGUILayout.Popup(_animationIndex, animationsNames[_animatorIndex].ToArray());

            GUILayout.Label("Infos : ");

            GUILayout.Label("Time (seconds) : " + Math.Round(_animationTime, 2) + " s / " + Math.Round(animations[_animatorIndex][_animationIndex].length, 2) + " s");
            GUILayout.Label("Frame : " + (int)(_animationTime * animations[_animatorIndex][_animationIndex].frameRate) + " / " + animations[_animatorIndex][_animationIndex].length * animations[_animatorIndex][_animationIndex].frameRate);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Play") && (!_isPlaying || _isPaused))
                {
                    if (_isPaused)
                    {
                        _isPlaying = true;
                        _isPaused = false;
                    }
                    else
                    {
                        if (_isReverse)
                            _animationTime = animations[_animatorIndex][_animationIndex].length;
                        AnimationMode.StartAnimationMode();
                        _lastEditorTime = Time.realtimeSinceStartup;
                        _isPlaying = true;
                    }

                }
                if (GUILayout.Button("Pause") && _isPlaying)
                {
                    _activeFrame = (int)(_animationTime * animations[_animatorIndex][_animationIndex].frameRate);
                    _isPaused = true;
                }
                if (GUILayout.Button("Stop") && (_isPlaying || _isPaused))
                {
                    if (_isPaused)
                        _isPaused = false;

                    _isPlaying = false;
                    _animationTime = 0f;
                    AnimationMode.StopAnimationMode();
                }
            }
            GUILayout.EndHorizontal();
            _isLooping = GUILayout.Toggle(_isLooping, "Loop");
            _isReverse = GUILayout.Toggle(_isReverse, "Reverse");
            _inPlace = GUILayout.Toggle(_inPlace, "In Place");
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Animation Play Rate");
                _animationPlayRate = (float)Math.Round(GUILayout.HorizontalSlider(_animationPlayRate, 0.1f, 3.0f), 1);
                GUILayout.Label(_animationPlayRate.ToString());
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                if (_isPaused)
                {
                    GUILayout.Label("Animation Frame");
                    _activeFrame = (int)GUILayout.HorizontalSlider(_activeFrame, 0, animations[_animatorIndex][_animationIndex].length * animations[_animatorIndex][_animationIndex].frameRate);
                    _animationTime = _activeFrame / animations[_animatorIndex][_animationIndex].frameRate;
                    GUILayout.Label(((int)_activeFrame).ToString());
                }
            }
            GUILayout.EndHorizontal();

        }
    }

    private void Update()
    {
        if (_isPlaying)
        {
            if (!_isPaused)
            {
                float deltaTime = Time.realtimeSinceStartup - _lastEditorTime;
                if (_isReverse)
                {
                    _animationTime -= deltaTime * _animationPlayRate;
                }
                else
                {
                    _animationTime += deltaTime * _animationPlayRate;
                }
                Repaint();
            }
            AnimationMode.SampleAnimationClip(animators[_animatorIndex].gameObject, animations[_animatorIndex][_animationIndex], _animationTime);
            if (_animationTime > animations[_animatorIndex][_animationIndex].length && !_isReverse)
            {
                if (_isLooping)
                {
                    _animationTime = 0f;
                    _lastEditorTime = Time.realtimeSinceStartup;
                }
                else
                {
                    _isPlaying = false;
                    _animationTime = 0f;
                    AnimationMode.StopAnimationMode();
                }
            }

            if (_animationTime <= 0 && _isReverse)
            {
                if (_isLooping)
                {
                    _animationTime = animations[_animatorIndex][_animationIndex].length;
                    _lastEditorTime = Time.realtimeSinceStartup;
                }
                else
                {
                    _isPlaying = false;
                    _animationTime = 0f;
                    AnimationMode.StopAnimationMode();
                }
            }
        }

        if (_inPlace)
            animators[_animatorIndex].gameObject.transform.localPosition = positionsList[_animatorIndex];
        _lastEditorTime = Time.realtimeSinceStartup;
    }

    public void GetAnimatorsInScene()
    {
        animators.Clear();
        positionsList.Clear();
        Scene scene = SceneManager.GetActiveScene();
        int index = 0;
        GameObject[] rootGameObjects = scene.GetRootGameObjects();
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            Animator anim = rootGameObject.GetComponentInChildren<Animator>();
            if (anim)
            {
                animators.Add(anim);
                positionsList.Add(anim.transform.localPosition);
                if (anim.runtimeAnimatorController.animationClips.Count() > 0)
                {
                    animations[index] = anim.runtimeAnimatorController.animationClips.ToList();
                }
                index++;
            }

        }

        //Setup Names Lists

        animatorsNames = new string[animators.Count];
        for (int i = 0; i < animators.Count; i++)
        {
            animatorsNames[i] = animators[i].transform.root.name;
        }
        animationsNames.Clear();
        for (int i = 0; i < animators.Count; i++)
        {
            animations[i] = animators[i].runtimeAnimatorController.animationClips.ToList();
            animationsNames.Add(i, new List<string>());
            for (int j = 0; j < animations[i].Count; j++)
            {
                animationsNames[i].Add(animations[i][j].name);
            }
        }
    }
}
