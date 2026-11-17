using UnityEngine; using UnityEngine.UIElements;
namespace Day113
{
    /// 中身を「はみ出さない大きさ」まで縮めて表示する箱。スクロールなしで画面に収めるために使う。
    /// widthOnly=true：幅だけに合わせて縮め、自分の高さは縮めたあとの高さになる（縦に積む用）
    /// widthOnly=false：与えられた幅と高さの両方に収める（全画面の重ね表示や横長の右側用）
    public class FitBox : VisualElement
    {
        public readonly VisualElement content = new VisualElement();
        readonly bool widthOnly;
        public FitBox(bool widthOnly)
        {
            this.widthOnly = widthOnly;
            style.flexShrink = 0; style.overflow = Overflow.Visible;
            content.style.position = Position.Absolute; content.style.left = 0; content.style.top = 0;
            content.style.flexDirection = FlexDirection.Column; content.style.alignItems = Align.Center;
            content.style.transformOrigin = new TransformOrigin(0, 0);
            Add(content);
            RegisterCallback<GeometryChangedEvent>(_ => Apply());
            content.RegisterCallback<GeometryChangedEvent>(_ => Apply());
        }
        void Apply()
        {
            var o = layout; var c = content.layout;
            if (float.IsNaN(o.width) || float.IsNaN(c.width) || c.width <= 1 || c.height <= 1 || o.width <= 1) return;
            float s = Mathf.Min(1f, o.width / c.width);
            if (!widthOnly && o.height > 1) s = Mathf.Min(s, o.height / c.height);
            content.style.scale = new Scale(new Vector3(s, s, 1));
            content.style.left = (o.width - c.width * s) / 2;
            content.style.top = widthOnly ? 0 : (o.height - c.height * s) / 2;
            if (widthOnly) { float h = c.height * s; if (Mathf.Abs(o.height - h) > .5f) style.height = h; }
        }
    }
}
