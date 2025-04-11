/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using UnityEngine.UI;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
using TMP_TextInfo = Yarn.Unity.TMPShim.TextInfo;
#endif

#nullable enable

namespace Yarn.Unity.Samples
{
    /// <summary>
    /// A layout element that updates its minimum size to be that of a <see
    /// cref="TMP_Text"/> in its children.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class UseSizeOfText : MonoBehaviour, ILayoutElement
    {
        /// <summary>
        /// The text view in this object's hierarchy.
        /// </summary>
        private TMP_Text? Text => GetComponentInChildren<TMP_Text>();

        /// <summary>
        /// The Rect Transform present on this object.
        /// </summary>
        private RectTransform RectTransform => GetComponent<RectTransform>();

        /// <summary>
        /// The preferred height of the item. This is equal to the minimum height.
        /// </summary>
        float ILayoutElement.preferredHeight => minHeight;
        /// <summary>
        /// The preferred width of the item. This is equal to the minimum width.
        /// </summary>
        float ILayoutElement.preferredWidth => minWidth;

        /// <summary>
        /// The fraction of flexible width that the item consumes. 
        /// </summary>
        /// <remarks>
        /// This is always zero - we don't want to take up any space we don't
        /// need.
        /// </remarks>
        float ILayoutElement.flexibleWidth => 0f;

        /// <summary>
        /// The fraction of flexible height that the item consumes. 
        /// </summary>
        /// <remarks>
        /// This is always zero - we don't want to take up any space we don't
        /// need.
        /// </remarks>
        float ILayoutElement.flexibleHeight => 0f;

        public float minWidth { get; private set; } = 0f;
        public float minHeight { get; private set; } = 0f;

        /// <summary>
        /// The priority of the layout item.
        /// </summary>
        int ILayoutElement.layoutPriority => 0;

        /// <summary>
        /// The default minimum width of the item.
        /// </summary>
        /// <remarks>
        /// The calculated size will never be less wide than this value.
        /// </remarks>
        [SerializeField] float minimumWidth = 100f;

        /// <summary>
        /// The default minimum height of the item.
        /// </summary>
        /// <remarks>
        /// The calculated size will never be less tall than this value.
        /// </remarks>
        [SerializeField] float minimumHeight = 30f;

        /// <summary>
        /// Sets up the object to update its layout whenever the text displayed
        /// in <see cref="Text"/> changes.
        /// </summary>
        protected void OnEnable()
        {
            if (Text != null)
            {
                // OnPreRenderText is called every time the Text object needs to
                // update its contents. When this happens, it's likely that our
                // required size has changed, so we tell the layout to update
                Text.OnPreRenderText += UpdateLayout;
                UpdateLayout(Text.textInfo);
            }
        }

        /// <summary>
        /// Tears down the setup performed in <see cref="OnEnable"/>.
        /// </summary>
        protected void OnDisable()
        {
            if (Text != null)
            {
                Text.OnPreRenderText -= UpdateLayout;
            }
        }

        /// <summary>
        /// Called by the Text object whenever its text content changes. Recalculates  and
        /// requests that the UI layout system recalculate the layout info for
        /// this item.
        /// </summary>
        /// <param name="info"></param>
        private void UpdateLayout(TMP_TextInfo? info)
        {
            if (info == null || info.textComponent == null || string.IsNullOrEmpty(info.textComponent.text))
            {
                minHeight = minimumHeight;
                minWidth = minimumWidth;
                return;
            }

            // Calculate the maximum width available to us by getting our
            // parent's width
            var parentWidth = RectTransform.parent.GetComponent<RectTransform>().rect.width;

            // Get the left and right margins of the text component
            var xMargin = info.textComponent.margin.x + info.textComponent.margin.z;

            // Get the total width available for drawing text
            var insetSize = parentWidth - xMargin;

            // Compute the rectangle we'd need to draw the text in, given our
            // available width and an (effectively) unlimited amount of vertical
            // space
            var size = info.textComponent.GetPreferredValues(info.textComponent.text, insetSize, float.MaxValue);

            // Our minimum width and height are now based on this (we add a
            // slight padding to the width)
            minHeight = Mathf.Max(minimumHeight, size.y);
            minWidth = Mathf.Max(minimumWidth, size.x + 5);

            // Now that we know our minimum width and height, ask the layout
            // system to rebuild our layout
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }

        /// <summary>
        /// Called by the layout system before <see cref="minWidth"/>, <see
        /// cref="ILayoutElement.preferredWidth"/> and <see
        /// cref="ILayoutElement.flexibleWidth"/> are accessed.
        /// </summary>
        /// <remarks>This method takes no action, because the appropriate values
        /// have already been calculated by <see cref="UpdateLayout"/>.
        /// </remarks>
        void ILayoutElement.CalculateLayoutInputHorizontal() { }

        /// <summary>
        /// Called by the layout system before <see cref="minHeight"/>, <see
        /// cref="ILayoutElement.preferredHeight"/> and <see
        /// cref="ILayoutElement.flexibleHeight"/> are accessed.
        /// </summary>
        /// <remarks>This method takes no action, because the appropriate values
        /// have already been calculated by <see cref="UpdateLayout"/>.
        /// </remarks>
        void ILayoutElement.CalculateLayoutInputVertical() { }

        /// <summary>
        /// Updates the layout of the item every time the inspector changes.
        /// </summary>
        public void OnValidate() => UpdateLayout(null);
    }
}
