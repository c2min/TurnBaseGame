using System.Collections.Generic;
using UnityEngine;

namespace Drawinguy
{
    public class ViewScene : MonoBehaviour
    {
        public static ViewScene Instance { get; private set; }

        List<ViewUnit> _unitList = new List<ViewUnit>();
        public FFGrid _grid;

        public void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            var objectList = Resources.LoadAll("Prefabs");
            for (var i = 0; i < objectList.Length; ++i)
            {
                if (objectList[i] == null)
                {
                    continue;
                }

                var unitObject = GameObject.Instantiate(objectList[i]) as GameObject;
                if (unitObject == null)
                {
                    continue;
                }
                _unitList.Add(unitObject.AddComponent<ViewUnit>());
                _grid.AddChild(unitObject.transform);
            }

            _grid.Refresh = true;
        }

        public void Update()
        {
        }
        public void MoveAnimation()
        {
            SetAnimation("Move");
        }

        public void AttackAnimation()
        {
            SetAnimation("Attack");
        }

        public void SkillAnimation()
        {
            SetAnimation("Skill");
        }

        public void HitAnimation()
        {
            SetAnimation("Hit");
        }

        public void DeadAnimation()
        {
            SetAnimation("Dead");
        }

        public void SetAnimation(string aniName)
        {
            for (int i = 0; i < _unitList.Count; ++i)
            {
                _unitList[i].SetAnimation(aniName, false);
            }
        }

        public void ChangePSkin1()
        {
            ChangeSkin("p1");
        }
        public void ChangePSkin2()
        {
            ChangeSkin("p2");
        }
        public void ChangePSkin3()
        {
            ChangeSkin("p3");
        }
        public void ChangePSkin4()
        {
            ChangeSkin("p4");
        }
        public void ChangePSkin5()
        {
            ChangeSkin("p5");
        }
        public void ChangePSkin6()
        {
            ChangeSkin("p6");
        }

        public void ChangeESkin1()
        {
            ChangeSkin("e1");
        }
        public void ChangeESkin2()
        {
            ChangeSkin("e2");
        }
        public void ChangeESkin3()
        {
            ChangeSkin("e3");
        }
        public void ChangeESkin4()
        {
            ChangeSkin("e4");
        }
        public void ChangeESkin5()
        {
            ChangeSkin("e5");
        }
        public void ChangeESkin6()
        {
            ChangeSkin("e6");
        }

        public void ChangeSkin(string skinName)
        {
            for (int i = 0; i < _unitList.Count; ++i)
            {
                _unitList[i].ChangeSkin(skinName);
            }

            SetAnimation("Idle");
        }
    }
}