namespace SimKit
{
    public interface ISelectable
    {
        public void OnHighlight();
        public void OnSelect();
        public void OnDehighlight();
        public void OnDeselect(bool noOtherSelected = false);
    }
}