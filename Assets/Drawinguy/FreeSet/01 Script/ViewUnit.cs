using UnityEngine;
using Spine.Unity;

namespace Drawinguy
{
    public class ViewUnit : MonoBehaviour
    {
        float _currTime;
        bool _animation;
        Spine.TrackEntry _entry;
        SkeletonAnimation _spine;

        public void Awake()
        {
            _spine = gameObject.GetComponent<SkeletonAnimation>();
        }

        public void Update()
        {
            if (_animation == false)
            {
                return;
            }

            _currTime += Time.deltaTime;
            if (_currTime < _entry.AnimationEnd)
            {
                return;
            }

            _animation = false;
            SetAnimation("Idle", true);
        }

        public void SetAnimation(string aniName, bool loop)
        {
            _currTime = 0;
            _animation = true;
            _spine.state.ClearTracks();
            _spine.skeleton.SetToSetupPose();
            _entry = _spine.state.SetAnimation(0, aniName, loop);
        }

        public void ChangeSkin(string skinName)
        {
            _spine.skeleton.SetSkin(skinName);
            _spine.skeleton.SetToSetupPose();
        }
    }
}