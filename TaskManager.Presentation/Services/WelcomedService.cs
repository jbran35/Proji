namespace TaskManager.Presentation.Services
{
    public class WelcomedService
    {
        bool hasBeenWelcomed = false; 

        public void MarkWelcomed()
        {
            hasBeenWelcomed = true;
        }

        public bool GetWelcomedStatus()
        {
            return hasBeenWelcomed;
        }
    }
}
