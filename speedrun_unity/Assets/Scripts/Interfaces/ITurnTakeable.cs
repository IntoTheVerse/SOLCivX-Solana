namespace SimKit
{
    public interface ITurnTakeable
    {
        public void OnTurn(int currentTurn, out float timeToExecute);
    }
}