using Spine.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;


public enum EAnimationPlayType
{
    Loop,
    Once
}

[Serializable]
public class StateNameToAnimationReference
{
    public string StateName;

    public AnimationReferenceAsset Animation;

    public EAnimationPlayType PlayType;
}

[Serializable]
public class AnimationTransition
{
    public AnimationReferenceAsset From;

    public AnimationReferenceAsset To;

    public AnimationReferenceAsset Transition;
}

[CreateAssetMenu(fileName = "SpineAnimationConfig", menuName = "Spine/SpineAnimationConfig")]
public class SpineAnimationConfig : ScriptableObject
{
    [SerializeField]
    public SkeletonDataAsset SkeletonData;

    [SerializeField]
    private List<StateNameToAnimationReference> _statesAndAnimations = new();

    [SerializeField]
    private AnimationTransition[] _transitionArray = System.Array.Empty<AnimationTransition>();

    public IReadOnlyList<StateNameToAnimationReference> StatesAndAnimations => _statesAndAnimations;
    public IReadOnlyList<AnimationTransition> TransitionArray => _transitionArray;
}