/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using UnityEngine.UI;
using System;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
using TMP_TextInfo = Yarn.Unity.TMPShim.TextInfo;
#endif

#nullable enable

namespace Yarn.Unity.Samples
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class UseSizeOfText : MonoBehaviour, ILayoutElement //ILayoutSelfController
    {
        private TMP_Text? Text => GetComponentInChildren<TMP_Text>();
        private RectTransform RectTransform => GetComponent<RectTransform>();


        float ILayoutElement.preferredHeight => 0f;
        float ILayoutElement.preferredWidth => 0f;

        float ILayoutElement.flexibleWidth => 0f;
        float ILayoutElement.flexibleHeight => 0f;

        public float minWidth { get; private set; } = 0f;
        public float minHeight { get; private set; }

        int ILayoutElement.layoutPriority => 0;

        [SerializeField] float minimumWidth = 100f;
        [SerializeField] float minimumHeight = 30f;

        public void OnEnable()
        {
            if (Text != null)
            {
                Text.OnPreRenderText += UpdateLayout;
            }
        }

        public void OnDisable()
        {

            if (Text != null)
            {
                Text.OnPreRenderText -= UpdateLayout;
            }
        }

        private void UpdateLayout(TMP_TextInfo? info)
        {
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }

        void ILayoutElement.CalculateLayoutInputHorizontal()
        {
            return;
        }

        void ILayoutElement.CalculateLayoutInputVertical()
        {
            if (Text == null || string.IsNullOrEmpty(Text.text))
            {
                minHeight = minimumHeight;
                minWidth = minimumWidth;
                return;
            }

            var parentWidth = RectTransform.parent.GetComponent<RectTransform>().rect.width;
            var xMargin = (Text.margin.x + Text.margin.z);
            var insetSize = parentWidth - xMargin;
            var size = Text.GetPreferredValues(Text.text, insetSize, float.MaxValue);
            minHeight = Mathf.Max(minimumHeight, size.y);
            minWidth = Mathf.Max(minimumWidth, size.x + 5);
        }

        public void OnValidate()
        {
            UpdateLayout(null);
        }
    }
}