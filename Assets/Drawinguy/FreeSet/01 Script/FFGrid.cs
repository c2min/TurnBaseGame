using UnityEngine;

namespace Drawinguy
{
    [ExecuteInEditMode]
    public class FFGrid : MonoBehaviour
    {
        public bool Refresh = false;
        public float Width;
        public float Height;
        public int RowCount;

        public void Update()
        {
#if UNITY_EDITOR
            if (Refresh == false)
            {
                return;
            }

            Refresh = false;

            int childCount = transform.childCount;
            int numHeight = (int)((float)childCount / (float)RowCount);
            float totalWidth = 0;
            float totalHeight = 0;
            if (RowCount != 0 && childCount > RowCount)
            {
                totalWidth = RowCount * Width;
                totalHeight = numHeight * Height;
            }
            else
            {
                totalWidth = childCount * Width;
                totalHeight = 0;
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                int widthIndex = i;
                int heightIndex = 0;
                if (RowCount > 0)
                {
                    widthIndex = i % RowCount;
                    heightIndex = (int)((float)i / (float)RowCount);
                }

                child.localPosition = new Vector3(-(totalWidth / 2) + Width * widthIndex + (float)Width / 2, (totalHeight / 2) - Height * heightIndex, 0);
            }
#endif
        }

        public void AddChild(Transform child)
        {
            child.parent = transform;
        }
    }

}