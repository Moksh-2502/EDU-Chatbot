namespace ReusablePatterns.SharedCore.Runtime.PauseSystem
{
    public delegate void PauseEventHandler(PauseData data);

    public delegate void PauseEventHandler<T>(PauseData data, T value);
    public struct PauseData
    {
        public static readonly PauseData Default = new PauseData()
        {
            displayMenu = true,
            countdownTime = null,
            countdownSpeed = null,
            animateCharacter = false,
            resumeWithCountdown = true
        };

        public bool displayMenu;
        public bool resumeWithCountdown;
        public float? countdownTime;
        public float? countdownSpeed;
        public bool animateCharacter;
        public bool ignoreGameState;

        public override string ToString()
        {
            return
                $"PauseData: displayMenu={displayMenu}, resumeWithCountdown={resumeWithCountdown}, countdownTime={countdownTime}, countdownSpeed={countdownSpeed}, animateCharacter={animateCharacter}";
        }
    }
}